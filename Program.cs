using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace PingDropMonitor
{
    internal static class Program
    {
        private const string dateTimeFormat = "MM/dd/yyyy HH:mm:ss";
        private static DateTime lastOutageStart = DateTime.Now;
        private static DateTime lastOutageStop = DateTime.Now;
        private static TimeSpan lastOutageLength;

        public static string DateTimeFormat => dateTimeFormat;

        private static void Main()
        {
            int timeoutMS = Properties.Settings.Default.timeOutMS;  // wait for a ping response
            int sleepMS = Properties.Settings.Default.sleepMS;      // sleep before another ping round
            bool outageIsOngoing = false;
            bool outageTimingIsOn = false;
            //bool outageJustEnded = true;
            //bool ranOnceAfterDisplayedOutageTime = false;

            string docPath = Properties.Settings.Default.outputFolder;
            if (!Directory.Exists(docPath))
            {
                docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

// function to validate an email with regex and comment each line

            string fileName = Path.Combine(docPath, DateTime.Now.ToFileTimeUtc() + "-log.txt");
            int count = 0;
            string message = "";

            Console.Title = $"Began: {DateTime.Now.ToString(dateTimeFormat)}";

            using (StreamWriter sw = new StreamWriter(fileName, append: true))// by Using, the stream will be flushed and closed even on an exception 
            {
                ToScreen(Console.Title);
                ToScreen($"Logging to: {fileName}");
                ToLog(sw, Console.Title);
                while (true)
                {
                    // At the beginning of each pass checking 'n' addresses 
                    int fails = 0;
                    message = "";

                    //
                    // Try all addresses in group
                    ///
                    foreach (string address in Properties.Settings.Default.ipAddresses)
                    {
                        if (ReturnPingResult(address, timeoutMS, ref message))
                        {
                            if (outageIsOngoing)
                            {
                                outageIsOngoing = false;
                                //outageJustEnded = true; // this will let us display all n pings before setting to false
                            }
                            //if (outageJustEnded | ranOnceAfterDisplayedOutageTime)
                            //{
                                ToLog(sw, message);
                                ToScreen(message);
                            //}
                        }
                        else
                        {
                            fails++;
                            //if (!outageIsOngoing) // log the first failure pass of n pings; when we check for an outage below, we'll set to true if n fails 
                            //{
                                ToLog(sw, message);
                                ToScreen(message);
                            //}
                        }
                    }
                    //outageJustEnded = false; // only now, after displaying all n pings, can we set to true
                    //ranOnceAfterDisplayedOutageTime = false;

                    //
                    // If every ping in the group failed, and we're not yet in an outage...
                    //
                    if (fails > Properties.Settings.Default.ipAddresses.Count - 1 && !outageIsOngoing)
                    {   // ... then we're in an outage now...
                        lastOutageStart = DateTime.Now;
                        // ... and we've begun timing it.
                        outageIsOngoing = true;
                        outageTimingIsOn = true;
                        ToLog(sw, "Outage ongoing...");
                        ToScreen("Outage ongoing...");
                    }

                    // If we're timing an outage, but it has just ended ...
                    if (outageTimingIsOn && (!outageIsOngoing))
                    {   // ... we stop timing.
                        lastOutageStop = DateTime.Now;
                        outageTimingIsOn = false;
                        lastOutageLength = lastOutageStop.Subtract(lastOutageStart);
                        ToLog(sw, ">> Outage time was: " + lastOutageLength.TotalSeconds + " seconds");
                        ToScreen(">> Outage time was: " + lastOutageLength.TotalSeconds + " seconds");
                        Console.Title = "Last outage ended: " + lastOutageStop.ToString(dateTimeFormat) + " and was: " + lastOutageLength.TotalSeconds;
                        //ranOnceAfterDisplayedOutageTime = true;
                    }
                    count++;
                    sw.Flush();                    
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
                { message = reply.Address.ToString() + "," + reply.RoundtripTime.ToString() + "ms"; //," + reply.Options.Ttl;
                    return true; }
                else
                {   message = ipAddress + "," + reply.Status.ToString();
                    return false; }
            }
            catch
            {   message = "Unable to ping " + ipAddress;
                return false; }
        }
    }
}