/*
 * Author: Fabio carfì
 * Purpose: this control manage all the features relative to a board. It manage the case where the user
 *          want to change the coordinates or the mac of the board, or he want to change is position up and down,
 *          if he want to delete the board and if he want that the board start flashing
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using LiveCharts;                   //those three library are mandatory
using LiveCharts.Wpf;               //for all the operations that we have to do
using LiveCharts.Defaults;          //on the chart

namespace GUI.Frontend
{
    public partial class BoardRow : UserControl
    {
        //----------------------------- ATTRIBUTES -----------------------------------------------------------------------------------
        private Setup_Window parent;
        private int boardId;


        //------------------------- CONSTRUCTOR ----------------------------------------------------------------------------------
        public BoardRow(Setup_Window parent,int uid, Boolean blink_mode)
        {
            InitializeComponent();

            this.parent = parent;
            this.boardId = uid;

            if (!blink_mode)
            {
                this.blink.Visibility = Visibility.Collapsed;
                Canvas.SetLeft(this.up, 15);
                Canvas.SetRight(this.down, 15);
            }
        }


        //---------------------- INTERNAL METHODS --------------------------------------------------------------------------------
        //Method used to remove some rows of textboxes, meaning we want to remove some boards from the configuration
        private void Remove_board_row()
        {
            int index_row = 2 * (boardId - 1);
            int index_space = index_row - 1;
            
            //Remove the element from all the list of the setup
            this.parent.coordinatesGrid.Children.RemoveAt(boardId - 1);
            this.parent.Boards.RemoveAt(boardId - 1);
            this.parent.Inserted_boards.RemoveAt(boardId - 1);

            //Remove the textboxes row and Space row from the RowDefinition of the grid
            this.parent.coordinatesGrid.RowDefinitions.RemoveAt(index_row);
            if (boardId == 1)
                this.parent.coordinatesGrid.RowDefinitions.RemoveAt(index_row + 1);
            else
                this.parent.coordinatesGrid.RowDefinitions.RemoveAt(index_space);

            //Decreasing the number of boards in the room
            this.parent.CountBoards--;

            //Removing the point from the map
            this.parent.polygon.Values.RemoveAt(boardId - 1);
            if(boardId == 1)
            {
                double newX = this.parent.Inserted_boards[0].X;
                double newY = this.parent.Inserted_boards[0].Y;
                this.parent.polygon.Values[this.parent.polygon.Values.Count - 1] = new ObservablePoint(newX,newY);
            }
            if (this.parent.CountBoards == Features.leastBoardNumber)
                this.parent.polygon.Values.RemoveAt(this.parent.polygon.Values.Count - 1);
            
            return;
        }

        //This method is called when 2 boards have to be place swapped
        private void BoardSwap(int otherBoard_ID)
        {
            int row_indexThis = 2 * (boardId - 1);
            int row_indexOther = 2 * (otherBoard_ID - 1);
            int list_indexThis = boardId - 1;
            int list_indexOther = otherBoard_ID - 1;
            BoardRow other_boardRow = this.parent.Boards[list_indexOther];
            Board this_board = this.parent.Inserted_boards[list_indexThis];
            Board other_board = this.parent.Inserted_boards[list_indexOther];
            ChartPoint thisPoint = this.parent.polygon.Values.GetPoints(this.parent.polygon).ElementAt(list_indexThis);
            ChartPoint otherPoint = this.parent.polygon.Values.GetPoints(this.parent.polygon).ElementAt(list_indexOther);

            //Change the row of the elements in the grid
            Grid.SetRow(this, row_indexOther);
            Grid.SetRow(other_boardRow, row_indexThis);

            //Change the position of the elements in the boards list
            this.parent.Boards[list_indexThis] = other_boardRow;
            this.parent.Boards[list_indexOther] = this;

            //Change the positions in the coordinateGrid.children list
            this.parent.coordinatesGrid.Children.Clear();
            for (int i = 0; i<this.parent.Boards.Count; i++)
                this.parent.coordinatesGrid.Children.Add(this.parent.Boards[i]);
            
            //Change the position of the elements in the inserted boards list
            this.parent.Inserted_boards[list_indexThis] = other_board;
            this.parent.Inserted_boards[list_indexOther] = this_board;

            //Swapping the point representation
            this.parent.polygon.Values[list_indexThis] = new ObservablePoint(otherPoint.X, otherPoint.Y);
            this.parent.polygon.Values[list_indexOther] = new ObservablePoint(thisPoint.X, thisPoint.Y);

            if((boardId == 1 || otherBoard_ID == 1) && this.parent.CountBoards > 2)
            {
                double x = this.parent.Inserted_boards[0].X;
                double y = this.parent.Inserted_boards[0].Y;
                this.parent.polygon.Values[this.parent.polygon.Values.Count - 1] = new ObservablePoint(x, y);
            }

            return;
        }

        //This method admit to convert a decimal number in another one with the separator of the current culture
        private string ConvertStringToCurrentCulture(string text)
        {
            if (text.Split(",".ToCharArray()).Length > 1)
                return text.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            else
                return text.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        //This method do all the operation to update the map, considering all the possible cases
        private void Update_BoardMap(TextBox x_textbox, TextBox y_textbox)
        {
            double x_point = double.Parse(ConvertStringToCurrentCulture(x_textbox.Text));
            double y_point = double.Parse(ConvertStringToCurrentCulture(y_textbox.Text));
            int point_index = boardId - 1;
            int last_point_index = parent.polygon.Values.Count - 1;

            //Here I check if the point was previously inserted or not. If yes I modify it, otherwise I insert a new one
            this.parent.polygon.Values[point_index] = new ObservablePoint(x_point, y_point);
            if (point_index == 0 && this.parent.CountBoards != 2)
                this.parent.polygon.Values[last_point_index] = new ObservablePoint(x_point, y_point);
            this.parent.Axis_Focus_Point();

            return;
        }

        //---------------------- EVENTS HANDLING ---------------------------------------------------------------------------------
        //This method manage the events that arrive on the textBox for the X of the board
        private void CoordinateX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.parent.Call_Event)
                return;
            if (e.Changes.Count == 0)
                return;
            
            Regex coordinate_pattern = new Regex("^(-)?([0-9]|[1-9][0-9])([.,][0-9]{1,2})?$");
            Regex MAC_pattern = new Regex("^(([0-9a-f][0-9a-f]:){5}([0-9a-f][0-9a-f]))|(([0-9a-f][0-9a-f]-){5}([0-9a-f][0-9a-f]))$");

            //Check if the text inserted follow the pattern of an integer or a decimal number
            if (!coordinate_pattern.IsMatch(this.x.Text) || this.x.Text.Equals(""))
            {
                this.parent.save_data_button.IsEnabled = false;
                this.parent.start_button.IsEnabled = false;
                this.x.BorderBrush = Brushes.Red;
                this.x.BorderThickness = new Thickness(2);
                if (this.x.Text.Equals(""))
                    ToolTipService.SetToolTip(this.x, "You have to insert a proper number!");
                else
                {
                    ToolTipService.SetToolTip(this.x, "Have been inserted a number not in the range allowed!\n" +
                                                         "The range accepted is [-99.99 , 99.99]");
                    if (double.TryParse(this.x.Text, out double inserted_num))
                        ToolTipService.SetToolTip(this.x, "Have been inserted some non integer characters!");
                }
                ToolTipService.SetIsEnabled(this.x, true);
                return;
            }
            else
            {
                ToolTipService.SetIsEnabled(this.x, false);
                this.x.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)20, (byte)27, (byte)31));
                this.x.BorderThickness = new Thickness(1);
            }

            //Update the x coordinate in the list of boards
            this.parent.Inserted_boards[boardId - 1].X = double.Parse(ConvertStringToCurrentCulture(this.x.Text));

            //If all the patterns are satisfied then I'll update the map
            if (MAC_pattern.IsMatch(this.MAC.Text) && coordinate_pattern.IsMatch(this.y.Text))
            {
                Update_BoardMap(this.x, this.y);
                
                this.parent.save_data_button.IsEnabled = true;
                this.parent.start_button.IsEnabled = false;
            }

            return;
        }

        //This method manage the events that arrive on the textBox for the Y of the board
        private void CoordinateY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.parent.Call_Event)
                return;
            if (e.Changes.Count == 0)
                return;
            
            Regex coordinate_pattern = new Regex("^(-)?([0-9]|[1-9][0-9])([.,][0-9]{1,2})?$");
            Regex MAC_pattern = new Regex("^(([0-9a-f][0-9a-f]:){5}([0-9a-f][0-9a-f]))|(([0-9a-f][0-9a-f]-){5}([0-9a-f][0-9a-f]))$");

            //Check if the text inserted follow the pattern of an integer or a decimal number
            if (!coordinate_pattern.IsMatch(this.y.Text) || this.y.Text.Equals(""))
            {
                this.parent.save_data_button.IsEnabled = false;
                this.parent.start_button.IsEnabled = false;
                this.y.BorderBrush = Brushes.Red;
                this.y.BorderThickness = new Thickness(2);
                if (this.y.Text.Equals(""))
                    ToolTipService.SetToolTip(this.y, "You have to insert a proper number!");
                else
                {
                    ToolTipService.SetToolTip(this.y, "Have been inserted a number not in the range allowed!\n" +
                                                         "The range accepted is [-99.99 , 99.99]");
                    if (double.TryParse(this.y.Text, out double inserted_num))
                        ToolTipService.SetToolTip(this.y, "Have been inserted some non integer characters!");
                }
                ToolTipService.SetIsEnabled(this.y, true);
                return;
            }
            else
            {
                ToolTipService.SetIsEnabled(this.y, false);
                this.y.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)20, (byte)27, (byte)31));
                this.y.BorderThickness = new Thickness(1);
            }

            //Update the y coordinate in the list of boards
            this.parent.Inserted_boards[boardId - 1].Y = double.Parse(ConvertStringToCurrentCulture(this.y.Text));

            //If all the patterns are satisfied then I'll update the map
            if (MAC_pattern.IsMatch(this.MAC.Text) && coordinate_pattern.IsMatch(this.x.Text))
            {
                Update_BoardMap(this.x, this.y);
                
                this.parent.save_data_button.IsEnabled = true;
                this.parent.start_button.IsEnabled = false;
            }

            return;
        }

        //This method manage the events that arrive on the MAC textBox of a specific board
        private void MAC_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.parent.Call_Event)
                return;
            if (e.Changes.Count == 0)
                return;
            
            Regex coordinate_pattern = new Regex("^(-)?([0-9]|[1-9][0-9])([.,][0-9]{1,2})?$");
            Regex MAC_pattern = new Regex("^(([0-9a-f][0-9a-f]:){5}([0-9a-f][0-9a-f]))|(([0-9a-f][0-9a-f]-){5}([0-9a-f][0-9a-f]))$");

            //Check if the text inserted follow the pattern of a rigth MAC address
            if (!MAC_pattern.IsMatch(this.MAC.Text) || this.MAC.Text.Equals(""))
            {
                this.parent.save_data_button.IsEnabled = false;
                this.parent.start_button.IsEnabled = false;
                this.MAC.BorderBrush = Brushes.Red;
                this.MAC.BorderThickness = new Thickness(2);
                if (this.MAC.Text.Equals(""))
                    ToolTipService.SetToolTip(this.MAC, "You have to insert a proper MAC address");
                else
                {
                    int count_error = 0;
                    string mac = this.MAC.Text;
                    string tooltip_message;
                    Regex hexa_pattern = new Regex("^[0-9a-fA-F]$");
                    char char_splitter;

                    if (mac.Split(":".ToCharArray()).Count() > mac.Split("-".ToCharArray()).Count())
                    {
                        if (mac.Split(":".ToCharArray()).Count() > mac.Split(".".ToCharArray()).Count())
                            char_splitter = ":".ToCharArray()[0];
                        else
                            char_splitter = ".".ToCharArray()[0];
                    }
                    else
                    {
                        if (mac.Split("-".ToCharArray()).Count() > mac.Split(".".ToCharArray()).Count())
                            char_splitter = "-".ToCharArray()[0];
                        else
                            char_splitter = ".".ToCharArray()[0];
                    }

                    if (mac.Split(char_splitter).Count() < Features.numberOfCouples)
                    {
                        tooltip_message = "These are the errors founded:";
                        tooltip_message += "\n  - The MAC address is too short!";
                        for (int i = 0; i < mac.Length && count_error < 4; i++)
                        {
                            if (i % 3 == 2)
                            {
                                if (!mac[i].Equals(char_splitter) && !hexa_pattern.IsMatch(mac[i].ToString()) && !tooltip_message.Contains("\n  - The MAC address has a delimiter different from the others used"))
                                {
                                    tooltip_message += "\n  - The MAC address has a delimiter different from the others used";
                                    count_error++;
                                }
                                else if (!mac[i].Equals(char_splitter) && hexa_pattern.IsMatch(mac[i].ToString()) && !tooltip_message.Contains("\n  - The MAC address miss a delimiter character!"))
                                {
                                    tooltip_message += "\n  - The MAC address miss a delimiter character!";
                                    count_error++;
                                }
                            }
                            else
                            {
                                if (!hexa_pattern.IsMatch(mac[i].ToString()) && !tooltip_message.Contains("\n  - The MAC address contain at least 1 non hexadecimal character!"))
                                {
                                    tooltip_message += "\n  - The MAC address contain at least a non hexadecimal character!";
                                    count_error++;
                                }
                            }
                        }
                    }
                    else if (mac.Split(char_splitter).Count() > Features.numberOfCouples)
                        tooltip_message = "This is the error found:\n  - The MAC address is too long!";
                    else
                        tooltip_message = "This is the error found:\n  - The MAC address miss at least an hexadecimal character!";

                    ToolTipService.SetToolTip(this.MAC, tooltip_message);
                }
                ToolTipService.SetIsEnabled(this.MAC, true);
                return;
            }
            else
            {
                ToolTipService.SetIsEnabled(this.MAC, false);
                this.MAC.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)20, (byte)27, (byte)31));
                this.MAC.BorderThickness = new Thickness(1);
            }

            this.parent.Inserted_boards[boardId - 1].BoardID = this.MAC.Text;
            if (coordinate_pattern.IsMatch(this.x.Text) && coordinate_pattern.IsMatch(this.y.Text))
            {
                this.parent.save_data_button.IsEnabled = true;
                this.parent.start_button.IsEnabled = false;
            }

            return;
        }

        //This method manage the delete button click
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.parent.CountBoards == Features.leastBoardNumber - 1)
            {
                MessageBox.Show("It's not admitted to have a configuration without any boards!","Board communication");
                return;
            }

            //Removing the board from the window
            Remove_board_row();
            this.parent.Axis_Focus_Point();

            //Updating all the uid of the boards
            for (int i=0; i < this.parent.Boards.Count; i++)
            {
                Grid.SetRow(this.parent.Boards[i], 2 * i);
                this.parent.Boards[i].boardId = i + 1;

                this.parent.UpdateEnableUpDown(i);
            }

            this.parent.save_data_button.IsEnabled = true;
            this.parent.start_button.IsEnabled = false;

            return;
        }

        //This method manage the blink button click
        private void BlinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Synchronizer.BlinkBoard(this.parent.Inserted_boards.ElementAt(this.boardId - 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if(Features.openedWindow.Equals(Features.Window_Mode.Create) || Features.openedWindow.Equals(Features.Window_Mode.Modify))
                {
                    this.parent.Hide();
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.parent.Close();
                }
            }
        }

        //This method manage the up button click
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Button caller = sender as Button;
            int otherBoard_id = boardId - 1;
            int thisBoard_id = boardId;
            BoardRow other_boardRow = this.parent.Boards[otherBoard_id - 1];

            caller.IsEnabled = false;

            //Doing the swap in the collections of the setup window
            BoardSwap(otherBoard_id);

            //Updating the board ids
            this.boardId = otherBoard_id;
            other_boardRow.boardId = thisBoard_id;

            //Updating the enabled buttons
            for(int i = 0; i < this.parent.Boards.Count; i++)
                this.parent.UpdateEnableUpDown(i);

            this.parent.save_data_button.IsEnabled = true;
            this.parent.start_button.IsEnabled = false;

            return;
        }

        //This method manage the down button click
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Button caller = sender as Button;
            int otherBoard_id = boardId + 1;
            int thisBoard_id = boardId;
            BoardRow other_boardRow = this.parent.Boards[otherBoard_id - 1];

            caller.IsEnabled = false;

            //Doing the swap in the collections of the setup window
            BoardSwap(otherBoard_id);

            //Updating the board ids
            this.boardId = otherBoard_id;
            other_boardRow.boardId = thisBoard_id;

            //Updating the enabled buttons
            for (int i = 0; i < this.parent.Boards.Count; i++)
                this.parent.UpdateEnableUpDown(i);

            this.parent.save_data_button.IsEnabled = true;
            this.parent.start_button.IsEnabled = false;

            return;
        }
    }
}
