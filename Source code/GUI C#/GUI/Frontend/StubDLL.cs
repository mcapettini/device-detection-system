/*
 * Author: Fabio carfì
 * Purpose: This class has been implemented for a debug purpose, to substitut the C++ DLL part
 *          for generating random data and test all the functionalities of the statistic window
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity.Migrations;
using GUI.Backend;
using GUI.Backend.Database;

namespace GUI.Frontend
{
    public class StubDLL
    {
        //------------------- ATTRIBUTES -------------------------------------------------------------------------------------------------
        private static Thread dataGenerator = null;
        private static Boolean generate_condition = true;
        private static object _lock = new object();
        private static int counter = 20;
        private static List<String> MACs = new List<string>();

        //------------------- INTERNAL METHODS -------------------------------------------------------------------------------------------
        //This method generate a valid MAC address
        private static string MAC_generator()
        {
            int number;
            Random rand = new Random();
            string mac = "";

            for(int i=0; i<6; i++)
            {
                number = rand.Next(0, 256);
                mac += number.ToString("X2") + ":";
            }

            return mac.Substring(0,mac.Length-1);
        }

        //------------------- METHODS ----------------------------------------------------------------------------------------------------
        //This method has to start the generator thread
        public static void StartEngine(Configuration configuration)
        {
            int i = 0;
            double x, y, minX, minY, maxX, maxY;
            double probability;
            string mac, SSID = "ci piace lo stub", conf_id = configuration.ConfigurationID;
            Random rand = new Random();

            generate_condition = true;

            //Here I generate all the macs to be used by the thread
            while(i < counter)
            {
                mac = MAC_generator();
                if (!MACs.Contains(mac))
                {
                    MACs.Add(mac);
                    i++;
                }
            }

            minX = configuration.Boards.Select(b => b.X).Min();
            minY = configuration.Boards.Select(b => b.Y).Min();
            maxX = configuration.Boards.Select(b => b.X).Max();
            maxY = configuration.Boards.Select(b => b.Y).Max();

            //Here I instanciate and start the thread
            dataGenerator = new Thread(() =>
            {
                while (generate_condition)
                {
                    //Entering in te atomic block
                    Monitor.Enter(_lock);

                    using (var db = new DBmodel())
                    {
                        for (int j = 0; j < counter; j++)
                        {
                            //Generating the data to use
                            probability = rand.NextDouble();
                            if (probability < 0.9)
                            {
                                int index = rand.Next(0, counter);
                                mac = MACs[index];
                            }
                            else
                            {
                                mac = MACs[j];
                                while (MACs.Contains(mac))
                                {
                                    mac = MAC_generator();
                                }
                            }
                            x = rand.NextDouble() * (maxX - minX) + minX;
                            y = rand.NextDouble() * (maxY - minY) + minY;

                            //inserting the data on the DB
                            position pos = new position
                            {
                                configuration_id = conf_id,
                                MACaddress = mac,
                                SSID = SSID,
                                timestamp = DateTime.Now.Subtract(new TimeSpan(0, 0, rand.Next(0, 60))),
                                x = x,
                                y = y
                            };
                            db.position.AddOrUpdate(pos);
                        }
                        db.SaveChanges();
                    }

                    //Setting the wait for 1 minute
                    Monitor.Wait(_lock, new TimeSpan(0, 1, 0));
                }
            });
            dataGenerator.Start();
        }

        //This method has to stop the generator thread
        public static void StopEngine(Configuration configuration)
        {
            //stop the loop of generating data
            generate_condition = false;

            //notify on the CV
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }

            //joining the thread released before
            dataGenerator.Join();
        }
    }
}
