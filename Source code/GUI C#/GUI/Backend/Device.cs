using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUI.Backend.Database;
using GUI.Backend;

namespace GUI.Backend
{
    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * represent a detected smartphone
     */
    public class Device
    {
        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private Configuration configuration;
        private string deviceID; //MAC address



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Device(Configuration configuration, string MACaddress)
        {
            this.configuration = configuration;
            this.deviceID = MACaddress;
        }



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
        * return the collection of devices (i.e. MACs) and relative activity timeline
        */
        static public Dictionary<string, Dictionary<DateTime, TimeSpan>> FrequentDevices(Configuration configuration, DateTime start, TimeSpan rangeOfInterest)
        {
            // local variables
            DateTime end = start.Add(rangeOfInterest);
            Dictionary<string, Dictionary<DateTime, TimeSpan>> output = new Dictionary<string, Dictionary<DateTime, TimeSpan>>();

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var detections = db.position
                                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                                    .Where(p => p.timestamp.CompareTo(start) >= 0)
                                    .Where(p => p.timestamp.CompareTo(end) <= 0)
                                    .ToList()
                                    .Select(p => new Position(configuration, p.timestamp, new Device(configuration, p.MACaddress), new Coordinates(p.x, p.y)))
                                    .ToList();

                var devices_distinct = detections
                                        .Select(p => p.Device.DeviceID)
                                        .Distinct()
                                        .Select(d => new Device(configuration, d));

                foreach (Device device in devices_distinct)
                {
                    Dictionary<DateTime, TimeSpan> device_storyline = new Dictionary<DateTime, TimeSpan>();
                    DateTime windowRightLimit = end;

                    while (windowRightLimit.CompareTo(start) >= 0)
                    {
                        var query = detections
                                .Where(p => p.Device.DeviceID.Equals(device.DeviceID))
                                .Where(p => p.Timestamp.CompareTo(start) >= 0)
                                .Where(p => p.Timestamp.CompareTo(windowRightLimit) < 0)
                                .ToList();

                        if (query.Count() == 0)
                            break;

                        DateTime youngest = query
                                            .OrderByDescending(p => p.Timestamp)
                                            .First()
                                            .Timestamp;

                        TimeSpan range = device.ContinuativePresence_Last(youngest);
                        if (range >= DataCaching.discreteTimeSpan)  //the presence is continuative
                        {
                            if (youngest.Subtract(range).CompareTo(start) >= 0)   //the 'range' does not exceed the 'start'
                                device_storyline.Add(youngest.Subtract(range), range);
                            else
                                device_storyline.Add(start, youngest.Subtract(start));
                        }

                        windowRightLimit = youngest.Subtract(range);
                    }

                    if (device_storyline.Count > 0)
                        output.Add(device.DeviceID, device_storyline);
                }
            }

            // return correct computation
            return output;
        }

        /* Nicolò:
         * return the collection of devices (i.e. MACs) and relative activity timeline
         */
        static public int ContinuativeDevices_Count(Configuration configuration, DateTime start, TimeSpan rangeOfInterest)
        {
            // local variables
            DateTime end = start.Add(rangeOfInterest);
            int count = 0;

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var detections = db.position
                                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                                    .Where(p => p.timestamp.CompareTo(start) >= 0)
                                    .Where(p => p.timestamp.CompareTo(end) <= 0)
                                    .ToList()
                                    .Select(p => new Position(configuration, p.timestamp, new Device(configuration, p.MACaddress), new Coordinates(p.x, p.y)))
                                    .ToList();

                var devices_distinct = detections
                                        .Select(p => p.Device.DeviceID)
                                        .Distinct()
                                        .Select(d => new Device(configuration, d));

                foreach (Device device in devices_distinct)
                {
                    DateTime windowRightLimit = end;

                    while (windowRightLimit.CompareTo(start) >= 0)
                    {
                        var query = detections
                                .Where(p => p.Device.DeviceID.Equals(device.DeviceID))
                                .Where(p => p.Timestamp.CompareTo(start) >= 0)
                                .Where(p => p.Timestamp.CompareTo(windowRightLimit) < 0)
                                .ToList();

                        if (query.Count() == 0)
                            break;

                        DateTime youngest = query
                                            .OrderByDescending(p => p.Timestamp)
                                            .First()
                                            .Timestamp;

                        TimeSpan range = device.ContinuativePresence_Last(youngest);
                        if (range.CompareTo(DataCaching.discreteTimeSpan) >= 0) // the presence is continuative
                        {
                            count++;
                            break;
                        }

                        windowRightLimit = youngest.Subtract(range);
                    }
                }
            }

