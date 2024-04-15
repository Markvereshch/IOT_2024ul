﻿using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Text.RegularExpressions;
using VirtualDevices;
using DeviceReader;
using System.Text;

internal class ProgramEntryPoint
{
    private static ClientManager? clientManager;
    internal static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Enter URL of your OPC UA server:");
            string? input = Console.ReadLine();
            using (var client = new OpcClient(input))
            {
                client.Connect();
                BrowseConnectionStringsAndDevices(client, out var connections, out var devices);
                clientManager = new ClientManager(connections, devices, client);
                try
                {
                    await clientManager.InitializeClientManager();
                }
                catch (Exception ex) 
                {
                    Console.WriteLine("Something bad happened during connection. Please, check your connection strings or server");
                    Console.WriteLine("-----------------------");
                    Console.WriteLine(ex.Message);
                }
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
        catch(FileLoadException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch(UriFormatException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch(ArgumentException ex)
        {
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
            throw new FileLoadException("Devices not found");
        }
        else if (devices.Count <= connections.Count)
        {
            Console.WriteLine("Accessing connection strings...");
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Unable to connect real devices to IoT Hub due to the lack of connection string.");
            sb.AppendLine($"Please, open {path} and add {devices.Count - connections.Count} connection string(s) and restart this programm");
            throw new FileLoadException(sb.ToString());
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
