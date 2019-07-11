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

        public long[,] Build(List<Stop> stops)
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

        public long[,] BuildFullMatrix(DirectedGraph<Stop, double> directedGraph)
        {
            var i = 0;
            var j = 0;
            long[,] distanceMatrix = new long[directedGraph.VerticesNumber(), directedGraph.VerticesNumber()];
            HaversineDistanceCalculator distanceCalculator = new HaversineDistanceCalculator();
            Console.WriteLine(this.ToString() + "Generating Distance Matrix...");
            foreach (var stopO in directedGraph.GetVertexSet())
            {
                foreach (var stopD in directedGraph.GetVertexSet())
                {

                    if (stopD == stopO)
                    {
                        distanceMatrix[i, j] = 0;
                    }
                    else
                    {
                        var dist = Convert.ToInt32(distanceCalculator.Calculate(stopO.Latitude, stopO.Longitude,
                            stopD.Latitude, stopD.Longitude));
                        distanceMatrix[i, j] = dist;
                    }

                    j++;
                }

                j = 0;
                i++;
            }

            Console.WriteLine(this.ToString() + "Distance matrix successfully generated, matrix size:" + distanceMatrix.Length);

            return distanceMatrix;
        }
    }
}
