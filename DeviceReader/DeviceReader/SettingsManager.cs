using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentApp
{
    internal static class SettingsManager
    {
        private const int sleepTime = 1000;
        private const string opcPattern = @"^opc\.tcp:\/\/[\S-]+$";
        private const string emailPattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
        private const string communicationServicesPattern = @"^endpoint=https:\/\/[\S-]+$";
        private const string deviceConnectionStringPattern = @"^HostName=.+.azure-devices.net;DeviceId=.+;SharedAccessKey=.+=$";
        public static void Menu()
        {
            bool ready = false;
            while (!ready)
            {
                Console.Clear();
                Console.WriteLine("Welcome to the agent app.");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine("1.Enter '1' and press 'Enter' to run the application.");
                Console.WriteLine("2.Enter '2' and press 'Enter' to open app settings.");
                Console.WriteLine("\nWhenever you want to stop this application, just close this window.");
                string input = Console.ReadLine().Trim();
                switch (input)
                {
                    case "1":
                        ready = true;
                        Console.Clear();
                        break;
                    case "2":
                        Settings();
                        break;
                    default:
                        Console.WriteLine("Invalid input.");
                        Thread.Sleep(sleepTime);
                        break;
                }
            }
        }
        private static void Settings()
        {
            bool goBack = false;
            AppSettings settings = AppSettings.GetSettings();
            while (!goBack)
            {
                Console.Clear();
                Console.WriteLine("Settings:");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine("'0' - Go back to the main menu.\n" +
                                  "'1' - Connect to another OPC UA server.\n" +
                                  "'2' - Connect to another Azure Communication Services.\n" +
                                  "'3' - Manage email addresses.\n" +
                                  "'4' - Manage delays.\n" +
                                  "'5' - Manage connection strings to Azure IoT Hub devices.\n" +
                                  "'6' - Show current settings.\n" +
                                  "'7' - Delete all data (back to default settings).");
                string input = Console.ReadLine().Trim();
                switch (input)
                {
                    case "0":
                        goBack = true;
                        break;
                    case "1":
                        ChangeServerConnectionString(settings);
                        break;
                    case "2":
                        ChangeCommunicationServices(settings);
                        break;
                    case "3":
                        ManageEmailAddresses(settings);
                        break;
                    case "4":
                        ChangeDelays(settings);
                        break;
                    case "5":
                        ManageConnectionStringsToAzureDevices(settings);
                        break;
                    case "6":
                        ShowCurrentSettings(settings);
                        break;
                    case "7":
                        ClearAllData(settings);
                        break;
                    default:
                        Console.WriteLine("Invalid input.");
                        Thread.Sleep(sleepTime);
                        break;
                }
            }
        }
        private static void ClearAllData(AppSettings settings)
        {
            settings.ServerConnectionString = null;
            settings.TelemetrySendingDelayInMs = AppSettings.defaultDelay;
            settings.ErrorCheckingDelayInMs = AppSettings.defaultDelay;
            settings.ProductionRateCheckingDelayInMs = AppSettings.defaultDelay;
            settings.CommunicationServicesConnectionString = null;
            settings.CommunicationServicesSender = null;
            settings.EmailAddresses.Clear();
            settings.AzureDevicesConnectionStrings.Clear();

            settings.SaveSettings();
            Console.WriteLine("Settings have been successfully reset.");
            Thread.Sleep(sleepTime);
        }
        private static void ChangeServerConnectionString(AppSettings settings)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Enter a new connection string to your OPC UA server.");
                Console.WriteLine("'0' - Go back to settings menu.");
                string connectionString = Console.ReadLine().Trim();
                if (connectionString.Equals("0"))
                {
                    break;
                }
                Regex regex = new Regex(opcPattern);
                if (regex.IsMatch(connectionString))
                {
                    settings.ServerConnectionString = connectionString;
                    settings.SaveSettings();
                    Console.WriteLine("Connection string has been successfuly modified.");
                    Thread.Sleep(sleepTime);
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid OPC UA server connection string.");
                    Thread.Sleep(sleepTime);
                }
            }
        }
        private static void ChangeCommunicationServices(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Enter a new connection string to your Azure Communication Services.");
                Console.WriteLine("'0' - Go back to settings menu.");
                string connectionString = Console.ReadLine().Trim();
                if (connectionString.Equals("0"))
                {
                    success = true;
                    break;
                }
                Regex regex = new Regex(communicationServicesPattern);
                if (!regex.IsMatch(connectionString))
                {
                    Console.WriteLine("Invalid Azure Communication Services connection string.");
                    Thread.Sleep(sleepTime);
                    continue;
                }
                while (!success)
                {
                    Console.Clear();
                    Console.WriteLine("Enter an email generated by Azure Communication Services, which will be a sender.");
                    string azureSender = Console.ReadLine().Trim();

                    regex = new Regex(emailPattern);
                    if (regex.IsMatch(azureSender))
                    {
                        settings.CommunicationServicesConnectionString = connectionString;
                        settings.CommunicationServicesSender = azureSender;
                        settings.SaveSettings();
                        Console.WriteLine("Azure Communication Services have been successfully connected.");
                        Thread.Sleep(sleepTime);
                        success = true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid Azure Communication Service connection string.");
                        Thread.Sleep(sleepTime);
                    }
                }
            }
        }
        private static void ChangeDelays(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Enter the new delay time in milliseconds (not less than 1000) in the following format:" +
                    "\n'telemetry' 'errors' 'production rate'" +
                    "\nExample: '1000 1000 5000'");
                Console.WriteLine("Current delays: {0} {1} {2}", settings.TelemetrySendingDelayInMs, settings.ErrorCheckingDelayInMs, settings.ProductionRateCheckingDelayInMs);
                Console.WriteLine("'0' - Go back to settings menu.");
                string input = Console.ReadLine().Trim();

                if (input.Equals("0"))
                {
                    break;
                }
                List<string> inputs = input.Split(' ', StringSplitOptions.TrimEntries).ToList();
                List<int> delays = new List<int>();
                if (inputs.Count != 3)
                {
                    Console.WriteLine("Invalid input. Please enter three delay values separated by white spaces.");
                }
                else
                {
                    try
                    {
                        foreach (string value in inputs)
                        {
                            if (!int.TryParse(value, out int delay) || delay < 1000)
                            {
                                throw new InvalidOperationException($"Invalid delay value: {value}. Please enter a valid delay value (not less than 1000).");
                            }
                            else
                            {
                                delays.Add(delay);
                            }
                        }
                        settings.TelemetrySendingDelayInMs = delays[0];
                        settings.ErrorCheckingDelayInMs = delays[1];
                        settings.ProductionRateCheckingDelayInMs = delays[2];
                        settings.SaveSettings();
                        Console.WriteLine("Delays have been changed successfully!");
                        success = true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                Thread.Sleep(sleepTime);
            }
        }
        private static void ShowCurrentSettings(AppSettings settings)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(settings);
                Console.WriteLine("'0' - Go back to settings menu.");
                string input = Console.ReadLine().Trim();
                if (input.Equals("0"))
                {
                    break;
                }
            }
        }
        #region Connection to Azure IoT devices
        private static void ManageConnectionStringsToAzureDevices(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Choose an operation on the list of connection strings to Azure IoT Hub Devices:");
                Console.WriteLine("'1' - Remove all connection strings.");
                Console.WriteLine("'2' - Add a connection string.");
                Console.WriteLine("'3' - Remove a connection string under some index.");
                Console.WriteLine("'0' - Go back to settings menu.");
                Console.WriteLine("Currently added connection strings to IoT Hub devices:");
                for (int i = 0; i < settings.AzureDevicesConnectionStrings.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {settings.AzureDevicesConnectionStrings[i]},");
                }
                string input = Console.ReadLine().Trim();
                switch (input)
                {
                    case "0":
                        success = true;
                        break;
                    case "1":
                        RemoveAllConnectionStringsToAzureDevices(settings);
                        break;
                    case "2":
                        AddConnectionStringToAzureDevice(settings);
                        break;
                    case "3":
                        RemoveConnectionStringToAzureDeviceAt(settings);
                        break;
                    default:
                        break;
                }
            }
        }
        private static void RemoveConnectionStringToAzureDeviceAt(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Enter the number of the connection string you want to delete (starting from 1).");
                Console.WriteLine("'0' - Go back to the menu of connection strings management.");
                string input = Console.ReadLine().Trim();
                if (input.Equals("0"))
                {
                    break;
                }
                if (Int32.TryParse(input, out int value))
                {
                    value -= 1;
                    if (value >= 0 && value < settings.AzureDevicesConnectionStrings.Count)
                    {
                        settings.AzureDevicesConnectionStrings.RemoveAt(value);
                        Console.WriteLine("String has been successfully removed");
                    }
                    else
                    {
                        Console.WriteLine("Number was out of range. Try again");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Try again");
                }
                Thread.Sleep(sleepTime);
            }
        }
        private static void AddConnectionStringToAzureDevice(AppSettings settings)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Enter a connection string to the IoT Hub device.");
                Console.WriteLine("'0' - Go back to the menu of connection strings management.");
                string input = Console.ReadLine().Trim();
                if (input.Equals("0"))
                {
                    settings.SaveSettings();
                    break;
                }
                Regex regex = new Regex(deviceConnectionStringPattern);
                if (regex.IsMatch(input))
                {
                    if (settings.AzureDevicesConnectionStrings.Contains(input))
                    {
                        Console.WriteLine("There is already such a connection string in the list.");
                    }
                    else
                    {
                        settings.AzureDevicesConnectionStrings.Add(input);
                        Console.WriteLine("Connection string has been successfully added.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid format. Try again.");
                }
                Thread.Sleep(sleepTime);
            }
        }
        private static void RemoveAllConnectionStringsToAzureDevices(AppSettings settings)
        {
            settings.AzureDevicesConnectionStrings.Clear();
            settings.SaveSettings();
            Console.WriteLine("All connection strings have been removed");
            Thread.Sleep(sleepTime);
        }
        #endregion
        #region Email Addresses
        private static void ManageEmailAddresses(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Choose an operation on list of email adresses of recipients:");
                Console.WriteLine("'1' - Remove all emails.");
                Console.WriteLine("'2' - Add a new email address.");
                Console.WriteLine("'3' - Remove an email address under some index.");
                Console.WriteLine("'0' - Go back to settings menu.");
                Console.WriteLine("Currently added email addresses:");
                for(int i = 0; i < settings.EmailAddresses.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {settings.EmailAddresses[i]},");
                }
                string input = Console.ReadLine().Trim();
                switch (input)
                {
                    case "0":
                        success = true;
                        break;
                    case "1":
                        RemoveAllEmailAddresses(settings);
                        break;
                    case "2":
                        AddEmailAddress(settings);
                        break;
                    case "3":
                        RemoveEmailAddressAt(settings);
                        break;
                    default:
                        break;
                }
            }
        }
        private static void RemoveAllEmailAddresses(AppSettings settings)
        {
            settings.EmailAddresses.Clear();
            settings.SaveSettings();
            Console.WriteLine("All email addresses of recipients have been removed");
            Thread.Sleep(sleepTime);
        }
        private static void AddEmailAddress(AppSettings settings)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Enter an email address of the recipient.");
                Console.WriteLine("'0' - Go back to the menu of email addresses management.");
                string input = Console.ReadLine().Trim();
                if (input.Equals("0"))
                {
                    settings.SaveSettings();
                    break;
                }
                Regex regex = new Regex(emailPattern);
                if (regex.IsMatch(input))
                {
                    if (settings.EmailAddresses.Contains(input))
                    {
                        Console.WriteLine("There is already such an email in the list.");
                    }
                    else
                    {
                        settings.EmailAddresses.Add(input);
                        Console.WriteLine("New recipient has been successfully added.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid format. Try again.");
                }
                Thread.Sleep(sleepTime);
            }
        }
        private static void RemoveEmailAddressAt(AppSettings settings)
        {
            bool success = false;
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Enter the number of the email address you want to delete (starting from 1).");
                Console.WriteLine("'0' - Go back to the menu of email addresses management.");
                string input = Console.ReadLine().Trim();
                if (input.Equals("0"))
                {
                    break;
                }
                if (Int32.TryParse(input, out int value))
                {
                    value -= 1;
                    if (value >= 0 && value < settings.EmailAddresses.Count)
                    {
                        settings.EmailAddresses.RemoveAt(value);
                        Console.WriteLine("Email has been successfully removed.");
                    }
                    else
                    {
                        Console.WriteLine("Number was out of range. Try again.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Try again.");
                }
                Thread.Sleep(sleepTime);
            }
        }
        #endregion
    }
}
