using AgentApp;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;
using VirtualDevices;

namespace DeviceReader
{
    internal class ClientManager
    {
        private readonly int telemetryReadingDelay;
        private readonly int errorReadingDelay;
        private readonly int productionRateReadingDelay;

        private readonly List<string> connections;
        private readonly List<OpcNodeInfo> devices;
        private readonly OpcClient client;
        private readonly List<Device> connectedDevices;
        public ClientManager(List<string> connections, List<OpcNodeInfo> devices, OpcClient client)
        {
            this.connections = connections;
            this.devices = devices;
            this.client = client;
            this.connectedDevices = new List<Device>();

            var settings = AppSettings.GetSettings();
            this.telemetryReadingDelay = settings.TelemetrySendingDelayInMs;
            this.errorReadingDelay = settings.ErrorCheckingDelayInMs;
            this.productionRateReadingDelay = settings.ProductionRateCheckingDelayInMs;
        }
        public async Task InitializeClientManager()
        {
            await InitializeDevices();
            Task deviceErrorTask = Task.Run(WaitForErrorsContinuously);
            Task deviceProdRate = Task.Run(WaitForRateChangeContinuously);
            await ReadMessagesContinuously();
        }
        private async Task InitializeDevices()
        {
            for(int i = 0; i < devices.Count; i++)
            {
                var deviceClient = DeviceClient.CreateFromConnectionString(connections[i]);
                await deviceClient.OpenAsync();

                Device device = new Device(deviceClient, devices[i], client);
                await device.InitializeHandlersAsync();
                connectedDevices.Add(device);
            }
            Console.WriteLine($"Connection success.");
        }
        private async Task ReadMessagesContinuously()
        {
            while(true)
            {
                foreach (var device in connectedDevices)
                {
                    await device.ReadTelemetryAndSendToHubAsync();
                }
                await Task.Delay(telemetryReadingDelay);
            }
        }
        private async Task WaitForErrorsContinuously()
        {
            while(true)
            {
                foreach (var device in connectedDevices)
                {
                    await device.ReadErrorsAndSendToHubIfOccuredAsync();
                }
                await Task.Delay(errorReadingDelay);
            }
        }
        private async Task WaitForRateChangeContinuously()
        {
            while(true)
            {
                foreach(var device in connectedDevices)
                {
                    await device.ReadProductionRateAndSendChangeToHubAsync(); 
                }
                await Task.Delay(productionRateReadingDelay);
            }
        }
    }
}
