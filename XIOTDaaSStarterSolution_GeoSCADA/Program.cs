using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Security;
// To build this for your version of Geo SCADA, add a reference to c:\Program Files\Schneider Electric\ClearSCADA\ClearSCADA.Client.dll
using ClearScada.Client;

// Ensure you set the Geo SCADA parameters and the ServiceBusConnectionString below


delegate T Load<T>(T Tag);

namespace XIOTDaaSStarterSolution_GeoSCADA
{
    /// <summary>
    /// ProgramReceived : will receive the message and writes into Geo SCADA
    /// </summary>
    class Program
    {

        #region variables
        // Set to the base name of database points - name = "<tag base>.<device name>.<point name>"
        const string TagBase = "My XIOT Devices";

        // Enter your Geo SCADA user credentials. The user should have Control privilege to write point data
        const string GeoSCADAuser = ""; // TYPE YOUR USER NAME FOR ACCESS TO THE DATABASE
        // Your solution should store and retrieve this securely, not using a constant
        const string GeoSCADApass = ""; // YOUR PASSWORD CAN BE PUT HERE - WE RECOMMEND YOU SECURE THIS WITH YOUR OWN METHODS
        // Geo SCADA server
        const string GeoSCADAnode = "127.0.0.1";
        const int GeoSCADAport = 5481;

        // Set this to the string given to you by the web site - NOTE THAT THE STRING BELOW IS NOT VALID, YOU ENTER YOUR OWN TEXT
        const string ServiceBusConnectionString = "Endpoint=sb://dass-test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=slVnJLa7QXMm8qEc9/pGm3xtrs1wKzf+q3aImwq1iZI=;EntityPath=dass-queue1";

        static IQueueClient queueClient;

        static readonly ClearScada.Client.Simple.Connection connection = new ClearScada.Client.Simple.Connection("Utility");

        #endregion

        #region methods and functions

        /// <summary>
        /// setUpQueueConnection: does all required intial setup of Service Bus connection
        /// </summary>
        static bool SetUpQueueConnection()
        {
            try
            {
                //opening the connection of Service Bus
                var builder = new ServiceBusConnectionStringBuilder(ServiceBusConnectionString);
                queueClient = new QueueClient(builder);
            } catch (Exception e)
			{
                Console.WriteLine("Queue Connection Fault: " + e.Message);
                return false;
			}
            return true;
        }

