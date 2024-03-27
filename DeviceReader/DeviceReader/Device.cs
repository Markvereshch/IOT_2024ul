using DeviceReader;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
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
        public Device(DeviceClient deviceClient, OpcNodeInfo serverDevice, string connectionString, OpcClient client)
        {
            this.deviceClient = deviceClient;
            this.serverDevice = serverDevice;
            this.connectionString = connectionString;
            this.opcClient = client;
        }
        public void PrintInfo()
        {
            Console.WriteLine($"Device '{serverDevice.Attribute(OpcAttribute.DisplayName).Value}' was connected to {connectionString}");
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
        #region Sending Telemetry
        public async Task ReadTelemetryAndSendToHub() //Read all telemetry values and prepare them for sending.
        {
            string nodeId = CreateDeviceNodeId();

            OpcReadNode node = new OpcReadNode(nodeId + "/ProductionStatus");
            OpcValue info = opcClient.ReadNode(node);
            int status = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "/WorkorderId");
            info = opcClient.ReadNode(node);
            string id = info.ToString();

            node = new OpcReadNode(nodeId + "/GoodCount");
            info = opcClient.ReadNode(node);
            int good = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "/BadCount");
            info = opcClient.ReadNode(node);
            int bad = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "/Temperature");
            info = opcClient.ReadNode(node);
            double temp = double.Parse(info.ToString());

            var data = new
            {
                productionStatus = status,
                workorderId = id,
                goodCount = good,
                badCount = bad,
                temperature = temp
            };

            var dataString = JsonConvert.SerializeObject(data);
            Console.WriteLine(dataString);
            await SendTelemetryToHub(dataString);
        }
        private async Task SendTelemetryToHub(string telemetry) //Send D2C message
        {
            Message message = new Message(Encoding.UTF8.GetBytes(telemetry));
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
        private async Task InitializeTwinOnStart()//Initial device twin report
        {
            var twin = await deviceClient.GetTwinAsync();

            var initialReportedProperties = new TwinCollection();
            initialReportedProperties["DeviceError"] = 0;
            initialReportedProperties["ProductionRate"] = 0;

            await deviceClient.UpdateReportedPropertiesAsync(initialReportedProperties);
        }
        private async Task DesiredPropertyChanged(TwinCollection desiredProperties, object userContext) //Set the production rate so that it is equal to the desired value.
        {
            string nodeId = CreateDeviceNodeId();
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
            string nodeId = CreateDeviceNodeId();

            OpcReadNode node = new OpcReadNode(nodeId + "/DeviceError");
            OpcValue info = opcClient.ReadNode(node);
            int errorCode = int.Parse(info.ToString());

            if (errorCode != await GetReportedProperty("DeviceError"))
            {
                await SendErrorEventMessage(errorCode);
            }
        }
        private async Task SendErrorEventMessage(int errorCode)//Send a message with occured error
        {
            string deviceName = this.ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            var data = new
            {
                deviceError = $"The error code of {deviceName} has changed! Current error code is {errorCode}:" + CreateErrorMessage(errorCode)
            };
            var dataString = JsonConvert.SerializeObject(data);
            Message message = new Message(Encoding.UTF8.GetBytes(dataString));
            message.ContentType = MediaTypeNames.Application.Json;
            message.ContentEncoding = "utf-8";

            Console.WriteLine(dataString);
            await deviceClient.SendEventAsync(message);
            await ReportPropertyToTwin("DeviceError", errorCode);
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
            string nodeId = CreateDeviceNodeId();
            object[] result = opcClient.CallMethod(nodeId, nodeId + "/EmergencyStop");
            Console.WriteLine("EmergencyStop method executed:");
            if(result != null) 
            {
                foreach (object o in result)
                    Console.WriteLine(o.ToString());
            }
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest request, object userContext)
        {
            string nodeId = CreateDeviceNodeId();
            object[] result = opcClient.CallMethod(nodeId, nodeId + "/ResetErrorStatus");
            Console.WriteLine("ResetErrorStatus method executed:");
            if (result != null)
            {
                foreach (object o in result)
                    Console.WriteLine(o.ToString());
            }
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> DefaultMethod(MethodRequest request, object userContext)
        {
            Console.WriteLine("An unknown method was received");
            return new MethodResponse(0);
        }
        #endregion
        #region Sending Production Rate
        public async Task ReadProductionRateAndSendChangeToHub() //Read current production rate on the machine and report to twin if it has changed
        {
            string nodeId = CreateDeviceNodeId();

            OpcReadNode node = new OpcReadNode(nodeId + "/ProductionRate");
            OpcValue info = opcClient.ReadNode(node);
            int rate = int.Parse(info.ToString());
            if (rate != await GetReportedProperty("ProductionRate"))
            {
                await ReportPropertyToTwin("ProductionRate", rate);
            }
        }
        #endregion
    }
}
