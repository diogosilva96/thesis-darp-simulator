﻿using System;

namespace GraphLibrary.Objects
{
    public class
        DistanceCalculator //class to calculate the distance between two points using haversine formula (calculates the shortest distance between two latlong points over the earth surface - using as the crow-flies distance (ignoring hills,etc))
    {
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            //Radian = (PI*Degree)/180
            double R = 6371000; //earth radius in meters
            var lat1Rad = Math.PI * lat1 / 180; //lat1 in radians
            var lat2Rad = Math.PI * lat2 / 180; //lat2 in radians
            var lat2Lat1Rad = Math.PI * (lat2 - lat1) / 180; //lat2-lat1 in radians
            var lon2Lon1Rad = Math.PI * (lon2 - lon1) / 180; //lon2-lon1 in radians
            var a = Math.Sin(lat2Lat1Rad / 2) * Math.Sin(lat2Lat1Rad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Sin(lon2Lon1Rad / 2) * Math.Sin(lon2Lon1Rad / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return distance;
        }
    }
}