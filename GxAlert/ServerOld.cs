using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Message;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BigPicture
{
    class ServerOld
    {
        //the listener that listens for GX connection at a certain port
        private TcpListener tcpListener;

        //this is the thread for the listener
        private Thread listenThread;

        /// <summary>
        /// Constructor, initializes listener and starts listening-thread
        /// </summary>
        public ServerOld()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        /// <summary>
        /// Listener that listens for new clients
        /// </summary>
        private void ListenForClients()
        {
            this.tcpListener.Start();

            //log that server started
            Logger.Log("Server Started", LogLevel.Info);

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        /// <summary>
        /// This is a thread for the client that has connected
        /// </summary>
        /// <param name="client">Client (GX-machine)</param>
        private void HandleClientComm(object client)
        {
            //TCP client that connected
            TcpClient tcpClient = (TcpClient)client;


            //Get reference to stream for client so we can read/write data
            NetworkStream networkStream = tcpClient.GetStream();

            //log that new client connected
            Logger.Log(string.Format("New client connected: {0}", tcpClient.Client.RemoteEndPoint.ToString()), LogLevel.Info);

            //variable to keep track of the bytes we received in the client stream
            int bytesRead;

            //variable for constructing the message
            string hl7Message = string.Empty;

            //this will store all messages we receive for later ACK:
            List<DataForHL7Acknowledgement> dataForAck = new List<DataForHL7Acknowledgement>();
            bool awaitingHl7Ack = false;

            //keep track of connection state:
            ConnectionState connState = ConnectionState.Neutral;

            while (true)
            {

                bytesRead = 0;
                string message = string.Empty;

                try
                {
                    //blocks until a client sends a message
                    //bytesRead = networkStream.Read(message, 0, 1024 * 1024);
                    if (networkStream.CanRead)
                    {
                        byte[] msg = new byte[1];//TODO: set a higher value here or some other method to prevent reading lots of null-bytes/ Research: how to get perfect buffer size?
                        StringBuilder myCompleteMessage = new StringBuilder();

                        // Incoming message may be larger than the buffer size. 
                        do
                        {
                            bytesRead = networkStream.Read(msg, 0, msg.Length);

                            myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(msg, 0, msg.Length));

                        }
                        while (networkStream.DataAvailable);

                        message = myCompleteMessage.ToString();
                    }
                    else
                    {
                        Console.WriteLine("Sorry.  You cannot read from this NetworkStream.");
                    }
                }
                catch
                {
                    //a socket error has occurred
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();

                //Logger.Log(encoder.GetString(message), LogLevel.Info);

                //if we got an ENQ, respond with ACK:
                if (message[0] == Constants.ENQ)
                {
                    Logger.Log("Sending ACK in response to ENQ...", LogLevel.Info);
                    networkStream.WriteByte(Constants.ACK);
                    networkStream.Flush();

                    connState = ConnectionState.Receiving;
                }
                //if eot of full message, confirm end of receiving and start transmission of HL7 ack
                else if (connState == ConnectionState.Receiving && message[0] == Constants.EOT)
                {
                    //send enq to go from neutral state to transmission:
                    Logger.Log("Sending ENQ...", LogLevel.Info);
                    networkStream.WriteByte(Constants.ENQ);
                    networkStream.Flush();

                    connState = ConnectionState.Sending;
                }
                //did we get an eot in response to our hl7-eot?
                else if (connState == ConnectionState.Sending && message[0] == Constants.EOT)
                {
                    //do nothing (we're back in neutral state)
                    Logger.Log("Received EOT from GX", LogLevel.Info);

                    //set vars back to neutral
                    connState = ConnectionState.Neutral;
                }
                //did we receive an ack in response to our enq and do we have messages to send acks for?
                else if (connState == ConnectionState.Sending && message[0] == Constants.ACK)
                {
                    //GX acknowledged last message -> remove from list:
                    if (dataForAck.Any() && awaitingHl7Ack)
                    {
                        var data = dataForAck.First();
                        dataForAck.Remove(data);
                    }

                    if (!dataForAck.Any())
                    {
                        //send eot
                        Logger.Log("Sending EOT in response to ACK for HL7-ACK...", LogLevel.Info);
                        networkStream.WriteByte(Constants.EOT);
                        networkStream.Flush();
                        awaitingHl7Ack = false;
                        connState = ConnectionState.Neutral;
                    }
                    else
                    {
                        //get first data in list to acknowledge
                        var data = dataForAck.First();
                        awaitingHl7Ack = true;


                        PipeParser parser = new PipeParser();



                        //also need to send ack of complete message:
                        Logger.Log("Sending ack of entire message", LogLevel.Info);

                        NHapi.Model.V25.Message.ACK ack = new NHapi.Model.V25.Message.ACK();

                        //MSH-part of message acknowledgement:
                        ack.MSH.SendingApplication.NamespaceID.Value = "LIS";//TODO: set to "bigpicture"
                        ack.MSH.ReceivingApplication.NamespaceID.Value = data.SendingApplicationNamespaceID;
                        ack.MSH.ReceivingApplication.UniversalID.Value = data.SendingApplicationUniversalID;
                        ack.MSH.ReceivingApplication.UniversalIDType.Value = data.SendingApplicationUniversalIDType;
                        //ack.MSH.DateTimeOfMessage.Time.Set(DateTime.Now, "yyyyMMddHHmmss");
                        string guid = Guid.NewGuid().ToString();
                        ack.MSH.MessageControlID.Value = data.MessageControlID;
                        ack.MSH.ProcessingID.ProcessingID.Value = "P";
                        ack.MSH.AcceptAcknowledgmentType.Value = "NE";
                        ack.MSH.ApplicationAcknowledgmentType.Value = "NE";

                        //MSA-part
                        ack.MSA.AcknowledgmentCode.Value = "CA";
                        ack.MSA.MessageControlID.Value = data.MessageControlID;
                        //ack.MSA.TextMessage.Value = "Test";







                        PipeParser parserResponse = new PipeParser();

                        string hl7Response = parserResponse.Encode(ack);


                        //wrapping message in hl7 llp start and end chars?
                        //hl7Response = encoder.GetString(new byte[] { PIPE }) + hl7Response + encoder.GetString(new byte[] { SEPARATOR }) + encoder.GetString(new byte[] { CR });

                        //get checksum for sending:
                        byte[] hl7ResponseForChecksum = encoder.GetBytes("1" + hl7Response + encoder.GetString(new byte[1] { Constants.ETX }));
                        int checksum = 0;
                        for (int i = 0; i < hl7ResponseForChecksum.Length; i++)
                        {
                            checksum += hl7ResponseForChecksum[i];
                        }

                        //convert checksum to hex so we can get eight least significant bits:
                        string checksumHex = checksum.ToString("X");


                        //wrapping message in astm control chars
                        // "0" = Frame Number
                        hl7Response = encoder.GetString(new byte[1] { Constants.STX }) + "1" + hl7Response + encoder.GetString(new byte[1] { Constants.ETX }) + checksumHex.Substring(checksumHex.Length - 2, 1) + checksumHex.Substring(checksumHex.Length - 1, 1) + encoder.GetString(new byte[1] { Constants.CR }) + encoder.GetString(new byte[1] { Constants.LF });

                        //hl7Response = encoder.GetString(hl7Response );


                        byte[] response = encoder.GetBytes(hl7Response);

                        networkStream.Write(response, 0, response.Length);
                        networkStream.Flush();
                    }

                }
                else if (connState == ConnectionState.Receiving && message[0] == Constants.STX && (message.Any(m => m == Constants.ETX) || message.Any(m => m == Constants.ETB))) //we received a message frame
                {
                    //TODO: verify that this is the frame number we expected, return nak if not
                    int frameNumber = Convert.ToInt32(message[1]) - 48;
                    bool isEndFrame = false;

                    Logger.Log(string.Format("Received {0}frame (Nr. {1})", isEndFrame ? "end" : "", frameNumber), LogLevel.Info);

                    //get text of message. It's the 2nd byte up until the ETB or ETX control character:
                    if (message.Any(m => m == Constants.ETX))
                        isEndFrame = true;

                    int indexOfEndMessage = message.IndexOf(isEndFrame ? Encoding.ASCII.GetString(new byte[] { Constants.ETX }) : Encoding.ASCII.GetString(new byte[] { Constants.ETB }));// Array.IndexOf(message, isEndFrame ? Constants.ETX : Constants.ETB);

                    //string encodedMessage = encoder.GetString(message, 2, indexOfEndMessage);




                    //TODO: verify checksum, send NAK if not correct
                    int cs1 = message[indexOfEndMessage + 1];
                    int cs2 = message[indexOfEndMessage + 2];

                    int checksum = 0;
                    for (int i = 1; i < indexOfEndMessage + 1; i++)
                    {
                        checksum += message[i];
                    }

                    //check checksum; send nak if not correct:
                    if (!checksum.ToString("X").EndsWith(message[indexOfEndMessage + 1].ToString() + message[indexOfEndMessage + 2].ToString()))
                    {
                        //send NAK for this frame
                        networkStream.WriteByte(Constants.NAK);
                        networkStream.Flush();
                        Logger.Log("Sent NAK because checksum incorrect", LogLevel.Warning);
                    }
                    else
                    {
                        hl7Message += message.Substring(2, indexOfEndMessage - 2);

                        //use nhapi to parse message if message complete:
                        if (isEndFrame)
                        {
                            PipeParser parser = new PipeParser();
                            ORU_R30 hl7 = new ORU_R30();

                            try
                            {
                                IMessage im = parser.Parse(hl7Message);
                                hl7 = im as ORU_R30;
                            }
                            catch (Exception e)
                            {
                                //send NAK for this frame
                                networkStream.WriteByte(Constants.EOT);
                                networkStream.Flush();
                                Logger.Log("Error converting message: " + e.Message, LogLevel.Error, hl7Message);
                                return;
                            }

                            try
                            {
                                //genexpert sends spm which isn't officially part of ORU_R30:
                                hl7.addNonstandardSegment("SPM");

                                //TODO: validate
                                parser.Parse((NHapi.Model.V25.Segment.SPM)hl7.GetStructure("SPM"), hl7Message.Substring(hl7Message.IndexOf("SPM"), 40), new EncodingCharacters('|', hl7.MSH.EncodingCharacters.Value));
                            }
                            catch (Exception e)
                            {
                                //send NAK for this frame
                                networkStream.WriteByte(Constants.EOT);
                                networkStream.Flush();
                                Logger.Log("Error parsing specimen part of message: " + e.Message, LogLevel.Error, hl7Message);
                                return;
                            }

                            //get all values that are of interest to us:
                            if (hl7 != null)
                            {
                                //make sure it's a Tb-result:
                                if (hl7.OBR.UniversalServiceIdentifier.Identifier.Value == "MTB-RIF")
                                {
                                    //log message: 
                                    //Logger.Log(string.Format("HL7 Message: {0}", hl7Message), LogLevel.Info);

                                    //try to store in db:
                                    try
                                    {
                                        //store in database
                                        using (BigPictureEntities bpe = new BigPictureEntities())
                                        {
                                            //first, see if we know this machine already and create it if we don't:
                                            string instrumentSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(4)).EntityIdentifier.Value;
                                            device device = bpe.devices.FirstOrDefault(d => d.Serial == instrumentSerial);

                                            //create device if we don't have it yet
                                            if (device == null)
                                            {
                                                device = new device();
                                                device.Serial = instrumentSerial;
                                                device.InsertedBy = device.UpdatedBy = "BigPicture Listener";
                                                device.InsertedOn = device.InsertedOn = DateTime.Now;
                                                bpe.devices.Add(device);
                                            }

                                            //Add test to database if it doesn't exist already (=re-upload)
                                            string cartridgeSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(2)).EntityIdentifier.Value;
                                            test test = bpe.tests.FirstOrDefault(t => t.CartridgeSerial == cartridgeSerial);

                                            //create test if not already exists
                                            if (test == null)
                                            {
                                                test = new test();
                                                test.InsertedOn = DateTime.Now;
                                                test.InsertedBy = "BigPicture Listener";
                                                test.CartridgeSerial = cartridgeSerial;
                                                bpe.tests.Add(test);
                                            }

                                            //fill test with new data we got:
                                            test.AssayHostTestCode = hl7.OBR.UniversalServiceIdentifier.Identifier.Value;
                                            test.AssayName = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(1).Data.ToString();
                                            test.AssayVersion = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(2).Data.ToString();
                                            test.CartridgeExpirationDate = DateTime.ParseExact(((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(0)).EntityIdentifier.Value.ToString(), "yyyyMMdd", CultureInfo.CurrentCulture);
                                            test.ComputerName = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(5)).EntityIdentifier.Value.ToString();
                                            test.SenderUser= ((NHapi.Model.V25.Datatype.XCN)hl7.GetOBSERVATION(0).OBX.GetField(16)[0]).FamilyName.Surname.ToString();
                                            test.SenderVersion = hl7.MSH.SendingApplication.Components[2].ToString();
                                            test.deployment = device.deployment;
                                            test.MessageSentOn = hl7.MSH.DateTimeOfMessage.Time.GetAsDate();
                                            test.ModuleSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(3)).EntityIdentifier.Value.ToString();
                                            //test.Notes = ;
                                            //test.PatientId = ;
                                            test.ReagentLotId = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(1)).EntityIdentifier.Value.ToString();
                                            test.ResultText = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString() + "|" + ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString() + "|";
                                            test.SampleId = ((NHapi.Model.V25.Datatype.EI)(((NHapi.Model.V25.Datatype.EIP)((NHapi.Model.V25.Segment.SPM)hl7.GetStructure("SPM")).SpecimenID)[0])).EntityIdentifier.Value;
                                            test.SystemName = hl7.MSH.SendingApplication.Components[0].ToString();
                                            //test.TestEndedOn = ;
                                            test.TestStartedOn = hl7.ORC.DateTimeOfTransaction.Time.GetAsDate();
                                            test.UpdatedBy = "BigPicture Listener";
                                            test.UpdatedOn = DateTime.Now;

                                            //normalize test results. TODO: handle if not exits yet
                                            //TB-result:
                                            //TODO: Throw godo error/log when not present in db:
                                            string resultTestCodeTb = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(0).Data.ToString().Replace("4", "");
                                            int resultTestCodeIdTb = bpe.resulttestcodes.First(r => r.ResultTestCode1 == resultTestCodeTb).ResultTestCodeId;
                                            testresult testResultTb = new testresult();
                                            testResultTb.InsertedBy = testResultTb.UpdatedBy = "BigPicture Listener";
                                            testResultTb.InsertedOn = testResultTb.UpdatedOn = DateTime.Now;
                                            testResultTb.Result = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data == null || ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString() == null ? "" : ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString();
                                            testResultTb.TestId = test.TestId;
                                            testResultTb.ResultTestCodeId = resultTestCodeIdTb;
                                            bpe.testresults.Add(testResultTb);

                                            //Rif-resistance-result:
                                            //TODO: Throw godo error/log when not present in db:
                                            string resultTestCodeRif = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(19).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(0).Data.ToString();
                                            int resultTestCodeIdRif = bpe.resulttestcodes.First(r => r.ResultTestCode1 == resultTestCodeRif).ResultTestCodeId;
                                            testresult testResultRif = new testresult();
                                            testResultRif.InsertedBy = testResultRif.UpdatedBy = "BigPicture Listener";
                                            testResultRif.InsertedOn = testResultRif.UpdatedOn = DateTime.Now;
                                            testResultRif.Result = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data == null || ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString() == null ? "" : ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString();
                                            testResultRif.TestId = test.TestId;
                                            testResultRif.ResultTestCodeId = resultTestCodeIdRif;
                                            bpe.testresults.Add(testResultRif);

                                            //finally, save everything to db:
                                            bpe.SaveChanges();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Log("Store in DB failed: " + e.Message, LogLevel.Error, hl7Message);
                                    }
                                }
                            }


                            //add to list of received messages for later HL7 ACK:
                            DataForHL7Acknowledgement data = new DataForHL7Acknowledgement();
                            data.SendingApplicationNamespaceID = hl7.MSH.SendingApplication.NamespaceID.Value.ToString();
                            data.SendingApplicationUniversalID = hl7.MSH.SendingApplication.UniversalID.Value.ToString();
                            data.SendingApplicationUniversalIDType = hl7.MSH.SendingApplication.UniversalIDType.Value.ToString();
                            data.MessageControlID = hl7.MSH.MessageControlID.Value.ToString();
                            dataForAck.Add(data);

                            //empty message-var for the next message to be sent
                            hl7Message = string.Empty;

                            //for some reason, our system is too quick for GX. need a delay here (really!)
                            //TODO: Double-check that this is really necessary
                            //Thread.Sleep(1000);

                            //send ACK for this frame
                            networkStream.WriteByte(Constants.ACK);
                            networkStream.Flush();
                            Logger.Log("Sent ACK of endframe", LogLevel.Info);
                        }
                        else
                        {
                            //send ACK for this frame
                            networkStream.WriteByte(Constants.ACK);
                            networkStream.Flush();
                            Logger.Log("Sent ACK of regular frame", LogLevel.Info);
                        }
                    }
                }
                else
                {
                    //send ACK in any other case
                    networkStream.WriteByte(Constants.ACK);
                    networkStream.Flush();
                    Logger.Log("Sent ACK for random frame", LogLevel.Info);
                }
            }
            tcpClient.Close();
        }
    }
}