            // return correct computation
            return count;
        }

        /* Nicolò:
        * return how many time the device stays in the room, since its last entrance
        */
        public TimeSpan ContinuativePresence_Last(DateTime start)
        {
            // local variables
            DateTime windowLeftLimit = start; // left limit of the sliding window (sliding to the left, so going backward in the timeline)
            DateTime eldest = start; // oldest detected presence of the device

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))   // selection: only current configuration
                    .Where(p => p.MACaddress.Equals(DeviceID))                              // selection: only current device
                    .Select(p => p.timestamp)                                               // projection: only timestamp
                    .OrderByDescending(t => t);

                while (true)
                {
                    windowLeftLimit = eldest.Subtract(DataCaching.discreteTimeSpan); //set the new sliding window (according to the atomic discrete timespan)
                    var slidingWindow = query
                        .Where(t => t.CompareTo(windowLeftLimit) >= 0 && t.CompareTo(eldest) < 0); // retrieve all the detection of the device, in the defined sliding window

                    if (slidingWindow.Count() > 0) // if the device has been detected
                        eldest = slidingWindow.OrderBy(t => t).First(); // keep the oldest detection
                    else
                        return start.Subtract(eldest); // if the device has not been detected for an entire atomic timespan, the presence is no more continuative
                }
            }
        }

        /* Nicolò:
        * return the number of times the device has entered in the room in the last day/week/month (depicted by the rangeOfInterest parameter)
        */
        public int ContinuativePresence_Count(DateTime start, TimeSpan rangeOfInterest)
        {
            // local variables
            DateTime windowRigthLimit   = start;
            DateTime end                = start.Subtract(rangeOfInterest);
            int cnt                     = 0;

            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                // retrive data in the interesting timespan
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))   // selection: only current configuration
                    .Where(p => p.MACaddress.Equals(DeviceID))                              // selection: only current device
                    .Where(p => p.timestamp.CompareTo(end) >= 0)                            // selection: only in the interesting timespan
                    .Select(p => p.timestamp);                                              // projection: only timestamp

                while (windowRigthLimit.CompareTo(end) >= 0) // while right limit is more to the left than the start-rangeOfInterest
                {
                    var slidingWindow = query
                        .Where(t => t.CompareTo(windowRigthLimit) < 0);

                    if (slidingWindow.Count() == 0)
                        return cnt;
                    windowRigthLimit = slidingWindow.OrderByDescending(t => t).First(); // keep the newest detection
                    TimeSpan continuativeSpan = ContinuativePresence_Last(windowRigthLimit);
                    windowRigthLimit = windowRigthLimit.Subtract(continuativeSpan); // prepare for next iteration
                    cnt++;  // increment the counter
                }
            }

            return 0;
        }

        /* Nicolò:
        * return the average of the time the device spent in the room in the last day/week/month (depicted by the rangeOfInterest parameter)
        */
        public TimeSpan ContinuativePresence_Average(DateTime start, TimeSpan rangeOfInterest)
        {
            // local variables
            DateTime windowRigthLimit   = start;
            DateTime end                = start.Subtract(rangeOfInterest);
            TimeSpan total              = new TimeSpan(0, 0, 0);
            int cnt                     = 0;


            // retrieve the interesting data from the DB
            using (var db = new DBmodel())
            {
                // retrive data in the interesting timespan
                var query = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))   // selection: only current configuration
                    .Where(p => p.MACaddress.Equals(DeviceID))                              // selection: only current device
                    .Where(p => p.timestamp.CompareTo(end) >= 0)                            // selection: only in the interesting timespan
                    .Select(p => p.timestamp);                                              // projection: only timestamp

                while (windowRigthLimit.CompareTo(end) >= 0) // while right limit is more to the left than the start-rangeOfInterest
                {
                    var slidingWindow = query
                        .Where(t => t.CompareTo(windowRigthLimit) < 0);

                    // exiting condition
                    if (slidingWindow.Count() == 0)
                    {
                        double doubleAverageTicks = total.Ticks / cnt;
                        long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
                        return new TimeSpan(longAverageTicks);
                    }

                    windowRigthLimit = slidingWindow.OrderByDescending(t => t).First(); // keep the newest detection
                    TimeSpan continuativeSpan = ContinuativePresence_Last(windowRigthLimit);
                    windowRigthLimit = windowRigthLimit.Subtract(continuativeSpan); // prepare for next iteration
                    total.Add(continuativeSpan); // add the total continuative presence
                    cnt++;  // increment the counter
                }
            }

            if (cnt == 0)
                return new TimeSpan(0, 0, 0);
            return new TimeSpan(Convert.ToInt64(total.Ticks / cnt));
        }




        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string DeviceID
        {
            get { return deviceID; }
            set { deviceID = value; }
        }


        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return base.ToString();
        }
    }

}
