using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvokeEmergencyStopFunction
{
    public class InvokeEmergencyStopFunction
    {
        private readonly ILogger<InvokeEmergencyStopFunction> logger;
        private const string pattern = @"{""WindowEndTime"":""\d{4}-\d{2}-\d{2}T(\d{2}:){2}\d{2}.\d{7}Z"",""ConnectionDeviceId"":"".+"",""OccuredErrors"":\d+.\d+}";

        private string? iotHubConnectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
        private readonly ServiceClient? serviceClient;

        public InvokeEmergencyStopFunction(ILogger<InvokeEmergencyStopFunction> logger)
        {
            this.logger = logger;
            try
            {
                this.serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to connect to the IoT Hub. Please, check your connection string to IoT Hub - {0}", ex.Message);
            }
        }

        [Function(nameof(InvokeEmergencyStopFunction))]
        public async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            string content = Encoding.UTF8.GetString(message.Body);
            Regex correctLines = new Regex(pattern);
            Match match = correctLines.Match(content);

            logger.LogInformation($"Received message body: {content}");
            ErrorMessageContent? deserialized = null;
            try
            {
                deserialized = JsonSerializer.Deserialize<ErrorMessageContent>(match.ToString());
            }
            catch(Exception ex)
            {
                logger.LogError("Cannot deserialize this object: \n {0}", ex.Message);
            }
            if (deserialized != null)
            {
                logger.LogInformation("Deserialized object: " + deserialized.ToString());
                logger.LogWarning("Invoking emergency stop on device {0}...", deserialized.ConnectionDeviceId);
                CloudToDeviceMethod directMethod = new CloudToDeviceMethod("EmergencyStop");
                if (serviceClient != null)
                {
                    try
                    {
                        var result = await serviceClient.InvokeDeviceMethodAsync(deserialized.ConnectionDeviceId, directMethod);
                        logger.LogWarning("Emergency stop has been invoked with status - {0}", result.Status);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error occurred while trying to execute direct method: {0}", ex.Message);
                    }
                }
                else
                {
                    logger.LogError("Service Client is not instantiated. Please, check the IotHubConnectionString property in the local.settings.json");
                }
            }
            await messageActions.CompleteMessageAsync(message);
        }
    }
    public class ErrorMessageContent
    {
        public DateTimeOffset WindowEndTime { get; set; }
        public string? ConnectionDeviceId { get; set; }
        public double OccuredErrors { get; set; }

        public override string ToString()
        {
            return $"ConnectionDeviceId: {ConnectionDeviceId}, OccuredErrors: {OccuredErrors}";
        }
    }
}
