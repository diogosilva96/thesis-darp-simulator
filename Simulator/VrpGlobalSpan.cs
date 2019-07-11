using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;

namespace Simulator
{
    public class VrpGlobalSpan
    {
        public class DataModel
        {
            public long[,] DistanceMatrix;

            public int VehicleNumber;
            public int Depot;



            public DataModel(long[,] distanceMatrix, int vehicleNumber, int depot)
            {
                DistanceMatrix = distanceMatrix;
                VehicleNumber = vehicleNumber;
                Depot = depot;
            }

        };

        public static void PrintSolution(
            in DataModel data,
            in RoutingModel routing,
            in RoutingIndexManager manager,
            in Assignment solution)
        {
            // Inspect solution.
            long maxRouteDistance = 0;
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                Console.WriteLine("Route for Vehicle {0}:", i);
                long routeDistance = 0;
                var index = routing.Start(i);
                while (routing.IsEnd(index) == false)
                {
                    Console.Write("{0} -> ", manager.IndexToNode((int)index));
                    var previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
                }
                Console.WriteLine("{0}", manager.IndexToNode((int)index));
                Console.WriteLine("Distance of the route: {0}m", routeDistance);
                maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
            }
            Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
        }
    }
}
