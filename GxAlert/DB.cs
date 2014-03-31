namespace GxAlert
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using NHapi.Model.V25.Message;

    /// <summary>
    /// Class for storing things in the database
    /// </summary>
    public class DB
    {
        /// <summary>
        /// Store the raw, unparsed HL7 message we got over the wire in the DB
        /// </summary>
        /// <param name="hl7Message">HL7 Message</param>
        /// <param name="testId">ID of the test (null if save of parsed message was unsuccessful)</param>
        internal static void StoreRawMessage(string hl7Message, int? testId)
        {
            using (GxAlertEntities bpe = new GxAlertEntities())
            {
                rawmessage rawMessage = new rawmessage();
                rawMessage.TestId = testId;
                rawMessage.Message = hl7Message;
                bpe.rawmessages.Add(rawMessage);
                bpe.SaveChanges();
            }
        }

        /// <summary>
        /// Store the parsed HL7 object in the DB
        /// </summary>
        /// <param name="hl7">The HL7 object</param>
        /// <param name="hl7Message">The raw HL7 message</param>
        /// <param name="senderIp">IP that message was sent from.</param>
        /// <returns>ID of test that was inserted into DB. Null if insert unsuccessful</returns>
        internal static int? StoreParsedMessage(ORU_R30 hl7, string hl7Message, string senderIp)
        {
            // try to store in db:
            try
            {
                // testId we'll return:
                int? testId = null;

                // store in database
                using (GxAlertEntities bpe = new GxAlertEntities())
                {
                    string deviceSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(5)).EntityIdentifier.Value;
                    string hostId = hl7.MSH.ReceivingApplication.NamespaceID.Value.ToString();

                    // if we don't have a deployment for this device and hostId yet, create one:
                    deployment deployment = bpe.deployments.FirstOrDefault(l => l.HostId == hostId && l.devicedeploymenthistories.Any(h => h.device.Serial == deviceSerial));
                    if (deployment == null)
                    {
                        deployment = new deployment();
                        deployment.HostId = hostId;
                        deployment.Approved = false;
                        deployment.Insertedby = deployment.Updatedby = ConfigurationManager.AppSettings["appName"];
                        deployment.InsertedOn = deployment.UpdatedOn = DateTime.Now;
                        bpe.deployments.Add(deployment);

                        // notify admins of new machine:
                        new Notifications().SendNewDeploymentNotification(hostId, deviceSerial, senderIp);
                    }

                    // create device if we don't have it yet
                    device device = bpe.devices.FirstOrDefault(d => d.Serial == deviceSerial);
                    if (device == null)
                    {
                        //get device type id for GeneXpert
                        int deviceTypeId = bpe.devicetypes.First(d => d.Name.Contains("GeneXpert")).DeviceTypeId;

                        device = new device();
                        device.DeviceTypeId = deviceTypeId;
                        device.Serial = deviceSerial;
                        device.InsertedBy = device.UpdatedBy = ConfigurationManager.AppSettings["appName"];
                        device.InsertedOn = device.UpdatedOn = DateTime.Now;
                        bpe.devices.Add(device);
                    }

                    // keep device deployment history:
                    if (device.deployment != deployment)
                    {
                        device.deployment = deployment;

                        devicedeploymenthistory history = new devicedeploymenthistory();
                        history.device = device;
                        history.deployment = deployment;
                        history.InsertedBy = ConfigurationManager.AppSettings["appName"];
                        history.InsertedOn = DateTime.Now;
                        bpe.devicedeploymenthistories.Add(history);
                    }

                    // Add test to database if it doesn't exist already (=re-upload)
                    string cartridgeSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(2)).EntityIdentifier.Value;
                    test test = bpe.tests.FirstOrDefault(t => t.CartridgeSerial == cartridgeSerial);

                    // create test if not already exists
                    if (test == null)
                    {
                        test = new test();
                        test.TestTypeId = 4;//GeneXpert TB/RIF Result
                        test.InsertedOn = DateTime.Now;
                        test.InsertedBy = ConfigurationManager.AppSettings["appName"];
                        test.CartridgeSerial = cartridgeSerial;
                        bpe.tests.Add(test);
                    }

                    // fill test with new data we got:
                    test.AssayHostTestCode = hl7.OBR.UniversalServiceIdentifier.Identifier.Value;
                    test.AssayName = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(1).Data.ToString();
                    test.AssayVersion = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(2).Data.ToString();
                    test.CartridgeExpirationDate = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(0)).EntityIdentifier.Value != null ? DateTime.ParseExact(((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(0)).EntityIdentifier.Value.ToString(), "yyyyMMdd", CultureInfo.CurrentCulture) : new DateTime?();
                    test.ComputerName = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(5)).EntityIdentifier.Value.ToString();
                    test.SenderUser = ((NHapi.Model.V25.Datatype.XCN)hl7.GetOBSERVATION(0).OBX.GetField(16)[0]).FamilyName.Surname.ToString();
                    test.SenderVersion = hl7.MSH.SendingApplication.Components[2].ToString();
                    test.SenderIp = senderIp;
                    test.deployment = device.deployment;
                    test.MessageSentOn = hl7.MSH.DateTimeOfMessage.Time.GetAsDate();
                    test.ModuleSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(3)).EntityIdentifier.Value.ToString();
                    test.InstrumentSerial = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(4)).EntityIdentifier.Value;

                    var notes1 = ((NHapi.Model.V25.Datatype.FT)hl7.GetNTE(0).GetComment(0)).ExtraComponents.getComponent(1).Data;
                    var notes2 = ((NHapi.Model.V25.Datatype.FT)hl7.GetNTE(0).GetComment(0)).ExtraComponents.getComponent(2).Data;
                    test.Notes = ((notes1 != null ? notes1.ToString() + " " : string.Empty) + (notes2 != null ? notes2.ToString() : string.Empty)).Trim();
                    test.Notes = string.IsNullOrWhiteSpace(test.Notes) ? null : test.Notes;

                    test.PatientId = hl7.PID.GetPatientIdentifierList(0).IDNumber.Value;
                    test.ReagentLotId = ((NHapi.Model.V25.Datatype.EI)hl7.GetOBSERVATION(0).OBX.GetField(18).GetValue(1)).EntityIdentifier.Value.ToString();
                    test.ResultText = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString() + "|" + ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString() + "|";
                    test.SampleId = ((NHapi.Model.V25.Datatype.EI)((NHapi.Model.V25.Datatype.EIP)((NHapi.Model.V25.Segment.SPM)hl7.GetStructure("SPM")).SpecimenID)[0]).EntityIdentifier.Value;
                    test.SystemName = hl7.MSH.SendingApplication.Components[0].ToString();
                    test.TestStartedOn = hl7.GetTIMING_QTY(0).TQ1.StartDateTime.Time.GetAsDate();

                    // for some reason, we got a handful of results (we have over 10000 now) that have an invalid end date. 
                    // we'll just use the start date for those
                    try
                    {
                        test.TestEndedOn = hl7.GetTIMING_QTY(0).TQ1.EndDateTime.Time.GetAsDate();
                    }
                    catch
                    {
                        test.TestEndedOn = test.TestStartedOn;
                    }


                    test.UpdatedBy = ConfigurationManager.AppSettings["appName"];
                    test.UpdatedOn = DateTime.Now;

                    // normalize test results.
                    // TB-result:
                    string resultTestCodeTb = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(0).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(0).Data.ToString();
                    int resultTestCodeIdTb = bpe.resulttestcodes.First(r => r.ResultTestCode1 == resultTestCodeTb).ResultTestCodeId;
                    testresult resultTb = bpe.testresults.FirstOrDefault(t => t.TestId == test.TestId && t.ResultTestCodeId == resultTestCodeIdTb);

                    if (resultTb == null)
                    {
                        resultTb = new testresult();
                        resultTb.InsertedBy = ConfigurationManager.AppSettings["appName"];
                        resultTb.InsertedOn = DateTime.Now;
                        resultTb.TestId = test.TestId;
                        resultTb.ResultTestCodeId = resultTestCodeIdTb;
                        bpe.testresults.Add(resultTb);
                    }

                    resultTb.Result = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data == null || ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString() == null ? string.Empty : ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(0).OBX.GetField(5).GetValue(0)).Data.ToString();
                    resultTb.UpdatedBy = ConfigurationManager.AppSettings["appName"];
                    resultTb.UpdatedOn = DateTime.Now;

                    // Rif-resistance-result:
                    string resultTestCodeRif = ((NHapi.Model.V25.Datatype.CE)hl7.GetOBSERVATION(19).OBX.GetField(3).GetValue(0)).Identifier.ExtraComponents.getComponent(0).Data.ToString();
                    int resultTestCodeIdRif = bpe.resulttestcodes.First(r => r.ResultTestCode1 == resultTestCodeRif).ResultTestCodeId;
                    testresult resultRif = bpe.testresults.FirstOrDefault(t => t.TestId == test.TestId && t.ResultTestCodeId == resultTestCodeIdRif);

                    if (resultRif == null)
                    {
                        resultRif = new testresult();
                        resultRif.InsertedBy = ConfigurationManager.AppSettings["appName"];
                        resultRif.InsertedOn = DateTime.Now;
                        resultRif.TestId = test.TestId;
                        resultRif.ResultTestCodeId = resultTestCodeIdRif;
                        bpe.testresults.Add(resultRif);
                    }

                    resultRif.Result = ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data == null || ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString() == null ? string.Empty : ((NHapi.Base.Model.Varies)hl7.GetOBSERVATION(19).OBX.GetField(5).GetValue(0)).Data.ToString();
                    resultRif.UpdatedBy = ConfigurationManager.AppSettings["appName"];
                    resultRif.UpdatedOn = DateTime.Now;

                    // finally, save everything to db:
                    bpe.SaveChanges();

                    testId = test.TestId;
                }

                return testId;
            }
            catch (Exception e)
            {
                Logger.Log("Store in DB failed: " + e.Message, LogLevel.Error, hl7Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all notifications that match the given test from the database
        /// </summary>
        /// <param name="testId">ID of the test</param>
        /// <returns>A list of all matching notifications</returns>
        internal static List<PersonNotification> GetNotificationsByTest(int? testId)
        {
            using (GxAlertEntities bpe = new GxAlertEntities())
            {
                return (from n in bpe.notifications
                        join np in bpe.notificationpersons on n.NotificationId equals np.NotificationId
                        join p in bpe.people on np.PersonId equals p.PersonId
                        join nr in bpe.notificationresults on n.NotificationId equals nr.NotificationId
                        join t in bpe.tests on testId equals t.TestId
                        where (t.deployment.Approved || n.IncludeUnapprovedDeployments)
                        && t.testresults.Any(r => r.ResultTestCodeId == nr.ResultTestCodeId && r.Result == nr.Result) // alert for a given result
                        && n.notificationdeployments.Any(c => c.DeploymentId == t.DeploymentId) // was test taken in one of the deployments that match the notification?
                        select new PersonNotification()
                        {
                            PersonId = p.PersonId,
                            Name = p.Name,
                            PersonEmail = p.Email,
                            PersonCell = p.Cell,
                            PersonPhone = p.Phone,
                            PersonCulture = p.country.Culture,
                            NotificationId = n.NotificationId,
                            NotificationName = n.NotificationName,
                            EmailSubject = n.EmailSubject,
                            EmailBody = n.EmailBody,
                            SmsBody = n.SmsBody,
                            PhoneBody = n.PhoneBody,
                            Sms = np.Sms,
                            Phone = np.Phone,
                            Email = np.Email,
                            ResultText = t.ResultText,
                            MessageSentOn = t.MessageSentOn,
                            DeploymentHostId = t.deployment.HostId,
                            DeploymentDescription = t.deployment.Description,
                            DeploymentCountry = t.deployment.country.Name,
                            TestEndedOn = t.TestEndedOn
                        }).Distinct().ToList();
            }
        }

        /// <summary>
        /// Inserts a new entry in the notification log
        /// </summary>
        /// <param name="n">Person Notification object that the notification is based on</param>
        /// <param name="body">Body text of the notification</param>
        /// <param name="email">Was the notification sent as an email?</param>
        /// <param name="sms">Was the notification sent as an SMS?</param>
        /// <param name="phone">Was the notification sent as a phone call?</param>
        internal static void InsertNotificationLog(PersonNotification n, string body, bool email, bool sms, bool phone)
        {
            InsertNotificationLog(n, null, body, email, sms, phone);
        }

        /// <summary>
        /// Inserts a new entry in the notification log
        /// </summary>
        /// <param name="n">Person Notification object that the notification is based on</param>
        /// <param name="subject">Subject of the notification (for emails)</param>
        /// <param name="body">Body text of the notification</param>
        /// <param name="email">Was the notification sent as an email?</param>
        /// <param name="sms">Was the notification sent as an SMS?</param>
        /// <param name="phone">Was the notification sent as a phone call?</param>
        internal static void InsertNotificationLog(PersonNotification n, string subject, string body, bool email, bool sms, bool phone)
        {
            using (GxAlertEntities bpe = new GxAlertEntities())
            {
                notificationlog nl = new notificationlog();
                nl.Body = body;
                nl.Email = email;
                nl.NotificationId = n.NotificationId;
                nl.NotificationName = n.NotificationName;
                nl.PersonId = n.PersonId;
                nl.PersonName = n.Name;
                nl.Phone = phone;
                nl.SentBy = ConfigurationManager.AppSettings["appName"];
                nl.SentOn = DateTime.Now;
                nl.Sms = sms;
                nl.Subject = subject;
                bpe.notificationlogs.Add(nl);
                bpe.SaveChanges();
            }
        }

        internal static List<rawmessage> GetRawMessagesWithoutTestId()
        {
            using (GxAlertEntities bpe = new GxAlertEntities())
            {
                return bpe.rawmessages.Where(r => r.TestId == null && r.RawMessageId == 20446).ToList();
            }
        }

        internal static void UpdateRawMessageTestId(int rawMessageId, int testId)
        {
            using (GxAlertEntities bpe = new GxAlertEntities())
            {
                var rawMessage = bpe.rawmessages.First(r => r.RawMessageId == rawMessageId);
                rawMessage.TestId = testId;
                bpe.SaveChanges();
            }
        }
    }
}
