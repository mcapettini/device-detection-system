using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUI.Backend.Database;
using System.Data.Entity.Migrations;

namespace GUI.Backend
{
    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * links a Coordinates with a Device at a give Timestamp
     * used to denote the detected position of a Device, at a specific Timestamp
     */
    public class Position
    {
        // ~-----enumerations-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------



        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private Configuration configuration;
        private DateTime timestamp;
        private Device device;
        private Coordinates coordinates;



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Position(Configuration configuration, DateTime timestamp, Device device, Coordinates coordinates)
        {
            Configuration = configuration;
            Timestamp = timestamp;
            Device = device;
            Coordinates = coordinates;
        }

        public Position(Configuration configuration, DateTime timestamp, string MACaddress, Coordinates coordinates)
        {
            Configuration = configuration;
            Timestamp = timestamp;
            Device = new Device(configuration, MACaddress);
            Coordinates = coordinates;
        }

        public Position(string configurationID, DateTime timestamp, string MACaddress, Coordinates coordinates)
        {
            Configuration = Configuration.LoadConfiguration(configurationID);
            Timestamp = timestamp;
            Device = new Device(Configuration, MACaddress);
            Coordinates = coordinates;
        }



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * convert C# datetime to Unix timestamp
         */
        public static long DatetimeToTimestamp(DateTime datetime)
        {
            return ((DateTimeOffset)datetime).ToUnixTimeMilliseconds();
        }

        /* Nicolò:
         * convert Unix timestamp to C# datetime
         */
        public static DateTime TimestampToDatetime(long timestamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(timestamp);
            return dtDateTime;
        }


        // ~-----getters and setters------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * insert a record in the Position table
         * it represents the position of a device at a certain time
         */
        public static void StorePosition(Configuration configuration, DateTime timestamp, string MACaddress, Coordinates coord)
        {
            // create a new 'position' and exploit the existing method 
            Position pos = new Position(configuration,
                timestamp,
                new Device(configuration, MACaddress),
                coord);

            pos.StorePosition();
        }

        /* Nicolò:
         * insert a record in the Position table
         * it represents the position of a device at a certain time
         */
        public static void StorePosition(Configuration configuration, DateTime timestamp, Device device, Coordinates coord)
        {
            // create a new 'position' and exploit the existing method 
            Position pos = new Position(configuration,
                timestamp,
                device,
                coord);

            pos.StorePosition();
        }


        /* Nicolò:
         * insert a record in the Position table
         * it represents the position of a device at a certain time
         */
        public void StorePosition()
        {
            using (var db = new DBmodel())
            {
                // create an object 'position' that suits the DB
                position pos = new position
                {
                    timestamp = Timestamp,
                    MACaddress = Device.DeviceID,
                    configuration_id = Configuration.ConfigurationID,
                    x = Coordinates.X,
                    y = Coordinates.Y
                };

                // save the current Position
                db.position.AddOrUpdate(pos);
                db.SaveChanges();
            }
        }

        /* Nicolò:
         * retrieve a record from the Position table
         * it represents the position of a device at a certain time
         */
        public static Coordinates LoadPosition(Configuration configuration, DateTime timestamp, string MACaddress)
        {
            using (var db = new DBmodel())
            {
                // retrieve the Position having the given key
                var dataset = db.position
                    .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                    .Where(p => p.timestamp.Equals(timestamp) && p.MACaddress.Equals(MACaddress))
                    .Select(p => new Coordinates(p.x, p.y))
                    .ToList();

                if (dataset.Count() > 1)
                {
                    throw new Exception("Multiple items, in table 'position' share the same primary key\n");
                }
                if (dataset.Count() <= 0)
                {
                    return null;
                }
                return dataset.ElementAt(0);
            }
        }




        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public DateTime Timestamp
        {
            get => timestamp;
            set => timestamp = value;
        }

        internal Configuration Configuration
        {
            get => configuration;
            set => configuration = value;
        }

        internal Device Device
        {
            get => device;
            set => device = value;
        }

        public Coordinates Coordinates
        {
            get => coordinates;
            set => coordinates = value;
        }



        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return base.ToString();
        }

    }
}
