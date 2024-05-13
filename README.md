## AGENT APP - USER DOCUMENTATION (the README variant)

### Connection to OPC UA Server

#### 1. How to launch the application?
- Download and unpack the archive from _https://github.com/Markvereshch/IOT_2024ul_

- Find the DeviceReader file and open DeviceReader.sln _(make sure you have Visual Studio intalled on your computer - https://visualstudio.microsoft.com)_.

- Click on the green triangle on top.

- Congratulations! Application launched successfully _(but that doesn't mean it's ready to run)_!

#### 2. How to connect to the server?
You must provide a URL of your OPC UA server, which usually looks like this: `opc.tcp://localhost:4840/`

You have two options for passing this URL to the application:

* Find **app_settings.json** file in the `/bin/Debug/net6.0` folder _(it is created after first launch of the application.)_ and write your URL near the _"ServerConnectionString"_ property like this: 
`"ServerConnectionString": "YOUR_URL",`

 * Just launch the application and you will see a message, which will ask you to either start the program or open settings. In settings you will easily find where to enter your URL. About settings and configuration we will talk later.

#### 3. How is data read and written and how often are nodes-methods called?
- When the program starts, it reads the object folder node to collect all devices on the server _(devices names looks like 'Device [0-9]+', where [0-9]+ means they have a number from 0 to 'infinity' in their name)_.

- Then every 3 seconds _(you can change this delay in settings)_ it reads all read-only nodes (5 in total) from all devices in order to create telemetry message for each device. 

- Every 3 seconds _(you can change this delay in settings too)_, the program reads production rate of devices and device errors to track changes in their values.

- Method nodes are called only when the appropriate direct methods are called.

----
### Configuration of the Agent App and application settings

#### 1. Basic configuration 

_(if you just want to have device-cloud communication without notifications to your emails, when an error occurs):_
> [!NOTE]
> If you just want the basic functionality, prepare such data:
> 1. Connection string to your OPC UA server.
> 2. Connection string(s) to your Azure Devices _(the number of Azure devices must be greater than or equal to the number of devices on your OPC UA server. Otherwise, the application will throw an exception.)._
> 3. Delays in milliseconds for telemetry sending, production rate checking and error checking.

###### 1.1 Adding data using app_settings.json file
Find the **app_settings.json** file in the `/bin/Debug/net6.0` folder _(it is created after first launch of the application.) Open it and add your data. The result should look like this:

```
{
  "ServerConnectionString": "opc.tcp://localhost:3030/",
  "AzureDevicesConnectionStrings": [
    "HostName=IoT2024ul.azure-devices.net;DeviceId=pr_device1;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IoT2024ul.azure-devices.net;DeviceId=pr_device2;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IoT2024ul.azure-devices.net;DeviceId=pr_device3;SharedAccessKey=SHARED_ACCESS_KEY="
  ],
  "TelemetrySendingDelayInMs": 5000,
  "ErrorCheckingDelayInMs": 2000,
  "ProductionRateCheckingDelayInMs": 2000,
  "CommunicationServicesConnectionString": null,
  "CommunicationServicesSender": null,
  "EmailAddresses": []
}
```
> [!IMPORTANT]
> 1. If you don't enter information related to the sending notifications to emails, then program will work correctly, but you will see error messages in the console. Don't worry, this is fine.
> 2. The number of Azure devices must be greater than or equal to the number of devices on your OPC UA server. Otherwise, the application will throw an exception.
> 3. If something goes wrong (for example, incorrect data has been entered), the application will in most cases notify you and stop working.

###### 1.2 Adding data using settings menu

Launch the application. You will see such a message:

> Welcome to the agent app.
> 
> 1.Enter '1' and press 'Enter' to run the application.
> 
> 2.Enter '2' and press 'Enter' to open app settings.
> 
> Whenever you want to stop this application, just close this window.

Type "2" and press "Enter" to go to settings. You will see this navigation menu. I don't think it is necessary to go deep into this. Just type the number, press "Enter" and follow instructions. If something goes wrong, you will be notified by the program.

> Settings:
> 
>'0' - Go back to the main menu.
> 
>'1' - Connect to another OPC UA server.
> 
>'2' - Connect to another Azure Communication Services.
> 
>'3' - Manage email addresses.
> 
>'4' - Manage delays.
> 
>'5' - Manage connection strings to Azure IoT Hub devices.
> 
>'6' - Show current settings.
> 
>'7' - Delete all data (back to default settings).

#### 2. Full configuration

_(if you also want to sent notifications about occurred errors to email addresses):_

> [!NOTE]
> In addition to the basic data, prepare the following things:
> 1. Connection string to Azure Communication Services.
> 2. Azure Communications Services sender address.
> 3. Email addresses of recipients.

###### 1.1 Adding data using app_settings.json file
This is how the **app_settings.json** file should look like:
```
{
  "ServerConnectionString": "opc.tcp://localhost:3030/",
  "AzureDevicesConnectionStrings": [
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
  ],
  "TelemetrySendingDelayInMs": 5000,
  "ErrorCheckingDelayInMs": 2000,
  "ProductionRateCheckingDelayInMs": 2000,
  "CommunicationServicesConnectionString": "endpoint=https://AZURE_COMMUNICATION_SERVICES_NAME.europe.communication.azure.com/;accesskey=ACCESS_KEY==",
  "CommunicationServicesSender": "DoNotReply@SOME_SYMBOLS.azurecomm.net",
  "EmailAddresses": [
    "example@edu.uni.pl",
    "example2@gmail.com"
  ]
}
```
###### 1.2 Adding data using settings menu
Just follow instruction from the console. If something goes wrong, then application will notify you directly in the settings menu or during runtime.

----
### D2C Messages 
There are two types of D2C (device to cloud) messages: 
1. Telemetry 
2. Error messages

###### 1. Telemetry messages
Telemetry is sent to the IoT Hub every N seconds. 
> [!TIP]
> By default N is 3 seconds, but the user can change this value in the settings.

Here is an example of a telemetry message in .json format:
```
  {
    "deviceName": "Device 1",
    "productionStatus": 1,
    "workorderId": "06e01904-66ce-4211-aae6-2f41c26f44e4",
    "goodCount": 297,
    "badCount": 33,
    "temperature": 69.7541473423772,
    "EventProcessedUtcTime": "2024-05-10T17:45:28.1105086Z",
    "PartitionId": 1,
    "EventEnqueuedUtcTime": "2024-05-10T17:44:19.7840000Z",
    "IoTHub": {
      "MessageId": null,
      "CorrelationId": null,
      "ConnectionDeviceId": "pr_device1",
      "ConnectionDeviceGenerationId": "638472175037688109",
      "EnqueuedTime": "2024-05-10T17:44:19.5140000Z"
    }
  },
```
And here is an example of a telemetry message printed in the console:
```
10.05.2024 23:08:07: {"deviceName":"Device 1","productionStatus":1,"workorderId":"faaaba6a-6733-4980-b027-383fb9e88b8d","goodCount":89,"badCount":10,"temperature":61.07527874962923}
```
So we can see that each telemetry message has the following properties, that come from the server:
`deviceName, productionStatus, workorderId, goodCount, badCount and temperature`
###### 2. Error messages
Error messages are sent to IoT Hub only when an error occurs. The agent checks every device for new errors every N seconds.

> [!TIP]
> By default N is 3 seconds, but the user can change this value in the settings.

Here is an example of an error message: 
```
 { 
    "errorName": "PowerFailure, SensorFailure, Unknown",
    "newErrors": 3,
    "deviceName": "Device 2",
    "currentErrors": "'Power Failure' 'Sensor Failure' 'Unknown'",
    "currentErrorCode": 14,
    "EventProcessedUtcTime": "2024-05-10T17:45:28.1105086Z",
    "PartitionId": 1,
    "EventEnqueuedUtcTime": "2024-05-10T17:45:03.7540000Z",
    "IoTHub": {
      "MessageId": null,
      "CorrelationId": null,
      "ConnectionDeviceId": "pr_device2",
      "ConnectionDeviceGenerationId": "638472175108313361",
      "EnqueuedTime": "2024-05-10T17:45:03.6130000Z"
    }
  },
```
And here is an example of an error message printed in the console:
```
10.05.2024 23:09:36: {"errorName":"PowerFailure, SensorFailure","newErrors":2,"deviceName":"Device 1","currentErrors":"'Power Failure' 'Sensor Failure'","currentErrorCode":6}
```
Each error message contains such properties:  `errorName, newErrors, deviceName, currentErrors, currentErrorCode`

In addition, if a new error _(which means the currentErrorCode is not 0)_ occurs, a notification will be sent to predefined emails _(implemented by using Azure Communication Services, but we will talk more specifically about this later. )_ 

----

### Device twin

A device twin has two kinds of properties: desired and reported. 

Here is an example of a device twin:
```
{
	"deviceId": "pr_device1",
	"etag": "AAAAAAAAABI=",
	"deviceEtag": "MTMyMjgyMDQ5",
	"status": "enabled",
	"statusUpdateTime": "0001-01-01T00:00:00Z",
	"connectionState": "Disconnected",
	"lastActivityTime": "2024-05-10T17:45:41.348459Z",
	"cloudToDeviceMessageCount": 0,
	"authenticationType": "sas",
	"x509Thumbprint": {
		"primaryThumbprint": null,
		"secondaryThumbprint": null
	},
	"modelId": "",
	"version": 462,
	"properties": {
		"desired": {
			"ProductionRate": 100,
			"$metadata": {
				"$lastUpdated": "2024-05-10T07:11:30.9707312Z",
				"$lastUpdatedVersion": 18,
				"ProductionRate": {
					"$lastUpdated": "2024-05-10T07:11:30.9707312Z",
					"$lastUpdatedVersion": 18
				}
			},
			"$version": 18
		},
		"reported": {
			"DeviceError": 0,
			"ProductionRate": 100,
			"$metadata": {
				"$lastUpdated": "2024-05-10T17:44:18.3633681Z",
				"DeviceError": {
					"$lastUpdated": "2024-05-10T17:44:18.3633681Z"
				},
				"ProductionRate": {
					"$lastUpdated": "2024-05-10T17:44:18.3633681Z"
				}
			},
			"$version": 444
		}
	},
	"capabilities": {
		"iotEdge": false
	}
}
```

##### 1. Desired properties

> [!WARNING]
> There is only one valid desired property: `ProductionRate`

The desired property can only be set from the IoT Hub and the device will read it immediately. Furthermore, when the program starts, the production rate of the production line is initialized with values retrieved from the desired device twin.

Here is the device production rate initialization message at the beginning of the program.
```
10.05.2024 23:08:07: Initial production rate of 'ns=2;s=Device 2', which is determined by the Desired DT, is 70
```
Here is the console output when the desired production rate has been changed:
```
10.05.2024 23:20:10: Desired Production Value has changed to 90
10.05.2024 23:20:10: Production rate of Device 1 has been changed to 90
Code=[0]: Good - The operation completed successfully.
```
This is what the desired device twin looks like: 
```
	"properties": {
		"desired": {
			"ProductionRate": 100,
			"$metadata": {
				"$lastUpdated": "2024-05-10T07:11:30.9707312Z",
				"$lastUpdatedVersion": 18,
				"ProductionRate": {
					"$lastUpdated": "2024-05-10T07:11:30.9707312Z",
					"$lastUpdatedVersion": 18
				}
			},
			"$version": 18
		},
```
_(without the desired “ProductionRate” property, the device will have 0 as initial Production Rate. In addition, remember that Production Rate property must be an integer)_

##### 2. Reported properties 

> [!WARNING]
> There are two reported properties: `ProductionRate and DeviceError`

Reported properties are sent by the device. Device errors are initialized to 0 and the Production Rate is set to either the desired value or 0. Every N seconds, device errors and production rate are checked. If they don’t match their reported counterparts, then a new value is reported to the Device Twin. 

This is what the reported properties of the device twin look like: 
```
"reported": {
			"DeviceError": 0,
			"ProductionRate": 100,
			"$metadata": {
				"$lastUpdated": "2024-05-10T17:44:18.3633681Z",
				"DeviceError": {
					"$lastUpdated": "2024-05-10T17:44:18.3633681Z"
				},
				"ProductionRate": {
					"$lastUpdated": "2024-05-10T17:44:18.3633681Z"
				}
			},
			"$version": 444
		}
```
Here is the console message about changing reported production rate of the device:
```
10.05.2024 23:16:39: Production rate of Device 2 has been changed to 80
```

----

### Direct methods

You can call 2 direct methods on each device: `Emergency Stop and Reset Error Status`. 

#### 1. Emergency Stop

The production on the line stops and all error triggers are unchecked, but the EmergencyStop trigger is set.  

This results in the agent, which compares the current error code with the reported one every N seconds, detecting the difference between them. It then reports the new errorCode, which is equal to 1 (indicating only the EmergencyStop is active), to the device twin. 

> [!IMPORTANT]
> To invoke this method use the **EmergencyStop** keyword. 
> This method has no parameters and returns 0 if it was successful. Also remember that if the Emergency Stop flag is set, you won’t be able to start production on this line. To uncheck the Emergency Stop flag, you need to call the Reset Error Status direct method. 

###### Example of execution:
>
> Imagine this situation: we have a device with 3 active error flags.
>
> We go to the IoT Explorer, select this device and call the `EmergencyStop` method on it.
>
> Now we can observe that our device has only one active error flag - the Emergency Stop flag. We also now cannot run production on this device because it is locked.
>

* Device error flags in the simulator application before calling Emergency Stop:
  
![beforeES](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/5679767c-d6c4-44b8-acd9-af95e2f5a181)

* Invocation of the EmergencyStop direct method from the IoT Explorer:
  
![EmergencyStopFromIoTExplorer](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/47b17a68-31f8-48c2-9aa4-4482f9cba96d)

* IoT Explorer message about successful invocation:
```
Successfully invoked method 'EmergencyStop' on device 'pr_device1' with response {"status":0,"payload":null}
```

* Here is the console output after performing an Emergency Stop:
```
10.05.2024 23:22:33: EmergencyStop method executed on ns=2;s=Device 1
10.05.2024 23:22:34: {"errorName":"EmergencyStop","newErrors":1,"deviceName":"Device 1","currentErrors":"'Emergency Stop'","currentErrorCode":1}
```
* Device error flags in the simulator application after calling Emergency Stop:

![afterES](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/ea5bbc5f-c99e-4b8e-b53a-c0d1d9635239)

#### 2. Reset Error Status

Removes all error flags, including the Emergency Stop flag. As with the Emergency Stop direct method, it modifies error data on the server. The next time the agent compares the error codes of the device and its twin, it reports a new error code (which is 0, because all error flags were removed) from the server to the device twin. 

> [!IMPORTANT]
> To invoke this method use the **ResetErrorStatus** keyword. This method has no parameters and returns 0 if it was successful. 

###### Example of execution:
>
> Let's continue: Our device now have only one active error flag - the Emergency Stop flag. How can we uncheck it? 
>
> We go to the IoT Explorer, select this device and call the `ResetErrorStatus` method on it.
>
> Now we can see that the Emergency Stop flag is unchecked. This means we are ready to start production once again.

* Device error flags in the simulator application before calling Reset Error Status:

![beforeRES](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/1a5db963-cbbf-404c-bddf-8b4b086e2596)

* Invocation of the ResetErrorStatus direct method from the IoT Explorer:

![ResetErrorStatusFromIoTExplorer](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/f7e36fd6-559f-4817-b0b4-d00bd5f38045)

* IoT Explorer message about successful invocation:
```
Successfully invoked method 'ResetErrorStatus' on device 'pr_device1' with response {"status":0,"payload":null}
```

* Here is the console output after performing a Reset Error Status:
```
10.05.2024 23:24:17: ResetErrorStatus method executed on ns=2;s=Device 1
10.05.2024 23:24:18: {"errorName":"None","newErrors":0,"deviceName":"Device 1","currentErrors":"'None'","currentErrorCode":0}
```

* Device error flags in the simulator application after calling Reset Error Status:

![afterRES](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/1753fd6d-ef43-4e0e-b416-bde6f15a96ae)

#### 3. Default method

If you call a method that does not exist or make a mistake in the keyword of one of the methods mentioned above, the Default Method will be executed. Its purpose is to write in the console that an unknown method was called. 

###### Example of execution:
>
> Let's continue: We have successfully reset the error status on our device, so it is fair to celebrate. But how exactly?  
>
> We go to the IoT Explorer, select this device and call the `PizzaTime` method on it.
>
> Unfortunately, the agent doesn't provide such a method, so the Default Method will be executed instead:(

* Invocation of the ResetErrorStatus direct method from the IoT Explorer:

![DefaultMethodFromIoTExplorer](https://github.com/Markvereshch/IOT_2024ul/assets/113990877/f1866686-9dae-4e83-b1cc-6711e3c1b1c9)

* IoT Explorer message about successful invocation:
```
Successfully invoked method 'PizzaTime' on device 'pr_device1' with response {"status":0,"payload":null}
```

* Here is the console output:
```
10.05.2024 23:26:38: An unknown method was received on ns=2;s=Device 1
```
----
### Calculations

There are 3 types of calculations in the project that work with data from the IoT Hub: 

#### 1. Production KPIs 

Gives procent of good production in total volume, grouped by device in 5-minute windows.

Results are stored in blobs of the `production-rate-management` container.

Here is an example of the contents of the blob:
```
{"WindowEndTime":"2024-05-09T15:40:00.0000000Z","ConnectionDeviceId":"pr_device3","GoodCount":20825.0,"TotalVolume":23000.0,"ProcentOfGoodProduction":90.54347826086956}
{"WindowEndTime":"2024-05-09T15:40:00.0000000Z","ConnectionDeviceId":"pr_device2","GoodCount":15285.0,"TotalVolume":16933.0,"ProcentOfGoodProduction":90.26752495127857}
{"WindowEndTime":"2024-05-09T15:40:00.0000000Z","ConnectionDeviceId":"pr_device1","GoodCount":25132.0,"TotalVolume":27908.0,"ProcentOfGoodProduction":90.05303138884908}
{"WindowEndTime":"2024-05-09T15:45:00.0000000Z","ConnectionDeviceId":"pr_device3","GoodCount":48314.0,"TotalVolume":53360.0,"ProcentOfGoodProduction":90.54347826086956}
{"WindowEndTime":"2024-05-09T15:45:00.0000000Z","ConnectionDeviceId":"pr_device2","GoodCount":38976.0,"TotalVolume":43674.0,"ProcentOfGoodProduction":89.24302788844622}
{"WindowEndTime":"2024-05-09T15:45:00.0000000Z","ConnectionDeviceId":"pr_device1","GoodCount":62582.0,"TotalVolume":69774.0,"ProcentOfGoodProduction":89.69243557772236}
{"WindowEndTime":"2024-05-09T15:50:00.0000000Z","ConnectionDeviceId":"pr_device3","GoodCount":48314.0,"TotalVolume":53360.0,"ProcentOfGoodProduction":90.54347826086956}
{"WindowEndTime":"2024-05-09T15:50:00.0000000Z","ConnectionDeviceId":"pr_device2","GoodCount":42755.0,"TotalVolume":47907.0,"ProcentOfGoodProduction":89.2458304631891}
{"WindowEndTime":"2024-05-09T15:50:00.0000000Z","ConnectionDeviceId":"pr_device1","GoodCount":64473.0,"TotalVolume":71848.0,"ProcentOfGoodProduction":89.73527446832202}
{"WindowEndTime":"2024-05-09T15:55:00.0000000Z","ConnectionDeviceId":"pr_device3","GoodCount":48479.0,"TotalVolume":53549.0,"ProcentOfGoodProduction":90.53203607910513}
{"WindowEndTime":"2024-05-09T15:55:00.0000000Z","ConnectionDeviceId":"pr_device2","GoodCount":65526.0,"TotalVolume":73311.0,"ProcentOfGoodProduction":89.38085689732782}
{"WindowEndTime":"2024-05-09T15:55:00.0000000Z","ConnectionDeviceId":"pr_device1","GoodCount":75216.0,"TotalVolume":83800.0,"ProcentOfGoodProduction":89.75656324582339}
```

#### 2. Temperature 

Every 1 minute gives the average, minumun and maximum temperature over the last 5 minutes (grouped by device). 

Results are stored in blobs of the `temperature-measurements` container.

Here is an example of the contents of the blob:
```
{"WindowEndTime":"2024-05-10T17:45:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":64.8224820577887,"MaxTemp":148.0161017092273,"AvgTemp":93.91139210938522}
{"WindowEndTime":"2024-05-10T17:45:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":61.97338865614253,"MaxTemp":118.28735260430481,"AvgTemp":78.20113460297809}
{"WindowEndTime":"2024-05-10T17:46:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":64.8224820577887,"MaxTemp":148.0161017092273,"AvgTemp":95.2115624669086}
{"WindowEndTime":"2024-05-10T17:46:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":-509.0,"MaxTemp":885.0,"AvgTemp":71.90981008149583}
{"WindowEndTime":"2024-05-10T17:47:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":-509.0,"MaxTemp":885.0,"AvgTemp":71.90981008149583}
{"WindowEndTime":"2024-05-10T17:47:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":64.8224820577887,"MaxTemp":148.0161017092273,"AvgTemp":95.2115624669086}
{"WindowEndTime":"2024-05-10T17:48:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":-509.0,"MaxTemp":885.0,"AvgTemp":71.90981008149583}
{"WindowEndTime":"2024-05-10T17:48:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":64.8224820577887,"MaxTemp":148.0161017092273,"AvgTemp":95.2115624669086}
{"WindowEndTime":"2024-05-10T17:49:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":-509.0,"MaxTemp":885.0,"AvgTemp":71.90981008149583}
{"WindowEndTime":"2024-05-10T17:49:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":64.8224820577887,"MaxTemp":148.0161017092273,"AvgTemp":95.2115624669086}
{"WindowEndTime":"2024-05-10T17:50:00.0000000Z","ConnectionDeviceId":"pr_device2","MinTemp":-509.0,"MaxTemp":885.0,"AvgTemp":66.31752161795606}
{"WindowEndTime":"2024-05-10T17:50:00.0000000Z","ConnectionDeviceId":"pr_device1","MinTemp":72.3568517049645,"MaxTemp":142.27698614053816,"AvgTemp":96.36726945137383}
```
#### 3. Device errors 

Stores situations, whenever a device experienced more than 3 errors in under 1 minute. 

Results are stored in blobs of the `error-evaluations` container.

Here is an example of the contents of the blob:
```
{"WindowEndTime":"2024-05-09T18:16:32.4440000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T18:16:43.4920000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T18:17:19.4630000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":5.0}
{"WindowEndTime":"2024-05-09T18:18:13.3250000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T18:20:19.5180000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T18:20:54.7070000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T18:21:57.6160000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T20:05:41.5000000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T20:06:08.5940000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T20:06:50.4260000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":6.0}
{"WindowEndTime":"2024-05-09T20:07:01.1410000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":5.0}
{"WindowEndTime":"2024-05-09T20:07:08.5940000Z","ConnectionDeviceId":"pr_device3","OccuredErrors":3.0}
{"WindowEndTime":"2024-05-09T20:26:08.1690000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":4.0}
{"WindowEndTime":"2024-05-09T20:26:43.7790000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":6.0}
{"WindowEndTime":"2024-05-09T20:26:47.0440000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":4.0}
```
#### How are the calculations implemented?
Generally, data calculations are implemented in the following way:
1. Azure Stream Analytics reads data from the IoT Hub events.
2. Then it performs calculations on this data using different queries with different types of windows _(`Sliding` for error evaluations, `Hopping` for temperature measurements and `Tumbling` for production KPI)_.
3. Results are stored in blobs of different containers in the storage account.

----

### Business logic

There are 3 types of business logic implemented in the project:

#### 1. Emergency Stop Trigger

If a device experiences more than 3 errors in under 1 minute, then the *Emergency Stop* direct method is called.

##### How it is implemented?
1. Azure Stream Analytics reads data from the IoT Hub events.
2. Then it performs calculations on this data using special query, which checks for errors.
3. If more then 3 errors occurred in under 1 minute, then ASA sends a special message to the Service Bus Queue called `error-queue`.
   
   Here is an example message:
   
   ```{"WindowEndTime":"2024-05-10T17:45:03.7540000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":3.0}```
   
5. The new message triggers a _`invoke-emergency-stop` Function App_ that calls the _Emergency Stop_ direct method on the device.

Function app log output:
```
[2024-05-11T09:27:42.045Z] Received message body: {"WindowEndTime":"2024-05-11T09:27:35.4630000Z","ConnectionDeviceId":"pr_device1","OccuredErrors":3.0}
[2024-05-11T09:27:42.045Z] Deserialized object: ConnectionDeviceId: pr_device1, OccuredErrors: 3
[2024-05-11T09:27:42.049Z] Invoking emergency stop on device pr_device1...
[2024-05-11T09:27:42.385Z] Emergency stop has been invoked with status - 0
```
Agent app console output:
```
11.05.2024 11:27:42: EmergencyStop method executed on ns=2;s=Device 1
11.05.2024 11:27:47: {"errorName":"None","newErrors":0,"deviceName":"Device 1","currentErrors":"'Emergency Stop'","currentErrorCode":1}
```
> [!IMPORTANT]
> For the function app to work correctly, you must provide the following information:
> 1. Connection string to your IoT Hub.
> 2. Connection string to the Service Bus.
> 3. Service Bus Queue name.

If you launch this function app locally on your PC, then **local.settings.json** should look like this:
```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "IotHubConnectionString": "HostName=IoT2024ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=SOME_ACCESS_KEY=",
        "ServiceBusConnectionString": "Endpoint=sb://sb-messager.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SOME_ACCESS_KEY=",
        "ServiceBusQueueName": "error-queue"
    }
}
```
If you are running this function app from Azure, provide these parameters as environment variables.

#### 2. Decrease Production Rate Trigger

If a device experiences drop of good production rate below 90%, then desired production rate is decreased by 10 points.

##### How it is implemented?
1. Azure Stream Analytics reads data from the IoT Hub events.
2. Then it performs calculations on this data using special query and sends a special message to the Service Bus Queue called `production-rate-queue`.

   Here is an example message:
   
   ```{"WindowEndTime":"2024-05-10T17:45:03.7540000Z","ConnectionDeviceId":"pr_device2","OccuredErrors":3.0}```
   
3. The new message triggers a _`invoke-emergency-stop` Function App_. This function app checks if the total volume of the device has dropped below 90%. If so, the desired production rate of the device is reduced by 10 points.

Function app log output:
```
[2024-05-11T10:58:56.246Z] Deserialized object: ConnectionDeviceId: pr_device2, ProcentOfGoodProduction: 63,534265413706166
[2024-05-11T10:58:56.264Z] Decreasing production rate of device pr_device2...
[2024-05-11T10:58:56.287Z] Start processing HTTP request POST http://127.0.0.1:57253/Settlement/Complete
[2024-05-11T10:58:56.289Z] Sending HTTP request POST http://127.0.0.1:57253/Settlement/Complete
[2024-05-11T10:58:56.519Z] Received HTTP response headers after 203.0014ms - 200
[2024-05-11T10:58:56.523Z] End processing HTTP request after 245.7208ms - 200
[2024-05-11T10:58:56.640Z] Executed 'Functions.DecreaseDesiredProductionRateFunction' (Succeeded, Id=fa67b371-b4c4-41cd-8bc9-4b19ac51ecf3, Duration=865ms)
[2024-05-11T10:58:57.096Z] Desired production rate has been changed successfully! New desired rate: 60
```
Agent app console output:
```
11.05.2024 12:58:57: Desired Production Value has changed to 60
11.05.2024 12:58:57: Production rate of Device 2 has been changed to 60
Code=[0]: Good - The operation completed successfully.
```
> [!IMPORTANT]
> For the function app to work correctly, you must provide the following information:
> 1. Connection string to your IoT Hub.
> 2. Connection string to the Service Bus.
> 3. Service Bus Queue name.

If you launch this function app locally on your PC, then **local.settings.json** should look like this:
```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "IotHubConnectionString": "HostName=IoT2024ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=SOME_ACCESS_KEY=",
        "ServiceBusConnectionString": "Endpoint=sb://sb-messager.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SOME_ACCESS_KEY=",
        "ServiceBusQueueName": "production-rate-queue"
    }
}
```
If you are running this function app from Azure, provide these parameters as environment variables.

#### 3. Sending an email if a device error occurs

If a device error occurs, then an email will be send to predefined addresses.

##### How it is implemented?
1. Agent App constantly checks device errors.
2. If a new error occurs on any device, the agent runs the EmailSender class, which sends a custom message to the Azure communications service.
3. Azure Communication Services sends the email to all recipients specified in the application settings _(**app_settings.json**)_.

Here is an example email:

```
  DEVICE ERROR OCCURED!!!
  ​
  An error has occured on one of your devices. Please, take actions.
  
  {"errorName":"PowerFailure","newErrors":1,"deviceName":"Device 3","currentErrors":"'Emergency Stop' 'Power Failure'","currentErrorCode":3}
```
   
Agent app console output:
```
11.05.2024 13:45:30: {"errorName":"PowerFailure","newErrors":1,"deviceName":"Device 3","currentErrors":"'Emergency Stop' 'Power Failure'","currentErrorCode":3}
11.05.2024 13:45:30: Sending message to 2 email address/addresses...
*...some other messages in the console...*
11.05.2024 13:45:35: Notification about an error has been sent successfully.
```

> [!IMPORTANT]
> For the function app to work correctly, you must provide the following information:
> 1. Connection string to Azure Communication Services.
> 2. Azure Communications Services sender address.
> 3. Email addresses of recipients.

So this is an example how the **app_settings.json** file of the Agent app should look like:
```
{
  "ServerConnectionString": "opc.tcp://localhost:3030/",
  "AzureDevicesConnectionStrings": [
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
    "HostName=IOT_HUB_NAME.azure-devices.net;DeviceId=AZURE_DEVICE_ID;SharedAccessKey=SHARED_ACCESS_KEY=",
  ],
  "TelemetrySendingDelayInMs": 5000,
  "ErrorCheckingDelayInMs": 2000,
  "ProductionRateCheckingDelayInMs": 2000,
  "CommunicationServicesConnectionString": "endpoint=https://AZURE_COMMUNICATION_SERVICES_NAME.europe.communication.azure.com/;accesskey=ACCESS_KEY==",
  "CommunicationServicesSender": "DoNotReply@SOME_SYMBOLS.azurecomm.net",
  "EmailAddresses": [
    "example@edu.uni.pl",
    "example2@gmail.com"
  ]
}
```
Of course, you can add all this data using the settings menu.

As we already know, if you don't provide the correct information about Azure Communication Services or recipients, everything will work fine, but the program will print error messages when tries to send an email. _(see basic and full configuration)_

----
> [!TIP]
> If something goes wrong during execution _(for example, the program freezes)_, then simply close the application and restart it.
