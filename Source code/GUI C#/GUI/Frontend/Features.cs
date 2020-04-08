/*
 * Author: Fabio carfì
 * Purpose: This class have to contains all the constant and features that can be needed
 *          to all the windows of the application.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GUI.Frontend
{
    public class Features
    {
        //--------------------- ATTRIBUTES --------------------------------------------------------------------------------------
        public const int leastBoardNumber = 2;
        public const int textboxes_x_row = 3;
        public const int defaultNumBoards = 4;
        public const int showComboBoxItems = 4;
        public const int numberOfCouples = 6;
        public const int delayUpdateGraphs = 30;
        public const int delayUpdateStats = 2;
        public const int MaxMinDifferenceRange = 300;
        public const double percentageUpgradeSteps = 0.025;
        public const double percentageSeparator = 0.15;
        public static Window_type openedWindow;

        public static List<SolidColorBrush> colors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromRgb((byte)0, (byte)0, (byte)179)),    //blue      #0000b3
            new SolidColorBrush(Color.FromRgb((byte)255, (byte)0, (byte)0)),    //red       #ff0000
            new SolidColorBrush(Color.FromRgb((byte)0, (byte)128, (byte)0)),    //green     #008000
            new SolidColorBrush(Color.FromRgb((byte)204, (byte)51, (byte)0)),   //brown     #cc3300
            new SolidColorBrush(Color.FromRgb((byte)128, (byte)0, (byte)128)),  //purple    #800080
            new SolidColorBrush(Color.FromRgb((byte)255, (byte)204, (byte)0)),  //yellow    #ffcc00
            new SolidColorBrush(Color.FromRgb((byte)0, (byte)255, (byte)204)),  //azure     #00ffcc
            new SolidColorBrush(Color.FromRgb((byte)128, (byte)0, (byte)0)),    //bordeaux  #800000
            new SolidColorBrush(Color.FromRgb((byte)0, (byte)102, (byte)255)),  //indigo    #0066ff
            new SolidColorBrush(Color.FromRgb((byte)255, (byte)51, (byte)0))    //orange    #ff3300
        };

        public enum Window_Mode
        {
            Create = 0,     //when we want to create a new configuration
            Load = 1,       //when we want to load a previous configuration that has some data stored yet
            Modify = 2,     //when we want to modify the informations about a specific configuration
            Remove = 3      //when we want to remove all the information about a specific configuration from the DB
        };

        public enum Detection_Status
        {
            Running = 0,            //when the detection is taking place
            Temporarly_Stopped = 1, //when the application is not detecting anything from less than 5 minutes
            Stopped = 2             //when the application is not detecting anything from more than 5 minutes
        };

        public enum Window_type
        {
            Main = 0,
            Setup = 1,
            Statistic = 2
        };

        //--------------------- METHODS -----------------------------------------------------------------------------------------
        //This methos is used for generate a random MAC address
        public static string Generate_Random_MAC()
        {
            string MAC_address = string.Empty;
            Random rand = new Random();
            int number;

            for (int i = 0; i < Features.numberOfCouples; i++)
            {
                number = rand.Next(0, 256);
                MAC_address += number.ToString("X2") + ":";
            }

            return MAC_address.TrimEnd(":".ToCharArray());
        }
    }
}