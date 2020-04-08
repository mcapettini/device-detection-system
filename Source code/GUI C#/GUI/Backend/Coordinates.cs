using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.Backend
{
    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * represent a point in a bidimensional space
     * used to denote the position of a device
     */
    public class Coordinates
    {
        // ~-----constants----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static Coordinates origin = new Coordinates(0, 0);


        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private double x, y;


        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Coordinates() {
            X = 0;
            Y = 0;
        }

        public Coordinates(double x, double y) {
            X = x;
            Y = y;
        }


        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /* Nicolò:
         * computes the Euclidean distance between the current point and the one provided as parameter
         */
        public double Distance(Coordinates c) {
            return Math.Sqrt(Math.Pow(this.x - c.x, 2) + Math.Pow(this.y - c.y, 2));
        }

        /* Nicolò:
         * computes the legth \rho of the vector identified by the current point (distance with respect to the origin of the axis)
         */
        public double Radius() {
            return Distance(origin);
        }

        /* Nicolò:
         * computes the angular coordinate \theta of the vector identified by the current point (arctan of the ration of the two cartesian coordinates)
         */
        public double Angle() {
            return Angle(origin);
        }

        /* Nicolò:
         * computes the angular coordinate \theta of the vector identified by the current point (arctan of the ration of the two cartesian coordinates),
         * with respect to the new origin of the axis (passed as argument)
         */
        public double Angle(Coordinates newOrigin)
        {
            double angle = Math.Atan2(this.y - newOrigin.y, this.x - newOrigin.x);
            if (angle < 0)
                angle += 2 * Math.PI;
            return angle;
        }

        /* Nicolò:
         * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
         * (with respect to the positive X axis)
         */
        public static List<Coordinates> sort(List<Coordinates> list)
        {
            // local variables
            Coordinates centroid = new Coordinates(0, 0);
            List<Coordinates> orderedList = new List<Coordinates>(list);

            // compute centroid
            foreach (Coordinates c in list)
            {
                centroid.x += c.x;
                centroid.y += c.y;
            }
            centroid.x = centroid.x / list.Count;
            centroid.y = centroid.y / list.Count;

            // sort in anti-clockwise order, according to ascending angle
            orderedList = orderedList.OrderBy(a => a.Angle(centroid)).ToList();

            return orderedList;
        }

        /* Nicolò:
         * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
         * (with respect to the positive X axis)
         */
        public static List<Coordinates> EquispacedPoints(int numberBoards)
        {
            // local variables
            double r = 5, angle_quantum, angle_sum = 0;
            List<Coordinates> list = new List<Coordinates>();

            // split \pi into N equivalent angles
            angle_quantum = 2 * Math.PI / numberBoards;

            // compute Re[z] and Im[z] for each point
            angle_sum = angle_quantum / 2;
            for (int i=0; i<numberBoards; i++)
            {
                Coordinates c = new Coordinates();
                c.x = r * Math.Cos(angle_sum);
                c.y = r * Math.Sin(angle_sum);

                list.Add(c);
                angle_sum += angle_quantum;
            }

            // return computed points
            return list;
        }

        /* Nicolò:
         * given a 2D segment (identified by its starting and ending point) and the number of desired points, 
         * compute the coordinates of equispaced points that stands on this segment
         */
        public static List<Coordinates> PointsInTheMiddle(Coordinates pointBegin, Coordinates pointEnd, int numberBoards)
        {
            // local variables
            double totX, totY, fragmentX, fragmentY;
            Coordinates curr, last = pointBegin;
            List<Coordinates> list = new List<Coordinates>();

            // check input parameters
            if (numberBoards == 0)
                return list;
            if (numberBoards < 0)
                return null;

            // handle special cases
            if (pointBegin.Equals(pointEnd))
            {
                // single point to add
                if (numberBoards == 1)
                {
                    Coordinates pointOpposite;
                    if (pointBegin.X != 0 && pointBegin.Y != 0)
                        pointOpposite = new Coordinates(-pointBegin.X, -pointBegin.Y);
                    else
                        pointOpposite = new Coordinates(1, 0);
                    list.Add(pointOpposite);
                }
                // several points to add
                else
                {
                    list = EquispacedPoints(numberBoards);                    
                }
                return list;
            }

            // compute distances
            totX = pointEnd.X - pointBegin.X;
            totY = pointEnd.Y - pointBegin.Y;
            fragmentX = totX / (numberBoards + 1);
            fragmentY = totY / (numberBoards + 1);

            // compute points position
            for (int i=0; i<numberBoards; i++, last = curr)
            {
                curr = new Coordinates(last.X + fragmentX, last.Y + fragmentY);
                list.Add(curr);
            }

            // return computed points
            return list;
        }

        /* Nicolò:
         * given a polygon (identified by the ordered list of its nodes),
         * checks if there are no intersections between its edges (i.e. it is simple)
         */
        public static Boolean IsASimplePolygon(List<Coordinates> polygon)
        {
            // local variables
            List<Coordinates> closedPolygon;

            // check input parameters
            if (polygon.Count <= 0)
                throw new Exception("The provided list of points does not constitute a polygon");

            // special case
            if (polygon.Count == 3)
                return true;    //triangles never have intersections

            // add first node, to the end of the list
            closedPolygon = new List<Coordinates>(polygon);
            closedPolygon.Add(polygon[0]);

            // iterate over edges
            for (int i=0; i< polygon.Count-1; i++)
            {
                // first edge
                Coordinates seg1_start = closedPolygon[i];
                Coordinates seg1_end = closedPolygon[i + 1];

                for (int j=i+2; j< polygon.Count; j++)
                {
                    // second edge
                    Coordinates seg2_start = closedPolygon[j];
                    Coordinates seg2_end = closedPolygon[j + 1];

                    // avoid consecutive edges
                    if ((j+1) % polygon.Count == i)
                        continue;

                    // if the segments have intersections, the polygon is NOT simple
                    if (doIntersect(seg1_start, seg1_end, seg2_start, seg2_end))
                        return false;
                }
            }
            
            // return value for no intersections
            return true;
        }

        // Given three colinear Coordinatess p, q, r, the function checks if 
        // Coordinates q lies on line segment 'pr' 
        private static bool onSegment(Coordinates p, Coordinates q, Coordinates r)
        {
            if (q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int orientation(Coordinates p, Coordinates q, Coordinates r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-Coordinatess/ for details of below formula. 
            double val = (q.y - p.y) * (r.x - q.x) -
                      (q.x - p.x) * (r.y - q.y);

            if (val == 0) return 0;  // colinear 

            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }

        // The main function that returns true if line segment 'p1q1' and 'p2q2' intersect. 
        private static bool doIntersect(Coordinates p1, Coordinates q1, Coordinates p2, Coordinates q2)
        {
            // Find the four orientations needed for general and 
            // special cases 
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases 
        }



        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }


        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString() {
            return base.ToString();
        }
    }
}
