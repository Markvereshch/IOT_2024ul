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
            Console.WriteLine($"Unknown error: {ex.Message}");
        }
    }
    private static List<OpcNodeInfo> ConnectDevicesWithIoTDevices(OpcClient client, List<string> connections) //Checks, whether we can connect server devices to azure devices. If no, then FileLoadException is thrown.
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
            throw new FileLoadException("Unable to connect real devices to the IoT Hub due to the lack of a connection string.\n" +
                                        $"Please, add {devices.Count - connections.Count} connection string(s).");
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
    private static bool IsDeviceNode(OpcNodeInfo nodeInfo) //Method for understanding, whether node is a serverDevice node or not
    {
        string pattern = @"^Device [0-9]+$";
        Regex correctName = new Regex(pattern);
        string nodeName = nodeInfo.Attribute(OpcAttribute.DisplayName).Value.ToString();
        Match matchedName = correctName.Match(nodeName);
        if (matchedName.Success)
            return true;
        else
            return false;
    }
}
