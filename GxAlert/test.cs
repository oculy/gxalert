//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GxAlert
{
    using System;
    using System.Collections.Generic;
    
    public partial class test
    {
        public test()
        {
            this.rawmessages = new HashSet<rawmessage>();
            this.testresults = new HashSet<testresult>();
            this.apilogs = new HashSet<apilog>();
        }
    
        public int TestId { get; set; }
        public int DeploymentId { get; set; }
        public System.DateTime MessageSentOn { get; set; }
        public string SenderVersion { get; set; }
        public string SenderUser { get; set; }
        public string SenderIp { get; set; }
        public string PatientId { get; set; }
        public System.DateTime TestStartedOn { get; set; }
        public System.DateTime TestEndedOn { get; set; }
        public string AssayHostTestCode { get; set; }
        public string CartridgeSerial { get; set; }
        public Nullable<System.DateTime> CartridgeExpirationDate { get; set; }
        public string ReagentLotId { get; set; }
        public string SystemName { get; set; }
        public string ModuleSerial { get; set; }
        public string ComputerName { get; set; }
        public string AssayName { get; set; }
        public string AssayVersion { get; set; }
        public string ResultText { get; set; }
        public string SampleId { get; set; }
        public string Notes { get; set; }
        public System.DateTime InsertedOn { get; set; }
        public string InsertedBy { get; set; }
        public System.DateTime UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> SendToMshSuccessOn { get; set; }
        public string InstrumentSerial { get; set; }
        public int TestTypeId { get; set; }
        public string ExternalTestId { get; set; }
        public string AssayId { get; set; }
        public string BarcodeCheck { get; set; }
        public string ExpiryDateCheck { get; set; }
        public string VolumeCheck { get; set; }
        public string DeviceCheck { get; set; }
        public string ReagentCheck { get; set; }
    
        public virtual ICollection<rawmessage> rawmessages { get; set; }
        public virtual ICollection<testresult> testresults { get; set; }
        public virtual deployment deployment { get; set; }
        public virtual ICollection<apilog> apilogs { get; set; }
        public virtual testtype testtype { get; set; }
    }
}
