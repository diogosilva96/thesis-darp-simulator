using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Events;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Simulation;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //int[] Starts = { 0, 0, 0, 0 };
            //int[] Ends = { 0, 0, 0, 0 };
            //long[] VehicleCapacities = { 10, 10, 10, 10 };
            //int[][] CustomersVehicle = new int[2][];
            //long[,] TimeMatrix =
            //{
            //    {0, 6, 9, 8, 7, 3, 6},
            //    {6, 0, 8, 3, 2, 6, 8},
            //    {9, 8, 0, 11, 10, 6, 3},
            //    {8, 3, 11, 0, 1, 7, 10},
            //    {7, 2, 10, 1, 0, 6, 9},
            //    {3, 6, 6, 7, 6, 0, 2},
            //    {6, 8, 3, 10, 9, 2, 0},

            //};
            //for (int i = 0; i < TimeMatrix.GetLength(0); i++)
            //{
            //    for (int j = 0; j < TimeMatrix.GetLength(1); j++)
            //    {
            //        TimeMatrix[i, j] = (long)TimeSpan.FromMinutes(TimeMatrix[i, j]).TotalSeconds;
            //    }
            //}

            //long[,] TimeWindows =
            //{
            //    {0, 5}, // depot
            //    {7, 12}, // 1
            //    {10, 15}, // 2
            //    {16, 30}, // 3
            //    {10, 13}, // 4
            //    {0, 5}, // 5
            //    {5, 20}, // 6

            //};

            //for (int i = 0; i < TimeWindows.GetLength(0); i++)
            //{
            //    for (int j = 0; j < TimeWindows.GetLength(1); j++)
            //    {
            //        TimeWindows[i, j] = (long)TimeSpan.FromMinutes(TimeWindows[i, j]).TotalSeconds;
            //    }
            //}
            //int[][] PickupsDeliveries = {
            // new int[] {1, 6},
            // new int[] {2, 3},

            //};
            //long[] Demands = { 0, 1, 6, 1, 2, 1, 1 };

            //for (int MaxUpperBound = 0; MaxUpperBound < 30; MaxUpperBound++)
            //{


            //    RoutingIndexManager manager = new RoutingIndexManager(
            //        TimeMatrix.GetLength(0),
            //        VehicleCapacities.Length,
            //        Starts, Ends);
            //    RoutingModel routing = new RoutingModel(manager);
            //    int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
            //    {
            //        var fromNode = manager.IndexToNode(fromIndex);
            //        return Demands[fromNode];
            //    });


            //    int transitCallbackIndex = routing.RegisterTransitCallback(
            //        (long fromIndex, long toIndex) =>
            //        {
            //        // Convert from routing variable Index to distance matrix NodeIndex.
            //        var fromNode = manager.IndexToNode(fromIndex);
            //            var toNode = manager.IndexToNode(toIndex);
            //            return TimeMatrix[fromNode, toNode];
            //        }
            //    );
            //    routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);
            //    routing.AddDimension(
            //        transitCallbackIndex, // transit callback
            //        999999999, // allow waiting time
            //        999999999, // vehicle maximum capacities
            //        false, // start cumul to zero
            //        "Time");
            //    routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, VehicleCapacities, false, "Capacity");
            //    RoutingDimension capacityDimension = routing.GetMutableDimension("Capacity");

            //    RoutingDimension timeDimension = routing.GetMutableDimension("Time");
            //    // Add time window constraints for each location except depot.
            //    var solver = routing.solver();
            //    for (int i = 1; i < TimeWindows.GetLength(0); ++i)
            //    {
            //        long index = manager.NodeToIndex(i);
            //        var lowerBound = TimeWindows[i, 0]; //minimum time to be at current index (lower bound for the timeWindow of current Index)
            //        var softUpperBound = TimeWindows[i, 1]; //soft maxUpperBound for the timeWindow at current index
            //        var upperBound = softUpperBound + MaxUpperBound; //maxUpperBound to be at current index (upperbound for the timeWindow at current index)
            //                                                         //softupperbound and upperbound are different because the upperbound is usually bigger than the softuppberbound in order to soften the current timeWindows, enabling to generate a solution that accomodates more requests
            //        timeDimension.CumulVar(index).SetRange(lowerBound, upperBound); //sets the maximum upper bound and lower bound limit for the timeWindow at the current index
            //        timeDimension.CumulVar(index).SetMax(upperBound);
            //        timeDimension.SetCumulVarSoftUpperBound(index, softUpperBound,
            //            10000); //adds soft upper bound limit which is the requested time window
            //        routing.AddToAssignment(
            //            timeDimension.SlackVar(index)); //add slack var for current index to the assignment
            //        routing.AddToAssignment(
            //            timeDimension.TransitVar(index)); // add transit var for current index to the assignment
            //        routing.AddToAssignment(capacityDimension.TransitVar(index));
            //    }

            //    // Add time window constraints for each vehicle start node.
            //    for (int i = 0; i < VehicleCapacities.Length; i++)
            //    {
            //        long index = routing.Start(i);
            //        if (index != -1)
            //        {
            //            var startDepotIndex = Starts[i];
            //            timeDimension.CumulVar(index).SetRange(
            //                TimeWindows[startDepotIndex, 0],
            //                TimeWindows[startDepotIndex, 1]);
            //            routing.AddToAssignment(timeDimension.SlackVar(index));
            //            routing.AddToAssignment(timeDimension.TransitVar(index));
            //            routing.AddToAssignment(capacityDimension.TransitVar(index));
            //        }
            //    }

            //    for (int i = 0; i < routing.Size(); i++)
            //    {
            //        var pickupDeliveryPairs =
            //            Array.FindAll(PickupsDeliveries,
            //                pd => pd[0] == i); //finds all the pickupdelivery pairs with pickup index i 
            //        foreach (var pickupDelivery in pickupDeliveryPairs) //iterates over each deliverypair to ensure the maximum ride time constraint
            //        {
            //            if (pickupDelivery[0] != -1) //if the pickupDelivery isnt a customer inside a vehicle
            //            {
            //                var deliveryIndex = manager.NodeToIndex(pickupDelivery[1]);
            //                var directRideTimeDuration = TimeMatrix[pickupDelivery[0], pickupDelivery[1]];
            //                var realRideTimeDuration =
            //                    timeDimension.CumulVar(deliveryIndex) -
            //                    timeDimension
            //                        .CumulVar(
            //                            i); //subtracts cumulative value of the ride time of the delivery index with the current one of the current index to get the real ride time duration
            //                solver.Add(realRideTimeDuration <
            //                           directRideTimeDuration +
            //                           30); //adds the constraint so that the current ride time duration does not exceed the directRideTimeDuration + maxCustomerRideTimeDuration
            //            }

            //        }
            //    }

            //    foreach (var pickupDelivery in PickupsDeliveries)
            //    {
            //        long pickupIndex = manager.NodeToIndex(pickupDelivery[0]);
            //        long deliveryIndex = manager.NodeToIndex(pickupDelivery[1]);
            //        routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
            //        solver.Add(solver.MakeEquality(
            //            routing.VehicleVar(pickupIndex),
            //            routing.VehicleVar(deliveryIndex)));
            //        solver.Add(solver.MakeLessOrEqual(
            //            timeDimension.CumulVar(pickupIndex),
            //            timeDimension.CumulVar(deliveryIndex)));
            //    }
            //    //constraints to enforce that if there is a customer inside a vehicle, it has to be served by that vehicle
            //    if (CustomersVehicle != null) //if vehicleDeliveries is null it means there are no customer inside the vehicle for the current routing problem
            //    {
            //        for (int vehicleIndex = 0; vehicleIndex < CustomersVehicle.GetLength(0); vehicleIndex++)
            //        {
            //            if (CustomersVehicle[vehicleIndex] != null)
            //            {
            //                for (int j = 0; j < CustomersVehicle[vehicleIndex].GetLength(0); j++)
            //                {
            //                    var vehicleStartIndex = routing.Start(vehicleIndex);
            //                    var deliveryIndex =PickupsDeliveries[CustomersVehicle[vehicleIndex][0]][1]; //gets the deliveryIndex
            //                    var nodeDeliveryIndex = manager.NodeToIndex(deliveryIndex);
            //                    solver.Add(solver.MakeEquality(routing.VehicleVar(vehicleStartIndex),
            //                        routing.VehicleVar(
            //                            nodeDeliveryIndex))); //vehicle with vehicleIndex has to be the one that delivers customer with nodeDeliveryIndex;
            //                    //this constraint enforces that the vehicle indexed by vehicleIndex has to be the vehicle which services (goes to) the nodeDeliveryIndex as well
            //                }
            //            }
            //        }
            //    }

            //    for (int i = 0; i < VehicleCapacities.Length; ++i)
            //    {
            //        routing.AddVariableMinimizedByFinalizer(
            //            timeDimension.CumulVar(routing.Start(i)));
            //        routing.AddVariableMinimizedByFinalizer(
            //            timeDimension.CumulVar(routing.End(i)));
            //    }



            //    RoutingSearchParameters searchParameters =
            //        operations_research_constraint_solver.DefaultRoutingSearchParameters();
            //    searchParameters.FirstSolutionStrategy =
            //        FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
            //    searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing;
            //    searchParameters.TimeLimit = new Duration { Seconds = 5 };
            //    Assignment solution = routing.SolveWithParameters(searchParameters);
            //    if (solution != null)
            //    {
            //        // Inspect solution.
            //        long totalTime = 0;
            //        long totalLoad = 0;
            //        for (int i = 0; i < VehicleCapacities.Length; ++i)
            //        {
            //            Console.WriteLine("Route for Vehicle {0}:", i);
            //            var index = routing.Start(i);
            //            long routeLoad = 0;
            //            while (routing.IsEnd(index) == false)
            //            {
            //                var capVar = capacityDimension.CumulVar(index);
            //                var capTransit = capacityDimension.TransitVar(index);
            //                var timeVar = timeDimension.CumulVar(index);
            //                var slackVar = timeDimension.SlackVar(index);
            //                var transitVar = timeDimension.TransitVar(index);
            //                Console.Write("{0} Time: C({1},{2}) S({3},{4}) T({5}) Capacity: C({6}) T({7})-> ",
            //                    manager.IndexToNode(index),
            //                    solution.Min(timeVar),
            //                    solution.Max(timeVar), solution.Min(slackVar), solution.Max(slackVar),
            //                    solution.Value(transitVar), solution.Value(capVar), solution.Value(capTransit));
            //                index = solution.Value(routing.NextVar(index));
            //            }
            //            var endTimeVar = timeDimension.CumulVar(index);
            //            var endCapVar = capacityDimension.CumulVar(index);
            //            routeLoad += solution.Value(endCapVar);
            //            totalLoad += routeLoad;
            //            Console.WriteLine("{0} Time: C({1},{2}) Capacity: C({3})",
            //                manager.IndexToNode(index),
            //                solution.Min(endTimeVar),
            //                solution.Max(endTimeVar), solution.Value(endCapVar));
            //            Console.WriteLine("Time of the route: {0}min", solution.Min(endTimeVar));
            //            Console.WriteLine("Route load: " + routeLoad);
            //            totalTime += solution.Min(endTimeVar);
            //        }
            //        Console.WriteLine("Total time of all routes: {0}min", totalTime);
            //        Console.WriteLine("Total Load of all routes: " + totalLoad + "min");
            //        Console.WriteLine("Avg route Total Time:{0}", totalTime / VehicleCapacities.Length);
            //        break;
            //    }
            //    else
            //    {
            //        Console.WriteLine("No sol found upperbound:" + MaxUpperBound);
            //    }
            //}

            //var routingDataModel = new RoutingDataModel(15*60);
            //routingDataModel.Starts = Starts;
            //routingDataModel.Ends = Ends;
            //routingDataModel.Demands = Demands;
            //routingDataModel.MaxAllowedUpperBoundTime = 30 * 60;
            //routingDataModel.PickupsDeliveries = PickupsDeliveries;
            //routingDataModel.TimeWindows = TimeWindows;
            //routingDataModel.TravelTimes = TimeMatrix;
            //routingDataModel.VehicleCapacities = VehicleCapacities;
            //routingDataModel.CustomersVehicle = CustomersVehicle;
            //RoutingSolver Solver = new RoutingSolver(routingDataModel,false);
            //var Solution = Solver.TryGetSolution(null);
            //Solver.PrintSolutionUsingRoutingVars(Solution);

            //var dataStructurer = new AlgorithmDataStructurer();
            //dataStructurer.StructureFile(Path.Combine(@Path.Combine(Environment.CurrentDirectory, @"Logger", @"Algorithms.csv")));
            var stops = TransportationNetwork.Stops;
            SimulationParams simulationParams = new SimulationParams(30 * 60, 30 * 60, 0.02);
            AbstractSimulation sim = new Simulation(simulationParams);
            sim.MainLoop();
            Console.Read();

        }
    }
}
