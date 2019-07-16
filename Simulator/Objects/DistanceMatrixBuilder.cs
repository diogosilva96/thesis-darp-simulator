using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.GraphLibrary;
using Simulator.GraphLibrary;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class DistanceMatrixBuilder
    {
        public override string ToString()
        {
            return "[" + this.GetType().Name + "] ";
        }

        public long[,] Generate(List<Stop> stops)
        {

            long[,] distanceMatrix = new long[stops.Count, stops.Count];
            HaversineDistanceCalculator distanceCalculator = new HaversineDistanceCalculator();
            Console.WriteLine(this.ToString()+"Generating Distance Matrix...");
            for (int i = 0; i < stops.Count; i++)
            {
                for (int j = 0; j < stops.Count; j++)
                {
                    var distance=0;
                    if (i == j)
                    {
                        distance = 0;
                    }
                    else
                    {
                        
                        distance = Convert.ToInt32(distanceCalculator.Calculate(stops[i].Latitude, stops[i].Longitude,
                            stops[j].Latitude, stops[j].Longitude));
                    }
                    distanceMatrix[i, j] = distance;
                }
            }
            Console.WriteLine(this.ToString()+"Distance matrix successfully generated, matrix size:" + distanceMatrix.Length);
 
            return distanceMatrix;
        }

        public void Print(long[,] distanceMatrix)
        {
            var counter = 0;
            foreach (var val in distanceMatrix)
            {
                if (counter == distanceMatrix.GetLength(1)-1)
                {
                    counter = 0;
                    Console.WriteLine(val + " ");
                }
                else
                {
                    Console.Write(val + " ");
                    counter++;
                }
            }
        }


    }
}
