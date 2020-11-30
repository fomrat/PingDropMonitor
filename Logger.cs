using System;
using System.IO;

namespace PingDropMonitor
{
    /// <summary>
    /// Adapted from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-open-and-append-to-a-log-file
    /// </summary>
    class Logger
    {
        public static void Log(string logMessage, TextWriter tw)
        {
            tw.WriteLine(logMessage);
        }

        public static void DumpLog(StreamReader sr)
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }
}