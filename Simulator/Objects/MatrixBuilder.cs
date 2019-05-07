using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.GraphLibrary;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class MatrixBuilder
    {
        public override string ToString()
        {
            return "[" + this.GetType().Name + "] ";
        }

        public long[,] BuildDistanceMatrix(DirectedGraph<Stop,double> dGraph)
        {
            var i = 0;
            var j = 0;
            long[,] distanceMatrix = new long[dGraph.VerticesNumber(), dGraph.VerticesNumber()];
            DistanceCalculator dcalc = new DistanceCalculator();
            Console.WriteLine(this.ToString()+"Generating distance matrix...");
            foreach (var stopO in dGraph.GetVertexSet())
            {
                foreach (var stopD in dGraph.GetVertexSet())
                {

                    if (stopD == stopO)
                    {
                        distanceMatrix[i, j] = 0;
                    }
                    else
                    {
                        var dist = Convert.ToInt32(dcalc.CalculateDistance(stopO.Latitude, stopO.Longitude,
                            stopD.Latitude, stopD.Longitude));
                        distanceMatrix[i, j] = dist;
                    }

                    j++;
                }

                j = 0;
                i++;
            }

            Console.WriteLine(this.ToString()+"Distance matrix successfully generated, matrix size:" + distanceMatrix.Length);
 
            return distanceMatrix;
        }
    }
}
