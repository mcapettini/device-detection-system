/*
 * Author: Fabio carfì
 * Purpose: This class is used to stop some static methods usefull for the Statistic window
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUI.Backend;
using LiveCharts;                   //those three library are mandatory
using LiveCharts.Wpf;               //for all the operations that we have to do
using LiveCharts.Defaults;          //on the chart
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GUI.Frontend
{
    public static class Statistic_Auxiliary
    {
        //------------------------- METHODS -----------------------------------------------------------------------------------------
        //This method admit to update the max and min value of the x and y axis
        public static void UpdateCoTAxisConfiguration(Statistic_Window window, Dictionary<DateTime, TimeSpan> activity_timeline)
        {
            DateTime max = Translate_Slider_Value(window.CoT_rangeSlider.HigherValue, activity_timeline);
            DateTime min = Translate_Slider_Value(window.CoT_rangeSlider.LowerValue, activity_timeline);
            
            //Update the date labels of the CoT representation
            window.CoT_date_start.Text = min.ToString();
            window.CoT_date_end.Text = max.ToString();

            //Compute the max values put on the graph
            List<double> maxCoT_Yvalues = window.CoT_devices.Values.GetPoints(window.CoT_devices).Where(t =>
            {
                DateTime date = new DateTime((long)t.X);
                DateTime m = min.Subtract(new TimeSpan(0, 0, Features.delayUpdateGraphs));
                DateTime M = max.AddSeconds(Features.delayUpdateGraphs);
                if (date.CompareTo(M) <= 0 && date.CompareTo(m) >= 0)
                    return true;
                else
                    return false;
            }).Select(p => p.Y).ToList();

            //Update the min and max value of the x and y axis of the CoT graph
            window.CoT_Map.AxisX[0].MaxValue = max.AddSeconds(Features.delayUpdateGraphs).Ticks;
            window.CoT_Map.AxisX[0].MinValue = min.Subtract(new TimeSpan(0,0,Features.delayUpdateGraphs)).Ticks;
            if (maxCoT_Yvalues.Count > 0)
                window.CoT_Map.AxisY[0].MaxValue = maxCoT_Yvalues.Max() + 5;
            else
                window.CoT_Map.AxisY[0].MaxValue = 5;

            return;
        }

        //This method is used to translate a value of a slider into a DateTime object
        public static DateTime Translate_Slider_Value(double sliderValue, Dictionary<DateTime, TimeSpan> activity_timeline)
        {
            double seconds = sliderValue;
            double max = activity_timeline.Select(t => t.Value.TotalSeconds).Sum();
            
            if (seconds > max)
                throw new Exception("It's not possible to translate a value that is higher than the maximum");

            if (seconds == max)
                return activity_timeline.OrderBy(t => t.Key).Last().Key.Add(activity_timeline.OrderBy(t => t.Key).Last().Value);
            if (seconds == 0)
                return activity_timeline.OrderBy(t => t.Key).First().Key;

            foreach (KeyValuePair<DateTime, TimeSpan> tuple in activity_timeline.OrderBy(t => t.Key))
            {
                if (seconds <= tuple.Value.TotalSeconds)
                {
                    //Range of activity found so we compute the date
                    return tuple.Key.AddSeconds(seconds);
                }
                else
                {
                    //This is not the range activity I was looking for so I update the variable and go on next iteration
                    seconds -= tuple.Value.TotalSeconds;
                }
            }

            throw new Exception("No traslation possible from slider value to DateTime!");
        }

        //This method is used to retrieve the active time of a device in the right format
        public static string Retrieve_Active_Time(TimeSpan activePeriod)
        {
            string message = string.Empty;
            
            if (activePeriod.TotalSeconds < (double)Features.delayUpdateGraphs)
                message =  "Just connected";
            else if (activePeriod.TotalSeconds < 60)
                message = "Active from " + activePeriod.Seconds + " seconds";
            else if (activePeriod.TotalMinutes < 60)
            {
                if (activePeriod.Minutes > 1)
                    message = "Active from " + activePeriod.Minutes + " minutes";
                else
                    message = "Active from " + activePeriod.Minutes + " minute";
            }
            else if (activePeriod.TotalHours < 24)
            {
                if (activePeriod.Hours > 1)
                {
                    if (activePeriod.Minutes > 1)
                        message = "Active from " + activePeriod.Hours + " hourse and " + activePeriod.Minutes + " minutes";
                    else
                        message = "Active from " + activePeriod.Hours + " hourse and " + activePeriod.Minutes + " minute";
                }
            }
            else if (activePeriod.Days == 1)
                message = "Active from " + activePeriod.Days + " day";
            else
                message = "Active from " + activePeriod.Days + " days";

            return message;
        }

        //This method manage the insertion of splitters in the tickbar
        public static void SplittersDisplay(Statistic_Window window , Dictionary<DateTime, TimeSpan> activity_timeline)
        {
            //Here I clear the entire splitters
            window.DM_splitters.Children.Clear();
            window.CoT_splitters.Children.Clear();
            window.FM_splitters.Children.Clear();

            //Here I create and assign the splitters
            SolidColorBrush splitters_color = new SolidColorBrush(Color.FromRgb((byte)43, (byte)151, (byte)33));
            Thickness margin_splitters = new Thickness(0, 0, 0, 5);
            foreach(KeyValuePair<DateTime,TimeSpan> activity in activity_timeline)
            {
                Rectangle DM_splitter = new Rectangle
                {
                    Height = window.DM_splitters.ActualHeight * 3 / 4,
                    Width = 3,
                    Fill = splitters_color,
                    //Margin = margin_splitters
                };
                Rectangle CoT_splitter = new Rectangle
                {
                    Height = window.CoT_splitters.ActualHeight * 3 / 4,
                    Width = 3,
                    Fill = splitters_color,
                    //Margin = margin_splitters
                };
                Rectangle FM_splitter = new Rectangle
                {
                    Height = window.FM_splitters.ActualHeight * 3 / 4,
                    Width = 3,
                    Fill = splitters_color,
                    //Margin = margin_splitters
                };

                //Here I set the DM and CoT splitters
                var timespans = activity_timeline.Where(t => t.Key < activity.Key).Select(t => t.Value);

                if(timespans.Count() != 0)
                {
                    double totSeconds = timespans.Sum(t => t.TotalSeconds);
                    double DM_leftValue = (totSeconds * window.DM_splitters.ActualWidth) / window.DM_slider.Maximum;
                    double CoT_leftValue = (totSeconds * window.CoT_splitters.ActualWidth) / window.CoT_rangeSlider.Maximum;
                    double FM_leftValue = (totSeconds * window.FM_splitters.ActualWidth) / window.FM_rangeSlider.Maximum;

                    window.DM_splitters.Children.Add(DM_splitter);
                    Canvas.SetLeft(DM_splitter, DM_leftValue);
                    Canvas.SetBottom(DM_splitter, 0);

                    window.CoT_splitters.Children.Add(CoT_splitter);
                    Canvas.SetLeft(CoT_splitter, CoT_leftValue);
                    Canvas.SetBottom(CoT_splitter, 0);

                    window.FM_splitters.Children.Add(FM_splitter);
                    Canvas.SetLeft(FM_splitter, FM_leftValue);
                    Canvas.SetBottom(FM_splitter, 0);
                }
            }
        }

        //This method compute what, of the given MACs, should be displayed in the FM graph
        public static List<FrequentMAC> RetriveNewFrequentMACs (List<FrequentMAC> frequentMACs, Configuration conf, DateTime start, TimeSpan period)
        {
            List<FrequentMAC> newMACs = new List<FrequentMAC>();
            double threshold;
            DateTime stop = start.Add(period);

            frequentMACs = frequentMACs.Where(x =>
            {
                DateTime startRange = x.Activity.OrderBy(t => t.Key).First().Key;
                KeyValuePair<DateTime, TimeSpan> lastActivityRange = x.Activity.OrderBy(t => t.Key).Last();
                DateTime stopRange = lastActivityRange.Key.Add(lastActivityRange.Value);

                if (startRange >= start && stopRange <= stop)
                    return true;
                else
                    return false;

            }).ToList();

            //Here I populate my new collection of frequent MACs
            foreach (KeyValuePair<string, Dictionary<DateTime, TimeSpan>> mac in Device.FrequentDevices(conf, start, period))
            {
                newMACs.Add(new FrequentMAC(mac.Key, mac.Value));
            }

            //Here I have to filter my data and choose who to display in the graph using the progressive mean method
            if (newMACs.Count != 0)
                threshold = newMACs.Sum(x => x.TotalSecondsOfActiity) / newMACs.Count;
            else
                threshold = 0;
            if(newMACs.Count > 10)
            {
                newMACs = newMACs.Where(x => x.TotalSecondsOfActiity >= threshold).ToList();
                if (newMACs.Count > 10)
                    newMACs = newMACs.OrderByDescending(x => x.TotalSecondsOfActiity).Take(10).ToList();
            }
            frequentMACs = frequentMACs.Where(x => x.TotalSecondsOfActiity >= threshold).ToList();

            //Modelling the new list of MACs to display
            SolidColorBrush MACcolor;
            FrequentMAC mac_to_add;
            foreach (FrequentMAC mac in newMACs)
            {
                //Managing the case there is an object with the same MAC address
                if(frequentMACs.Select(t => t.MAC).Contains(mac.MAC))
                {
                    frequentMACs.Find(t => t.MAC.Equals(mac.MAC)).LastTimeSeen = DateTime.Now;
                    frequentMACs.Find(t => t.MAC.Equals(mac.MAC)).Activity = mac.Activity;
                }
                else
                {
                    if(frequentMACs.Count == 10)
                    {
                        //Managing the case there are 10 objects yet, so I have to decide what to take and what to discard
                        FrequentMAC mac_to_remove = frequentMACs.First(x => x.LastTimeSeen.Equals(frequentMACs.Min(t => t.LastTimeSeen)));
                        MACcolor = mac_to_remove.Color;

                        mac_to_add = new FrequentMAC(mac.MAC, mac.Activity)
                        {
                            Color = MACcolor,
                            LastTimeSeen = DateTime.Now
                        };
                        frequentMACs.Remove(mac_to_remove);
                        frequentMACs.Add(mac_to_add);
                    }
                    else
                    {
                        //Managing the case there are not 10 objects yet
                        MACcolor = Features.colors.Except(frequentMACs.Select(t => t.Color).ToList()).First();
                        mac_to_add = new FrequentMAC(mac.MAC, mac.Activity)
                        {
                            Color = MACcolor,
                            LastTimeSeen = DateTime.Now
                        };
                        frequentMACs.Add(mac_to_add);
                    }
                }
            }
            
            return frequentMACs;
        }

        //Display the new MACs on screen
        public static void DisplayNewFrequentMACs (Statistic_Window window, List<FrequentMAC> frequentMACs)
        {
            int index = 1;

            frequentMACs = frequentMACs.OrderByDescending(t => t.TotalSecondsOfActiity).ToList();

            //First clear the values insider the FM graph
            window.FM_Map.Series.Clear();

            //Updating the FM graph
            foreach (FrequentMAC mac in frequentMACs)
            {
                LineSeries serie = new LineSeries
                {
                    DataContext = window,
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    StrokeThickness = 3,
                    PointGeometrySize = 11,
                    PointForeground = Brushes.WhiteSmoke,
                    Stroke = mac.Color,
                    Title = "",
                    Values = new ChartValues<DateTimePoint>()
                };
                foreach (KeyValuePair<DateTime, TimeSpan> tuple in mac.Activity.OrderBy(t => t.Key))
                {
                    serie.LabelPoint = value =>
                    {
                        return "Device " + mac.MAC + "\n" +
                               tuple.Key.ToString(System.Globalization.CultureInfo.CurrentCulture) + " - " +
                               tuple.Key.Add(tuple.Value).ToString(System.Globalization.CultureInfo.CurrentCulture);
                    };
                    serie.Values.Add(new DateTimePoint(tuple.Key, index));
                    serie.Values.Add(new DateTimePoint(tuple.Key.Add(tuple.Value), index));
                    serie.Values.Add(new DateTimePoint(tuple.Key.Add(tuple.Value), double.NaN));
                }
                window.FM_Map.Series.Add(serie);
                index++;
            }

            /*foreach (FrequentMAC mac in frequentMACs.OrderByDescending(t => t.GetTotalSecondsOFActivity()).ToList())
            {
                List<LineSeries> series = new List<LineSeries>();
                foreach (KeyValuePair<DateTime, TimeSpan> tuple in mac.Activity.OrderBy(t => t.Key))
                {
                    LineSeries serie = new LineSeries
                    {
                        DataContext = window,
                        Fill = Brushes.Transparent,
                        LabelPoint = value =>
                        {
                            return "Device " + mac.MAC + "\n" +
                                   tuple.Key.ToString(System.Globalization.CultureInfo.CurrentCulture) + " - " +
                                   tuple.Key.Add(tuple.Value).ToString(System.Globalization.CultureInfo.CurrentCulture);
                        },
                        LineSmoothness = 0,
                        PointGeometrySize = 11,
                        PointForeground = Brushes.WhiteSmoke,
                        Stroke = mac.Color,
                        StrokeThickness = 3,
                        Title = "",
                        Values = new ChartValues<DateTimePoint> {
                                new DateTimePoint(tuple.Key, index),
                                new DateTimePoint(tuple.Key.Add(tuple.Value), index)
                        }
                    };
                    series.Add(serie);
                }
                window.FM_Map.Series.AddRange(series);
                index++;
            }*/
        }
    }
}
