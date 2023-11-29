using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{

    public class TurnCalculation
    {

        public static bool CalculateTurn(Coord point1, Coord point2, Coord point3)
        {
            bool turn;

            // Calculate vectors v1 and v2
            double v1x = point2.X - point1.X;
            double v1y = point2.Y - point1.Y;

            double v2x = point3.X - point2.X;
            double v2y = point3.Y - point2.Y;

            // Calculate the cross product of v1 and v2
            double crossProduct = v1x * v2y - v2x * v1y;

            // Determine direction based on the cross product
            if (crossProduct > 0)
            {
                // Left turn
                turn = true;
            }
            else if (crossProduct < 0)
            {
                // Right turn
                turn = true;
            }
            else
            {
                // Forward
                turn = false;
            }

            return turn;
        }
    }

    public class Coord
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Coord(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

}
