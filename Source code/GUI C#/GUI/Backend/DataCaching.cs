using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUI.Backend.Database;

namespace GUI.Backend
{

    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * represent a point in a bidimensional space
     * used to denote the position of a device
     */
    public class DataCaching
    {
        // ~-----constants----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static readonly TimeSpan discreteTimeSpan = new TimeSpan(0, 5, 0); //(hours, minutes, seconds)



        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private Configuration configuration;



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public DataCaching(string configurationID)
        {
            configuration = Configuration.LoadConfiguration(configurationID);
        }



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * retrieve the number of devices that are consecutively detected for the last 'span' time
         */
        public int NumberContinuativeDevices(DateTime start, TimeSpan span)
        {
            // local variable
            int cnt = 0;

            // check input parameters
            if (start.CompareTo(DateTime.Now) > 0)
                throw new Exception("The required time is in the future");

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Select(p => p.MACaddress)
                    .Distinct();

                foreach (var item in query)
                {
                    Device device = new Device(configuration, item);
                    TimeSpan continuativePresence = device.ContinuativePresence_Last(start);
                    if (continuativePresence.CompareTo(span) >= 0)
                        cnt++;
                }

                return cnt;
            }
        }

        /* Nicolò:
         * return a dictionary containing, for each MAC address recently present, its Coordinates
         */
        public Dictionary<string, Coordinates> LastPosition_Dictionary(DateTime start)
        {
            // local variables
            DateTime end = start.Subtract(discreteTimeSpan);

            // check input parameters
            if (start.CompareTo(DateTime.Now) > 0)
                throw new Exception("The required time is in the future");
            if (start.Subtract(new DateTime()).CompareTo(discreteTimeSpan) < 0)
                throw new Exception("The required time '" + start.ToString() + "' is not valid");

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Where(p => p.timestamp.CompareTo(end) >= 0)
                    .Where(p => p.timestamp.CompareTo(start) <= 0)
                    .ToList()
                    .GroupBy(p => p.MACaddress)
                    .Select(g => g.OrderByDescending(c => c.timestamp).FirstOrDefault());

                return query.ToDictionary(p => p.MACaddress, p => new Coordinates(p.x, p.y)); // ToDictionary(key, value)
            }
        }

        /* Nicolò:
         * return a list containing, for each MAC address recently present, its Position
         */
        public List<Position> LastPosition_List(DateTime start)
        {
            // check input parameters
            if (start.CompareTo(DateTime.Now) > 0)
                throw new Exception("The required time is in the future");
            if (start.Subtract(new DateTime()).CompareTo(discreteTimeSpan) < 0)
                throw new Exception("The required time '" + start.ToString() + "' is not valid");

            // local variables
            DateTime end = start.Subtract(discreteTimeSpan);

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Where(p => p.timestamp.CompareTo(end) >= 0)
                    .Where(p => p.timestamp.CompareTo(start) <= 0)
                    .ToList()
                    .GroupBy(p => p.MACaddress)
                    .Select(g => g.OrderByDescending(c => c.timestamp).FirstOrDefault());

                var temp = query
                    .Select(p => new Position(configuration, p.timestamp, p.MACaddress, new Coordinates(p.x, p.y)))
                    .ToList();

                return temp;
            }
        }


        /* Nicolò:
         * return the total number of detected private MACs
         */
        public int LocalMAC_Number()
        {
            using (DBmodel db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Where(p => p.sequence_number != null)
                    .Where(p => p.SSID != null)
                    .Where(p => p.fingerprint != null)
                    .Count();

                return query;
            }
        }

        /* Nicolò:
         * return the number of distinct devices having private MACs
         */
        public int LocalMAC_DistinctNumber()
        {
            using (DBmodel db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Where(p => p.sequence_number != null)
                    .Where(p => p.SSID != null)
                    .Where(p => p.fingerprint != null)
                    .Select(p => p.MACaddress)
                    .Distinct()
                    .Count();

                return query;
            }
        }

        /* Nicolò:
         * return identification accuracy of private MACs
         */
        public double LocalMAC_Accuracy()
        {
            // local variable
            int tot_nr = LocalMAC_Number();
            int distinct_nr = LocalMAC_DistinctNumber();

            // compute fraction
            if (tot_nr == 0)
                return 0d;
            return (double) ( distinct_nr / tot_nr);
        }

        /* Nicolò:
         * return the total number of detected MACs (local + global)
         */
        public int TotalMAC_Number()
        {
            using (DBmodel db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Count();

                return query;
            }
        }

        /* Nicolò:
         * return the number of distinct devices having private MACs
         */
        public int TotalMAC_DistinctNumber()
        {
            using (DBmodel db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Select(p => p.MACaddress)
                    .Distinct()
                    .Count();

                return query;
            }
        }

        /* Nicolò:
         * return identification accuracy of private MACs
         */
        public double TotalMAC_Accuracy()
        {
            // local variable
            int tot_nr = TotalMAC_Number();
            int distinct_nr = TotalMAC_DistinctNumber();

            // compute fraction
            if (tot_nr == 0)
                return 0d;
            return (double)(distinct_nr / tot_nr);
        }



        // ~-----getters and setters------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Configuration Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }
    }
}
