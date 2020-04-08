using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.Backend
{

    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * tool for creating a WiFi access point (with specific name) to which the boards can bind
     * to communicate with the DLL server
     */
    public class Hotspot
    {
        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string ssid         = "ESP32_test";
        private string password     = "11235813";
        private ProcessStartInfo ps = null;
        private string message;


        // ~-----constructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Hotspot()
        {
            Init();
            create(ssid, password);
        }

        private void Init()
        {
            ps = new ProcessStartInfo("cmd.exe");
            ps.UseShellExecute = false;
            ps.RedirectStandardOutput = true;
            ps.CreateNoWindow = true;
            ps.FileName = "netsh";

            ps.Verb = "runas";
        }

        // ~-----methods-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void create(string ssid, string key)
        {
            ps.Arguments = String.Format("wlan set hostednetwork mode=allow ssid={0} key={1}", ssid, key);
            Execute(ps);
        }

        public void start()
        {
            ps.Arguments = "wlan start hosted network";
            Execute(ps);
        }

        public void stop()
        {
            ps.Arguments = "wlan stop hosted network";
            Execute(ps);
        }

        // ~-----internal facilities-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Execute(ProcessStartInfo ps)
        {
            bool isExecuted = false;
            try
            {
                using (Process p = Process.Start(ps))
                {
                    message = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    isExecuted = true;
                }
            } catch (Exception e)
            {
                message = "EXCEPTION: " + e.Message;
                isExecuted = false;
            }

            //Console.WriteLine("----- Hotspot creation -----");
            Console.WriteLine(message);
        }

        // ~-----properties-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Message {
            get => message;
        }
    }
}
