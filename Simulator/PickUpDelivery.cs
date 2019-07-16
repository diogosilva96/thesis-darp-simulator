using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects;

namespace Simulator
{
    public class VrpGlobalSpan
    {
        public class DataModel
        {
            public long[,] DistanceMatrix;

            public int VehicleNumber;
            public int Depot;
            public int[][] PickupsDeliveries;


            public DataModel(long[,] distanceMatrix, int vehicleNumber, int depot, int[][] pickupsDeliveries)
            {
                DistanceMatrix = distanceMatrix;
                VehicleNumber = vehicleNumber;
                Depot = depot;
                PickupsDeliveries = pickupsDeliveries;

            }

        };

        public static void PrintSolution(
            in DataModel data,
            in RoutingModel routing,
            in RoutingIndexManager manager,
            in Assignment solution, StreamWriter sw, List<Stop> stops)
        {
            // Inspect solution.
            long maxRouteDistance = 0;
            long totalDistance = 0;
            Console.WriteLine("-----------------------------------------------------------------------");
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                Console.WriteLine("Route for Vehicle {0}:", i);
                var stopInd = 0;
                long routeDistance = 0;
                List<int> stopIdList = new List<int>();
                var index = routing.Start(i);
                while (routing.IsEnd(index) == false)
                {
                    stopInd = manager.IndexToNode((int)index);
                    Console.Write("{0} -> ", stops[stopInd].Id);
                    stopIdList.Add(stops[stopInd].Id);
                    var previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    var distance = routing.GetArcCostForVehicle(previousIndex, index, 0);
                    routeDistance += distance;
                }

                stopInd = manager.IndexToNode((int)index);
                Console.WriteLine("{0}", stops[stopInd].Id);
                stopIdList.Add(stopInd > stops.Count ? stops[0].Id : stops[stopInd].Id);
                var Order = 1;
                foreach (var stopId in stopIdList)
                {

                    sw.WriteLine(i + "," + stopId + "," + Order + "," + data.VehicleNumber);
                    Console.WriteLine(i + "," + stopId + "," + Order + "," + data.VehicleNumber);
                    Order++;
                }
                Console.WriteLine("Distance of the route: {0}m", routeDistance);
                totalDistance = routeDistance + totalDistance;
                maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
            }
            Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
            Console.WriteLine("Total distance traveled: {0}m", totalDistance);
            sw.Flush();
        }

        public static void PrintSolution(
            in DataModel data,
            in RoutingModel routing,
            in RoutingIndexManager manager,
            in Assignment solution, List<Stop> stops)
        {
            long totalDistance = 0;
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                Console.WriteLine("Route for Vehicle {0}:", i);
                var stopInd = 0;
                long routeDistance = 0;
                var index = routing.Start(i);
                while (routing.IsEnd(index) == false)
                {
                    stopInd = manager.IndexToNode((int)index);
                    Console.Write("{0} -> ", stops[stopInd].Id);
                    var previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
                }
                stopInd = manager.IndexToNode((int)index);
                Console.WriteLine("{0}", stops[stopInd].Id);
                Console.WriteLine("Distance of the route: {0}m", routeDistance);
                totalDistance += routeDistance;
            }
            Console.WriteLine("Total Distance of all routes: {0}m", totalDistance);
        }
    }
}
