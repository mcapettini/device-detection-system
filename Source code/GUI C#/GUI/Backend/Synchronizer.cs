using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using GUI.Backend.Database;
using System.Threading;
using System.Windows;

namespace GUI.Backend
{
    // ~-----delegates--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // error handling
    public delegate void DllErrorCallback (string type, string message);
    public delegate void GuiErrorCallback (Synchronizer.Reason type, string message);

    // setup handling
    public delegate void DllSetupCallback (string MAC_boards);
    public delegate void GuiSetupCallback (Configuration conf);


    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * is a tool to handle the Collector (Socket+Aggregator+Interpolator) written in C++
     * used to start/stop the engine and retrieve eventual errors
     */
    public static class Synchronizer
    {
        // ~-----enumeratives-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public enum Reason
        {
            NoError,
            BadConfiguration,
            DataStructureCollapsed,
            ThreadStopped,
            UnreachableDatabase,
            AggregatorError,
            InterpolatorError,
            SocketError,
            WinsockBadInitialization,
            MemoryError,
            Unrecognized
            //TODO add more
        }

        public enum Lead
        {
            setting_up,
            running,
            alt,
        };


        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // target
        private static Configuration targetConfiguration    = new Configuration("");
        private static Lead status                          = Lead.alt;
        private static Hotspot hotspot                      = new Hotspot();

        // error handling
        private static Reason           errorType           = Reason.NoError;
        private static string           message             = "";
        private static DllErrorCallback dllErrorCallback    = (string type, string message) => DllErrorPublish(type, message);
        private static GuiErrorCallback guiErrorCallback    = null;
        private static Object           _lock               = new Object();
        private static Boolean          errorNotified       = false;

        // automatic setup
        private static DllSetupCallback dllSetupCallback    = (string list_boards) => DllSetupPublish(list_boards);
        private static GuiSetupCallback guiSetupCallback    = null;



        // ~-----DLL methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * used by the C# GUI to start the entire engine:
         *    - boards sending data
         *    - socket module synchronizing boards and forwarding data
         *    - aggregator module grouping together detection from different boards
         *    - interpolator module computing the position the detected devices
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void start_engine([MarshalAs(UnmanagedType.LPStr)]String configuration_id, [MarshalAs(UnmanagedType.I4)]int number_boards, [MarshalAs(UnmanagedType.LPStr)]String boards_info, DllErrorCallback callback);


