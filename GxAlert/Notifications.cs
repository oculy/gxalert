namespace GxAlert
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Net;
    using System.Net.Mail;
    using Twilio;

    /// <summary>
    /// This class deals with all the notifications that the system
    /// sends out based on incoming tests or other events such as errors,
    /// or a new deployment submitting data
    /// </summary>
    public class Notifications
    {
        // these delegates will let us send sms, phone, email asynchronously
        private SendEmailDelegate sendEmailDelegate;
        private SendSmsDelegate sendSmsDelegate;
        private InitiateCallDelegate initiateCallDelegate;
        private SendToMshDelegate sendToMshDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="Notifications"/> class.
        /// </summary>
        public Notifications()
        {
            this.sendEmailDelegate = new SendEmailDelegate(this.SendEmail);
            this.sendSmsDelegate = new SendSmsDelegate(this.SendSms);
            this.initiateCallDelegate = new InitiateCallDelegate(this.InitiateCall);
            this.sendToMshDelegate = new SendToMshDelegate(this.SendToMsh);
        }

        private delegate void InitiateCallDelegate(PersonNotification n);

        private delegate void SendSmsDelegate(PersonNotification n);

        private delegate void SendEmailDelegate(PersonNotification n);

        private delegate void SendToMshDelegate(int? testId);

        /// <summary>
        /// This function takes a test ID and figures out who to notify from there.
        /// </summary>
        /// <param name="testId">The test for which to send notifications</param>
        public void SendNotifications(int? testId)
        {
            if (!testId.HasValue)
            {
                Logger.Log("Test ID null - no notifications sent", LogLevel.Error);
                return;
            }

            this.sendToMshDelegate(testId);

            // get all notifications that match the test:
            var notifications = DB.GetNotificationsByTest(testId);

            // fire off notifications:
            foreach (var n in notifications)
            {
                // send sms only if:
                // - notification is a text notification
                // - person has a cell phone number
                // - there is content in the message body
                // - the test isn't older than 30 days
                if (n.Sms && !string.IsNullOrWhiteSpace(n.PersonCell) && !string.IsNullOrWhiteSpace(n.SmsBody) && n.TestEndedOn > DateTime.Now.AddDays(-30))
                {
                    this.sendSmsDelegate.BeginInvoke(n, null, null);
                }

                /* no phone for now
                if (n.Phone && !string.IsNullOrWhiteSpace(n.PersonPhone) && !string.IsNullOrWhiteSpace(n.PhoneBody))
                {
                    this.initiateCallDelegate.BeginInvoke(n, null, null);
                }*/

                if (n.Email && !string.IsNullOrWhiteSpace(n.PersonEmail) && !string.IsNullOrWhiteSpace(n.EmailSubject))
                {
                    this.sendEmailDelegate.BeginInvoke(n, null, null);
                }
            }

            return;
        }

        /// <summary>
        /// Notify MSH via API
        /// </summary>
        /// <param name="testId">The ID of the test to send to MSH</param>
        private void SendToMsh(int? testId)
        {
            // don't send to msh any more
            return;

            try
            {
                new MshApi().Send(testId);
            }
            catch (Exception e)
            {
                Logger.Log("Calling eTB Manager API error:" + e.Message, LogLevel.Error);
            }

            return;
        }

        /// <summary>
        /// Send a notification email to someone
        /// </summary>
        /// <param name="n">The notification details from the database</param>
        private void SendEmail(PersonNotification n)
        {
            try
            {
                // send email
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

                message.To.Add(new MailAddress(n.PersonEmail, n.Name));
                message.From = new MailAddress("noreply@email.gxalert.com", "GxAlert");

                message.Subject = this.FillPlaceholders(n.EmailSubject, n, false);
                message.Body = this.FillPlaceholders(n.EmailBody, n, false);

                SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["smtpServer"], 587);
                smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUser"], ConfigurationManager.AppSettings["smtpPassword"]);
                smtp.Send(message);

                // store in log
                DB.InsertNotificationLog(n, message.Subject, message.Body, true, false, false);
            }
            catch (Exception e)
            {
                Logger.Log("Sending email to \"" + n.PersonEmail + "\" error:" + e.Message, LogLevel.Error);
            }

            return;
        }

        /// <summary>
        /// Send SMS to a person
        /// </summary>
        /// <param name="n">The notification details from the database</param>
        private void SendSms(PersonNotification n)
        {
            try
            {
                // send sms
                string sms = this.FillPlaceholders(n.SmsBody, n, true);
                var twilio = new TwilioRestClient(ConfigurationManager.AppSettings["twilioAccountSid"], ConfigurationManager.AppSettings["twilioAuthToken"]);
                var msg = twilio.SendSmsMessage(ConfigurationManager.AppSettings["twilioFromNumber"], n.PersonCell, sms);

                // store in log
                DB.InsertNotificationLog(n, sms, false, true, false);
            }
            catch (Exception e)
            {
                Logger.Log("Sending SMS to \"" + n.PersonCell + "\" error:" + e.Message, LogLevel.Error);
            }

            return;
        }

        /// <summary>
        /// Call a person according to the passed notification
        /// </summary>
        /// <param name="n">The notification details from the database</param>
        private void InitiateCall(PersonNotification n)
        {
            try
            {
                // call phone
                string phone = this.FillPlaceholders(n.PhoneBody, n, false);
                var twilio = new TwilioRestClient(ConfigurationManager.AppSettings["twilioAccountSid"], ConfigurationManager.AppSettings["twilioAuthToken"]);
                var call = twilio.InitiateOutboundCall(ConfigurationManager.AppSettings["twilioFromNumber"], n.PersonPhone, "http://twimlets.com/echo?Twiml=%3CResponse%3E%3CSay%3ENew+MDR-TB+case+in+Nigeria%21%3C%2FSay%3E%3C%2FResponse%3E");

                // store in log
                DB.InsertNotificationLog(n, phone, false, false, true);
            }
            catch (Exception e)
            {
                Logger.Log("Calling \"" + n.PersonPhone + "\" error:" + e.Message, LogLevel.Error);
            }

            return;
        }

        /// <summary>
        /// Fills the placeholders in the given string with details from a test
        /// </summary>
        /// <param name="message">Message with placeholders to replace</param>
        /// <param name="n">Person Notification object that we use to fill placeholders</param>
        /// <param name="isSms">Indicate whether this is an SMS (results in slightly different formatting)</param>
        /// <returns>Message with all placeholders replaced</returns>
        private string FillPlaceholders(string message, PersonNotification n, bool isSms)
        {
            CultureInfo ci = new CultureInfo(n.PersonCulture);

            message = message.Replace("[DeploymentDescription]", n.DeploymentDescription ?? n.DeploymentHostId)
                            .Replace("[DeploymentCountry]", n.DeploymentCountry)
                            .Replace("[MessageSentOn]", n.MessageSentOn.ToString("d", ci) + " " + n.MessageSentOn.ToString("t", ci))
                            .Replace("[TestEndedOn]", n.TestEndedOn.ToString("d", ci) + " " + n.TestEndedOn.ToString("t", ci))
                            .Replace("[DeploymentHostId]", n.DeploymentHostId);

            if (!isSms)
            {
                message = message.Replace("[Results]", n.ResultText.Replace("|", ", ").Trim(new char[] { ' ', ',' }));
            }
            else
            {
                message = message.Replace("[Results]", n.ResultText.Replace("|", System.Environment.NewLine).Trim(new char[] { ' ', '\\', 'n' }));
            }

            return message;
        }

        /// <summary>
        /// Sends a notification about a new deployment to the people defined in the configuration settings
        /// </summary>
        /// <param name="hostId">The hostId of the device</param>
        /// <param name="deviceSerial">The serial number of the device</param>
        /// <param name="senderIp">IP that the device is sending from</param>
        public void SendNewDeploymentNotification(string hostId, string deviceSerial, string senderIp)
        {
            try
            {
                // send email
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

                string toAddresses = ConfigurationManager.AppSettings["newDeploymentTo"];

                foreach (var to in toAddresses.Split(','))
                {
                    var address = to.Replace(",", string.Empty).Trim();

                    if (!string.IsNullOrWhiteSpace(address))
                        message.To.Add(new MailAddress(address));
                }

                message.From = new MailAddress("noreply@email.gxalert.com", "GxAlert");

                message.Subject = "A new GeneXpert deployment submitted data";
                message.Body = "Details of the deployment: \n\n";
                message.Body += "\nHostID: " + hostId;
                message.Body += "\nSerial: " + deviceSerial;
                message.Body += "\nIP Address: " + senderIp;
                message.Body += "\n\n\n";

                SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["smtpServer"], 587);
                smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUser"], ConfigurationManager.AppSettings["smtpPassword"]);
                smtp.Send(message);
            }
            catch (Exception e)
            {
                Logger.Log("Sending notification about new deployment to \"" + ConfigurationManager.AppSettings["newDeploymentTo"] + "\" error:" + e.Message, LogLevel.Error);
            }

            return;
        }

        /// <summary>
        /// Sends an email with error details to the admin
        /// </summary>
        /// <param name="logMessage">The error message to send in the body of the email</param>
        public static void SendErrorEmail(string logMessage)
        {
            // don't send email on socket error:
            if (logMessage.Contains("A socket error has occurred"))
                return;

            // send email
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            message.To.Add(ConfigurationManager.AppSettings["errorTo"]);
            message.From = new MailAddress("noreply@email.gxalert.com", "GxAlert");
            message.Subject = "An error has occurred in " + ConfigurationManager.AppSettings["appName"];
            message.Body = logMessage;

            SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["smtpServer"], 587);
            smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUser"], ConfigurationManager.AppSettings["smtpPassword"]);
            smtp.Send(message);
        }
    }
}
