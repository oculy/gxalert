using System;
using System.Text;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Message;

namespace GxAlert
{
    public class Reparse
    {
        /// <summary>
        /// For debugging purposes: Gets raw messages from database that couldn't be parsed (testId = null) and tries parsing them again
        /// </summary>
        internal static void ReparseRawMessages()
        {
            var messages = DB.GetRawMessagesWithoutTestId();

            Console.WriteLine("Reparsing " + messages.Count + " raw messages");

            foreach (var msg in messages)
            {
                ORU_R30 hl7 = ParseMessage(msg.Message);

                if (hl7 != null)
                {
                    if (hl7.OBR.UniversalServiceIdentifier.Identifier.Value == "MTB-RIF")
                    {
                        var testId = DB.StoreParsedMessage(hl7, msg.Message, "192.168.1.110");

                        if (testId.HasValue)
                        {
                            DB.UpdateRawMessageTestId(msg.RawMessageId, testId.Value);
                        }

                        Console.WriteLine(testId);
                    }
                    else
                    {
                        Console.WriteLine("Non-Tb result");
                    }
                }
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Take the HL7 message string and turn it into an HL7 object using NHAPI
        /// </summary>
        /// <param name="hl7Message">Raw HL7 string</param>
        /// <returns>HL7 Object</returns>
        private static ORU_R30 ParseMessage(string hl7Message)
        {
            // use NHapi to parse message if message complete:
            PipeParser parser = new PipeParser();
            ORU_R30 hl7 = new ORU_R30();

            IMessage im = parser.Parse(hl7Message);
            hl7 = im as ORU_R30;

            // Device may send SPM which isn't officially part of ORU_R30, so we have to add it by hand
            hl7.addNonstandardSegment("SPM");

            ASCIIEncoding encoder = new ASCIIEncoding();
            int spmStart = hl7Message.IndexOf("SPM");
            int spmEnd = hl7Message.IndexOf(encoder.GetString(new byte[] { Constants.CR }), hl7Message.IndexOf("SPM"));

            parser.Parse((NHapi.Model.V25.Segment.SPM)hl7.GetStructure("SPM"), hl7Message.Substring(spmStart, spmEnd - spmStart), new EncodingCharacters('|', hl7.MSH.EncodingCharacters.Value));

            // Parse notes section
            if (hl7Message.Contains("NTE") && hl7Message.Contains("OBX|2") && hl7Message.IndexOf("OBX|2") > hl7Message.IndexOf("NTE"))
            {
                parser.Parse((NHapi.Model.V25.Segment.NTE)hl7.GetStructure("NTE"), hl7Message.Substring(hl7Message.IndexOf("NTE"), hl7Message.IndexOf("OBX|2") - hl7Message.IndexOf("NTE")), new EncodingCharacters('|', hl7.MSH.EncodingCharacters.Value));
            }

            return hl7;
        }
    }
}
