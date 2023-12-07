using NetTopologySuite.Operation.Distance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data;

    public class SlowZone
    {
        /* Calculates the speed in a slow zone based on the given distance and current speed */
        public static float CalculateSlowZone(double distance, double slowZoneDistance, float currentSpeed, float acceleration, float maxSpeed)
        {
            float slowzoneSpeed = 10;
           // double slowZoneDistance = 10;
            float deceleration = 2;

            // Check if the distance is within the slow zone
            if (distance <= slowZoneDistance)
            {
                // If inside the slow zone, reduce the speed until it reaches the slow zone speed
                if (currentSpeed > slowzoneSpeed)
                {
                    currentSpeed -= deceleration;

                    // Ensure the speed doesn't go below the slow zone speed
                    currentSpeed = Math.Max(currentSpeed, slowzoneSpeed);
                }
            }
            // If outside the slow zone, increase the speed until it reaches the maximum speed
            else if (distance > slowZoneDistance)
            {
                    currentSpeed += acceleration;

                    // Ensure the speed doesn't go above the maximum speed
                    currentSpeed = Math.Min(currentSpeed, maxSpeed);
            }

            return currentSpeed;
        }
}