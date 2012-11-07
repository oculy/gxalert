namespace GxAlert
{
    /// <summary>
    /// Little utility class for methods that come in handy
    /// </summary>
    public class Util
    {
        /// <summary>
        /// Calculates the checksum (modulo-256) from the given byte array
        /// </summary>
        /// <param name="bytes">The byte array that we'll calculate the checksum for</param>
        /// <param name="startIndex">With which byte should we start when calculating the checksum?</param>
        /// <returns>The checksum</returns>
        public int GetChecksum(byte[] bytes, int startIndex)
        {
            int checksum = 0;

            for (int i = startIndex; i < bytes.Length; i++)
            {
                checksum += bytes[i];
            }

            return checksum;
        }

        /// <summary>
        /// Calculates the checksum (modulo-256) from the given string
        /// </summary>
        /// <param name="message">String that we'll calculate the checksum for</param>
        /// <param name="startIndex">With which byte should we start when calculating the checksum?</param>
        /// <param name="length">Up to which byte should we calculate?</param>
        /// <returns>The checksum</returns>
        public int GetChecksum(string message, int startIndex, int length)
        {
            int checksum = 0;

            for (int i = 1; i < length; i++)
            {
                checksum += message[i];
            }

            return checksum;
        }
    }
}
