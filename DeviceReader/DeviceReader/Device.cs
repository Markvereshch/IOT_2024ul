using DeviceReader;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.Ua;
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
        private readonly string connectionString;

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
        public Device(DeviceClient deviceClient, OpcNodeInfo serverDevice, string connectionString)
        {
            this.deviceClient = deviceClient;
            this.serverDevice = serverDevice;
            this.connectionString = connectionString;
        }
        public void PrintInfo()
        {
            Console.WriteLine($"Device '{serverDevice.Attribute(OpcAttribute.DisplayName).Value}' was connected to {connectionString}");
        }
        #region Sending Telemetry
        public async Task SendTelemetryToHub(string telemetry) //Send D2C message
        {
            Message message = new Message(Encoding.UTF8.GetBytes(telemetry));
            message.ContentType = MediaTypeNames.Application.Json;
            message.ContentEncoding = "utf-8";
            await deviceClient.SendEventAsync(message);
        }
        #endregion
        public async Task InitializeTwinOnStart()//Initial device twin report
        {
            var twin = await deviceClient.GetTwinAsync();

            var initialReportedProperties = new TwinCollection();
            initialReportedProperties["DeviceError"] = 0;
            initialReportedProperties["ProductionRate"] = 0;

            await deviceClient.UpdateReportedPropertiesAsync(initialReportedProperties);
        }
        #region Sending Errors
        public async Task<int> GetOldErrorCode()//Get error from reported properties to check whether a new error occured or not.
        {
            try
            {
                var twin = await deviceClient.GetTwinAsync();
                var reportedProperties = twin.Properties.Reported;
                int deviceErrors = reportedProperties.Contains("DeviceError") ? reportedProperties["DeviceError"] : 0;
                return deviceErrors;
            }
            catch
            {
                return 0;
            }
        }
        public async Task SendErrorEventMessage(int errorCode)//Send message with occured error
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
            await Task.Delay(1000);
            await deviceClient.SendEventAsync(message);
            await ReportErrorToTwin(errorCode);
        }
        public async Task ReportErrorToTwin(int errorCode)//Write new error code into reported values of device twin
        {
            var twin = deviceClient.GetTwinAsync();
            var reportedProperties = new TwinCollection();
            reportedProperties["DeviceError"] = errorCode;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
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
                if(error.HasFlag(DeviceErrors.EmergencyStop))
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
    }
}
