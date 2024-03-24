using Microsoft.Azure.Devices.Client;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace VirtualDevices
{
    public class Device
    {
        private readonly OpcNodeInfo device;
        private readonly DeviceClient deviceClient;
        private readonly string connectionString;
        public Device(DeviceClient deviceClient, OpcNodeInfo device, string connectionString)
        {
            this.deviceClient = deviceClient;
            this.device = device;
            this.connectionString = connectionString;
        }
        public void PrintInfo()
        {
            Console.WriteLine($"Device '{device.Attribute(OpcAttribute.DisplayName).Value}' was connected to {connectionString}");
        }
        #region Send Message Template
        public void SendMessage(string name, int delay) //Send D2C message
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
