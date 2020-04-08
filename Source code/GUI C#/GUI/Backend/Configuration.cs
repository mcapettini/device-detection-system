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
     * represent a configuration for a specific environment lined to a set of boards
     * used to denote a room its boards inside
     */
    public class Configuration
    {

        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string configurationID;
        private List<Board> boards;



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Configuration(string configurationID, List<Board> boards)
        {
            this.configurationID = configurationID;
            foreach (var b in boards)
            {
                b.Configuration = this;
            }
            this.boards = boards;
        }

        public Configuration(string configurationID)
        {
            this.configurationID = configurationID;
            boards = new List<Board>();
        }



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * retrieve the list of the IDs of all the saved configurations
         */
        public static List<string> SavedConfigurations_Names()
        {
            using (DBmodel db = new DBmodel())
            {
                var query = db.configuration
                    .Select(c => c.configuration_id)
                    .Distinct();

                return query.ToList();
            }
        }


        /* Nicolò:
         * retrieve the number of saved configurations
         */
        public static int SavedConfigurations_Number()
        {
            return SavedConfigurations_Names().Count;
        }


        /* Nicolò:
         * retrieve the list of the IDs of all the saved configurations
         */
        public static Dictionary<string, Configuration> SavedConfigurations()
        {
            // local variables
            Dictionary<string, Configuration> allConfigurations = new Dictionary<string, Configuration>();
            List<string> keys = SavedConfigurations_Names();

            // retrieve data iterativelly for each configuration ID
            foreach (string id in keys)
            {
                Configuration conf = LoadConfiguration(id);
                allConfigurations.Add(conf.ConfigurationID, conf);
            }
            return allConfigurations;
        }


        /* Nicolò:
         * retrieve the list of the boards related to the given configuration
         */
        public static List<Board> SavedConfiguration_Boards(Configuration configuration)
        {
            // local variables
            List<Board> boards = new List<Board>();

            // retrieve the interesting data from the DB
            using (DBmodel db = new DBmodel())
            {
                var query = db.configuration
                    .Where(c => c.configuration_id.Equals(configuration.ConfigurationID))
                    .OrderBy(b => b.order)
                    .ToList()
                    .Select(b => new Board(configuration, b.board_id, new Coordinates(b.x, b.y)));

                return query.ToList();
            }
        }

        /* Nicolò:
         * check if already exists a configuration having the given name
         */
        public static Boolean IsAlreadyPresent(string configurationID)
        {
            // retrieve the interesting data from the DB
            using (DBmodel db = new DBmodel())
            {
                var query = db.configuration
                    .Where(c => c.configuration_id.Equals(configurationID))
                    .Count();

                return (query > 0);
            }

        }

        /* Nicolò:
         * check if the database is aligned to the values of the current object
         */
        public Boolean IsUpdated()
        {
            // locally store the old configuration
            Configuration oldConfiguration = Configuration.LoadConfiguration(ConfigurationID);

            // look for differences
            foreach (Board oldBoard in oldConfiguration.boards)
            {
                // retrieve current Board
                Board newBoard;
                try
                {
                    newBoard = this.boards.Find(b => b.BoardID == oldBoard.BoardID);
                }
                catch (Exception)
                {
                    return false;
                }

                // check coordinates differences
                if (oldBoard.X != newBoard.X || oldBoard.Y != newBoard.Y)
                    return false;
            }

            // return value for 'no changes to perform'
            return true;
        }


        // ~-----getters and setters------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static Configuration LoadConfiguration(string configurationID)
        {
            // local variables
            Configuration conf = new Configuration(configurationID);

            // retrieve the boards composing the requested DB
            using (DBmodel db = new DBmodel())
            {
                var query = db.configuration
                    .Where(c => c.configuration_id.Equals(configurationID))
                    .OrderBy(b => b.order);

                foreach (var item in query)
                    conf.BoardAdd(new Board(conf,
                        item.board_id,
                        new Coordinates(item.x, item.y))
                        );
            }

            // check actual presence of required Configuration
            if (conf.Boards.Count == 0)
                return null;
            return conf;
        }


        public void StoreConfiguration()
        {
            // check input parameters
            if (this.Boards.Count < 2)
                throw new Exception("Invalid configuration: less than 2 boards");

            // locally store the old configuration
            Configuration oldConfiguration = Configuration.LoadConfiguration(ConfigurationID);                

            using (var db = new DBmodel())
            {
                // remove old configuration from DB
                if (oldConfiguration != null)
                {
                    db.configuration
                        .RemoveRange(db.configuration
                                        .Where(c => c.configuration_id.Equals(this.configurationID)));
                    db.SaveChanges();
                }

                // save each dictionary pair as a new record
                foreach (Board b in boards)
                {
                    // create a 'configuration' object that suits the DB
                    configuration DBconf = new configuration();
                    DBconf.configuration_id = ConfigurationID;
                    DBconf.board_id = b.BoardID;
                    DBconf.x = b.Coordinates.X;
                    DBconf.y = b.Coordinates.Y;
                    DBconf.order = boards.IndexOf(b);
                    db.configuration.AddOrUpdate(DBconf); // needs System.Data.Entity.Migrations;
                }

                // save the changes
                db.SaveChanges();
            }

            // notify the occurence of an update in 'configuration' table, by inserting in the 'log' table
            if (oldConfiguration != null && this.ToString().Equals(oldConfiguration.ToString()))    // serialization needed just to avoid implementing Equals() method
                return;
            string message;
            Log.MessageType type;
            if (oldConfiguration == null)
            {
                message = "Created configuration '" + ConfigurationID + "'";
                type = Log.MessageType.configuration_created;
            }
            else
            {
                message = "Updated configuration '" + ConfigurationID + "': ";
                type = Log.MessageType.configuration_update;
                int diff = oldConfiguration.Boards.Count - Boards.Count; //diff = table - object = old - new
                if (diff < 0)
                    message += "inserted " + diff + " new boards, ";
                else if (diff > 0)
                    message += "discarded " + diff + " old boards, ";
                foreach (Board b in oldConfiguration.Boards)
                    if (!b.Coordinates.X.Equals(Boards.Find((Board a) => { return a.BoardID == b.BoardID; }).Coordinates.X) || !b.Coordinates.Y.Equals(Boards.Find((Board a) => { return a.BoardID == b.BoardID; }).Coordinates.Y)) //one coordinate has changed
                        message += "board '" + b.BoardID + "' moved (" + b.Coordinates.X + "," + b.Coordinates.Y + ")->(" + Boards.Find((Board a) => { return a.BoardID == b.BoardID; }).Coordinates.X + "," + Boards.Find((Board a) => { return a.BoardID == b.BoardID; }).Coordinates.Y + "), ";
            }
            if (message.EndsWith(", "))
                message = message.Substring(0, message.Length - 2);
            Log.InsertLog(this, type, message);
        }


        public static void RemoveConfiguration(string configurationID)
        {
            // retrieve the old configuration
            Configuration oldConfiguration = Configuration.LoadConfiguration(configurationID);
            if (oldConfiguration == null)
                throw new Exception("There is no such configuration in the database");

            using (var db = new DBmodel())
            {
                //----- delete in Configuration table -----
                // select the desired rows
                var query_conf = db.configuration
                    .Where(c => c.configuration_id.Equals(configurationID));
                // delete the selected records
                foreach (var item in query_conf)
                {
                    db.configuration.Remove(item);
                }

                //----- delete in Log table -----
                // select the desired rows
                var query_log = db.log
                    .Where(c => c.configuration.Equals(configurationID));
                // delete the selected records
                foreach (var item in query_log)
                {
                    db.log.Remove(item);
                }

                //----- delete in Position table -----
                // select the desired rows
                var query_pos = db.position
                    .Where(c => c.configuration_id.Equals(configurationID));
                // delete the selected records
                foreach (var item in query_pos)
                {
                    db.position.Remove(item);
                }

                //----- save the changes -----
                db.SaveChanges();
            }

            // notify the occurence of an update in 'configuration' table, by inserting in the 'log' table
            string message = "Removed configuration '" + configurationID + "'";
            Log.InsertLog(oldConfiguration, Log.MessageType.configuration_removed, message);
        }


        public int BoardCount()
        {
            return boards.Count();
        }

        public bool BoardAdd(Board board)
        {
            if (boards.Contains(board))
                return false;

            boards.Add(board);
            return true;
        }

        public bool BoardRemove(Board board)
        {
            if (!boards.Contains(board))
                return false;

            boards.Remove(board);
            return true;
        }

        public void BoardReplace(List<Board> boards)
        {
            this.BoardClear();

            foreach (Board b in boards)
            {
                b.Configuration = this;
                this.BoardAdd(b);
            }
        }

        public void BoardClear()
        {
            foreach (Board b in boards)
            {
                b.Configuration = null;
            }

            boards.Clear();
        }

        public bool BoardContains(Board target)
        {
            return boards.Contains(target);
        }

        public Board BoardFind(Predicate<Board> predicate)
        {
            return boards.Find(predicate);
        }

        public int BoardIndexOf(Board target)
        {
            return boards.IndexOf(target);
        }

        public Board BoardElementAt(int index)
        {
            return boards.ElementAt(index);
        }



        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string ConfigurationID
        {
            get { return configurationID; }
            set { configurationID = value; }
        }


        public List<Board> Boards
        {
            get { return new List<Board>(boards); }
            //set { boards = value; }
        }



        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
