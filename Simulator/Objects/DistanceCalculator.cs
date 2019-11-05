using System;

namespace Simulator.Objects
{
    public static class DistanceCalculator  
    {
        public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)//calculates the distance between two points using haversine formula(calculates the shortest distance between two latlong points over the earth surface - using as the crow-flies distance(ignoring hills, etc))
        {
            //Radian = (PI*Degree)/180
            double R = 6371000; //earth radius in meters
            var lat1Rad = ToRadians(lat1); //lat1 in radians
            var lat2Rad = ToRadians(lat2); //lat2 in radians
            var lat2Lat1Rad =ToRadians(lat2-lat1); //lat2-lat1 in radians
            var lon2Lon1Rad = ToRadians(lon2-lon1); //lon2-lon1 in radians
            var a = Math.Sin(lat2Lat1Rad / 2) * Math.Sin(lat2Lat1Rad / 2) + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Sin(lon2Lon1Rad / 2) * Math.Sin(lon2Lon1Rad / 2);
            var c = 2 * Math.Asin(Math.Min(1,Math.Sqrt(a)));
            var distance = R * c;

            return distance;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180;
        }
        public static double DistanceToTravelTime(int speedInKMH, double distance)
        {
            var speedInMetersPerSecond = speedInKMH / 3.6; //transforms speed in km/h to speed in m/s
            var timeToTravel =
                distance /
                speedInMetersPerSecond; // time it takes to travel the distance, travelTime [seconds] = (Distance [meters]/Speed [meters per second])
            return timeToTravel; //time to travel in seconds
        }

        public static double TravelTimeToDistance(int timeToTravelInSeconds, int speedInKMH) //timetotravel in seconds, vehicle speed in km/h
        {
            double distance = 0;
            var speedInMetersPerSecond = speedInKMH / 3.6; //transforms speed in km/h to speed in m/s
            distance = timeToTravelInSeconds * speedInMetersPerSecond;
            return distance;

        }

        public static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            var x = (x1 - x2);
            var y = (y1- y2);
            var distance = Math.Sqrt((x * x) + (y * y));
            return distance;
        }
    }
}