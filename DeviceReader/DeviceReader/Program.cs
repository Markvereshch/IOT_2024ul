using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Text.RegularExpressions;
using VirtualDevices;
using DeviceReader;
using System.Text;
using AgentApp;
using System.IO;

internal static class ProgramEntryPoint
{
    private static ClientManager? clientManager;
    internal static async Task Main(string[] args)
    {
        try
        {
            SettingsManager.Menu();
            using (var client = new OpcClient(AppSettings.GetSettings().ServerConnectionString))
            {
                client.Connect();
                
                var connections = AppSettings.GetSettings().AzureDevicesConnectionStrings;
                var devices = ConnectDevicesWithIoTDevices(client, connections);
                clientManager = new ClientManager(connections, devices, client);
                try
                {
                    await clientManager.InitializeClientManager();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Something bad happened during connection. Please, check your connection strings or the OPC UA server.");
                    Console.WriteLine("-----------------------");
                    Console.WriteLine(ex.Message);
                }
            }
        }
        catch(OpcException ex)
        {
            Console.WriteLine("The server is offline. ");
            Console.WriteLine("-----------------------");
            Console.WriteLine(ex.Message);
        }
        catch(UriFormatException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch(ArgumentException ex)
        {
            Console.WriteLine($"Invalid address to OPC UA server. {ex.Message}");
        }
        catch(FileLoadException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch(Exception ex) 
        {
            Console.WriteLine($"Unknown errror: {ex.Message}");
        }
    }
    private static List<OpcNodeInfo> ConnectDevicesWithIoTDevices(OpcClient client, List<string> connections)
    {
        List<OpcNodeInfo> devices = new List<OpcNodeInfo> ();
        devices = BrowseDevices(client);
        if (devices.Count == 0)
        {
            throw new FileLoadException("Devices not found.");
        }
        else if (devices.Count <= connections.Count)
        {
            Console.WriteLine("Accessing connection strings...");
            return devices;
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Unable to connect real devices to the IoT Hub due to the lack of a connection string.");
            sb.AppendLine($"Please, add {devices.Count - connections.Count} connection string(s).");
            throw new FileLoadException(sb.ToString());
        }
    }
    private static List<OpcNodeInfo> BrowseDevices(OpcClient client) //Method for counting how many devices we have in our system (Stopped and working)
   {
        var objectFolder = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
        var devices = new List<OpcNodeInfo>();
        foreach (var childNode in objectFolder.Children())
        {
            if (IsDeviceNode(childNode))
                devices.Add(childNode);
        }
        return devices;
    }
    private static bool IsDeviceNode(OpcNodeInfo nodeInfo) //Method for understanding, whether node is serverDevice node or not
    {
        string pattern = @"^Device [0-9]+$";
        Regex exp = new Regex(pattern);
        string nodeName = nodeInfo.Attribute(OpcAttribute.DisplayName).Value.ToString();
        MatchCollection matchedName = exp.Matches(nodeName);
        if (matchedName.Count == 1)
            return true;
        else
            return false;
    }
}