        /* Nicolò:
         * used by the C# GUI to stop the entire engine (due to some problem or setting changes):
         *    - shut down the boards
         *    - join the threads
         *    - empty the data structures queue
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void stop_engine([MarshalAs(UnmanagedType.LPStr)]String configuration_id);


        /* Nicolò:
         * allow the the C# GUI to retrieve the specific of an error (type (enum) and message (string))
         * C++ -> C#
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern int retrieve_error([MarshalAs(UnmanagedType.LPStr)]String configuration_id, byte[] return_value, int available_len);


        /* Nicolò:
         * specify the delegate used to handle a failure in the DLL engine
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void error_handling(DllErrorCallback callback);

        /* Nicolò:
         * used by the C# GUI to automatically setup the current configuration (i.e. all the present boards)
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void start_setup(DllSetupCallback setup_handling_delegate, DllErrorCallback error_handling_delegate);

        /* Nicolò:
         * used by the C# GUI to stop the automatic setup
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void stop_setup();

        /* Nicolò:
         * used by the C# GUI to blink a specific board
         * C# -> C++
         */
        [DllImport(".\\CollectorCpp\\Collector.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern void blink([MarshalAs(UnmanagedType.LPStr)]String MAC_board);



        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * used to start the computation of the C++ software module (by DLL)
         */
        public static void StartEngine(Configuration configuration, GuiErrorCallback errorHandlingDelegate)
        {
            // local variables
            string boardsInfo = "";

            // check number of boards
            if (configuration.BoardCount() < 2)
                throw new Exception("Impossible to start a configuration having less than 2 listening boards");

            // check for existing and running configuration
            if ( targetConfiguration == null )
                targetConfiguration = new Configuration("");
            if ( status == Lead.running )
            {
                if ( targetConfiguration.ConfigurationID.Equals("") )
                    throw new Exception("The previous target configuration was cleared, but its status is still '" + status.ToString() + "'");
                else
                    throw new Exception("Requested '" + configuration.ConfigurationID + "' configuration, but the '" + targetConfiguration.ConfigurationID + "' one was already running");
            }

            // set the target configuration and its status
            targetConfiguration = configuration;
            status              = Lead.setting_up;

            // concatenate board information
            foreach (Board b in configuration.Boards)
            {
                boardsInfo += b.BoardID + "|" + b.X + "|" + b.Y + "_";
            }
            boardsInfo = boardsInfo.Substring(0, boardsInfo.Length - 1);

            // set the DLL callback
            //dllErrorCallback = (string type, string message) => DllErrorPublish(type, message);
            Synchronizer.error_handling(dllErrorCallback);  //set in the C++
            Synchronizer.guiErrorCallback = errorHandlingDelegate;
            Log.InsertLog(configuration, Log.MessageType.configuration_update, "Changed the delegate that handles the errors");

            // clear the previously occurred errors
            message     = "";
            errorType   = Reason.NoError;

            // activate WiFi access point
            hotspot.start();

            // actually start the C++ Collector engine
            try
            {
                start_engine(configuration.ConfigurationID, configuration.BoardCount(), boardsInfo, dllErrorCallback);
            }
            catch (Exception e)
            {
                Synchronizer.errorType = Reason.BadConfiguration;
                Synchronizer.message = e.Message;
                StopEngine(targetConfiguration);
                throw;  // re-throw the same exception
            }

            // update the targetConfiguration status
            status = Lead.running;

            // notify the starting of the engine, by inserting in the 'log' table
            Log.InsertLog(configuration, Log.MessageType.engine_start, "Started the configuration '" + configuration.ConfigurationID + "'");
        }


        /* Nicolò:
         * used to stop the computation of the C++ software module (by DLL)
         */
        public static void StopEngine(Configuration configuration)
        {
            // check for existing and running configuration
            if (status != Lead.running)
            {
                if ( !targetConfiguration.ConfigurationID.Equals("")
                    && !targetConfiguration.ConfigurationID.Equals(configuration.ConfigurationID) )
                {
                    // stored a configuration different from the requested one
                    throw new Exception("Requested '" + configuration.ConfigurationID + "' configuration, but the previous one was '" + targetConfiguration.ConfigurationID + "'");
                }
                else
                {
                    // stored configuration is already stopped
                    Console.WriteLine("Configuration '" + configuration.ConfigurationID + "' already stopped: request ignored");
                    return;
                }
            }
            else if ( !targetConfiguration.ConfigurationID.Equals(configuration.ConfigurationID) )
            {
                throw new Exception("Requested '" + configuration.ConfigurationID + "' configuration, but the running one was '" + targetConfiguration.ConfigurationID + "'");
            }

            // actually stop the C++ Collector engine
            stop_engine(configuration.ConfigurationID);

            // switch-off WiFi access point
            hotspot.stop();

            // notify the stopping of the engine, by inserting in the 'log' table
            Log.InsertLog(configuration, Log.MessageType.engine_stop, "Stopped the configuration '" + configuration.ConfigurationID + "'");

            // remove the assigned delegate
            ErrorHandlingCallback   = null;
            errorNotified           = false;

            // update the status
            status = Lead.alt;
        }


        /* Nicolò:
         * used to retrieve the errors occourred during the computation of the C++ software module (by DLL)
         */
        public static void RetrieveError(Configuration configuration)
        {
            // local variables
            string errorInfo = "", type;
            int available_len = 500, used_len;
            byte[] buffer = new byte[available_len];

            // actually retrieve the C++ Collector error
            used_len = retrieve_error(configuration.ConfigurationID, buffer, available_len);
            if (used_len < 1)
                return;

            // cast to string
            buffer = buffer.Take(used_len).ToArray();
            errorInfo   = System.Text.Encoding.ASCII.GetString( buffer );

            // parse the retrieved string
            type    = errorInfo.Split('_').ElementAt(0);
            message = errorInfo.Split('_').ElementAt(1);

            // cast to correct enum
            if (Enum.IsDefined(typeof(Reason), type))
                errorType = (Reason) Enum.Parse(typeof(Reason), type);
            else
                errorType = Reason.Unrecognized;
        }


        /* Nicolò:
         * used to start the automatic setup (by DLL)
         */
        public static void StartSetup(GuiSetupCallback configurationListenedDelegate, GuiErrorCallback errorHandlingDelegate)
        {
            //----- check for existing auto-setup -----
            if (status == Lead.setting_up)
            {
                Console.WriteLine("Auto-setup already running: request ignored");
                return;
            }

            //----- set the DLL callback -----
            // autopresentation handling
            //dllSetupCallback = (string list_boards) => DllSetupPublish(list_boards);
            // error handling
            //dllErrorCallback = (string type, string message) => DllErrorPublish(type, message);
            Synchronizer.error_handling(dllErrorCallback);  //set in the C++

            //----- set the GUI callback -----
            // autopresentation handling
            Synchronizer.guiSetupCallback = configurationListenedDelegate;
            // error handling
            Synchronizer.guiErrorCallback = errorHandlingDelegate;
            Configuration fake_conf = new Configuration("Auto-setup");
            Log.InsertLog(fake_conf, Log.MessageType.configuration_update, "Changed the delegate that handles the errors");

            //----- update the target and status -----
            targetConfiguration = fake_conf;
            status              = Lead.setting_up;

            //----- activate WiFi access point -----
            hotspot.start();

            //----- actually start the C++ automatic setup -----
            start_setup(dllSetupCallback, dllErrorCallback);
        }

        /* Nicolò:
         * used to stop the automatic setup thread
         */
        public static void StopSetup()
        {
            //----- check for existing auto-setup -----
            if (status != Lead.setting_up)
            {
                Console.WriteLine("There are not running auto-setup: request ignored");
                return;
            }

            //----- actually stop the C++ automatic setup -----
            stop_setup();

            //----- switch-off WiFi access point -----
            hotspot.stop();

            //----- remove the assigned delegate -----
            // autopresentation handling
            guiSetupCallback        = null;
            // error handling
            ErrorHandlingCallback   = null;
            errorNotified           = false;

            //----- update the status -----
            status = Lead.alt;
        }

        /* Nicolò:
         * used to blink and identify a single board
         */
        public static void BlinkBoard(Board target_board)
        {

            // check current status
            if (status != Lead.setting_up)
            {
                Console.WriteLine("Impossible to blink board " + target_board.BoardID + " because the system is not in auto-setup mode");
                return;
            }

            // actually request the DLL to blink the board
            try
            {
                blink(target_board.BoardID);
            }
            catch (Exception)
            {
                StopSetup();
                throw;  // re-throw the same exception
            }
        }




        // ~-----internal facilities------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private static void DllErrorPublish(string type, string message)
        {
            // cast error type to corresponding enumarative
            Reason errType = Reason.NoError;
            if (Enum.IsDefined(typeof(Reason), type))
                errType = (Reason) Enum.Parse(typeof(Reason), type);
            else
                errType = Reason.Unrecognized;

            // notify error occurrence in the 'log' table
            Log.InsertLog(targetConfiguration, Log.MessageType.error_occurred, type + ": " + message);

            // handle one problem at a time
            lock (_lock)
            {
                if ( !errorNotified )
                {
                    // fill internal fields
                    Synchronizer.errorType = errType;
                    Synchronizer.message = message;

                    // change status
                    errorNotified = true;

                    // invoke the error handling callbacks (through the dispatcher)
                    if (guiErrorCallback == null)
                        return;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        guiErrorCallback(ErrorType, Message);  // front-end defined method
                    }));
                    
                    // notify error handling in the 'log' table
                    Log.InsertLog(targetConfiguration, Log.MessageType.error_handled,
                        "With method " + guiErrorCallback.Target + "." + guiErrorCallback.Method.Name + "() -> " + type + ": " + message);

                } else
                {
                    // notify error discarding in the 'log' table
                    Log.InsertLog(targetConfiguration, Log.MessageType.error_ignored, type + ": " + message);
                }
            }
        }

