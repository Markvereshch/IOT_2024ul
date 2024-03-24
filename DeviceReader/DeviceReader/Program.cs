using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Text.RegularExpressions;
using VirtualDevices;
using DeviceReader;
internal class NodeReader
{
    private static ClientManager clientManager;
    public static async Task Main(string[] args)
    {
        try
        {
            using (var client = new OpcClient("opc.tcp://localhost:4840/"))
            {
                client.Connect();
                BrowseConnectionStringsAndDevices(client, out var connections, out var devices);
                clientManager = new ClientManager(connections, devices);
                await clientManager.TestMethod();
            }
        }
        catch(OpcException ex)
        {
            Console.WriteLine("The server is offline: ");
            Console.WriteLine("-----------------------");
            Console.WriteLine(ex.Message);
        }
        catch(FileNotFoundException ex)
        {
            Console.WriteLine("File \"connectionStrings.txt\" with connection strings not found: ");
            Console.WriteLine("-----------------------");
            Console.WriteLine(ex.Message);
        }
    }
    private static void BrowseConnectionStringsAndDevices(OpcClient client, out List<string> connections, out List<OpcNodeInfo> devices)
    {
        string binDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectDirectory = Directory.GetParent(binDirectory).Parent.Parent.Parent.FullName;
        string path = Path.Combine(projectDirectory, "connectionStrings.txt");
        if(!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }
        connections = ReadConnectionStrings(path);
        devices = BrowseDevices(client);
        if(devices.Count == 0)
        {
            Console.WriteLine("Devices not found");
        }
        else if (devices.Count <= connections.Count)
        {
            Console.WriteLine("Accessing connection strings...");
        }
        else
        {
            Console.WriteLine("Unable to connect real devices to IoT Hub due to the lack of connection string.");
            Console.WriteLine("Please, open {0} and add {1} connection string(s) and restart this programm", path, devices.Count - connections.Count);
        }
    }
    private static List<string> ReadConnectionStrings(string path) //method for reading all connection strings to IoT Hub devices
    {
        List<string> connections = new List<string>();
        string pattern = @"^HostName=.+.azure-devices.net;DeviceId=.+;SharedAccessKey=.+=$";
        Regex regex = new Regex(pattern);
        using(StreamReader sr = new StreamReader(path))
        { 
            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine() ?? "";
                if (line != "")
                {
                    var match = regex.Match(line);
                    if(match.Success)
                        connections.Add(match.Value);
                }
            }
        }
        return connections;
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
    private static bool IsDeviceNode(OpcNodeInfo nodeInfo) //Method for understanding, whether node is device node or not
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
    private static void TestMethod(OpcClient client) //Test to read all information from all available devices
    {
        var devices = BrowseDevices(client);
        foreach (var device in devices)
        {
            string name = device.Attribute(OpcAttribute.DisplayName).Value.ToString();
            Console.WriteLine(name + "\n---------------------------------------------->");
            string nodeId = $"ns=2;s={name}/";
            OpcReadNode[] commands = new OpcReadNode[] {
                    new OpcReadNode(nodeId + "ProductionStatus", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "ProductionStatus"),
                    new OpcReadNode(nodeId + "ProductionRate", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "ProductionRate"),
                    new OpcReadNode(nodeId + "WorkorderId", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "WorkorderId"),
                    new OpcReadNode(nodeId + "Temperature", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "Temperature"),
                    new OpcReadNode(nodeId + "GoodCount", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "GoodCount"),
                    new OpcReadNode(nodeId + "BadCount", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "BadCount"),
                    new OpcReadNode(nodeId + "DeviceError", OpcAttribute.DisplayName),
                    new OpcReadNode(nodeId + "DeviceError"),
                };
            IEnumerable<OpcValue> job = client.ReadNodes(commands);

            foreach (var item in job)
            {
                Console.WriteLine(item.Value);
            }
            Console.WriteLine();
        }
    }
}
