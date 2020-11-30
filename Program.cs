using System;
using System.Net.NetworkInformation;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PingDropMonitor
{
    static class Program
    {
        const string dateTimeFormat = "MM/dd/yyyy HH:mm:ss";
        const string PingNonSuccessMarker = "RETURN_STATUS_OTHER_THAN_SUCCESS";

        static StreamWriter sw;
        static DateTime lastOutageStart = DateTime.Now;
        static DateTime lastOutageStop = DateTime.Now;
        static TimeSpan lastOutageLength;

        static void Main()
        {
            const int timeout = 2000; // wait 2 seconds for a ping response
            const int sleepMs = 5000; // sleep 2 seconds before another ping round
            const string header = "Address,RoundTrip time,Time to live,Don't fragment,Buffer size";

            bool outageIsOngoing = false;
            bool outageTimingIsOn = false; 

            Console.WriteLine(header);
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileName = Path.Combine(docPath, DateTime.Now.ToFileTimeUtc() + "-log.txt");

            while (true)
            {
                sw = File.AppendText(fileName);
                {
                    int fails = 0;
                    string message = "";

                    foreach (string addr in Properties.Settings.Default.ipAddresses)
                    {
                        if (ReturnPingResult(addr, timeout, ref message))
                        {   outageIsOngoing = false; Out(message); }
                        else
                        {   fails += 1; Out(message); }
                    }

                    if (fails > Properties.Settings.Default.ipAddresses.Count && !outageIsOngoing)
                    {   lastOutageStart = DateTime.Now;
                        outageIsOngoing = true;
                        outageTimingIsOn = true;
                        Out("OUTAGE"); 
                    }

                    if (outageTimingIsOn && (!outageIsOngoing))
                    {   lastOutageStop = DateTime.Now;
                        outageTimingIsOn = false;
                        lastOutageLength = lastOutageStop.Subtract(lastOutageStart);
                        Out("Outage time was: " + lastOutageLength.TotalSeconds + " seconds");
                        Console.Title = "Last outage ended: " + lastOutageStop.ToString(dateTimeFormat) + " and was: " + lastOutageLength.TotalSeconds; 
                    }

                    sw.Close();
                }
                System.Threading.Thread.Sleep(sleepMs);
            }
        }

        private static void Out(string text)
        {
            Logger.Log(DateTime.Now.ToString(dateTimeFormat) + "," + text, sw);
            Console.WriteLine(DateTime.Now.ToString(dateTimeFormat) + ": " +  text); 
        }

        /// <summary>
        /// Adapted from https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping.send
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>Output from ping</returns>
        private static bool ReturnPingResult(string ipAddress, int timeout, ref string message)
        {
            Ping pingSender = new Ping();

            // Create a buffer of 32 bytes of data to be transmitted.

            const string data = "12345678901234567890123456789012";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Set options for transmission: the data can go through 64 gateways or routers before it is destroyed, and don't allow the data to be sent in separate packets.
            PingOptions options = new PingOptions(64, true);

            // Send the request.

            try
            {
                PingReply reply = pingSender.Send(ipAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    message = reply.Address.ToString() + "," + reply.RoundtripTime.ToString();
                    //  + "," + reply.Options.Ttl.ToString() + "," + reply.Options.DontFragment.ToString() + "," + reply.Buffer.Length.ToString();
                    return true;
                }
                else
                {
                    message = ipAddress + "," + reply.Status.ToString() + "," + PingNonSuccessMarker;
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = "Exception occurred." + ex.Message;
                return false;
            }
        }
    }
}