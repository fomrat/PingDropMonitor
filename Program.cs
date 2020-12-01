﻿using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace PingDropMonitor
{
    static class Program
    {
        const string dateTimeFormat = "MM/dd/yyyy HH:mm:ss";
        static DateTime lastOutageStart = DateTime.Now;
        static DateTime lastOutageStop = DateTime.Now;
        static TimeSpan lastOutageLength;
        static void Main()
        {
            int timeoutMS = Properties.Settings.Default.timeOutMS;  // wait for a ping response
            int sleepMS = Properties.Settings.Default.sleepMS;      // sleep before another ping round
            bool outageIsOngoing = false;
            bool outageTimingIsOn = false;
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileName = Path.Combine(docPath, DateTime.Now.ToFileTimeUtc() + "-log.txt");
            int count = 0;
            string message = "";

            Console.Title = "Began: " + DateTime.Now.ToString(dateTimeFormat);

            using (StreamWriter sw = new StreamWriter(fileName, append: true))
            {
                while (true)
                {
                    // At the beginning of each pass checking 'n' addresses 
                    int fails = 0;
                    message = "";

                    //
                    // Try all addresses in group
                    ///
                    foreach (string addr in Properties.Settings.Default.ipAddresses)
                    {
                        if (ReturnPingResult(addr, timeoutMS, ref message))
                        {
                            outageIsOngoing = false;
                            if ((count % Properties.Settings.Default.outputEvery) == 0)
                            {
                                ToLog(sw, message);
                                ToScreen(message);
                            }
                        }
                        else
                        {
                            fails += 1;
                        }
                    }

                    //
                    // If every ping in the group failed, and we're not yet in an outage...
                    //
                    if (fails > Properties.Settings.Default.ipAddresses.Count - 1 && !outageIsOngoing)
                    {   // ... then we're in an outage now...
                        lastOutageStart = DateTime.Now;
                        // ... and we've begun timing it.
                        outageIsOngoing = true;
                        outageTimingIsOn = true;
                        ToScreen("Outage ongoing...");
                    }

                    // If we're timing an outage, but it has just ended ...
                    if (outageTimingIsOn && (!outageIsOngoing))
                    {   // ... we stop timing.
                        lastOutageStop = DateTime.Now;
                        outageTimingIsOn = false;
                        lastOutageLength = lastOutageStop.Subtract(lastOutageStart);
                        ToLog(sw, "Outage time was: " + lastOutageLength.TotalSeconds + " seconds");
                        ToScreen("Outage time was: " + lastOutageLength.TotalSeconds + " seconds");
                        Console.Title = "Last outage ended: " + lastOutageStop.ToString(dateTimeFormat) + " and was: " + lastOutageLength.TotalSeconds;
                    }
                    count += 1;
                    System.Threading.Thread.Sleep(sleepMS);
                }
            }
        }

        private static void ToScreen(string message)
        {   Console.WriteLine(DateTime.Now.ToString(dateTimeFormat) + ": " + message); }

        private static void ToLog(StreamWriter sw, string message)
        {   sw.WriteLine(DateTime.Now.ToString(dateTimeFormat) + ": " + message); }

        // Adapted from https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping.send
        private static bool ReturnPingResult(string ipAddress, int timeout, ref string message)
        {   // Create a buffer of 32 bytes of data to be transmitted.
            const string data = "12345678901234567890123456789012";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            Ping pingSender = new Ping();

            // Set options for transmission: the data can go through 64 gateways or routers before it is destroyed, and don't allow the data to be sent in separate packets.
            PingOptions options = new PingOptions(64, true);

            try
            {   PingReply reply = pingSender.Send(ipAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {   message = reply.Address.ToString() + "," + reply.RoundtripTime.ToString();
                    return true; }
                else
                {   message = ipAddress + "," + reply.Status.ToString();                  
                    return false; }
            }
            catch (Exception ex)
            {   message = "Exception occurred." + ex.Message;
                return false; }
        }
    }
}