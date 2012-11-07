namespace GxAlert
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Net.Mail;

    /// <summary>
    /// Little class for logging stuff to both the console and a text file
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Log an entry to console and file
        /// </summary>
        /// <param name="logMessage">Message to log</param>
        /// <param name="logLevel">Severity level</param>
        public static void Log(string logMessage, LogLevel logLevel)
        {
            if ((int)logLevel >= Convert.ToInt16(ConfigurationManager.AppSettings["logLevel"]))
            {
                string logEntry = DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + (logLevel == LogLevel.Error ? " - ERROR" : string.Empty) + ": " + logMessage;
                Console.WriteLine(logEntry);
                WriteToFile(logEntry);
            }

            // send email on error:
            if (logLevel == LogLevel.Error)
            {
                Notifications.SendErrorEmail(logMessage);
            }
        }

        /// <summary>
        /// Log an entry to console and file
        /// </summary>
        /// <param name="logMessage">Message to log</param>
        /// <param name="logLevel">Severity level</param>
        /// <param name="additionalInfoForFile">Additional data that will only be added to the log file (not to console window)</param>
        public static void Log(string logMessage, LogLevel logLevel, string additionalInfoForFile)
        {
            if ((int)logLevel >= Convert.ToInt16(ConfigurationManager.AppSettings["logLevel"]))
            {
                Log(logMessage, logLevel);
                WriteToFile(additionalInfoForFile);
            }

            // send email on error:
            if (logLevel == LogLevel.Error)
            {
                Notifications.SendErrorEmail(logMessage);
            }
        }

        /// <summary>
        /// Write to the log-file
        /// </summary>
        /// <param name="logEntry">Message to log</param>
        private static void WriteToFile(string logEntry)
        {
            using (StreamWriter sw = File.AppendText("Log.txt"))
            {
                sw.WriteLine(logEntry);
                sw.WriteLine();
            }
        }
    }
}
