/*
 * Author: Fabio carfì
 * Purpose: This is the main window that will showed up at the start of the application
 */
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using GUI.Backend;
using GUI.Frontend;

namespace GUI.Frontend
{
    public partial class MainWindow : Window
    {
        //--------------------- ATTRIBUTES --------------------------------------------------------------------------------------
        private int countConfiguration;
        private int summary_labels;
        private string selectedValue;
        private Features.Window_Mode mode;

        //--------------------- CONSTRUCTORS -------------------------------------------------------------------------------------
        public MainWindow()
        {
            Features.openedWindow = Features.Window_type.Main;
            InitializeComponent();

            try
            {
                this.countConfiguration = Configuration.SavedConfigurations_Number();
            }
            catch (Exception)
            {
                this.createConfiguration.IsEnabled = false;
                this.loadConfiguration.IsEnabled = false;
                this.modifyConfiguration.IsEnabled = false;
                this.removeConfiguration.IsEnabled = false;

                Handle_DB_Error();
            }

            this.summary_labels = 0;

            try
            {
                List<string> names = Configuration.SavedConfigurations_Names();
                foreach (string name in names)
                    this.selector.Items.Add(name);
            }
            catch (Exception)
            {
                Handle_DB_Error();
            }

            if (this.countConfiguration == 0)
            {
                this.loadConfiguration.IsEnabled = false;
                this.modifyConfiguration.IsEnabled = false;
                this.removeConfiguration.IsEnabled = false;
            }
        }

        //----------------- INTERNAL METHODS --------------------------------------------------------------------------------------
        //This method is used to add some rows to the summary grid to have the same number of boards
        private void Add_rows(int to_add)
        {
            if (this.summary_labels > 0)
            {
                RowDefinition newSpace = new RowDefinition
                {
                    Height = new GridLength(25, GridUnitType.Pixel)
                };
                this.summary_labels_grid.RowDefinitions.Add(newSpace);
            }

            for (int i = 0; i < to_add; i++)
            {
                RowDefinition newRow = new RowDefinition();
                RowDefinition newRowSpace = new RowDefinition();
                this.summary_labels++;

                //Here I initialize the 2 new rows, the space and the
                newRow.Height = new GridLength(30, GridUnitType.Pixel);
                this.summary_labels_grid.RowDefinitions.Add(newRow);
                if (i != to_add - 1)
                {
                    newRowSpace.Height = new GridLength(15, GridUnitType.Pixel);
                    this.summary_labels_grid.RowDefinitions.Add(newRowSpace);
                }

                for (int j = 0; j < Features.textboxes_x_row; j++)
                {
                    //Here I create the label widjet
                    Label newLabel = new Label
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                        BorderBrush = new SolidColorBrush(Color.FromRgb((byte)40, (byte)47, (byte)62)),
                        Background = new SolidColorBrush(Color.FromRgb((byte)40, (byte)47, (byte)62)),
                        Foreground = Brushes.WhiteSmoke,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        IsEnabled = true
                    };

                    if (j == 0)
                        newLabel.Name = "board_" + summary_labels.ToString();
                    else if (j == 1)
                        newLabel.Name = "coordinates_" + summary_labels.ToString();
                    else
                        newLabel.Name = "MAC_" + summary_labels.ToString();

                    //Here I set the position of the new label in the grid
                    if (i != to_add - 1)
                        Grid.SetRow(newLabel, this.summary_labels_grid.RowDefinitions.Count - 2);
                    else
                        Grid.SetRow(newLabel, this.summary_labels_grid.RowDefinitions.Count - 1);
                    Grid.SetColumn(newLabel, 2 * j);

                    //Adding the control to the Grid of the window
                    this.summary_labels_grid.Children.Add(newLabel);
                }
            }
        }

