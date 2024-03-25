using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VirtualDevices;
namespace DeviceReader
{
    internal class ClientManager
    {
        private readonly List<string> connections;
        private readonly List<OpcNodeInfo> devices;
        private readonly OpcClient client;
        private List<Device> connectedDevices;
        public ClientManager(List<string> connections, List<OpcNodeInfo> devices, OpcClient client)
        {
            this.connections = connections;
            this.devices = devices;
            this.client = client;
        }
        public async Task InitializeClientManager()
        {
            await InitializeDevices();
            Thread t = new Thread(doSmth);
            t.Start();
            await ReadMessagesContinuously();
        }
        private void doSmth()
        {
            while(true)
            {
                Console.WriteLine("Waiting for messages...");
                Thread.Sleep(5000);
            }
        }
        private async Task InitializeDevices()
        {
            connectedDevices = new List<Device>();
            for(int i = 0; i < devices.Count; i++)
            {
                var deviceClient = DeviceClient.CreateFromConnectionString(connections[i]);
                await deviceClient.OpenAsync();

                Device device = new Device(deviceClient, devices[i], connections[i]);
                connectedDevices.Add(device);
            }
            Console.WriteLine("Connection success.");
        }
        private async Task ReadMessagesContinuously()
        {
            while(true)
            {
                foreach (var device in connectedDevices)
                {
                    await ReadTelemetryAndSendToDevice(device);
                }
                await Task.Delay(5000);
            }
        }
        private async Task ReadTelemetryAndSendToDevice(Device virtualDevice)
        {
            string deviceName = virtualDevice.ServerDevice.Attribute(OpcAttribute.DisplayName).Value.ToString();
            string nodeId = $"ns=2;s={deviceName}/";

            OpcReadNode node = new OpcReadNode(nodeId + "ProductionStatus");
            OpcValue info = client.ReadNode(node);
            int status = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "WorkorderId");
            info = client.ReadNode(node);
            string id = info.ToString();

            node = new OpcReadNode(nodeId + "GoodCount");
            info = client.ReadNode(node);
            int good = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "BadCount");
            info = client.ReadNode(node);
            int bad = int.Parse(info.ToString());

            node = new OpcReadNode(nodeId + "Temperature");
            info = client.ReadNode(node);
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
            await virtualDevice.SendTelemetryToHub(dataString);
        }
    }
}
