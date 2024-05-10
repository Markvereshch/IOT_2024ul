using System;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DecreaseDesiredProductionRateFunction
{
    public class DecreaseDesiredProductionRateFunction
    {
        private readonly ILogger<DecreaseDesiredProductionRateFunction> logger;
        private const string pattern = @"{""WindowEndTime"":""\d{4}-\d{2}-\d{2}T(\d{2}:){2}\d{2}.\d{7}Z"",""ConnectionDeviceId"":"".+"",""GoodCount"":\d+.\d+,""TotalVolume"":\d+.\d+,""ProcentOfGoodProduction"":\d+.\d+}";
        private const double minAcceptableRate = 90.0d;
        private const int numberOfPointsToDecrease = 10;

        private string? iotHubConnectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
        private readonly RegistryManager? registryManager;
        public DecreaseDesiredProductionRateFunction(ILogger<DecreaseDesiredProductionRateFunction> logger)
        {
            this.logger = logger;
            try
            {
                this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to connect to the IoT Hub. Please, check your connection string to IoT Hub - {0}", ex.Message);
            }
        }

        [Function(nameof(DecreaseDesiredProductionRateFunction))]
        public async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            string content = Encoding.UTF8.GetString(message.Body);
            Regex correctLines = new Regex(pattern);
            Match match = correctLines.Match(content);

            logger.LogInformation($"Received message body: {content}");
            ProductionRateMessageContent? deserialized = null;
            try
            {
                deserialized = JsonSerializer.Deserialize<ProductionRateMessageContent>(match.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot deserialize this object: \n {0}", ex.Message);
            }
            if (deserialized != null)
            {
                logger.LogInformation("Deserialized object: " + deserialized.ToString());
                if (deserialized.ProcentOfGoodProduction < minAcceptableRate)
                {
                    try
                    {
                        if (registryManager != null)
                        {
                            await DecreaseDesiredProductionRate(deserialized);
                        }
                        else
                        {
                            logger.LogError("Registry Manager is not instantiated. Please, check the IotHubConnectionString property in the local.settings.json");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error occurred while trying to decrease desired production rate: {0}", ex.Message);
                    }
                }
            }
            await messageActions.CompleteMessageAsync(message);
        }
        private async Task DecreaseDesiredProductionRate(ProductionRateMessageContent data)
        {
            logger.LogWarning($"Decreasing production rate of device {data.ConnectionDeviceId}...");

            var twin = await registryManager.GetTwinAsync(data.ConnectionDeviceId);

            if (twin != null && twin.Properties.Desired.Contains("ProductionRate"))
            {
                var productionRate = twin.Properties.Desired["ProductionRate"];

                if (productionRate - numberOfPointsToDecrease >= 0)
                {
                    twin.Properties.Desired["ProductionRate"] -= numberOfPointsToDecrease;
                    var result = await registryManager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
                    logger.LogWarning($"Desired production rate has been changed successfully! New desired rate: {twin.Properties.Desired["ProductionRate"]}");
                }
                else
                {
                    logger.LogWarning("Production rate cannot be decreased further.");
                }
            }
            else
            {
                logger.LogError($"Cannot retrieve the ProductionRate property from the device. The {data.ConnectionDeviceId} device either doesn't have the desired property or doesn't exist at all.");
            }
        }
    }
    public class ProductionRateMessageContent
    {
        public DateTimeOffset WindowEndTime { get; set; }
        public string? ConnectionDeviceId { get; set; }
        public double GoodCount { get; set; }
        public double TotalVolume { get; set; }
        public double ProcentOfGoodProduction { get; set; }

        public override string ToString()
        {
            return $"ConnectionDeviceId: {ConnectionDeviceId}, ProcentOfGoodProduction: {ProcentOfGoodProduction}";
        }
    }
}
