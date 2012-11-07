namespace GxAlert
{
    /// <summary>
    /// Little class for a HL7 Acknowledgement-object
    /// </summary>
    public class DataForHL7Acknowledgement
    {
        public string SendingApplicationNamespaceID { get; set; }

        public string SendingApplicationUniversalID { get; set; }

        public string SendingApplicationUniversalIDType { get; set; }

        public string MessageControlID { get; set; }
    }
}