        /// <summary>
        /// Setup: do the all required intial Setup (setup the connection of Service Bus and Geo SCADA)
        /// </summary>
        static bool SetUpGeoSCADAConnection()
        {
            // The arguments here will specify your server by its IP address and port. These are the defaults for local use.
            // Older Geo SCADA uses param: ClearScada.Client.ConnectionType.Standard
#pragma warning disable 612, 618
            ClearScada.Client.ServerNode node = new ClearScada.Client.ServerNode(ConnectionType.Standard, GeoSCADAnode, GeoSCADAport);
            try
            {
                connection.Connect(node);
            }
            catch (CommunicationsException)
            {
                Console.WriteLine("Unable to communicate with Geo SCADA server.");
                return false;
            }
#pragma warning restore 612, 618

            if (!connection.IsConnected)
            {
                Console.WriteLine("Not connected to Geo SCADA server.");
                return false;
            }
            using (var spassword = new System.Security.SecureString())
            {
                foreach (var c in GeoSCADApass)
                {
                    spassword.AppendChar(c);
                }
                try
                {
                    connection.LogOn(GeoSCADAuser, spassword);
                }
                catch (AccessDeniedException)
                {
                    Console.WriteLine("Access denied, incorrect user Id or password");
                    return false;
                }
                catch (PasswordExpiredException)
                {
                    Console.WriteLine("Credentials expired.");
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// CloseGeoSCADAConnection : close the connection
        /// </summary>
        static void CloseGeoSCADAConnection()
        {
            Console.WriteLine("Closing connection, please wait...");
            connection.Disconnect();
        }

        /// <summary>
        /// CloseQueueConnection : closing the Queue Connection
        /// </summary>
        static void CloseQueueConnection()
        {
            //closing the connection of Service Bus
            queueClient.CloseAsync();
        }

        /// <summary>
        /// Consume : consume the message from the Queue
        /// </summary>
        /// <returns></returns>
        static async Task Consume()
        {

            Console.WriteLine("=======================");
            Console.WriteLine("Press ENTER key to exit");
            Console.WriteLine("=======================");

            // Register QueueClient's MessageHandler and receive messages in a loop

            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ReceiveMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ReceiveMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            queueClient.RegisterMessageHandler(Transform, messageHandlerOptions);

            Console.ReadLine();
        }

        /// <summary>
        /// Transform :Receives the message from the queue and Transform
        /// </summary>
        /// <param name="message">massage</param>
        /// <param name="token"></param>
        /// <returns></returns>
        static async Task Transform(Message message, CancellationToken token)
        {
            // Receives the messages and Writing on to the console
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            //getting the recordType
            dynamic parseMessage = JObject.Parse(Encoding.UTF8.GetString(message.Body));
            int recordType = parseMessage.RecordType;

            //getting the record Type
            switch (recordType)
            {
                //case 16:
                    //var Tag = JsonConvert.DeserializeObject<dynamic>(message);
                    //break;
                //case 32:
                    //dynamic Tag = JsonConvert.DeserializeObject<dynamic>(message);
                    //break;
                case 48: // Most fields except switch data
                    var Tag48 = JsonConvert.DeserializeObject<TAGModel48>(Encoding.UTF8.GetString(message.Body));
                    Load<TAGModel48>(Tag48);
                    break;
                case 64: // Includes all data fields
                    var Tag64 = JsonConvert.DeserializeObject<TAGModel64>(Encoding.UTF8.GetString(message.Body));
                    Load<TAGModel64>(Tag64);
                    break;
                case 255: // Includes only lat and long, and device (but not station)
                    var Tag255 = JsonConvert.DeserializeObject<TAGModel255>(Encoding.UTF8.GetString(message.Body));
                    Load<TAGModel255>(Tag255);
                    break;
                default:
                    Console.WriteLine($"Message Type {recordType} Not Supported");
                    break;
            }

            //Clearing the message from the queue
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);

        }

        /// <summary>
        /// Load: Write/store message into Geo SCADA
        /// </summary>
        /// <param name="message">message from the queue</param>
        static void Load<T>(T Tag)
        {

            #region sampleTag
            // Tag64 format with all fields.
            /* {
                "device": "88AC1A",
                "time": "2018-09-28T13:56:31",
                "avgSnr": "20.77",
                "data": "4040000100000000000001",
                "duplicate": "false",
                "snr": "15.04",
                "station": "326D",
                "lat": "46.0",
                "lng": "0.0",
                "rssi": "-124.00",
                "seqNumber": "722",
                "RecordType": 64,
                "FrameCnt1": 2,
                "CommandDone": 0,
                "HWError": 0,
                "LowBatError": 0,
                "ConfigOK": 0,
                "S1ClosedCnt": 1,
                "S2ClosedCnt": 0,
                "S3ClosedCnt": 0,
                "S4ClosedCnt": 0,
                "S4PreviousState": 0,
                "S4State": 0,
                "S3PreviousState": 0,
                "S3State": 0,
                "S2PreviousState": 0,
                "S2State": 0,
                "S1PreviousState": 0,
                "S1State": 1,
                "FrameCnt2": 2,
                "FrameCnt3": 2,
                "FrameCnt4": 2,
                "SwitchError": 0,
                "S1OpenCnt": 1,
                "S2OpenCnt": 0
               } */
            #endregion

            // To iterate over the tags
            Type tagType = Tag.GetType();
            PropertyInfo[] props = tagType.GetProperties();

            // We need the device, time and station properties, the rest could be points
            string device = "";
            string time = "";
            string station = "";
            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    switch (prop.Name)
                    {
                        case "device":
                            device = prop.GetValue(Tag).ToString();
                            break;
                        case "time":
                            time = prop.GetValue(Tag).ToString();
                            break;
                        case "station":
                            station = prop.GetValue(Tag).ToString();
                            break;
                        default:
                            break;
                    }

                }
            }

            // Convert timestamp to a DateTime - default to now.
            DateTime MessageTime = DateTime.UtcNow;
            try
            {
                MessageTime = DateTime.Parse(time);
            }
			catch
			{
                Console.WriteLine( $"Cannot parse time: {time}");
			}

            // XIOT devices are identified uniquely by a "device" property.
            // We will use the "device" property and a fixed prefix "TagBase" to construct a group name.
            // e.g. "My XIOT Devices.88AC1A"
            // Then add the tag property as the point name (analog, digital or string Internal point)
            // e.g. "My XIOT Devices.88AC1A.S1State"
            // The "time" field (UTC) will be used to set the point's PresetTimestamp
            // If a point does not exist we log and move on.

            // Define the group reference 
            ClearScada.Client.Simple.DBObject myGroup;
            string tagname = TagBase + "." + device;

            // Find a group object which could contain the points
            myGroup = connection.GetObject(tagname);
            if (myGroup == null)
			{
                // Set breakpoints here to find device groups not in the database
                Console.WriteLine($"*** Cannot find group {TagBase + "." + device}");
                return;
			}
            if (!((string)myGroup["TypeName"] == "CGroup") && !((string)myGroup["TypeName"] == "CTemplateInstance"))
			{
                Console.WriteLine($"Object {(string)myGroup["TypeName"]} is not a group.");
                return;
			}

            // Get a list of all the child objects - could get all descendants here if points are actually in further groups.
            ClearScada.Client.Simple.DBObjectCollection childObjects;
            childObjects = myGroup.GetChildren("CDBPoint", "");
            if (childObjects.Count == 0)
			{
                Console.WriteLine($"Group {myGroup.FullName} contains no points");
                return;
			}

            // For each property, see if there is a matching point.
            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    // See if there is a point of this name
                    foreach (var PointObject in childObjects)
					{
                        if (prop.Name == PointObject.Name)
						{
                            // Get Property value from the JSON structure
                            string Value = "";
                            var tagVal = prop.GetValue(Tag);
                            // First check is in case a json property is missing
                            if (tagVal != null && tagVal.ToString() != null)
							{
                                Value = prop.GetValue(Tag).ToString();
                            }
                            Console.WriteLine($"Try to set point {PointObject.FullName} to value {Value} at time {time}");
                            try
                            {
                                TrySetPointValue(PointObject, Value, MessageTime);
                            }
                            catch (Exception e)
							{
                                Console.WriteLine("Exception writing point data: " + e.Message);
							}
						}
					}
                }
            }
        }

        static void TrySetPointValue(ClearScada.Client.Simple.DBObject PointObject, string Value, DateTime MessageTime)
		{
            // Check the type of point and item.
            switch ((string)PointObject["TypeName"])
            {
                case "CPointStringInternal":
                    PointObject["PresetTimestamp"] = MessageTime;
                    PointObject.InvokeMethod("CurrentValue", Value);
                    break;
                case "CPointDigitalManual":
                    int IntValue;
                    if (int.TryParse(Value, out IntValue))
                    {
                        if (IntValue < 8)
                        {
                            PointObject["PresetTimestamp"] = MessageTime;
                            PointObject.InvokeMethod("CurrentState", IntValue);
                        }
                    }
                    else
					{
                        Console.WriteLine($"Value {Value} not valid for: {PointObject.FullName}");
                    }
                    break;
                case "CPointAlgManual":
                    double AlgValue;
                    if (double.TryParse(Value, out AlgValue))
                    {
                        PointObject["PresetTimestamp"] = MessageTime;
                        PointObject.InvokeMethod("CurrentValue", AlgValue);
                    }
                    else
                    {
                        Console.WriteLine($"Value {Value} not valid for: {PointObject.FullName}");
                    }
                    break;
                default:
                    Console.WriteLine($"Point type not valid for: {PointObject.FullName}");
                    break;
            }
        }

        /// <summary>
        /// ExceptionReceived get called when Exception occurs from Service Bus
        /// </summary>
        /// <param name="exceptionReceivedEventArgs"></param>
        /// <returns></returns>
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Main : Method where program will start
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //setting Up the Queue Connection
            if (!SetUpQueueConnection())
			{
                return;
			}
            //setting Up the Wonderware Historian Connection
            if (!SetUpGeoSCADAConnection())
			{
                return;
			}

            //Consume the message
            Consume().GetAwaiter().GetResult();

            //Closing the Queue Connection
            CloseQueueConnection();
            //Closing the Geo SCADA Connection
            CloseGeoSCADAConnection();
        }

        #endregion
    }

    /// <summary>
    /// TAGModel : Model classes for Tag
    /// </summary>
    class TAGModel64
    {
        public string device { get; set; }
        public string time { get; set; }
        public string avgSnr { get; set; }
        public string data { get; set; }
        public string duplicate { get; set; }
        public string snr { get; set; }
        public string station { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string rssi { get; set; }
        public string seqNumber { get; set; }
        public string RecordType { get; set; }
        public string CommandDone { get; set; }
        public string HWError { get; set; }
        public string LowBatError { get; set; }
        public string ConfigOK { get; set; }
        public string SwitchError { get; set; }
        public string FrameCnt1 { get; set; }
        public string FrameCnt2 { get; set; }
        public string FrameCnt3 { get; set; }
        public string FrameCnt4 { get; set; }
        public string S1ClosedCnt { get; set; }
        public string S2ClosedCnt { get; set; }
        public string S3ClosedCnt { get; set; }
        public string S4ClosedCnt { get; set; }
        public string S1PreviousState { get; set; }
        public string S2PreviousState { get; set; }
        public string S3PreviousState { get; set; }
        public string S4PreviousState { get; set; }
        public string S1State { get; set; }
        public string S2State { get; set; }
        public string S3State { get; set; }
        public string S4State { get; set; }
        public string S1OpenCnt { get; set; }
        public string S2OpenCnt { get; set; }
        public string S3OpenCnt { get; set; }
        public string S4OpenCnt { get; set; }

    }
    class TAGModel48
    {
        public string device { get; set; }
        public string time { get; set; }
        public string avgSnr { get; set; }
        public string duplicate { get; set; }
        public string snr { get; set; }
        public string station { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string rssi { get; set; }
        public string seqNumber { get; set; }
        public string RecordType { get; set; }
        public string CommandDone { get; set; }
        public string HWError { get; set; }
        public string LowBatError { get; set; }
        public string ConfigOK { get; set; }
        public string SwitchError { get; set; }
        public string FrameCnt1 { get; set; }
        public string FrameCnt2 { get; set; }
        public string FrameCnt3 { get; set; }
        public string FrameCnt4 { get; set; }
    }
    class TAGModel255
    {
        public string device { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string RecordType { get; set; }
    }
}
