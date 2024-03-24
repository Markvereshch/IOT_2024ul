using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualDevices;
namespace DeviceReader
{
    internal class ClientManager
    {
        private readonly List<string> connections;
        private readonly List<OpcNodeInfo> devices;
        private List<Device> connectedClients;
       
        public ClientManager(List<string> connections, List<OpcNodeInfo> devices)
        {
            this.connections = connections;
            this.devices = devices;
        }
        public async Task TestMethod()
        {
            connectedClients = new List<Device>();
            for(int i = 0; i < devices.Count; i++)
            {
                using var deviceClient = DeviceClient.CreateFromConnectionString(connections[i]);
                await deviceClient.OpenAsync();
                Device device = new Device(deviceClient, devices[i], connections[i]);
                connectedClients.Add(device);
            }
            Console.WriteLine("Connection success");
            foreach(var client in connectedClients)
            {
                client.PrintInfo();
            }
        }
    }
}
