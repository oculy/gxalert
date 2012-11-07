namespace GxAlert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Just a class for all constants
    /// </summary>
    public static class Constants
    {
        // Beginning of frame
        public const byte STX = 0x2;

        // End frame
        public const byte ETX = 0x3;

        // End of transmission
        public const byte EOT = 0x4;

        // Enquiry, send at establishment
        public const byte ENQ = 0x5;

        // Acknowledge-character
        public const byte ACK = 0x6;

        // Not acknowledged-character
        public const byte NAK = 0x15;

        // Same as new line
        public const byte LF = 0x0A;

        // Pipe
        public const byte PIPE = 0x0B;

        // Field separator
        public const byte SEPARATOR = 0x1C;

        // Carriage return
        public const byte CR = 0x0D;

        // Intermediate Frame
        public const byte ETB = 0x17;
    }
}
