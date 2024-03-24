using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace VirtualDevice
{
    public class DeviceGroup
    {
        private string connectionString;
        private OpcNodeInfo device;

        public DeviceGroup(string connectionString, OpcNodeInfo device)
        {
            this.connectionString = connectionString;
            this.device = device;
        }

        public void PrintInfo()
        {
            Console.WriteLine($"Device '{device.Attribute(OpcAttribute.DisplayName).Value}' was connected to {connectionString}");
        }
    }
}
