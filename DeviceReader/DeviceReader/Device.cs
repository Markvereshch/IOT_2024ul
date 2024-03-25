using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;

namespace VirtualDevices
{
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
        #region Send Message Template
        public async Task SendTelemetryToHub(string telemetry) //Send D2C message
        {
            Message message = new Message(Encoding.UTF8.GetBytes(telemetry));
            message.ContentType = MediaTypeNames.Application.Json;
            message.ContentEncoding = "utf-8";
            await deviceClient.SendEventAsync(message);
        }
        #endregion
    }
}
