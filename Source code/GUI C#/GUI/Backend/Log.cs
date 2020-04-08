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
     * represent a log message to insert in the timetable
     * used to denote an important event
     */
    public class Log
    {
        // ~-----enumerations-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public enum MessageType
        {
            error_occurred,
            error_handled,
            error_ignored,
            configuration_update,
            configuration_created,
            configuration_removed,
            engine_start,
            engine_stop,
        }


        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private DateTime timestamp;
        private Configuration configuration;
        private MessageType type;
        private int numberBoards;
        private string message;



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Log(Configuration configuration, MessageType type, string message)
        {
            Timestamp = DateTime.Now;
            Configuration = configuration;
            Type = type;
            NumberBoards = configuration.Boards.Count;
            Message = message;
        }

        public Log(string configurationID, MessageType type, string message)
        {
            Timestamp = DateTime.Now;
            Configuration = Configuration.LoadConfiguration(configurationID);
            Type = type;
            NumberBoards = configuration.Boards.Count;
            Message = message;
        }

        public Log(DateTime timestamp, Configuration configuration, MessageType type, string message)
        {
            Timestamp = timestamp;
            Configuration = configuration;
            Type = type;
            NumberBoards = configuration.Boards.Count;
            Message = message;
        }

        public Log(DateTime timestamp, string configurationID, string type, int numberBoards, string message)
        {
            Timestamp = timestamp;
            Configuration = Configuration.LoadConfiguration(configurationID);
            Type = (MessageType)Enum.Parse(typeof(MessageType), type);
            NumberBoards = numberBoards;
            Message = message;
        }



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ~-----getters and setters------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
        * insert a record in the Log table
        * it represents an event that occours while the application is running
        */
        public void Insert()
        {
            using (var db = new DBmodel())
            {
                // create a 'log' object that suits the DB
                log DBlog = new log
                {
                    timestamp = timestamp,
                    configuration = configuration.ConfigurationID,
                    type = type.ToString(),
                    number_boards = numberBoards,
                    message = message
                };

                // save each dictionary pair as a new record
                db.log.Add(DBlog);

                // save the changes
                db.SaveChanges();
            }

        }

        /* Nicolò:
         * insert a record in the Log table
         * it represents an event that occours while the application is running
         */
        public static void InsertLog(Configuration configuration, MessageType type, string message)
        {
            // create a new high level 'Log' object
            Log log = new Log(configuration, type, message);

            // insert in the database
            log.Insert();
        }

        /* Nicolò:
         * insert a record in the Log table
         * it represents an event that occours while the application is running
         */
        public static void InsertLog(DateTime timestamp, Configuration configuration, MessageType type, string message)
        {
            // create a new high level 'Log' object
            Log log = new Log(timestamp, configuration, type, message);

            // insert in the database
            log.Insert();
        }


        /* Nicolò:
         * retrieve a record from the Log table, given its ordinal ID
         * it represents an event that occours while the application is running
         */
        public static List<Log> ReadLog(Configuration configuration)
        {
            // retrieve the log messages referred to the requested Configuration
            using (DBmodel db = new DBmodel())
            {
                var query = db.log
                    .Where(l => l.configuration.Equals(configuration.ConfigurationID))
                    .OrderBy(l => l.timestamp)
                    .ToList()
                    .Select(l => new Log(l.timestamp, l.configuration, l.type, l.number_boards, l.message));

                return query.ToList();
            }

        }

        /* Nicolò:
         * retrieve a record from the Log table, given its ordinal ID
         * it represents an event that occours while the application is running
         */
        public static Dictionary<DateTime, TimeSpan> GetEngineActivityTimeline(Configuration configuration, Boolean isRunning)
        {
            // local variables
            Dictionary<DateTime, TimeSpan> output = new Dictionary<DateTime, TimeSpan>();

            // make sure there are couples start/stop
            Adjust_StartStopPairs(configuration, isRunning);

            // retrieve the log messages referred to the requested Configuration
            using (DBmodel db = new DBmodel())
            {
                // retrieve the configuration startup
                DateTime begin = RetrieveBegin(configuration);

                // retrieve all engine activities 
                var query = db.log
                    .Where(l => l.configuration.Equals(configuration.ConfigurationID))
                    .Where(l => l.type.Equals(Log.MessageType.engine_start.ToString()) || l.type.Equals(Log.MessageType.engine_stop.ToString()))
                    .Where(l => l.timestamp.CompareTo(begin) > 0)
                    .OrderBy(l => l.timestamp)
                    .ToList()
                    .Select(l => new Log(l.timestamp, l.configuration, l.type, l.number_boards, l.message))
                    .ToList();

                // handle the current running detection
                if (isRunning)
                    query.Add(new Log(DateTime.Now, configuration, MessageType.engine_stop, "fake"));

                // compute the range of activity for each running
                for (int i = 0; i < query.Count() - 1; i += 2)
                {
                    // discard empty detections (1ms)
                    if ( query[i].Message.Contains("no detections") || query[i+1].Message.Contains("no detections") )
                        continue;

                    // we rely on 'Adjust_StartStopPairs()' for correct start-stop pairs
                    TimeSpan activity_duration = query[i + 1].Timestamp.Subtract(query[i].Timestamp);
                    output.Add(query[i].Timestamp, activity_duration);
                }

                return output;
            }
        }

        /* Nicolò:
         * adjust the information in 'log' table in order to have all start/stop pairs
         * in case of missing information, it appoximates them according to the first/last 'position' after/before the previous/following start/stop record
         */
        public static void Adjust_StartStopPairs(Configuration configuration, Boolean isRunning)
        {
            // retrieve the log messages referred to the requested Configuration
            using (DBmodel db = new DBmodel())
            {
                // retrieve all engine activities
                var query = db.log
                    .Where(l => l.configuration.Equals(configuration.ConfigurationID))
                    .Where(l => l.type.Equals(Log.MessageType.engine_start.ToString()) || l.type.Equals(Log.MessageType.engine_stop.ToString()))
                    .OrderBy(l => l.timestamp)
                    .ToList()
                    .Select(l => new Log(l.timestamp, l.configuration, l.type, l.number_boards, l.message))
                    .ToList();

                // add fake start/stop pairs
                Log firstStart = new Log(new DateTime(), configuration, MessageType.engine_start, "fake");
                query.Insert(0, firstStart);
                Log firstStop = new Log(new DateTime().AddMilliseconds(1), configuration, MessageType.engine_stop, "fake");
                query.Insert(1, firstStop);

                Log lastStart = new Log(DateTime.Now, configuration, MessageType.engine_start, "fake");
                query.Insert(query.Count(), lastStart);
                Log lastStop = new Log(DateTime.Now.AddMilliseconds(1), configuration, MessageType.engine_stop, "fake");
                query.Insert(query.Count(), lastStop);


                // find missing milestones and correct them
                for (int i = 2; i < query.Count() - 2; i+=2)
                {
                    // a 'start' mark is missing
                    if (query[i].Type != MessageType.engine_start && query[i].Message != "fake")
                    {
                        position prevDetection = new position();
                        string message_log;

                        // find the oldest detection in the current time range
                        DateTime rangeStart = query[i - 1].Timestamp;
                        DateTime rangeStop = query[i].Timestamp;
                        var detections = db.position
                            .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                            .Where(p => p.timestamp.CompareTo(rangeStart) > 0)
                            .Where(p => p.timestamp.CompareTo(rangeStop) < 0)
                            .OrderBy(p => p.timestamp)
                            .ToList();

                        if (detections.Count() != 0)
                        {
                            // add a 'start' just before the oldest detection
                            prevDetection = detections.First();
                            prevDetection.timestamp = prevDetection.timestamp.AddMilliseconds(-1);
                            message_log = "Added due to subsequent checks";
                        } else
                        {
                            // there are no detections, add a 'start' just before the 'stop' 
                            prevDetection.timestamp = query[i].Timestamp.AddMilliseconds(-1);
                            message_log = "Added due to subsequent checks (no detections)";
                        }

                        Log log = new Log(prevDetection.timestamp, configuration, MessageType.engine_start, message_log);
                        log.Insert();
                        query.Insert(i, log);
                    }

                    // a 'stop' mark is missing
                    if (query[i+1].Type != MessageType.engine_stop /*&& query[i+1].Message != "fake"*/)
                    {
                        // if still active, do not add 'stop' mark
                        if (i == query.Count-3 && isRunning)
                            continue;

                        position succDetection = new position();
                        string message_log;
                        
                        // find the newest detection in the current time range
                        DateTime rangeStart = query[i].Timestamp;
                        DateTime rangeStop = query[i + 1].Timestamp;
                        var detections = db.position
                            .Where(p => p.configuration_id.Equals(configuration.ConfigurationID))
                            .Where(p => p.timestamp.CompareTo(rangeStart) > 0)
                            .Where(p => p.timestamp.CompareTo(rangeStop) < 0)
                            .OrderBy(p => p.timestamp)
                            .ToList();

                        if (detections.Count() != 0)
                        {
                            // add a 'stop' just after the newest detection
                            succDetection = detections.Last();
                            succDetection.timestamp = succDetection.timestamp.AddMilliseconds(+1);
                            message_log = "Added due to subsequent checks";
                        } else
                        {
                            // there are no detections, add a 'stop' just after the 'start' 
                            succDetection.timestamp = query[i].Timestamp.AddMilliseconds(+1);
                            message_log = "Added due to subsequent checks (no detections)";
                        }

                        Log log = new Log(succDetection.timestamp, configuration, MessageType.engine_stop, message_log);
                        log.Insert();
                        query.Insert(i+1, log);
                    }
                }
            }
        }


        /* Nicolò:
         * retrieve a record from the Log table, given the time of its occurence
         * it represents an event that occours while the application is running
         */
        public Log ReadLog(Configuration configuration, DateTime timestamp)
        {
            // retrieve the log messages referred to the requested Configuration at the give Timestamp
            using (DBmodel db = new DBmodel())
            {
                var query = db.log
                    .Where(l => l.configuration.Equals(configuration.ConfigurationID))
                    .Where(l => l.timestamp.Equals(timestamp))
                    .Select(l => new Log(l.timestamp, l.configuration, l.type, l.number_boards, l.message));

                if (query.Count() > 1)
                {
                    throw new Exception("Multiple items, in table 'Log' share the same timestamp for the same configuration\n");
                }
                if (query.Count() < 0)
                {
                    return null;
                }
                return query.First();
            }
        }



        // ~-----internal functions--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- 

        private static DateTime RetrieveBegin(Configuration conf)
        {
            using (DBmodel db = new DBmodel())
            {
                // the configuration has been modified 
                var firstUpdate = db.log
                    .Where(l => l.configuration.Equals(conf.ConfigurationID))
                    .Where(l => l.type.Equals(Log.MessageType.configuration_update.ToString()))
                    .Where(l => l.message.ToLower().Contains("Updated configuration".ToLower()))
                    .OrderBy(l => l.timestamp);

                if (firstUpdate.Count() > 0)
                    return firstUpdate.First().timestamp;

                // the configuration has no modification 
                var firstStart = db.log
                    .Where(l => l.configuration.Equals(conf.ConfigurationID))
                    .Where(l => l.message.Contains("Inserted configuration"))
                    .OrderBy(l => l.timestamp);

                if (firstStart.Count() > 0)
                    return firstStart.First().timestamp;
            }

            // in case of error return the 1970 
            return new DateTime();
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
        internal MessageType Type
        {
            get => type;
            set => type = value;
        }
        public int NumberBoards
        {
            get => numberBoards;
            set => numberBoards = value;
        }
        public string Message
        {
            get => message;
            set => message = value;
        }



        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return base.ToString();
        }

    }
}
