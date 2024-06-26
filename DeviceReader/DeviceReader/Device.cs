﻿using AgentApp;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;

namespace VirtualDevices
{
    [Flags]
    public enum DeviceErrors
    {
        None = 0,
        EmergencyStop = 1,
        PowerFailure = 2,
        SensorFailure = 4,
        Unknown = 8
    }
    public class Device
    {
        private readonly OpcNodeInfo serverDevice;
        private readonly DeviceClient deviceClient;
        private readonly OpcClient opcClient;
        private readonly string nodeId;

        private int lastReportedErrorCode;
        private int lastReportedProductionRate;

        public OpcNodeInfo ServerDevice
        {
            get { return serverDevice; }
        }
        public DeviceClient DeviceClient
        {
            get { return deviceClient; } 
        }
        public Device(DeviceClient deviceClient, OpcNodeInfo serverDevice, OpcClient opcClient)
        {
            this.deviceClient = deviceClient;
            this.serverDevice = serverDevice;
            this.opcClient = opcClient;
            nodeId = CreateDeviceNodeId();
        }
        public async Task InitializeHandlersAsync() //Initialize handlers and reported twin values
        {
            await InitializeTwinOnStartAsync();

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredProductionRateChangedAsync, deviceClient);

            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, deviceClient);

            await deviceClient.SetMethodDefaultHandlerAsync(DefaultMethod, deviceClient);