        //This method is used to initialize the labels in the summary rows
        private void Initilize_rows(List<Board> boards_info)
        {
            List<Label> labels = this.summary_labels_grid.Children.OfType<Label>().ToList();
            Board board;

            //Here I update the content of each label, one by one
            for (int i = 0; i < boards_info.Count; i++)
            {
                int index = i + 1;
                board = boards_info[i];
                Label label_board = labels.First(l => l.Name.Equals("board_" + index.ToString()));
                Label label_coord = labels.First(l => l.Name.Equals("coordinates_" + index.ToString()));
                Label label_MAC = labels.First(l => l.Name.Equals("MAC_" + index.ToString()));

                label_board.Content = "Board " + index;
                label_coord.Content = "( " + board.X.ToString("N2") + " ; " + board.Y.ToString("N2") + " )";
                label_MAC.Content = board.BoardID;
            }
        }

        //This method is used to remove some rows to have the same numbers of rows and boards
        private void Remove_rows(int to_remove)
        {
            List<Label> labels = this.summary_labels_grid.Children.OfType<Label>().ToList();

            //Here I delete the labels
            for (int i = 0; i < to_remove; i++)
            {
                Label label_board = labels.First(l => l.Name.Equals("board_" + summary_labels.ToString()));
                Label label_coord = labels.First(l => l.Name.Equals("coordinates_" + summary_labels.ToString()));
                Label label_MAC = labels.First(l => l.Name.Equals("MAC_" + summary_labels.ToString()));

                this.summary_labels_grid.Children.Remove(label_board);
                this.summary_labels_grid.Children.Remove(label_coord);
                this.summary_labels_grid.Children.Remove(label_MAC);

                this.summary_labels--;
            }

            //Here I delete all the rowdefinitions that don't need anymore
            this.summary_labels_grid.RowDefinitions.RemoveRange(this.summary_labels_grid.RowDefinitions.Count - 2 * to_remove, 2 * to_remove);
        }
        
        //This method manage the start setup operations
        private void StartSetup()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            this.createConfiguration.IsEnabled = false;
            this.loadConfiguration.IsEnabled = false;
            this.modifyConfiguration.IsEnabled = false;
            this.removeConfiguration.IsEnabled = false;

            Synchronizer.StartSetup((Configuration conf) =>
            {
                this.Hide();
                Setup_Window newSetup = new Setup_Window(Features.Window_Mode.Create, conf);
                newSetup.Show();
                Mouse.OverrideCursor = null;
                this.Close();
            },
            (Synchronizer.Reason reason, String message) =>
            {
                Synchronizer.StopSetup();
                Mouse.OverrideCursor = null;
                MessageBoxResult result2 = MessageBox.Show("There was an error during the listening of the boards\nPlease try again!",
                                              "Auto-setup error");

                this.createConfiguration.IsEnabled = true;
                this.loadConfiguration.IsEnabled = true;
                this.modifyConfiguration.IsEnabled = true;
                this.removeConfiguration.IsEnabled = true;

                Console.WriteLine("----------------- ERROR -----------------");
                Console.WriteLine(reason.ToString() + ": " + message);
                Console.WriteLine("-----------------------------------------");
            });
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


