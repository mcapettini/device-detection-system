/*
 * Author: Fabio carfì
 * Purpose: This window is used to display all the requested information about the devices detected
 *          inside the area defined in the Setup window.   
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GUI.Backend;
using GUI.Frontend;
using LiveCharts;                   //those three library are mandatory
using LiveCharts.Wpf;               //for all the operations that we have to do
using LiveCharts.Defaults;          //on the chart

namespace GUI.Frontend
{
    public partial class Statistic_Window : Window
    {
        //--------------------- ATTRIBUTES --------------------------------------------------------------------------------------
        private Boolean no_detection;
        private Boolean DM_realtime;
        private Boolean CoT_realtime;
        private Features.Window_Mode opening_mode;
        private Features.Detection_Status detection_mode;
        private string current_view;
        private Configuration configuration;
        private DateTime last_detection;
        private DispatcherTimer update_Graph;
        private DispatcherTimer update_Stats;
        private DispatcherTimer timeout;
        private List<Board> configuration_boards;
        private List<Position> devices_position;
        private List<FrequentMAC> frequentMACs;
        private Dictionary<DateTime, TimeSpan> activity_timeline;
        private DataCaching dataCaching;
        public static object _lockObject;


        //-------------------- CONSTRUCTOR --------------------------------------------------------------------------------------
        public Statistic_Window(Configuration configuration, Features.Window_Mode opening_mode)
        {
            Features.openedWindow = Features.Window_type.Statistic;
            InitializeComponent();

            no_detection = true;
            DM_realtime = true;
            CoT_realtime = true;
            this.opening_mode = opening_mode;
            this.configuration = configuration;
            this.configurationName.Text = configuration.ConfigurationID;
            update_Graph = new DispatcherTimer();
            update_Stats = new DispatcherTimer();
            timeout = new DispatcherTimer();
            configuration_boards = configuration.Boards;            
            devices_position = new List<Position>();
            frequentMACs = new List<FrequentMAC>();
            activity_timeline = new Dictionary<DateTime, TimeSpan>();
            dataCaching = new DataCaching(configuration.ConfigurationID);
            _lockObject = new object();
        }


        //----------------- INTERNAL METHODS -------------------------------------------------------------------------------------------------
        //This method is used to initialize the axes of the graph
        private void Axis_Initialize()
        {
            //Initializing the axis of the DM_Map in the Device Monitor view
            DM_Map.AxisX = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => value.ToString("N2"),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                        Step = 0.5
                    },
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this
                }
            };
            DM_Map.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => value.ToString("N2"),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                        Step = 0.5
                    },
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this
                }
            };

            //Initializing the axis of the CoT_Map in the Counting over Time view
            CoT_Map.AxisX = new AxesCollection
            {
                new Axis
                {
                    MinValue = this.activity_timeline.OrderBy(t => t.Key).First().Key.Ticks,
                    LabelFormatter = value => new DateTime((long) value).ToString(System.Globalization.CultureInfo.CurrentCulture),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                    },
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this,
                }
            };
            CoT_Map.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => ((int)value).ToString(),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                        Step = 5
                    },
                    MinValue = 0,
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this
                }
            };

            //Initializing the axis of the FM_Map in the Frequent MACs view
            FM_Map.AxisX = new AxesCollection
            {
                new Axis
                {
                    MinValue = this.activity_timeline.OrderBy(t => t.Key).First().Key.Ticks,
                    LabelFormatter = value => new DateTime((long) value).ToString(System.Globalization.CultureInfo.CurrentCulture),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                    },
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this
                }
            };
            FM_Map.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => ((int) value).ToString(),
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb((byte)64,(byte)64,(byte)64)),
                        Step = 1
                    },
                    MinValue = 0,
                    MaxValue = 11,
                    Foreground = Brushes.WhiteSmoke,
                    DataContext = this
                }
            };

            return;
        }

        //This method is used to adapt  the representation of the graph to the points inserted
        private void Axis_Focus_Point()
        {
            //Select all the values of the points of the room
            var DM_Xvalues = DM_polygon.Values.GetPoints(DM_polygon).Select(value1 => value1.X);
            var DM_Yvalues = DM_polygon.Values.GetPoints(DM_polygon).Select(value1 => value1.Y);

            //Looking for the max and min for the Xs and Ys
            double max_X = DM_Xvalues.Max();
            double min_X = DM_Xvalues.Min();
            double max_Y = DM_Yvalues.Max();
            double min_Y = DM_Yvalues.Min();
            
            //Define the range of view of the graph
            DM_Map.AxisX[0].MinValue = min_X - 0.5;
            DM_Map.AxisX[0].MaxValue = max_X + 0.5;
            DM_Map.AxisY[0].MinValue = min_Y - 0.5;
            DM_Map.AxisY[0].MaxValue = max_Y + 0.5;

            return;
        }

        //This method initialize the LineSeries where will be added the points inserted by the user
        private void LineSeries_Initialize()
        {
            //This is the initialization of the serie reguarding the boards polygon
            DM_polygon.DataContext = this;
            DM_polygon.Values = new ChartValues<ObservablePoint>();
            DM_polygon.LabelPoint = value =>
            {
                int index;

                if (value.Key < DM_polygon.Values.Count - 1)
                    index = value.Key;
                else
                    index = 0;

                return configuration_boards[index].BoardID + "\n" +                               //MAC
                       "(" + value.X.ToString("N2") + " ; " + value.Y.ToString("N2") + ")";       //Coordintes
            };

            //This is the initialization of the serie reguarding the devices position
            try
            {
                DM_positions.DataContext = this;
                DM_positions.Values = new ChartValues<ObservablePoint>();
                DM_positions.LabelPoint = value =>
                {
                    int index = value.Key;
                    DateTime timestamp = devices_position[index].Timestamp;
                    Device device = devices_position[index].Device;
                    TimeSpan active;
                    String message;

                    try
                    {
                        active = device.ContinuativePresence_Last(timestamp);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine(ex.Message);
                        throw new Exception("Error during the connection with the DB");
                    }

                    message = Statistic_Auxiliary.Retrieve_Active_Time(active);

                    return device.DeviceID + "\n" +                                                      //MAC
                           timestamp.ToString(System.Globalization.CultureInfo.CurrentCulture) + "\n" +  //Timestamp
                           message + "\n" +                                                              //Timespan of activity
                           "(" + value.X.ToString("N2") + " ; " + value.Y.ToString("N2") + ")";          //Coordinates
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                Synchronizer.StopEngine(configuration);
            }

            //This is the initialization of the serie reguarding the counting over time graph
            CoT_devices.DataContext = this;
            CoT_devices.Values = new ChartValues<DateTimePoint>();
            CoT_devices.LabelPoint = value =>
            {
                return "Detected " + ((int)value.Y).ToString() + " devices\n" +                                //Number of Devices
                       new DateTime((long)value.X).ToString(System.Globalization.CultureInfo.CurrentCulture);  //Timestamp
            };

            return;
        }

        //This method admit to open a new main window, closing the actual one
        private void BackToMain()
        {
            this.Hide();

            MainWindow newMainWindow = new MainWindow();
            newMainWindow.Show();

            this.Close();
        }

        //This method is used to stop all the timers in order to close the statistic window in a proper way
        private void StopAllTimers()
        {
            update_Graph.Stop();
            update_Graph.Tick -= Update_Graph_handler;
            update_Stats.Stop();
            update_Stats.Tick -= Update_Stats_handler;
            timeout.Stop();
            timeout.Tick -= Timeout_handler;
        }

        //This method active the 2 method needed for the updates
        private void ActiveUpdateTimers()
        {
            update_Graph.Tick += Update_Graph_handler;
            update_Graph.Interval = new TimeSpan(0, 0, Features.delayUpdateGraphs);
            update_Graph.Start();
            update_Stats.Tick += Update_Stats_handler;
            update_Stats.Interval = new TimeSpan(0, Features.delayUpdateStats, 0);
            update_Stats.Start();
        }

        //This method do all the operations needed in case of c++ errors
        private void Handle_Cpp_Error(Synchronizer.Reason type, string errorMessage)
        {
            lock (_lockObject)
            {
                if (Features.openedWindow.Equals(Features.Window_type.Statistic))
                {
                    Synchronizer.StopEngine(configuration);

                    //Here I stop all the timers usedd for updates
                    this.StopAllTimers();

                    detection_mode = Features.Detection_Status.Stopped;
                    this.detection_status.Text = detection_mode.ToString();
                    this.detection_status.Foreground = Brushes.Red;

                    MessageBoxResult result = MessageBox.Show("We are sorry, something went wrong!\n" +
                           "Do you want to go back to main?", "Reconnection to database", MessageBoxButton.YesNo);

                    if (result.Equals(MessageBoxResult.No))
                    {
                        this.Close();
                        Environment.Exit(-1);
                    }
                    else
                    {
                        this.BackToMain();
                    }
                }
            }
        }

        //This is the method that have to be called when there is a DB error connection
        public void Handle_DB_Error()
        {
            //Critical section before closing this window after a DB error
            lock (_lockObject)
            {
                Synchronizer.StopEngine(configuration);
                this.StopAllTimers();

                //Here I change the window status label and layout
                detection_mode = Features.Detection_Status.Stopped;
                this.detection_status.Text = detection_mode.ToString();
                this.detection_status.Foreground = Brushes.Red;
            }

            if (Features.openedWindow.Equals(Features.Window_type.Statistic))
            {
                MessageBoxResult result = MessageBox.Show("We are sorry!\n" +
                       "Something went wrong with the connection with the database!\n" +
                       "Do you want to try to re-connect to the database?", "Reconnection to database", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                {
                    this.Close();
                    Environment.Exit(-1);
                }
                else
                {
                    this.BackToMain();
                }
            }
        }


        //----------------- EVENTS HANDLING -------------------------------------------------------------------------------------
        //This event manage the click on the toolbar and admit to move the window
        private void MoveWindow_Event(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

            return;
        }

        //This method manage the click on the resize button
        private void ResizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        //This method manage the click on the exit button
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Synchronizer.StopEngine(configuration);
            Environment.Exit(0);
        }

        //This method initialize the entire statistic window
        private void OnLoadEvent(object sender, RoutedEventArgs e)
        {
            try
            {
                Synchronizer.StartEngine(configuration, Handle_Cpp_Error);

                //Changing the detection mode and representation
                detection_mode = Features.Detection_Status.Running;
                this.detection_status.Text = detection_mode.ToString();
                this.detection_status.Foreground = Brushes.Green;
                no_detection = false;

                //Setting the default current view at start
                current_view = this.device_distribution.Name;

                //Store the activity timeline
                activity_timeline = Log.GetEngineActivityTimeline(configuration, true);
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException dex)
            {
                Console.WriteLine(dex.StackTrace);
                Console.WriteLine(dex.Message);
                Handle_DB_Error();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                
                //Here I stop the engine of the C++ code
                Synchronizer.StopEngine(configuration);
                MessageBox.Show("An unknown error occur during the initialization of the statistic window!\n" +
                                "The application will be closed!");
                Environment.Exit(-1);
            }

            //Initialization of all the axis and series of the views
            Axis_Initialize();
            LineSeries_Initialize();
            
            //Here I compute the last activity range
            KeyValuePair<DateTime, TimeSpan> lastActivityRange = activity_timeline.OrderByDescending(t => t.Key).First();

            //Initialize the device monitoring representation
            for (int i = 0; i < configuration_boards.Count; i++)
                DM_polygon.Values.Add(new ObservablePoint(configuration_boards[i].X, configuration_boards[i].Y));
            if (configuration_boards.Count > 2)
                DM_polygon.Values.Add(new ObservablePoint(configuration_boards[0].X, configuration_boards[0].Y));
            Axis_Focus_Point();
            
            //Only if the mode is Modify or Load we display the previous data detected
            if (opening_mode == Features.Window_Mode.Load || opening_mode == Features.Window_Mode.Modify)
            {
                DateTime lastActivity = activity_timeline.OrderBy(t => t.Key).Last().Key;
                foreach (KeyValuePair<DateTime, TimeSpan> tuple in activity_timeline.OrderBy(t => t.Key))
                {
                    //Initialize all the sliders in the representations
                    DM_slider.Maximum += tuple.Value.TotalSeconds;
                    CoT_rangeSlider.Maximum += tuple.Value.TotalSeconds;
                    FM_rangeSlider.Maximum += tuple.Value.TotalSeconds;
                    
                    //Initialize the counting over time representation
                    if (tuple.Key != lastActivity && tuple.Value.TotalSeconds >= Features.delayUpdateGraphs)
                    {
                        int iteration = (int)tuple.Value.TotalSeconds / Features.delayUpdateGraphs;
                        for (int i = 1; i <= iteration; i++)
                        {
                            try
                            {
                                last_detection = tuple.Key.AddSeconds(i * Features.delayUpdateGraphs);
                                devices_position = dataCaching.LastPosition_List(last_detection);
                                CoT_devices.Values.Add(new DateTimePoint(last_detection, devices_position.Count));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.StackTrace);
                                Console.WriteLine(ex.Message);
                                Handle_DB_Error();
                            }
                        }
                        CoT_devices.Values.Add(new DateTimePoint(last_detection, double.NaN));
                    }
                }

                //Updating the tick frequency of the sliders
                DM_slider.TickFrequency = Features.percentageUpgradeSteps * DM_slider.Maximum;
                CoT_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * CoT_rangeSlider.Maximum;
                FM_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * FM_rangeSlider.Maximum;

                //Here I add the splitters
                Statistic_Auxiliary.SplittersDisplay(this, activity_timeline);
                                
                //Here I initialize the statistics at startup
                try
                {
                    //Here I generate the data needed for update the 3 statistics
                    int totalDistinctMACs, local_MACs, distinct_MACs, percentage_MACs;

                    totalDistinctMACs = dataCaching.TotalMAC_DistinctNumber();
                    local_MACs = dataCaching.LocalMAC_Number();
                    distinct_MACs = dataCaching.LocalMAC_DistinctNumber();
                    if (local_MACs == 0)
                        percentage_MACs = 0;
                    else
                        percentage_MACs = (int)(distinct_MACs * 100 / local_MACs);

                    //Here I update the text of the labels showing the statistics
                    this.total_distinct_MACs.Text = totalDistinctMACs.ToString();
                    this.local_MACs.Text = local_MACs.ToString();
                    this.distinct_MACs.Text = distinct_MACs.ToString();
                    this.percentage_local_MACs.Text = percentage_MACs.ToString() + " %";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    Handle_DB_Error();
                }
            }
            else
            {
                //Initialize the date of the representations
                double max = activity_timeline.Sum(t => t.Value.TotalSeconds);
                DM_slider.Maximum = max;
                CoT_rangeSlider.Maximum = max;
                FM_rangeSlider.Maximum = max;

                //Here I update the Min and Max values of the Axis of the CoT graph at startup
                Statistic_Auxiliary.UpdateCoTAxisConfiguration(this, activity_timeline);
            }

            //Here I update DM_slider thumb
            DM_slider.Value = DM_slider.Maximum;

            //Here I update the CoT and FM slider values
            double sum = activity_timeline.Sum(x => x.Value.TotalSeconds);
            CoT_rangeSlider.HigherValue = CoT_rangeSlider.Maximum;
            CoT_rangeSlider.LowerValue = sum - lastActivityRange.Value.TotalSeconds;

            DateTime date_min = Statistic_Auxiliary.Translate_Slider_Value(FM_rangeSlider.LowerValue, activity_timeline);
            DateTime date_max = Statistic_Auxiliary.Translate_Slider_Value(FM_rangeSlider.HigherValue, activity_timeline);
            FM_rangeSlider.HigherValue = FM_rangeSlider.Maximum;
            FM_rangeSlider.LowerValue = sum - lastActivityRange.Value.TotalSeconds;
            FM_date_start.Text = date_min.ToString();
            FM_date_end.Text = date_max.ToString();

            last_detection = DateTime.Now;

            //Set the operation for the first update graph event call
            this.ActiveUpdateTimers();

            return;
        }

        //Managing the resize of the window
        private void ResizedWindow_Event(object sender, EventArgs e)
        {
            Statistic_Auxiliary.SplittersDisplay(this, activity_timeline);
        }

        //This method handle the event raised by the timer setted for the update of the grid views
        private void Update_Graph_handler(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            //Here I update all the sliders
            DM_slider.Maximum += now.Subtract(last_detection).TotalSeconds;
            CoT_rangeSlider.Maximum += now.Subtract(last_detection).TotalSeconds;
            FM_rangeSlider.Maximum += now.Subtract(last_detection).TotalSeconds;

            //Updating the tick frequency of the sliders
            DM_slider.TickFrequency = Features.percentageUpgradeSteps * DM_slider.Maximum;
            CoT_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * CoT_rangeSlider.Maximum;
            FM_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * FM_rangeSlider.Maximum;

            //Here I update the timespan of the last entry of the activity timeline dictionary
            KeyValuePair<DateTime, TimeSpan> lastActivityRange = activity_timeline.OrderByDescending(t => t.Key).First();
            DateTime lastActivityDate = lastActivityRange.Key;
            activity_timeline[lastActivityDate] = now.Subtract(lastActivityDate);

            //Here I update the splitters
            Statistic_Auxiliary.SplittersDisplay(this, activity_timeline);

            //Here I save the last detection of reference
            last_detection = DateTime.Now;

            //Here I update the slider of the DM representation
            if (DM_realtime)
                DM_slider.Value = DM_slider.Maximum;

            //Here I add some data to the counting over time graph
            try
            {
                devices_position = dataCaching.LastPosition_List(last_detection);
                CoT_devices.Values.Add(new DateTimePoint(last_detection, devices_position.Count));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                Handle_DB_Error();
            }

            //Here I update the slider of the CoT representation
            if (CoT_realtime)
            {
                CoT_rangeSlider.HigherValue = CoT_rangeSlider.Maximum;
                if (activity_timeline[lastActivityDate].TotalSeconds < Features.MaxMinDifferenceRange)
                    CoT_rangeSlider.LowerValue = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - activity_timeline[lastActivityDate].TotalSeconds;
                else
                    CoT_rangeSlider.LowerValue = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - Features.MaxMinDifferenceRange;
            }
            
            //At the end of all operations this handler set again the timer after what to retrive the new data
            update_Graph.Interval = new TimeSpan(0, 0, Features.delayUpdateGraphs);
            update_Graph.Start();
        }

        //This mehod handle the update statistics event raised by the update_Stats timer
        private void Update_Stats_handler(object sender, EventArgs e)
        {
            int totalDistinctMACs, local_MACs, distinct_MACs, percentage_MACs;

            try
            {
                //Here I generate the data needed for update the 3 statistics
                totalDistinctMACs = dataCaching.TotalMAC_DistinctNumber();
                local_MACs = dataCaching.LocalMAC_Number();
                distinct_MACs = dataCaching.LocalMAC_DistinctNumber();
                if (local_MACs == 0)
                    percentage_MACs = 0;
                else
                    percentage_MACs = (int)(distinct_MACs * 100 / local_MACs);

                //Here I update the text of the labels showing the statistics
                this.total_distinct_MACs.Text = totalDistinctMACs.ToString();
                this.local_MACs.Text = local_MACs.ToString();
                this.distinct_MACs.Text = distinct_MACs.ToString();
                this.percentage_local_MACs.Text = percentage_MACs.ToString() + " %";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                Handle_DB_Error();
            }

            update_Stats.Interval = new TimeSpan(0, Features.delayUpdateStats, 0);
            update_Stats.Start();
        }

        //This method handle the timeout event raised by the timer setted by the stop detection button click
        private void Timeout_handler(object sender, EventArgs e)
        {
            //Update of the detection status
            detection_mode = Features.Detection_Status.Stopped;
            this.detection_status.Text = detection_mode.ToString();
            this.detection_status.Foreground = Brushes.Red;

            //Clear the DM representation
            DM_positions.Values.Clear();

            //Turn the numbers and the percentage to 0
            local_MACs.Text = "0";
            distinct_MACs.Text = "0";
            percentage_local_MACs.Text = "0 %";
        }

        //This method handle the click event on the menu buttons
        private void Buttons_Click(object sender, RoutedEventArgs e)
        {
            Button caller = sender as Button;
            string[] button_name_split = caller.Name.Split("_".ToCharArray());
            string name_new_view;

            if (caller.Name.Equals(this.frequent_MACs_button.Name) || caller.Name.Equals(this.device_distribution_button.Name))
                name_new_view = button_name_split[0] + "_" + button_name_split[1];
            else
                name_new_view = button_name_split[0] + "_" + button_name_split[1] + "_" + button_name_split[2];

            if (current_view != name_new_view)
            {
                Grid oldView = this.different_views.Children.OfType<Grid>().First(g => g.Visibility == Visibility.Visible);
                Grid newView = this.different_views.Children.OfType<Grid>().First(g => g.Name.Equals(name_new_view));

                var normalStyle = (Style)this.button_menu.Resources["Buttons"];
                var pressedStyle = (Style)this.button_menu.Resources["pressedButton"];
                if (caller.Name.Equals("device_distribution_button"))
                {
                    caller.Style = pressedStyle;
                    this.counting_over_time_button.Style = normalStyle;
                    this.frequent_MACs_button.Style = normalStyle;
                }
                else if (caller.Name.Equals("counting_over_time_button"))
                {
                    this.device_distribution_button.Style = normalStyle;
                    caller.Style = pressedStyle;
                    this.frequent_MACs_button.Style = normalStyle;
                }
                else
                {
                    this.device_distribution_button.Style = normalStyle;
                    this.counting_over_time_button.Style = normalStyle;
                    caller.Style = pressedStyle;
                }

                //Here i change the visibility of the 2 views and update the current view displayed
                oldView.Visibility = Visibility.Hidden;
                newView.Visibility = Visibility.Visible;
                current_view = newView.Name;
            }

            return;
        }

        //This event manage the change value of the DM_slider, to change the view of the DM_graph
        private void DM_slider_ValueChanged_Event(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue.Equals(e.OldValue))
                return;

            //Here I update the view of the DM_graph
            try
            {
                DateTime dateOfInterest = Statistic_Auxiliary.Translate_Slider_Value(DM_slider.Value, activity_timeline);

                //Update the labels of the time
                this.DM_date.Text = dateOfInterest.ToString();

                //Clear the 2 list of positions
                devices_position.Clear();
                DM_positions.Values.Clear();

                //Re-update the list of values  and the graph
                devices_position = dataCaching.LastPosition_List(dateOfInterest);
                DM_numberOfDevices.Text = devices_position.Count.ToString();
                foreach (Position p in devices_position)
                    DM_positions.Values.Add(new ObservablePoint(p.Coordinates.X, p.Coordinates.Y));
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException dex)
            {
                Console.WriteLine(dex.StackTrace);
                Console.WriteLine(dex.Message);
                Handle_DB_Error();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                Synchronizer.StopEngine(configuration);
                if(Features.openedWindow.Equals(Features.Window_type.Statistic))
                    this.BackToMain();
            }

            //Here I update the real time variable to understand if I'm or not in real time mode
            if(DM_slider.Value == DM_slider.Maximum)
                DM_realtime = true;
            else
                DM_realtime = false;

            return;
        }

        //This method manage the event raised if the user move the higher thumb of the CoT range slider
        private void CoT_RangeSlider_ValueChanged_Event(object sender, RoutedEventArgs e)
        {
            //Here I update the axis max values
            try
            {
                Statistic_Auxiliary.UpdateCoTAxisConfiguration(this, activity_timeline);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                Synchronizer.StopEngine(configuration);
                if(Features.openedWindow.Equals(Features.Window_type.Statistic))
                    this.BackToMain();
            }

            //Here I compute the last activity range
            KeyValuePair<DateTime, TimeSpan> lastActivityRange = activity_timeline.OrderByDescending(t => t.Key).First();
            
            //Here I check the usability or not of the radio button
            if (CoT_rangeSlider.HigherValue == CoT_rangeSlider.Maximum)
            {
                double lowerValue_realTime;
                if (lastActivityRange.Value.TotalSeconds < Features.MaxMinDifferenceRange)
                    lowerValue_realTime = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - lastActivityRange.Value.TotalSeconds;
                else
                    lowerValue_realTime = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - Features.MaxMinDifferenceRange;

                if (CoT_rangeSlider.LowerValue >= lowerValue_realTime - 20)
                    CoT_realtime = true;
                else
                    CoT_realtime = false;
            }    
            else
                CoT_realtime = false;

            return;
        }
        
        //This event manage the change value of th lower and higher value of the FM_rangeSlider, to change the view of the FM_graph
        private void FM_rangeSlider_ValueChanged_Event(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime min = Statistic_Auxiliary.Translate_Slider_Value(FM_rangeSlider.LowerValue, activity_timeline);
                DateTime max = Statistic_Auxiliary.Translate_Slider_Value(FM_rangeSlider.HigherValue, activity_timeline);
               
                //Updated the date labels
                this.FM_date_start.Text = min.ToString();
                this.FM_date_end.Text = max.ToString();

                //Set the mouse in a wait statud
                FM_rangeSlider.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                
                //Updating the number of continuative presence
                String display_value = Device.ContinuativeDevices_Count(configuration, min, max.Subtract(min)).ToString();
                FM_numberContinuativePresence.Text = display_value;

                //Retriving the new list of MACs to display
                frequentMACs = Statistic_Auxiliary.RetriveNewFrequentMACs(frequentMACs, configuration, min, max.Subtract(min));

                //Updating the FM_graph
                Statistic_Auxiliary.DisplayNewFrequentMACs(this, frequentMACs);

                //Updating the min and max values of the x axis of FM graph
                FM_Map.AxisX[0].MaxValue = max.Ticks + 30000;
                FM_Map.AxisX[0].MinValue = min.Ticks - 30000;

                //Turn back to the normal status of the mouse
                Mouse.OverrideCursor = null;
                FM_rangeSlider.IsEnabled = true;
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException dex)
            {
                Console.WriteLine(dex.StackTrace);
                Console.WriteLine(dex.Message);

                Handle_DB_Error();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                Synchronizer.StopEngine(configuration);
                if (Features.openedWindow.Equals(Features.Window_type.Statistic))
                    this.BackToMain();
            }

            return;
        }

        //This event manage the event to turn the DM graph in the real time mode
        private void DM_RealTimeButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (!DM_realtime)
            {
                DM_realtime = true;

                //Here I update the view of the DM graph, raising the slider event
                DM_slider.Value = DM_slider.Maximum;
            }

            return;
        }

        //This event manage the event to turn the CoT graph in the real time mode
        private void CoT_RealTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CoT_realtime)
            {
                CoT_realtime = true;

                //Here I compute the last activity time range
                KeyValuePair<DateTime, TimeSpan> lastActivityRange = activity_timeline.OrderByDescending(t => t.Key).First();

                //Here I update the view of the CoT graph
                CoT_rangeSlider.HigherValue = CoT_rangeSlider.Maximum;
                if (lastActivityRange.Value.TotalSeconds < Features.MaxMinDifferenceRange)
                    CoT_rangeSlider.LowerValue = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - lastActivityRange.Value.TotalSeconds;
                else
                    CoT_rangeSlider.LowerValue = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - Features.MaxMinDifferenceRange;
            }

            return;
        }

        //This method handle the click event on the Stop Detection button
        private void Stop_Detection_Button_Click(object sender, RoutedEventArgs e)
        {
            //Critical Section that have to be executed atomically
            lock (_lockObject)
            {
                //Instantly stop the execution of the C++ code
                Synchronizer.StopEngine(configuration);

                //Stopping the timer needed to update the graphs
                update_Graph.Stop();
                update_Graph.Tick -= Update_Graph_handler;
                update_Stats.Stop();
                update_Stats.Tick -= Update_Stats_handler;

                //Here i put a point to create a discontinues range of interval 
                CoT_devices.Values.Add(new DateTimePoint(last_detection, double.NaN));

                //Change the button visibility in the window and the status of the detection
                this.stop_detection_button.Visibility = Visibility.Hidden;
                this.restart_detection_button.Visibility = Visibility.Visible;
                no_detection = true;
                detection_mode = Features.Detection_Status.Temporarly_Stopped;
                this.detection_status.Text = detection_mode.ToString().Replace("_", " ");
                this.detection_status.Foreground = Brushes.Orange;
                
                //Starting the timer needed to understand if we are in presence of timeout
                timeout.Tick += Timeout_handler;
                timeout.Interval = new TimeSpan(0, 5, 0);
                timeout.Start();
            }
            
            return;
        }

        //This method handle the click event on the Restart Detection button
        private void Restart_Detection_Button_Click(object sender, RoutedEventArgs e)
        {
            //Critical Section that have to be executed atomically
            lock (_lockObject)
            {
                try
                {
                    //Stopping the timer needed to understand if we are in presence of timeout
                    timeout.Stop();
                    timeout.Tick -= Timeout_handler;

                    //Instantly re-starting the execution of the C++ code
                    Synchronizer.StartEngine(configuration, Handle_Cpp_Error);

                    //Change the button visibility in the window
                    this.restart_detection_button.Visibility = Visibility.Hidden;
                    this.stop_detection_button.Visibility = Visibility.Visible;
                    no_detection = false;
                    detection_mode = Features.Detection_Status.Running;
                    this.detection_status.Text = detection_mode.ToString();
                    this.detection_status.Foreground = Brushes.Green;

                    //Update the activity timeline dictionary
                    activity_timeline = Log.GetEngineActivityTimeline(configuration, true);

                    //Update the maximum of all the sliders
                    double max = activity_timeline.Sum(t => t.Value.TotalSeconds);
                    DM_slider.Maximum = max;
                    CoT_rangeSlider.Maximum = max;
                    FM_rangeSlider.Maximum = max;

                    //Updating the tick frequency of the sliders
                    DM_slider.TickFrequency = Features.percentageUpgradeSteps * DM_slider.Maximum;
                    CoT_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * CoT_rangeSlider.Maximum;
                    FM_rangeSlider.TickFrequency = Features.percentageUpgradeSteps * FM_rangeSlider.Maximum;

                    //Here I update the splitters
                    Statistic_Auxiliary.SplittersDisplay(this, activity_timeline);

                    //Check if I have to update the graphs
                    if (DM_realtime)
                        DM_slider.Value = DM_slider.Maximum;

                    if (CoT_realtime)
                    {
                        //Here I compute the last activity range
                        KeyValuePair<DateTime, TimeSpan> lastActivityRange = activity_timeline.OrderByDescending(t => t.Key).First();

                        //Here I update the CoT slider values, without the raising of the event
                        CoT_rangeSlider.HigherValue = CoT_rangeSlider.Maximum;
                        CoT_rangeSlider.LowerValue = activity_timeline.Values.Sum(cot => cot.TotalSeconds) - lastActivityRange.Value.TotalSeconds;
                    }
                    
                    //Here I set the new last detection to the period of restart detection
                    last_detection = DateTime.Now;

                    //Re-starting the timer needed to update the graphs
                    this.ActiveUpdateTimers();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    Handle_DB_Error();
                }
            }
            
            return;
        }

        //This method handle the click event on the Close button
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            if (no_detection)
            {
                //Stopping all the timers to avoid any possible event call
                this.StopAllTimers();
            }
            else
            {
                MessageBoxResult question = MessageBox.Show("The application is still detecting data coming from the boards!\n" +
                                                            "Would you stop anyway the detection and go back to main?",
                                                            "Continue the detection", MessageBoxButton.YesNo);
                if (question == MessageBoxResult.Yes)
                {
                    //Critical Section that have to be executed atomically
                    lock (_lockObject)
                    {
                        //Instantly stop the C++ code and taking the error handler away before closing
                        Synchronizer.StopEngine(configuration);

                        //Change the status of the detection
                        detection_mode = Features.Detection_Status.Stopped;
                        this.detection_status.Text = detection_mode.ToString();
                        this.detection_status.Foreground = Brushes.Red;

                        //Stopping all the timers to avoid any possible event call
                        this.StopAllTimers();
                    }
                }
                else
                    return;
            }
            this.BackToMain();
        }
    }
}