            lastReportedProductionRate = await GetReportedPropertyAsync("ProductionRate");
            lastReportedErrorCode = await GetReportedPropertyAsync("DeviceError");
        }
        private string CreateDeviceNodeId()
        {
            string deviceName = ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            string nodeId = $"ns=2;s={deviceName}";
            return nodeId;
        }
        private string ReadDeviceNode(string name)
        {
            OpcReadNode node = new OpcReadNode(nodeId + name);
            OpcValue info = opcClient.ReadNode(node);
            return info.ToString();
        }
        #region Sending Telemetry
        public async Task ReadTelemetryAndSendToHubAsync() //Read all content values and prepare them for sending.
        {
            int status = int.Parse(ReadDeviceNode("/ProductionStatus"));

            string id = ReadDeviceNode("/WorkorderId");

            int good = int.Parse(ReadDeviceNode("/GoodCount"));

            int bad = int.Parse(ReadDeviceNode("/BadCount"));

            double temp = double.Parse(ReadDeviceNode("/Temperature"));

            string name = serverDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            var data = new
            {
                deviceName = name,
                productionStatus = status,
                workorderId = id,
                goodCount = good,
                badCount = bad,
                temperature = temp
            };

            var dataString = JsonConvert.SerializeObject(data);
            await SendMessageToHubAsync(dataString);
        }
        private async Task SendMessageToHubAsync(string content) //Send D2C message
        {
            Console.WriteLine($"{DateTime.Now}: {content}");
            Message message = new Message(Encoding.UTF8.GetBytes(content));
            message.ContentType = MediaTypeNames.Application.Json;
            message.ContentEncoding = "utf-8";
            await deviceClient.SendEventAsync(message);
        }
        #endregion
        #region Device Twin
        public async Task<int> GetReportedPropertyAsync(string name)//Get a DeviceError or ProductionRate properties
        {
            var twin = await deviceClient.GetTwinAsync();
            var reportedProperties = twin.Properties.Reported;
            int property = reportedProperties.Contains(name) ? reportedProperties[name] : 0;
            return property;
        }
        public async Task ReportPropertyToTwinAsync(string propertyName, int value)//Report a ProductionRate or DeviceError to the device twin
        {
            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = value;
            if (propertyName.Equals("ProductionRate"))
            {
                string deviceName = ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
                Console.WriteLine($"{DateTime.Now}: Production rate of {deviceName} has been changed to {value}");
            }
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
        private async Task InitializeTwinOnStartAsync()//InitializeHandlersAsync the reported device twin and set the ProductionRate to the value from the desired device twin.
        {
            int desiredInitialRate = await ReadDesiredRateIfExistsAsync();
            OpcStatus result = opcClient.WriteNode(nodeId + "/ProductionRate", desiredInitialRate);

            var initialReportedProperties = new TwinCollection();
            initialReportedProperties["DeviceError"] = 0;
            initialReportedProperties["ProductionRate"] = desiredInitialRate;

            Console.WriteLine($"{DateTime.Now}: Initial production rate of '{nodeId}', which is determined by the Desired DT, is {desiredInitialRate}");

            await deviceClient.UpdateReportedPropertiesAsync(initialReportedProperties);
        }
        private async Task<int> ReadDesiredRateIfExistsAsync() //Read the desired production rate or return 0
        {
            var desired = await deviceClient.GetTwinAsync();
            var desiredProperties = desired.Properties.Desired;
            var rate = desiredProperties.Contains("ProductionRate") ? desiredProperties["ProductionRate"] : 0;
            return rate;
        }
        private async Task DesiredProductionRateChangedAsync(TwinCollection desiredProperties, object userContext) //Set the production rate so that it is equal to the desired value.
        {
            if(desiredProperties.Contains("ProductionRate"))
            {
                Console.WriteLine($"{DateTime.Now}: Desired Production Value has changed to {desiredProperties["ProductionRate"]}");

                int rate = (int)desiredProperties["ProductionRate"];
                await ReportPropertyToTwinAsync("ProductionRate", rate);
                lastReportedProductionRate = rate;
                OpcStatus result = opcClient.WriteNode(nodeId + "/ProductionRate", rate);

                Console.WriteLine(result.ToString());
            }
            else
            {
                Console.WriteLine($"{DateTime.Now}: An unknown property has been set in the desired device twin");
            }
        }
        #endregion
        #region Sending Errors
        public async Task ReadErrorsAndSendToHubIfOccuredAsync()//Check whether the error code of the machine has changed or not. If it has, then send the error event.
        {
            int errorCode = int.Parse(ReadDeviceNode("/DeviceError"));

            if (errorCode != lastReportedErrorCode)
            {
                await SendErrorEventMessageAsync(errorCode);
                lastReportedErrorCode = errorCode;
            }
        }
        private async Task SendErrorEventMessageAsync(int errorCode)//Send a message with occured error
        {
            string serverDeviceName = ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            int newFoundErrors = FindTheNumberOfNewErrors(errorCode);
            var data = new
            {
                errorName = FindTheNameOfOccuredErrors(errorCode),
                newErrors = newFoundErrors,
                deviceName = serverDeviceName,
                currentErrors = CreateErrorMessage(errorCode),
                currentErrorCode = errorCode,
            };
            var dataString = JsonConvert.SerializeObject(data);

            await SendMessageToHubAsync(dataString);

            if(newFoundErrors != 0)
            {
                try
                {
                    await EmailSender.SendErrorMessageToEmailsAsync(dataString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now}: ERROR DURING SENDING AN EMAIL: {ex.Message}");
                }
            }

            await ReportPropertyToTwinAsync("DeviceError", errorCode);
        }
        private string FindTheNameOfOccuredErrors(int errorCode)
        {
            int difference = errorCode - lastReportedErrorCode;
            if (difference <= 0)
            {
                return "None";
            }
            string errorName = ((DeviceErrors)difference).ToString();
            return errorName;
        }
        private int FindTheNumberOfNewErrors(int errorCode)
        {
            int difference = errorCode - lastReportedErrorCode;
            int numberOfErrors = 0;
            DeviceErrors error = (DeviceErrors)difference;
            if (difference > 0)
            {
                if (error.HasFlag(DeviceErrors.EmergencyStop))
                {
                    numberOfErrors += 1;
                }
                if (error.HasFlag(DeviceErrors.PowerFailure))
                {
                    numberOfErrors += 1; 
                }
                if (error.HasFlag(DeviceErrors.SensorFailure))
                {
                    numberOfErrors += 1;
                }
                if (error.HasFlag(DeviceErrors.Unknown))
                {
                    numberOfErrors += 1;
                }
            }
            return numberOfErrors;
        }
        private string CreateErrorMessage(int errorCode)//Check the error code to find any errors that occur.
        {
            StringBuilder sb = new StringBuilder();
            DeviceErrors error = (DeviceErrors)errorCode;
            if(error == DeviceErrors.None)
            {
                sb.Append("'None' ");
            }
            else
            {
                if (error.HasFlag(DeviceErrors.EmergencyStop))
                {
                    sb.Append("'Emergency Stop' ");
                }
                if (error.HasFlag(DeviceErrors.PowerFailure))
                {
                    sb.Append("'Power Failure' ");
                }
                if (error.HasFlag(DeviceErrors.SensorFailure))
                {
                    sb.Append("'Sensor Failure' ");
                }
                if (error.HasFlag(DeviceErrors.Unknown))
                {
                    sb.Append("'Unknown' ");
                }
            }
            return sb.ToString().Trim();
        }
        #endregion
        #region Direct Methods
        private async Task<MethodResponse> EmergencyStop(MethodRequest request, object userContext)
        {
            object[] result = opcClient.CallMethod(nodeId, nodeId + "/EmergencyStop");
            Console.WriteLine("{1}: EmergencyStop method executed on {0}", nodeId, DateTime.Now);
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest request, object userContext)
        {
            object[] result = opcClient.CallMethod(nodeId, nodeId + "/ResetErrorStatus");
            Console.WriteLine("{1}: ResetErrorStatus method executed on {0}", nodeId, DateTime.Now);
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> DefaultMethod(MethodRequest request, object userContext)
        {
            Console.WriteLine("{1}: An unknown method was received on {0}", nodeId, DateTime.Now);
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        #endregion
        #region Sending Production Rate
        public async Task ReadProductionRateAndSendChangeToHubAsync() //Read current production rate on the machine and report to twin if it has changed
        {
            int rate = int.Parse(ReadDeviceNode("/ProductionRate"));

            if (rate != lastReportedProductionRate)
            {
                await ReportPropertyToTwinAsync("ProductionRate", rate);
                lastReportedProductionRate = rate;
            }
        }
        #endregion
    }
}
