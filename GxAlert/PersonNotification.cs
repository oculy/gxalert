namespace GxAlert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Little object for a notification that has to be 
    /// sent to a person.
    /// </summary>
    public class PersonNotification
    {
        public int PersonId { get; set; }

        public string Name { get; set; }

        public string PersonEmail { get; set; }

        public string PersonCell { get; set; }

        public string PersonPhone { get; set; }

        public string PersonCulture { get; set; }

        public int NotificationId { get; set; }

        public string NotificationName { get; set; }

        public string EmailSubject { get; set; }

        public string EmailBody { get; set; }

        public string SmsBody { get; set; }

        public string PhoneBody { get; set; }

        public bool Sms { get; set; }

        public bool Phone { get; set; }

        public bool Email { get; set; }

        public string ResultText { get; set; }

        public DateTime MessageSentOn { get; set; }

        public DateTime TestEndedOn { get; set; }

        public string DeploymentHostId { get; set; }

        public string DeploymentDescription { get; set; }

        public string DeploymentCountry { get; set; }
    }
}