        //---------------------- EVENTS HANDLING --------------------------------------------------------------------------------
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
            Environment.Exit(0);
        }

        //This method handle the event raised by the click on the create new configuration button
        private void Create_New_Configuration_button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("There is the possibility to auto configure the system!\nDo you want to proceed?",
                                                      "Auto-setup option available", MessageBoxButton.YesNo);

            if (result.Equals(MessageBoxResult.Yes))
            {
                try
                {
                    StartSetup();
                }catch(Exception ex)
                {
                    Synchronizer.StopSetup();
                    Mouse.OverrideCursor = null;

                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);

                    MessageBoxResult result3 = MessageBox.Show("There was an error during the listening of the boards\nWould you try again?",
                                                      "Auto-setup error", MessageBoxButton.YesNo);
                    if (result3.Equals(MessageBoxResult.Yes))
                    {
                        StartSetup();
                    }
                    else
                    {
                        this.createConfiguration.IsEnabled = true;
                        this.loadConfiguration.IsEnabled = true;
                        this.modifyConfiguration.IsEnabled = true;
                        this.removeConfiguration.IsEnabled = true;
                    }  
                }
            }
            else
            {
                this.Hide();
                Setup_Window nSetup = new Setup_Window(Features.Window_Mode.Create);
                nSetup.Show();
                this.Close();
            }

            return;
        }

        //This method handle the event raised by the click on the load configuration button
        private void Load_Configuration_button_Click(object sender, RoutedEventArgs e)
        {
            this.button_grid.Visibility = Visibility.Hidden;
            this.select_grid.Visibility = Visibility.Visible;

            this.mode = Features.Window_Mode.Load;
        }

        //This method handle the event raised by the click on the modify configuration button
        private void Modify_Configuration_button_Click(object sender, RoutedEventArgs e)
        {
            this.button_grid.Visibility = Visibility.Hidden;
            this.select_grid.Visibility = Visibility.Visible;

            this.mode = Features.Window_Mode.Modify;
        }

        //This method handle the event raised by the click on the remove configuration button
        private void Remove_Configuration_button_Click(object sender, RoutedEventArgs e)
        {
            this.button_grid.Visibility = Visibility.Hidden;
            this.select_grid.Visibility = Visibility.Visible;

            this.mode = Features.Window_Mode.Remove;
        }

        //----------------- This method manage the event that occur when the selected string from the combobox change -----------
        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox caller = sender as ComboBox;
            List<Board> boards = new List<Board>();
            Configuration configuration;

            selectedValue = caller.SelectedItem.ToString();

            //Here I retrieve all the information that I need to fill the summary
            try
            {
                configuration = Configuration.LoadConfiguration(selectedValue);
                boards = Configuration.SavedConfiguration_Boards(configuration);
            }
            catch (Exception)
            {
                Handle_DB_Error();
            }

            //Here I create the rows where to insert the information
            if (summary_labels > boards.Count)
                Remove_rows(summary_labels - boards.Count);
            else if (summary_labels < boards.Count)
                Add_rows(boards.Count - summary_labels);

            //Here I fill the rows with the data retrieved before
            Initilize_rows(boards);

            this.ok_button.IsEnabled = true;

            return;
        }

        //--------------- This method manage the Cancel button click event ---------------------------------------
        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.select_grid.Visibility = Visibility.Hidden;
            this.button_grid.Visibility = Visibility.Visible;

            return;
        }

        //--------------- This method manage the ok button click event -------------------------------------------
        private void Ok_button_Click(object sender, RoutedEventArgs e)
        {
            //Here there is no need to do something because the value is saved in the attribute of the window
            if (mode == Features.Window_Mode.Load)
            {
                string nameConf = selectedValue;
                if (nameConf != null)
                {
                    Configuration configuration = null;
                    try
                    {
                        configuration = Configuration.LoadConfiguration(nameConf);
                    }
                    catch (Exception)
                    {
                        Handle_DB_Error();
                    }
                    if (configuration == null)
                    {
                        MessageBoxResult result = MessageBox.Show("We are sorry!\n" +
                                        "There was an error during the load of the configuration\n" +
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
                }
            }
            else if (mode == Features.Window_Mode.Modify)
            {
                string nameConf = selectedValue;
                if (nameConf != null)
                {
                    Configuration configuration = null;

                    //Retrive the configuration from the DB
                    try
                    {
                        configuration = Configuration.LoadConfiguration(nameConf);
                    }
                    catch (Exception)
                    {
                        Handle_DB_Error();
                    }

                    //Managing the case where the configuration is null
                    if (configuration == null)
                    {
                        MessageBoxResult result = MessageBox.Show("We are sorry!\n" +
                                        "There was an error during the load of the configuration\n" +
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

                    //All is went good so let's go to the setup
                    this.Hide();
                    Setup_Window modifyConfiguration = new Setup_Window(Features.Window_Mode.Modify, configuration);
                    modifyConfiguration.Show();
                    this.Close();
                    return;
                }
            }
            else if (mode == Features.Window_Mode.Remove)
            {
                string configuration_to_remove = selectedValue;
                if (configuration_to_remove != null)
                {
                    try
                    {
                        Configuration.RemoveConfiguration(configuration_to_remove);
                    }
                    catch (Exception)
                    {
                        Handle_DB_Error();
                    }

                    this.Hide();
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                    return;
                }
            }
        }
    }
}
