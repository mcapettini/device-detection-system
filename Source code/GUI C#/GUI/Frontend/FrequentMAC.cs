/*
 * Author: Fabio carfì
 * Purpose: This class have to contains all the possible information
 *          reguarding a frequent MAC device and its activity time ranges
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GUI.Frontend
{
    public class FrequentMAC
    {
        //--------------------- ATTRIBUTES --------------------------------------------------------------------------------------
        private string MACaddress;
        private Dictionary<DateTime, TimeSpan> activity_timeranges;
        private SolidColorBrush color;
        private DateTime lastTimeSeen;
        private double totalSecondsOfActivity;

        //--------------------- CONSTRUCTORS ------------------------------------------------------------------------------------
        public FrequentMAC(String MACaddress, Dictionary<DateTime, TimeSpan> activity_timeranges)
        {
            this.MACaddress = MACaddress;
            this.activity_timeranges = activity_timeranges;
            this.totalSecondsOfActivity = activity_timeranges.Sum(x => x.Value.TotalSeconds);
        }

        //--------------------- METHODS -----------------------------------------------------------------------------------------
        //This method return the percentage of activity considering the range of interest specified
        public double GetActivityPercentage(TimeSpan totalTimeActivity)
        {
            return (totalSecondsOfActivity*100)/ totalTimeActivity.Seconds;
        }

        //--------------------- PROPERTIES --------------------------------------------------------------------------------------
        public String MAC
        {
            get { return MACaddress; }
        }

        public Dictionary<DateTime, TimeSpan> Activity
        {
            get { return activity_timeranges; }

            set {
                activity_timeranges = value;
                totalSecondsOfActivity = activity_timeranges.Sum(x => x.Value.TotalSeconds);
            }
        }

        public SolidColorBrush Color
        {
            get { return color; }

            set { color = value; }
        }

        public DateTime LastTimeSeen
        {
            get { return lastTimeSeen; }

            set { lastTimeSeen = value; }
        }

        public double TotalSecondsOfActiity
        {
            get { return totalSecondsOfActivity; }
        }
    }
}
