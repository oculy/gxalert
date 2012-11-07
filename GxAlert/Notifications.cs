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

        /// <summary>
        /// Initializes a new instance of the <see cref="Notifications"/> class.
        /// </summary>
        public Notifications()
        {
            this.sendEmailDelegate = new SendEmailDelegate(this.SendEmail);
            this.sendSmsDelegate = new SendSmsDelegate(this.SendSms);
            this.initiateCallDelegate = new InitiateCallDelegate(this.InitiateCall);
        }

        private delegate void InitiateCallDelegate(PersonNotification n);

        private delegate void SendSmsDelegate(PersonNotification n);

        private delegate void SendEmailDelegate(PersonNotification n);

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

            // get all notifications that match the test:
            var notifications = DB.GetNotificationsByTest(testId);

            // fire off notifications:
            foreach (var n in notifications)
            {
                if (n.Sms && !string.IsNullOrWhiteSpace(n.SmsBody))
                {
                    this.sendSmsDelegate.BeginInvoke(n, null, null);
                }

                /* no phone for now
                if (n.Phone && !string.IsNullOrWhiteSpace(n.PhoneBody))
                {
                    this.initiateCallDelegate.BeginInvoke(n, null, null);
                }*/
                if (n.Email && !string.IsNullOrWhiteSpace(n.EmailSubject))
                {
                    this.sendEmailDelegate.BeginInvoke(n, null, null);
                }
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

                message.To.Add(new MailAddress(n.PersonEmail, n.FirstName + " " + n.LastName));
                message.From = new MailAddress("noreply@gxalert.com");

                message.Subject = this.FillPlaceholders(n.EmailSubject, n);
                message.Body = this.FillPlaceholders(n.EmailBody, n);

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
                string sms = this.FillPlaceholders(n.SmsBody, n);
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
                // send sms
                string phone = this.FillPlaceholders(n.PhoneBody, n);
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
        /// <returns>Message with all placeholders replaced</returns>
        private string FillPlaceholders(string message, PersonNotification n)
        {
            CultureInfo ci = new CultureInfo(n.PersonCulture);

            return message.Replace("[DeploymentDescription]", n.DeploymentDescription)
                            .Replace("[DeploymentCountry]", n.DeploymentCountry)
                            .Replace("[Results]", n.ResultText.Replace("|", ", ").Trim(new char[] { ' ', ',' }))
                            .Replace("[MessageSentOn]", n.MessageSentOn.ToString("d", ci) + " " + n.MessageSentOn.ToString("t", ci))
                            .Replace("[DeploymentHostId]", n.DeploymentHostId);
        }

        /// <summary>
        /// Sends a notification about a new deployment to the people defined in the configuration settings
        /// </summary>
        /// <param name="hostId">The hostId of the device</param>
        /// <param name="instrumentSerial">The serial number of the device</param>
        /// <param name="senderIp">IP that the device is sending from</param>
        public void SendNewDeploymentNotification(string hostId, string instrumentSerial, string senderIp)
        {
            try
            {
                // send email
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

                message.To.Add(new MailAddress(ConfigurationManager.AppSettings["newDeploymentTo"]));
                message.From = new MailAddress("noreply@gxalert.com");

                message.Subject = "A new deployment submitted data";
                message.Body = "Details of the deployment: \n\n";
                message.Body += "\nHostID: " + hostId;
                message.Body += "\nSerial: " + instrumentSerial;
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
            // send email
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            message.To.Add(ConfigurationManager.AppSettings["errorTo"]);
            message.From = new MailAddress("noreply@gxalert.com");
            message.Subject = "An error has occurred in " + ConfigurationManager.AppSettings["appName"];
            message.Body = logMessage;

            SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["smtpServer"], 587);
            smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUser"], ConfigurationManager.AppSettings["smtpPassword"]);
            smtp.Send(message);
        }
    }
}
