using Newtonsoft.Json;
using System.Text;

namespace AgentApp
{
    public sealed class AppSettings
    {
        private const string filePath = "app_settings.json";
        public const int defaultDelay = 3000;
        private static AppSettings? instance = null;
        public string? ServerConnectionString { get; set; }
        public List<string> AzureDevicesConnectionStrings { get; set; }
        public int TelemetrySendingDelayInMs { get; set; }
        public int ErrorCheckingDelayInMs { get; set; }
        public int ProductionRateCheckingDelayInMs { get; set; }
        public string? CommunicationServicesConnectionString { get; set; }
        public string? CommunicationServicesSender { get; set; }
        public List<string> EmailAddresses { get; set; }
        private static AppSettings LoadSettings()
        {
            if (!File.Exists(filePath))
            {
                var settings = new AppSettings();
                SaveSettings(settings);
                return settings;
            }
            string settingsJson = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<AppSettings>(settingsJson) ?? new AppSettings();
        }
        public void SaveSettings()
        {
            SaveSettings(this);
        }
        private static void SaveSettings(AppSettings settings)
        {
            string serializedSettings = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(filePath, serializedSettings);
        }
        public static AppSettings GetSettings()
        {
            if (instance == null)
                instance = LoadSettings();
            return instance;
        }
        private AppSettings()
        {
            ServerConnectionString = null;
            TelemetrySendingDelayInMs = defaultDelay;
            ErrorCheckingDelayInMs = defaultDelay;
            ProductionRateCheckingDelayInMs = defaultDelay;
            CommunicationServicesConnectionString = null;
            CommunicationServicesSender = null;
            EmailAddresses = new List<string>();
            AzureDevicesConnectionStrings = new List<string>();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(">>>>APPLICATION SETTINGS<<<<");
            sb.AppendLine("----------------------------------------------");
            sb.AppendLine($">>Server connection string:");
            string connectionString = ServerConnectionString == null ? "None" : ServerConnectionString;
            sb.AppendLine("\t" + connectionString);

            sb.AppendLine($">>Delays (telemetry, errors, rate):\n\t{TelemetrySendingDelayInMs}ms, {ErrorCheckingDelayInMs}ms, {ProductionRateCheckingDelayInMs}ms");

            sb.AppendLine($">>Azure communication services:");
            string azureService = CommunicationServicesConnectionString == null ? "None" : CommunicationServicesConnectionString;
            sb.AppendLine("\t" + azureService);

            sb.AppendLine($">>Email sender:");
            string sender = CommunicationServicesSender == null ? "None" : CommunicationServicesSender;
            sb.AppendLine("\t" + sender);
            int index = 1;
            sb.AppendLine(">>Email addresses of receivers:");
            if (EmailAddresses.Count != 0)
            {
                foreach (var address in EmailAddresses)
                {
                    sb.AppendLine($"\t{index++}. {address},");
                }
            }
            else
            {
                sb.AppendLine("\tNone");
            }
            index = 1;
            sb.AppendLine(">>Connection strings to Azure devices:");
            if (AzureDevicesConnectionStrings.Count != 0)
            {
                foreach (var device in AzureDevicesConnectionStrings)
                {
                    sb.AppendLine($"\t{index++}. {device},");
                }
            }
            else
            {
                sb.AppendLine("\tNone");
            }
            sb.AppendLine("----------------------------------------------");
            return sb.ToString();
        }
    }
}
