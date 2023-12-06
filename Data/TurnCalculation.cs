using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for determining turns from Routes
    /// <br/>
    /// For Simulation Preprocessing
    /// </summary>
    public class TurnCalculation
    {
        public static void TurnCalculationTest()
        {
            Coor coordinate1 = new Coor(6, 5);
            Coor coordinate2 = new Coor(50, 3);
            Coor coordinate3 = new Coor(70, 1);

            CalculateTurn(coordinate1, coordinate2, coordinate3);
        }

        /// <summary>
        /// Calculates if a gap between 3 given points is a turn
        /// </summary>
        /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
        /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
        /// <param name="point3">(Coor(double X, double Y)) End point</param>
        /// <returns>(bool) Is gap between given points a turn</returns>
        public static void CalculateTurn(Coor point1, Coor point2, Coor point3)
        {
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
                System.Diagnostics.Debug.WriteLine("Left");
            }
            else if (crossProduct < 0)
            {
                System.Diagnostics.Debug.WriteLine("Right");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Forward");
            }
        }
    }

    public class Coor
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Coor(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

}
