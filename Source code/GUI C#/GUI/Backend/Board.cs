using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.Backend
{
    // ~-----class------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    /* Nicolò:
     * represent an ESP32
     */
    public class Board
    {
        // ~-----fields-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private Configuration configuration;
        private string boardID;
        private Coordinates coordinates;
        private string note;



        // ~-----constructors and destructors---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Board(Configuration configuration, string id, Coordinates coordinates)
        {
            // set current Board fields
            this.configuration = configuration;
            this.boardID = id;
            this.coordinates = coordinates;

            // update the Configuration to which this Board belongs to
            if (configuration.Boards.Where(b => b.boardID.Equals(this.boardID)).Count() == 0)
                this.configuration.BoardAdd(this);
        }

        public Board(Configuration configuration, string id, double x, double y)
        {
            // set current Board fields
            this.configuration = configuration;
            this.boardID = id;
            this.coordinates = new Coordinates(x, y);

            // update the Configuration to which this Board belongs to
            if (configuration.Boards.Where(b => b.boardID.Equals(this.boardID)).Count() == 0)
                this.configuration.BoardAdd(this);
        }

        public Board(string id, Coordinates coordinates)
        {
            // set current Board fields
            this.boardID = id;
            this.coordinates = coordinates;
        }

        public Board(string id, double x, double y)
        {
            // set current Board fields
            this.boardID = id;
            this.coordinates = new Coordinates(x, y);
        }


        // ~-----methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ~-----properties---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string BoardID
        {
            get { return boardID; }
            set { boardID = value; }
        }

        public Coordinates Coordinates
        {
            get { return coordinates; }
            set { coordinates = value; }
        }

        public double X {
            get { return coordinates.X; }
            set { coordinates.X = value; }
        }

        public double Y {
            get { return coordinates.Y; }
            set { coordinates.Y = value; }
        }

        public Configuration Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public string Note {
            get => note;
            set => note = value;
        }


        // ~-----output representation----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return base.ToString();
        }
    }

}