        private static void DllSetupPublish(string MAC_boards)
        {
            // local variables
            List<Coordinates> defaultLocations;
            string[] split;
            Configuration output = new Configuration("");

            // unmarshall serialized MACs and create configuration
            split = MAC_boards.Split('_');
            defaultLocations = Coordinates.EquispacedPoints( split.Count() );
            for (int i = 0; i < split.Count(); i++)
            {
                Board b = new Board(split[i], defaultLocations[i]);
                output.BoardAdd(b);
            }

            // invoke the setup handling callbacks (through the dispatcher)
            if (guiSetupCallback == null)
                return;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                guiSetupCallback(output);  // front-end defined method
            }));
        }


        // ~-----getters and setters------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        internal static Reason ErrorType
        {
            get => errorType;
        }

        public static string Message
        {
            get => message;
        }

        public static GuiErrorCallback ErrorHandlingCallback
        {
            get => guiErrorCallback;
            set
            {
                // check for running configurations
                if ( targetConfiguration.ConfigurationID.Equals("") || status == Lead.alt )
                    throw new Exception("Forbidden to act on a not running configuration, invoke StartEngine() first");

                // update the interesting callback
                guiErrorCallback += value; //outside methods

                // notify the updating of the delegate, by inserting in the 'log' table
                Log.InsertLog(targetConfiguration, Log.MessageType.configuration_update, "Changed the delegate that handles the errors");
            }
        }
    }
}
