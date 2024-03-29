﻿using DeviceReader;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
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
        private readonly string connectionString;
        private readonly OpcClient opcClient;
        private readonly string nodeId;
        public OpcNodeInfo ServerDevice
        {
            get { return serverDevice; }
        }
        public DeviceClient DeviceClient
        {
            get { return deviceClient; } 
        }
        public string ConnectionString
        {
            get { return connectionString;}
        }
        public Device(DeviceClient deviceClient, OpcNodeInfo serverDevice, string connectionString, OpcClient opcClient)
        {
            this.deviceClient = deviceClient;
            this.serverDevice = serverDevice;
            this.connectionString = connectionString;
            this.opcClient = opcClient;
            nodeId = CreateDeviceNodeId();
        }
        public async Task Initialize() //Initialize handlers and reported twin values
        {
            await InitializeTwinOnStart();

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyChanged, deviceClient);

            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, deviceClient);

            await deviceClient.SetMethodDefaultHandlerAsync(DefaultMethod, deviceClient);
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
        public async Task ReadTelemetryAndSendToHub() //Read all content values and prepare them for sending.
        {
            int status = int.Parse(ReadDeviceNode("/ProductionStatus"));

            string id = ReadDeviceNode("/WorkorderId");

            int good = int.Parse(ReadDeviceNode("/GoodCount"));

            int bad = int.Parse(ReadDeviceNode("/BadCount"));

            double temp = double.Parse(ReadDeviceNode("/Temperature"));

            string name = serverDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            var data = new
            {
                deviceId = name,
                productionStatus = status,
                workorderId = id,
                goodCount = good,
                badCount = bad,
                temperature = temp
            };

            var dataString = JsonConvert.SerializeObject(data);
            await SendMessageToHub(dataString);
        }
        private async Task SendMessageToHub(string content) //Send D2C message
        {
            Console.WriteLine(content);
            Message message = new Message(Encoding.UTF8.GetBytes(content));
            message.ContentType = MediaTypeNames.Application.Json;
            message.ContentEncoding = "utf-8";
            await deviceClient.SendEventAsync(message);
        }
        #endregion
        #region Device Twin
        public async Task<int> GetReportedProperty(string name)//Get a DeviceError or ProductionRate properties
        {
            try
            {
                var twin = await deviceClient.GetTwinAsync();
                var reportedProperties = twin.Properties.Reported;
                int property = reportedProperties.Contains(name) ? reportedProperties[name] : 0;
                return property;
            }
            catch
            {
                return 0;
            }
        }
        public async Task ReportPropertyToTwin(string propertyName, int value)//Report a ProductionRate or DeviceError to the device twin
        {
            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = value;
            if (propertyName.Equals("ProductionRate"))
            {
                string deviceName = ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
                Console.WriteLine($"Production rate of {deviceName} has been changed to {value}");
            }
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
        private async Task InitializeTwinOnStart()//Initialize the reported device twin and set the ProductionRate to the value from the desired device twin.
        {
            int desiredInitialRate = await ReadDesiredRateIfExists();
            OpcStatus result = opcClient.WriteNode(nodeId + "/ProductionRate", desiredInitialRate);

            var initialReportedProperties = new TwinCollection();
            initialReportedProperties["DeviceError"] = 0;
            initialReportedProperties["ProductionRate"] = desiredInitialRate;

            Console.WriteLine("Initial production rate of '{0}', which is determined by the Desired DT, is {1}", nodeId, desiredInitialRate);

            await deviceClient.UpdateReportedPropertiesAsync(initialReportedProperties);
        }
        private async Task<int> ReadDesiredRateIfExists() //Read the desired production rate or return 0
        {
            var desired = await deviceClient.GetTwinAsync();
            var desiredProperties = desired.Properties.Desired;
            var rate = desiredProperties.Contains("ProductionRate") ? desiredProperties["ProductionRate"] : 0;
            return rate;
        }
        private async Task DesiredPropertyChanged(TwinCollection desiredProperties, object userContext) //Set the production rate so that it is equal to the desired value.
        {
            if(desiredProperties.Contains("ProductionRate"))
            {
                Console.WriteLine($"Desired Production Value has changed to {desiredProperties["ProductionRate"]}");

                int value = (int)desiredProperties["ProductionRate"];
                await ReportPropertyToTwin("ProductionRate", value);
                OpcStatus result = opcClient.WriteNode(nodeId + "/ProductionRate", value);

                Console.WriteLine(result.ToString());
            }
            else
            {
                Console.WriteLine("An unknown property has been set in the desired device twin");
            }
        }
        #endregion
        #region Sending Errors
        public async Task ReadErrorsAndSendToHubIfOccured()//Check whether the error code of the machine has changed or not. If it has, then send the error event.
        {
            int errorCode = int.Parse(ReadDeviceNode("/DeviceError"));

            int reportedErrorCode = await GetReportedProperty("DeviceError");
            if (errorCode != reportedErrorCode)
            {
                await SendErrorEventMessage(errorCode, reportedErrorCode);
            }
        }
        private async Task SendErrorEventMessage(int errorCode, int reportedErrorCode)//Send a message with occured error
        {
            string deviceName = ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            string errorname = FindNewOccuredError(errorCode, reportedErrorCode);
            var data = new
            {
                errorName = errorname,
                deviceId = deviceName,
                currentErrors = CreateErrorMessage(errorCode),
                currentErrorCode = errorCode,
            };
            var dataString = JsonConvert.SerializeObject(data);

            await SendMessageToHub(dataString);
            await ReportPropertyToTwin("DeviceError", errorCode);
        }
        private string FindNewOccuredError(int errorCode, int oldErrorCode)
        {
            int difference = errorCode - oldErrorCode;
            if (difference <= 0)
                return "None";
            string errorName = ((DeviceErrors)difference).ToString();
            return errorName;
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
            Console.WriteLine("EmergencyStop method executed on {0}", nodeId);
            if(result != null) 
            {
                foreach (object o in result)
                    Console.WriteLine(o.ToString());
            }
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest request, object userContext)
        {
            object[] result = opcClient.CallMethod(nodeId, nodeId + "/ResetErrorStatus");
            Console.WriteLine("ResetErrorStatus method executed on {0}", nodeId);
            if (result != null)
            {
                foreach (object o in result)
                    Console.WriteLine(o.ToString());
            }
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> DefaultMethod(MethodRequest request, object userContext)
        {
            Console.WriteLine("An unknown method was received on {0}", nodeId);
            await Task.Delay(100);
            return new MethodResponse(0);
        }
        #endregion
        #region Sending Production Rate
        public async Task ReadProductionRateAndSendChangeToHub() //Read current production rate on the machine and report to twin if it has changed
        {
            int rate = int.Parse(ReadDeviceNode("/ProductionRate"));

            if (rate != await GetReportedProperty("ProductionRate"))
            {
                await ReportPropertyToTwin("ProductionRate", rate);
            }
        }
        #endregion
    }
}
