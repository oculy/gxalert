namespace GxAlert
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using NHapi.Base.Model;
    using NHapi.Base.Parser;
    using NHapi.Model.V25.Message;

    /// <summary>
    /// Center piece of the GXAlert solution: The Server/listener
    /// </summary>
    public class Server
    {
        // the listener that listens for a medical device connection at a certain port
        private TcpListener tcpListener;

        // this is the thread for the listener
        private Thread listenThread;

        // will use this encoder to convert bytes to string
        private ASCIIEncoding encoder = new ASCIIEncoding();

        // this delegate will let us take care of notifications asynchronously
        private delegate void SendNotificationsDelegate(int? testId);

        private SendNotificationsDelegate sendNotificationsDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// Starts listening-thread
        /// </summary>
        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, Convert.ToInt32(ConfigurationManager.AppSettings["portToListenOn"]));
            this.listenThread = new Thread(new ThreadStart(this.ListenForClients));
            this.listenThread.Start();
        }

        /// <summary>
        /// Listener that listens for new clients
        /// </summary>
        private void ListenForClients()
        {
            this.tcpListener.Start();

            // log that server started
            Logger.Log("Server Started", LogLevel.Info);

            while (true)
            {
                // blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                // create a thread to handle communication with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(this.HandleClientComm));
                clientThread.Start(client);
            }
        }

        /// <summary>
        /// This is a thread for the client that has connected
        /// </summary>
        /// <param name="client">Client (medical device)</param>
        private void HandleClientComm(object client)
        {
            #region Local Variables
            // TCP client that connected
            TcpClient tcpClient = (TcpClient)client;

            // Get reference to stream for client so we can read/write data
            NetworkStream networkStream = tcpClient.GetStream();

            // log that new client connected
            Logger.Log(string.Format("New client connected: {0}", tcpClient.Client.RemoteEndPoint.ToString()), LogLevel.Info);

            // variable to keep track of the bytes we received in the client stream
            int bytesRead;

            // variable for constructing the message
            string hl7Message = string.Empty;

            // this will store all messages we receive for later ACK:
            List<DataForHL7Acknowledgement> dataForAck = new List<DataForHL7Acknowledgement>();

            // keep track of whether we're awaiting acknowledgement of our HL7-ACK
            bool awaitingHl7Ack = false;

            // keep track of connection state:
            ConnectionState connState = ConnectionState.Neutral;

            this.sendNotificationsDelegate = new SendNotificationsDelegate(new Notifications().SendNotifications);

            #endregion

            while (true)
            {
                #region Lower-level stuff (dealing with streams and receiving it as a message)
                bytesRead = 0;
                string message = string.Empty;

                try
                {
                    if (networkStream.CanRead)
                    {
                        byte[] messagePart = new byte[1024];
                        string completeMessage = string.Empty;
                        string lf = this.encoder.GetString(new byte[] { Constants.LF });

                        // Incoming message may be larger than the buffer size; keep reading until complete
                        do
                        {
                            bytesRead = networkStream.Read(messagePart, 0, messagePart.Length);

                            completeMessage = string.Concat(completeMessage, this.encoder.GetString(messagePart, 0, bytesRead));
                        }
                        while (!completeMessage.EndsWith(lf) &&
                                completeMessage.Length != 1 &&
                                bytesRead != 0);
                        /* Read from the stream until we received everything. 
                           If we're reading an HL7 message, that means that the message ends with a line feed.
                           If it's just the low-level ASTM stuff, the length will be one
                           That way we can avoid the nasty workaround of putting a 10ms sleep in the read-loop*/

                        message = completeMessage;
                    }
                    else
                    {
                        Logger.Log("Cannot read from this NetworkStream.", LogLevel.Error);
                    }
                }
                catch (Exception e)
                {
                    // a socket error has occurred
                    Logger.Log("A socket error has occurred: " + e.Message, LogLevel.Error);
                    break;
                }

                // did client disconnect?
                if (bytesRead == 0)
                {
                    Logger.Log(string.Format("Client disconnected (IP: {0})", tcpClient.Client.RemoteEndPoint.ToString()), LogLevel.Info);
                    tcpClient.Close();
                    Thread.CurrentThread.Abort();
                    break;
                }
                #endregion

                // finally, handle the message itself:
                if (message[0] == Constants.ENQ)
                {
                    Logger.Log("Sending ACK in response to ENQ...", LogLevel.Info);
                    networkStream.WriteByte(Constants.ACK);
                    connState = ConnectionState.Receiving;
                }
                else if (connState == ConnectionState.Receiving && message[0] == Constants.EOT)
                {
                    // if EOT of full message, confirm end of receiving and start transmission of HL7 ACK
                    Logger.Log("Sending ENQ...", LogLevel.Info);
                    networkStream.WriteByte(Constants.ENQ);
                    connState = ConnectionState.Sending;
                }
                else if (connState == ConnectionState.Sending && message[0] == Constants.EOT)
                {
                    // We got an EOT in response to our hl7-EOT? - do nothing (we're back in neutral state)
                    Logger.Log("Received EOT from Device", LogLevel.Info);
                    connState = ConnectionState.Neutral;
                }
                else if (connState == ConnectionState.Sending && message[0] == Constants.ACK)
                {
                    // Device acknowledged last message -> remove from list:
                    if (dataForAck.Any() && awaitingHl7Ack)
                    {
                        dataForAck.Remove(dataForAck.First());
                    }

                    // if no more messages to acknowledge, send EOT
                    if (!dataForAck.Any())
                    {
                        Logger.Log("Sending EOT in response to ACK for HL7-ACK...", LogLevel.Info);
                        networkStream.WriteByte(Constants.EOT);
                        awaitingHl7Ack = false;
                        connState = ConnectionState.Neutral;
                    }
                    else
                    {
                        Logger.Log("Sending ACK of entire message...", LogLevel.Info);

                        awaitingHl7Ack = true;

                        // build ACK-response from message data
                        string hl7Response = this.ConstructHL7Acknowledgement(dataForAck.First());

                        // get checksum in hexadecimal for sending:
                        string checksumHex = new Util().GetChecksum(this.encoder.GetBytes("1" + hl7Response + this.encoder.GetString(new byte[1] { Constants.ETX })), 0).ToString("X");

                        // wrapping message in astm control chars ("1" = Frame Number)
                        hl7Response = this.encoder.GetString(new byte[1] { Constants.STX }) + "1" + hl7Response +
                                        this.encoder.GetString(new byte[1] { Constants.ETX }) +
                                        checksumHex.Substring(checksumHex.Length - 2, 1) + checksumHex.Substring(checksumHex.Length - 1, 1) +
                                        this.encoder.GetString(new byte[1] { Constants.CR }) + this.encoder.GetString(new byte[1] { Constants.LF });

                        // finally, convert message to byte array and write to stream
                        byte[] response = this.encoder.GetBytes(hl7Response);
                        networkStream.Write(response, 0, response.Length);
                    }
                }
                else if (connState == ConnectionState.Receiving &&
                            message[0] == Constants.STX &&
                            (message.Any(m => m == Constants.ETX) || message.Any(m => m == Constants.ETB)))
                {
                    int frameNumber = Convert.ToInt32(message[1]) - 48; // TODO: verify that this is the frame number we expected, return NAK if not
                    bool isEndFrame = message.Any(m => m == Constants.ETX);

                    Logger.Log(string.Format("Received {0}frame (Nr. {1})", isEndFrame ? "end" : string.Empty, frameNumber), LogLevel.Info);

                    // get end index of actual message so we can calculate checksum
                    int indexOfEndMessage = message.IndexOf(isEndFrame ? Encoding.ASCII.GetString(new byte[] { Constants.ETX }) : Encoding.ASCII.GetString(new byte[] { Constants.ETB })); // Array.IndexOf(message, isEndFrame ? Constants.ETX : Constants.ETB);
                    int checksum = new Util().GetChecksum(message, 1, indexOfEndMessage + 1);

                    // check checksum; send NAK if not correct (the two bytes following the end of the message should equal the 2 least significant chars in the checksum in hex)
                    if (!checksum.ToString("X").EndsWith(message[indexOfEndMessage + 1].ToString() + message[indexOfEndMessage + 2].ToString()))
                    {
                        networkStream.WriteByte(Constants.NAK);
                        Logger.Log("Sent NAK because checksum incorrect", LogLevel.Warning);
                    }
                    else
                    {
                        hl7Message += message.Substring(2, indexOfEndMessage - 2);

                        // if we're dealing with an end frame, the message is complete -> parse and store in db
                        if (isEndFrame)
                        {
                            int? testId = null;
                            ORU_R30 hl7 = this.ParseMessage(hl7Message, ref networkStream);

                            if (hl7 != null)
                            {
                                if (hl7.OBR.UniversalServiceIdentifier.Identifier.Value == "MTB-RIF")
                                {
                                    testId = DB.StoreParsedMessage(hl7, hl7Message, tcpClient.Client.RemoteEndPoint.ToString());

                                    this.sendNotificationsDelegate.BeginInvoke(testId, null, null);
                                }
                                else
                                {
                                    Logger.Log(string.Format("Received non-TB result. Assay Test Code: {0}", hl7.OBR.UniversalServiceIdentifier.Identifier.Value), LogLevel.Error);
                                }

                                // add to list of received messages for later HL7 ACK:
                                dataForAck.Add(this.GetDataForHL7Acknowledgement(hl7));
                            }

                            DB.StoreRawMessage(hl7Message, testId);

                            // empty message-var for the next message to be sent
                            hl7Message = string.Empty;

                            // send ACK for this frame
                            networkStream.WriteByte(Constants.ACK);
                            Logger.Log("Sent ACK of endframe", LogLevel.Info);
                        }
                        else
                        {
                            // send ACK for this frame
                            networkStream.WriteByte(Constants.ACK);
                            Logger.Log("Sent ACK of regular frame", LogLevel.Info);
                        }
                    }
                }
                else
                {
                    // send ACK in any other case
                    networkStream.WriteByte(Constants.ACK);
                    Logger.Log(string.Format("Sent ACK for random frame ({0})", message[0]), LogLevel.Info, message);
                }
            }

            tcpClient.Close();
        }

        /// <summary>
        /// Take the HL7 message string and turn it into an HL7 object using NHAPI
        /// </summary>
        /// <param name="hl7Message">Raw HL7 string</param>
        /// <param name="networkStream">Reference to the network stream so we can write to it.</param>
        /// <returns>HL7 Object</returns>
        private ORU_R30 ParseMessage(string hl7Message, ref NetworkStream networkStream)
        {
            // use NHapi to parse message if message complete:
            PipeParser parser = new PipeParser();
            ORU_R30 hl7 = new ORU_R30();

            try
            {
                IMessage im = parser.Parse(hl7Message);
                hl7 = im as ORU_R30;
            }
            catch (Exception e)
            {
                // send NAK for this frame
                networkStream.WriteByte(Constants.EOT);
                Logger.Log("Error converting message: " + e.Message, LogLevel.Error, hl7Message);
                return null;
            }

            try
            {
                // Device may send SPM which isn't officially part of ORU_R30, so we have to add it by hand
                hl7.addNonstandardSegment("SPM");
                parser.Parse((NHapi.Model.V25.Segment.SPM)hl7.GetStructure("SPM"), hl7Message.Substring(hl7Message.IndexOf("SPM"), 40), new EncodingCharacters('|', hl7.MSH.EncodingCharacters.Value));
            }
            catch (Exception e)
            {
                // send NAK for this frame
                networkStream.WriteByte(Constants.EOT);
                Logger.Log("Error parsing specimen part of message: " + e.Message, LogLevel.Error, hl7Message);
            }

            try
            {
                // Parse notes section
                if (hl7Message.Contains("NTE") && hl7Message.Contains("OBX|2") && hl7Message.IndexOf("OBX|2") > hl7Message.IndexOf("NTE"))
                {
                    parser.Parse((NHapi.Model.V25.Segment.NTE)hl7.GetStructure("NTE"), hl7Message.Substring(hl7Message.IndexOf("NTE"), hl7Message.IndexOf("OBX|2") - hl7Message.IndexOf("NTE")), new EncodingCharacters('|', hl7.MSH.EncodingCharacters.Value));
                }
            }
            catch (Exception e)
            {
                // send NAK for this frame
                networkStream.WriteByte(Constants.EOT);
                Logger.Log("Error parsing notes part of message: " + e.Message, LogLevel.Error, hl7Message);
            }

            return hl7;
        }

        /// <summary>
        /// Returns a simple HL7 data item with all the data needed for the later HL7 ACK message
        /// </summary>
        /// <param name="hl7">HL7 object</param>
        /// <returns>HL7 data item</returns>
        private DataForHL7Acknowledgement GetDataForHL7Acknowledgement(ORU_R30 hl7)
        {
            DataForHL7Acknowledgement data = new DataForHL7Acknowledgement();
            data.SendingApplicationNamespaceID = hl7.MSH.SendingApplication.NamespaceID.Value.ToString();
            data.SendingApplicationUniversalID = hl7.MSH.SendingApplication.UniversalID.Value.ToString();
            data.SendingApplicationUniversalIDType = hl7.MSH.SendingApplication.UniversalIDType.Value.ToString();
            data.MessageControlID = hl7.MSH.MessageControlID.Value.ToString();
            return data;
        }

        /// <summary>
        /// Turns a simple HL7 data item into a full blown HL7-ACK message
        /// </summary>
        /// <param name="messageData">Simple HL7 data item</param>
        /// <returns>HL7-ACK message</returns>
        private string ConstructHL7Acknowledgement(DataForHL7Acknowledgement messageData)
        {
            // construct HL7-ACK
            NHapi.Model.V25.Message.ACK ack = new NHapi.Model.V25.Message.ACK();

            // MSH-segment:
            ack.MSH.SendingApplication.NamespaceID.Value = "GxAlert";
            ack.MSH.ReceivingApplication.NamespaceID.Value = messageData.SendingApplicationNamespaceID;
            ack.MSH.ReceivingApplication.UniversalID.Value = messageData.SendingApplicationUniversalID;
            ack.MSH.ReceivingApplication.UniversalIDType.Value = messageData.SendingApplicationUniversalIDType;
            ack.MSH.MessageControlID.Value = messageData.MessageControlID;
            ack.MSH.ProcessingID.ProcessingID.Value = "P";
            ack.MSH.AcceptAcknowledgmentType.Value = "NE";
            ack.MSH.ApplicationAcknowledgmentType.Value = "NE";

            // MSA-segment
            ack.MSA.AcknowledgmentCode.Value = "CA";
            ack.MSA.MessageControlID.Value = messageData.MessageControlID;

            // use parser to convert or HL7-ACK to string for sending
            PipeParser parserResponse = new PipeParser();

            return parserResponse.Encode(ack);
        }
    }
}
