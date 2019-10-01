﻿using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class MatrixBuilder
    {
        public override string ToString()
        {
            return "[" + this.GetType().Name + "] ";
        }

        public long[,] GetDistanceMatrix(List<Stop> stops)
        {

            long[,] distanceMatrix = new long[stops.Count, stops.Count];
            //Console.WriteLine(this.ToString()+"Generating Distance TimeMatrix...");
            for (int i = 0; i < stops.Count; i++)
            {
                for (int j = 0; j < stops.Count; j++)
                {
                    long distance=0;
                    if (i == j)
                    {
                        distance = 0;
                    }
                    else
                    {
                        
                        distance = (long)DistanceCalculator.CalculateHaversineDistance(stops[i].Latitude, stops[i].Longitude,
                            stops[j].Latitude, stops[j].Longitude);
                    }
                    distanceMatrix[i, j] = distance;
                }

           
            }
            //Console.WriteLine(this.ToString()+"Distance matrix successfully generated, matrix size:" + distanceMatrix.Length);
 
            return distanceMatrix;
        }

        public long[,] GetTimeMatrix(List<Stop> stops,int speedInKmHour)
        {
            
            long[,] timeMatrix = new long[stops.Count, stops.Count];
            //Console.WriteLine(this.ToString()+"Generating Distance TimeMatrix...");
            for (int i = 0; i < stops.Count; i++)
            {
                for (int j = 0; j < stops.Count; j++)
                {
                    long distance = 0;
                    if (i == j)
                    {
                        distance = 0;
                    }
                    else
                    {

                        distance = (long)DistanceCalculator.CalculateHaversineDistance(stops[i].Latitude, stops[i].Longitude,
                            stops[j].Latitude, stops[j].Longitude);
                    }
                     var timeInSeconds = (long)DistanceCalculator.DistanceToTravelTime(speedInKmHour,distance);
                     //timeMatrix[i, j] = (long)TimeSpan.FromSeconds(timeInSeconds).TotalMinutes; // time in minutes
                     timeMatrix[i, j] = (long) timeInSeconds;
                }
            }
            //Console.WriteLine(this.ToString()+"Distance matrix successfully generated, matrix size:" + distanceMatrix.Length);

            return timeMatrix;
        }

    }
}
