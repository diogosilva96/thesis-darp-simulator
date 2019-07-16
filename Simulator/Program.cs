using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.GraphLibrary;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using GraphLibrary;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.Objects.Data_Objects;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph();
            //DirectedGraph<Stop, double> dGraph = stopsNetworkGraph.StopsGraph;
            //DistanceMatrixBuilder matrixBuilder = new DistanceMatrixBuilder();
            //var distanceMatrix = matrixBuilder.Generate(dGraph);


            //var _trips = tripStopsDataObject._trips;
            //DirectedGraph<Stop, double> stopsGraph = new DirectedGraph<Stop, double>();
            //stopsGraph.AddVertex(_trips[0].Stops);
            //var ind = 0;
            //var rnd = new Random();

            //HaversineDistanceCalculator dc = new HaversineDistanceCalculator();
            //foreach (var Stop in _trips[0].Stops)
            //{
            //    if (ind < _trips[0].Stops.Count - 1)
            //    {
            //        var nextStop = _trips[0].Stops[ind + 1];
            //        var w = dc.Calculate(Stop.Latitude, Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
            //        stopsGraph.AddEdge(Stop, nextStop, w);
            //        stopsGraph.AddEdge(nextStop, Stop, w + rnd.Next(1, 16));
            //    }

            //    ind++;
            //}





            //AbstractSimulation sim = new Simulation();
            //sim.Simulate();
            //Console.Read();

            var RoutesDataObject = new RoutesDataObject(true);
            var stopsNetworkGraph = new StopsNetworkGraphLoader(RoutesDataObject.Stops, RoutesDataObject.Routes);
            
            DistanceMatrixBuilder distanceMatrixBuilder = new DistanceMatrixBuilder();
            Random rand = new Random();
            var stopList = new List<Stop>();
            var depot = RoutesDataObject.Stops.Find(s => s.Id == 2183);
            Stop stopToBeAdded;
            //static generated stops
            Console.WriteLine("Static generated stops:");
            stopList.Add(depot);
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 438));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 2430));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 1359));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 1106));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 2270));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 2018));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 1523));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 2319));
            //stopList.Add(RoutesDataObject.Stops.Find(s => s.Id == 1884));

            //Console.WriteLine("Randomly generated stops:");
            //for (int index = 0; index < 10; index++)
            //{
            //    label:
            //    var stop = RoutesDataObject.Stops[rand.Next(0, RoutesDataObject.Stops.Count)];
            //    if (stopList.Contains(stop))
            //    {
            //       goto label;
            //    }
            //    stopList.Add(stop);
            //    Console.WriteLine(stop.Id);
            //}

            var numExecutions = 2;
            var depotInd = stopList.FindIndex(s => s.Id == depot.Id);
            var distanceMatrix = distanceMatrixBuilder.Generate(stopList);
            distanceMatrixBuilder.Print(distanceMatrix);
            StreamWriter sw = new StreamWriter(Path.Combine(Environment.CurrentDirectory, @"Logger/or_tools.txt"), false);
            sw.WriteLine("VehicleId,StopId,Order,VehicleNumber");

            int[][] pickupsDeliveriesStopId = {
                new int[] {438, 2430},
                new int[] {1106, 1359},
                new int[] {2270, 2018},
                new int[] {2319, 1523},
                new int[] {438, 1884},
                new int[] {399, 555}, 
            };
            int[][] pickupsDeliveries = { };
            // transforms from stop id into index of distance matrix
            foreach (var pickupDelivery in pickupsDeliveriesStopId)
            {
                for (int i = 0; i < 2; i++)
                {
                    var stop = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[i]);

                    if (!stopList.Contains(stop))
                    {
                        stopList.Add(stop);
                    }
                }

                var pickup = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[0]);
                var delivery = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[1]);
                var pickupDeliveryInd = new int[] { stopList.IndexOf(pickup), stopList.IndexOf(delivery) };
                pickupsDeliveries.Append(pickupDeliveryInd);
            }

            distanceMatrix = distanceMatrixBuilder.Generate(stopList);
            Console.WriteLine("Total stops:" + stopList.Count);





        //GOOGLE OR TOOLS
        // Instantiate the data problem.
        executeLabel:
        VrpGlobalSpan.DataModel data = new VrpGlobalSpan.DataModel(distanceMatrix,numExecutions,0,pickupsDeliveries);


            // Create Routing Index Manager
            RoutingIndexManager manager = new RoutingIndexManager(
                data.DistanceMatrix.GetLength(0),
                data.VehicleNumber,
                data.Depot);


            // Create Routing Model.
            RoutingModel routing = new RoutingModel(manager);

            // Create and register a transit callback.
            int transitCallbackIndex = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) => {
            // Convert from routing variable Index to distance matrix NodeIndex.
            var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return data.DistanceMatrix[fromNode, toNode];
                }
                );

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Add Distance constraint.
            routing.AddDimension(transitCallbackIndex, 0, 9999999,
                true,  // start cumul to zero
                "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Define Transportation Requests.
            Solver solver = routing.solver();
            for (int i = 0; i < data.PickupsDeliveries.GetLength(0); i++)
            {
                long pickupIndex = manager.NodeToIndex(data.PickupsDeliveries[i][0]);
                long deliveryIndex = manager.NodeToIndex(data.PickupsDeliveries[i][1]);
                routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                solver.Add(solver.MakeEquality(
                      routing.VehicleVar(pickupIndex),
                      routing.VehicleVar(deliveryIndex)));
                solver.Add(solver.MakeLessOrEqual(
                      distanceDimension.CumulVar(pickupIndex),
                      distanceDimension.CumulVar(deliveryIndex)));
            }

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
              operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
              FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // Solve the problem.
            Assignment solution = routing.SolveWithParameters(searchParameters);

            // Print solution on console.
            //VrpGlobalSpan.PrintSolution(data, routing, manager, solution,sw,stopList);
            VrpGlobalSpan.PrintSolution(data, routing, manager, solution,stopList);
            numExecutions--;
            if (numExecutions != 0)
            {
                goto executeLabel;
            }

        }
    }
}
