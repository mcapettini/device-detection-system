/*
 * Author: Fabio carfì
 * Purpose: This window is used to create a new configuration and add it and its information into the database
 *          It is also used in case we want to change some informtion of a specific configuration. In this case will be loaded
 *          the old one to admit the change on it.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using GUI.Backend;
using GUI.Frontend;
using LiveCharts;                   //those three library are mandatory
using LiveCharts.Wpf;               //for all the operations that we have to do
using LiveCharts.Defaults;          //on the chart

namespace GUI.Frontend
{
    public partial class Setup_Window : Window
    {
        //--------------------- ATTRIBUTES ----------------------------------------------------------------------------------------
        private Features.Window_Mode mode;
        private int countBoards;
        private Boolean call_event = false;
        private Boolean blinking;
        private List<Board> inserted_boards = new List<Board>();
        private List<BoardRow> boards = new List<BoardRow>();
        private Configuration configuration = null;


        //-------------------- PROPERTIES -----------------------------------------------------------------------------------------
        public int CountBoards
        {
            get { return countBoards; }
            set { countBoards = value; }
        }

        public List<Board> Inserted_boards
        {
            get { return inserted_boards; }
        }

        public List<BoardRow> Boards
        {
            get { return boards; }
        }

        public Boolean Call_Event
        {
            get { return call_event; }
        }

        //-------------------- CONSTRUCTOR ----------------------------------------------------------------------------------------
        //This constructor is used when we have to manage a request from the Main Window and we don't know
        //what configuration the user want to select
        public Setup_Window(Features.Window_Mode mode)
        {
            Features.openedWindow = Features.Window_type.Setup;
            InitializeComponent();

            this.mode = mode;
            if (mode != Features.Window_Mode.Create)
            {
                MessageBox.Show("An unknown mode has been specified!");
                this.insertConfiguration.IsEnabled = false;
                this.Close();
                return;
            }
            this.countBoards = 0;
            this.blinking = false;

            //Disable sava and start button but not the cancel button
            this.save_data_button.IsEnabled = false;
            this.start_button.IsEnabled = false;

            //Inserting the first 4 grid rows
            Add_some_TextBox_rows(4);

            //Initialize the boards and the starting list of boards
            boards[0].x.Text = "-1";
            boards[0].y.Text = "-1";
            boards[0].MAC.Text = "aa:aa:aa:aa:aa:aa";
            inserted_boards.Add(new Board("aa:aa:aa:aa:aa:aa", -1, -1));
            boards[1].x.Text = "1";
            boards[1].y.Text = "-1";
            boards[1].MAC.Text = "bb:bb:bb:bb:bb:bb";
            inserted_boards.Add(new Board("bb:bb:bb:bb:bb:bb", 1, -1));
            boards[2].x.Text = "1";
            boards[2].y.Text = "1";
            boards[2].MAC.Text = "cc:cc:cc:cc:cc:cc";
            inserted_boards.Add(new Board("cc:cc:cc:cc:cc:cc", 1, 1));
            boards[3].x.Text = "-1";
            boards[3].y.Text = "1";
            boards[3].MAC.Text = "dd:dd:dd:dd:dd:dd";
            inserted_boards.Add(new Board("dd:dd:dd:dd:dd:dd", -1, 1));

            //Initilize the graph properties
            Axis_Initialize();
            LineSeries_Initialize();
            polygon.Values = new ChartValues<ObservablePoint> { new ObservablePoint(-1, -1), new ObservablePoint(1, -1),
                                                                new ObservablePoint( 1 , 1), new ObservablePoint(-1, 1),
                                                                new ObservablePoint(-1, -1)};
            Axis_Focus_Point();
            
            call_event = true;
        }

        //This costructor is made for calling this window with all the specifications yet showed up, when we know the
        //configuration we will go to use
        public Setup_Window(Features.Window_Mode mode, Configuration configuration)
        {
            Features.openedWindow = Features.Window_type.Setup;
            InitializeComponent();
            
            if (mode != Features.Window_Mode.Create && mode != Features.Window_Mode.Modify)
            {
                MessageBox.Show("An unknown mode has been specified!");
                this.insertConfiguration.IsEnabled = false;
                this.Close();
                return;
            }
            else
                this.mode = mode;
            
            this.configuration = configuration;
            this.insertConfiguration.Text = configuration.ConfigurationID;
            this.countBoards = 0;
            
            if (configuration == null)
            {
                MessageBox.Show("This operation is not possible because there isn't a configuration with this name associated!");
                this.Close();
                return;
            }
            else
            {
                int numberBoards = configuration.BoardCount();

                //Control if we are in the auto-setup mode or not
                if (mode == Features.Window_Mode.Create)
                    this.blinking = true;
                else
                    this.blinking = false;
                
                //Here i initialize the graph
                Axis_Initialize();
                LineSeries_Initialize();
                
                //Inserting the needed rows' grid, initializing their elements and update the map
                Add_some_TextBox_rows(numberBoards);

                //Initializing the list of elements and displaying the points on the graph
                for (int i = 0; i < numberBoards; i++)
                {
                    Board board = configuration.BoardElementAt(i);
                    boards[i].x.Text = board.X.ToString("N2");
                    boards[i].y.Text = board.Y.ToString("N2");
                    boards[i].MAC.Text = board.BoardID;

                    inserted_boards.Add(board);
                    polygon.Values.Add(new ObservablePoint(board.X, board.Y));
                }
                if (polygon.Values.Count > Features.leastBoardNumber)
                {
                    Board start_board = configuration.BoardElementAt(0);
                    polygon.Values.Add(new ObservablePoint(start_board.X, start_board.Y));
                }
                Axis_Focus_Point();
                call_event = true;

                //Checking what button have to be enabled and what not
                this.save_data_button.IsEnabled = false;
                if (this.mode.Equals(Features.Window_Mode.Modify))
                    this.start_button.IsEnabled = true;
                else
                    this.start_button.IsEnabled = false;
            }
        }


        //----------------- INTERNAL METHODS --------------------------------------------------------------------------------------
        //Method used to add a specified number of rows of textboxes, meaning we want to add some board to the configuration
        public void Add_some_TextBox_rows(int to_add)
        {
            for(int i = 0; i < to_add; i++)
            {
                //The first board has not to have a space row before
                if (this.countBoards != 0)
                {
                    RowDefinition newSpace = new RowDefinition()
                    {
                        Height = new GridLength(25, GridUnitType.Pixel)
                    };
                    this.coordinatesGrid.RowDefinitions.Add(newSpace);
                }

                //Define the row for the new board
                RowDefinition newRow = new RowDefinition()
                {
                    Height = new GridLength(30, GridUnitType.Pixel)
                };
                this.coordinatesGrid.RowDefinitions.Add(newRow);
                this.countBoards++;

                //Initialize the new board
                BoardRow newBoard = new BoardRow(this, this.countBoards, blinking);

                //Adding the control to the Grid of the window and to the list of boards
                boards.Add(newBoard);
                this.coordinatesGrid.Children.Add(newBoard);

                //Setting the row of the board
                Grid.SetRow(newBoard, this.coordinatesGrid.RowDefinitions.Count - 1);
            }

            //Deciding what row have to have the buttons enabled and what not
            for (int i = 0; i < this.countBoards; i++)
                UpdateEnableUpDown(i);
        }

        //Method used to initialize all the textboxes in the grid
        public void Initialize_created_Textboxs()
        {
            Coordinates point_to_insert = null;
            string mac_board = string.Empty;
            BoardRow lastBoardInserted = boards[boards.Count - 1];

            //Define the indexs and generate the new coordinates
            int last_board = inserted_boards.Count - 1;
            int last_point = polygon.Values.Count - 1;
            point_to_insert = Coordinates.PointsInTheMiddle(inserted_boards[last_board].Coordinates, inserted_boards[0].Coordinates, 1)[0];
            
            //Check how many board I have inserted yet
            if(this.countBoards - 1 > Features.leastBoardNumber)
                polygon.Values.RemoveAt(last_point);
            
            //Here I update the text attribute of the texboxes' row
            call_event = false;
            lastBoardInserted.x.Text = point_to_insert.X.ToString("N2");
            lastBoardInserted.y.Text = point_to_insert.Y.ToString("N2");
            do
                mac_board = Features.Generate_Random_MAC().ToLower();
            while (inserted_boards.Select(b => b.BoardID).Contains(mac_board));
            lastBoardInserted.MAC.Text = mac_board;
            call_event = true;

            //Updating the map and the list of boards
            polygon.Values.Add(new ObservablePoint(point_to_insert.X, point_to_insert.Y));
            inserted_boards.Add(new Board(mac_board, point_to_insert.X, point_to_insert.Y));

            //Close the figure in the map
            if (this.countBoards > Features.leastBoardNumber)
                polygon.Values.Add(new ObservablePoint(inserted_boards[0].X, inserted_boards[0].Y));

            return;
        }

        //This method set the if an up and down buttons have to be or not eable
        public void UpdateEnableUpDown(int index)
        {
            //Setting up and down button enabled
            boards[index].up.IsEnabled = true;
            boards[index].down.IsEnabled = true;

            //Checking if they have to be enabled or disabled
            if (index == 0)
                boards[index].up.IsEnabled = false;
            if (index == this.countBoards - 1)
                boards[index].down.IsEnabled = false;
        }

        //This method is used to initialize the axes of the graph
        private void Axis_Initialize()
        {
            //Initialize the x and y axis of the graph
            boardsMap.AxisX = new AxesCollection
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
            boardsMap.AxisY = new AxesCollection
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

            //Inserting the x and y axis
            x_axis.Values = new ChartValues<ObservablePoint> { new ObservablePoint(1000, 0), new ObservablePoint(-1000, 0) };
            y_axis.Values = new ChartValues<ObservablePoint> { new ObservablePoint(0, 1000), new ObservablePoint(0, -1000) };
            
            return;
        }

        //This method is used to adapt  the representation of the graph to the points inserted
        public void Axis_Focus_Point()
        {
            //Select all the values of the points of the room
            var polygon_Xvalues = polygon.Values.GetPoints(polygon).Select(value1 => value1.X);
            var polygon_Yvalues = polygon.Values.GetPoints(polygon).Select(value1 => value1.Y);

            //Looking for the max and min for the Xs and Ys
            double max_X = polygon_Xvalues.Max();
            double min_X = polygon_Xvalues.Min();
            double max_Y = polygon_Yvalues.Max();
            double min_Y = polygon_Yvalues.Min();
            
            //This admit to focus the entire image, taking care of the coordinates of the boards
            boardsMap.AxisX[0].MinValue = polygon_Xvalues.Min() - 0.5;
            boardsMap.AxisX[0].MaxValue = polygon_Xvalues.Max() + 0.5;
            boardsMap.AxisY[0].MinValue = polygon_Yvalues.Min() - 0.5;
            boardsMap.AxisY[0].MaxValue = polygon_Yvalues.Max() + 0.5;
            return;
        }

        //This method initialize the LineSeries where will be added the points inserted by the user
        private void LineSeries_Initialize()
        {
            polygon.DataContext = this;

            //Initialize the list of points of the graph
            polygon.Values = new ChartValues<ObservablePoint>();

            //Defining the tooltip that have to be shown when the mouse is over a point of the graph
            polygon.LabelPoint = value =>
            {
                int index;

                if (this.countBoards == Features.leastBoardNumber)
                    index = value.Key;
                else
                {
                    if (value.Key < polygon.Values.Count - 1)
                        index = value.Key;
                    else
                        index = 0;
                }
                
                return inserted_boards[index].BoardID + "\n" + "(" + value.X.ToString("N2") + " ; " + value.Y.ToString("N2") + ")";
            };

            return;
        }

        //This is the method that have to be called when there is a DB error connection
        public void Handle_DB_Error()
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
                //this is done to have a better vision effect on the swapping windows
                this.Hide();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                //After that i really close the window
                this.Close();
                return;
            }
        }


        //---------------------- EVENTS HANDLING -------------------------------------------------------------------------------------------
        //This event manage the click on the toolbar and admit to move the window
        private void MoveWindow_Event(object sender, MouseButtonEventArgs e)
        {
            //Only if you left click on the canvas you can move the window over and over
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

            return;
        }

        //This method manage the click on the resize button
        private void ResizeButton_Click(object sender, RoutedEventArgs e)
        {
            //Put the window in background
            this.WindowState = WindowState.Minimized;
        }

        //This method manage the click on the exit button
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            //Checking if the window is in the auto-setup mode
            if (this.mode.Equals(Features.Window_Mode.Create) && blinking)
                Synchronizer.StopSetup();

            //Close the window and the application
            Environment.Exit(0);
        }

        //Those method manage the Click events raised from every textbox
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox caller = sender as TextBox;
            caller.Foreground = Brushes.WhiteSmoke;
            if (caller.Name.Equals("insertConfiguration") && caller.Text.Equals("Insert configuration name"))
            {
                call_event = false;
                caller.Clear();
                caller.Foreground = Brushes.WhiteSmoke;
                call_event = true;
            }
            return;
        }

        //This method manage the moment you move away the mouse from the textbox
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox caller = sender as TextBox;

            call_event = false;
            if (caller.Text.Equals(""))
            {
                caller.Foreground = Brushes.Gray;
                caller.Text = "Insert configuration name";
            }
            call_event = true;

            return;
        }

        //This method manage the TextChanged event raised from insertConfiguration textbox
        private void InsertConfiguration_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!call_event)
                return;
            if (e.Changes.Count == 0)
                return;

            string nameConf = this.insertConfiguration.Text;
            try
            {
                //Checking if here is a configuration with the same name yet
                if (Configuration.IsAlreadyPresent(nameConf))
                {
                    this.insertConfiguration.BorderBrush = Brushes.Red;
                    this.insertConfiguration.BorderThickness = new Thickness(2);
                    ToolTipService.SetToolTip(this.insertConfiguration, "This name is used for another configuration yet!\n" +
                                                                        "Change it if you want to proceed");
                    ToolTipService.SetIsEnabled(this.insertConfiguration, true);
                    this.save_data_button.IsEnabled = false;
                    this.start_button.IsEnabled = false;

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                Handle_DB_Error();
            }

            //Checking if the name of the configuration is empty
            if (this.insertConfiguration.Text.Equals(""))
            {
                this.insertConfiguration.BorderBrush = Brushes.Red;
                this.insertConfiguration.BorderThickness = new Thickness(2);
                ToolTipService.SetToolTip(this.insertConfiguration, "Insert a proper name!");
                ToolTipService.SetIsEnabled(this.insertConfiguration, true);
                this.save_data_button.IsEnabled = false;
                this.start_button.IsEnabled = false;

                return;
            }
            else
            {
                ToolTipService.SetIsEnabled(this.insertConfiguration, false);
                this.insertConfiguration.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)20, (byte)27, (byte)31)); ;
                this.insertConfiguration.BorderThickness = new Thickness(1);
                this.insertConfiguration.Foreground = Brushes.WhiteSmoke;
            }

            this.save_data_button.IsEnabled = true;
            this.start_button.IsEnabled = false;

            return;
        }

        //This method manage the click on the add board button
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Button caller = sender as Button;
            caller.IsEnabled = false;

            //Adding and initializing the new board row
            Add_some_TextBox_rows(1);
            Initialize_created_Textboxs();

            //Updating the focus of the graph
            Axis_Focus_Point();

            caller.IsEnabled = true;
        }

        //This method manage the event that arrive on the Cancel Button
        private void Back_button_Click(object sender, RoutedEventArgs e)
        {
            //Checking if we are in the auto-setup mode
            if (mode.Equals(Features.Window_Mode.Create) && blinking)
                Synchronizer.StopSetup();

            //this is done to have a better vision effect on the swapping windows
            this.Hide();
            MainWindow newMain = new MainWindow();
            newMain.Show();

            //After that i really close the window
            this.Close();

            return;
        }

        //This method manage the event that arrive on the Save Data Button
        private void Save_Data_button_Click(object sender, RoutedEventArgs e)
        {
            //Doing a check on the number of boards
            if (this.countBoards < Features.leastBoardNumber)
            {
                this.save_data_button.IsEnabled = false;
                this.start_button.IsEnabled = false;

                MessageBox.Show("At least there must be 2 boards to let the application works properly!");
                
                return;
            }

            //Checking if the name of the configuration is empty or not
            if (this.insertConfiguration.Text.Equals("") || this.insertConfiguration.Text.Equals("Insert configuration name"))
            {
                this.save_data_button.IsEnabled = false;
                this.start_button.IsEnabled = true;
                this.insertConfiguration.BorderBrush = Brushes.Red;
                this.insertConfiguration.BorderThickness = new Thickness(2);
                ToolTipService.SetToolTip(this.insertConfiguration, "You have to insert a proper name!");
                ToolTipService.SetIsEnabled(this.insertConfiguration, true);
                return;
            }
            else
            {
                ToolTipService.SetIsEnabled(this.insertConfiguration, false);
                this.insertConfiguration.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)20, (byte)27, (byte)31)); ;
                this.insertConfiguration.BorderThickness = new Thickness(1);
            }

            try
            {
                //Checking if there are intersections or not
                if (Coordinates.IsASimplePolygon(inserted_boards.Select(b => b.Coordinates).ToList()))
                {
                    foreach(Board b in inserted_boards)
                        b.BoardID = b.BoardID.Replace("-", ":").ToLower();

                    if (mode == Features.Window_Mode.Create)
                        configuration = new Configuration(this.insertConfiguration.Text, inserted_boards);
                    else
                        configuration.BoardReplace(inserted_boards);

                    configuration.StoreConfiguration();

                    this.save_data_button.IsEnabled = false;
                    this.start_button.IsEnabled = true;
                }
                else
                {
                    this.save_data_button.IsEnabled = false;
                    this.start_button.IsEnabled = false;
                    MessageBox.Show("There is at least 1 intersection in the polygon you have inserted!\n" +
                                    "Change some coordinates and check them in the map!");

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                Handle_DB_Error();
            }
        }

        //This method manage the event that arrive on the Start Button
        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null)
            {
                MessageBoxResult result = MessageBox.Show("We are sorry!\n" +
                                "There was an error during the loading of the configuration\n" +
                                "Do you want to restart the application?", "Restart the application", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                {
                    this.Close();
                    Environment.Exit(-1);
                }
                else
                {
                    this.Hide();
                    MainWindow newMainWindow = new MainWindow();
                    newMainWindow.Show();
                    this.Close();
                    return;
                }
            }
            this.Hide();

            //Start of the critical section in the middle of 2 window
            try
            {
                //Instantiation of the Statistic Window and closing this window
                Statistic_Window newStatisticWindow = new Statistic_Window(configuration, this.mode);
                newStatisticWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                //In case of exception I have to free the lockObject
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                
                MessageBox.Show("We are sorry!\n" +
                                "An error occure in the critical section before the opening of the Statistic window!\n");
                Environment.Exit(-1);
            }

            return;
        }
    }
}