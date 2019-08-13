using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP.DataModels;

namespace Simulator.Objects.Data_Objects.DARP.Solvers
{
    public class PickupDeliveryTimeWindowSolver:Solver //Vehicle routing problem with pickup and delivery time windows solver
    {
        public override void InitHookMethod()
        {
            if (DataModel is PickupDeliveryTimeWindowModel pdtwModel)
            {

                RoutingModel.SetArcCostEvaluatorOfAllVehicles(TransitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.

                // Add Distance constraint.
                RoutingModel.AddDimension(TransitCallbackIndex, 9999999, 99999999,
                    true, // start cumul to zero
                    "Distance"); 
                RoutingDimension distanceDimension = RoutingModel.GetMutableDimension("Distance");
                distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                var constraintSolver = RoutingModel.solver(); //Gets the underlying constraint solver
                for (int i = 0; i < DataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][1]); //delivery index
                    RoutingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    constraintSolver.Add(constraintSolver.MakeEquality(
                        RoutingModel.VehicleVar(pickupIndex),
                        RoutingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    constraintSolver.Add(constraintSolver.MakeLessOrEqual(
                        distanceDimension.CumulVar(pickupIndex),
                        distanceDimension.CumulVar(deliveryIndex)));//Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                }
                //Add Time window constraint.
                RoutingModel.AddDimension(
                    TransitCallbackIndex, // transit callback
                    999999, // allow waiting time
                    999999, // maximum travel time per vehicle
                    true, // start cumul to zero
                    "Time"); 
                RoutingDimension timeDimension = RoutingModel.GetMutableDimension("Time");
                // Add time window constraints for each location except depot.
                for (int i = 1; i < pdtwModel.TimeWindows.GetLength(0); ++i)
                {
                    long index = RoutingIndexManager.NodeToIndex(i); //gets the node index
                    timeDimension.CumulVar(index).SetMin(pdtwModel.TimeWindows[i,0]); //Sets the minimum upper bound limit
                    timeDimension.CumulVar(index).SetMax(pdtwModel.TimeWindows[i,1]+5); //Sets the maximum upper bound limit
                    timeDimension.SetCumulVarSoftUpperBound(index,pdtwModel.TimeWindows[i,1],1); //adds soft upper bound limit which is the requested time window
                }
                
                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    long index = RoutingModel.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        pdtwModel.TimeWindows[0, 0],
                        pdtwModel.TimeWindows[0, 1]); //this guarantees that a vehicle must visit the location during its time window
                }

                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    RoutingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(RoutingModel.Start(i)));
                    RoutingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(RoutingModel.End(i)));
                }
            }
        }

        public override void PrintSolution(Assignment solution)
        {
            if (solution != null)
            {
                var timeDim = RoutingModel.GetMutableDimension("Time");
                var distanceDim = RoutingModel.GetMutableDimension("Distance");
                long totalTime = 0;
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    int stopInd = 0;
                    Console.WriteLine("Route for Vehicle {0}:",i);
                    var index = RoutingModel.Start(i);
                    while (RoutingModel.IsEnd(index) == false)
                    {
                        var timeVar = timeDim.CumulVar(index);
                        stopInd = RoutingIndexManager.IndexToNode(index);
                        Console.Write(DataModel.IndexToStop(stopInd) + " Time({0},{1}) -> ",
                            solution.Min(timeVar),
                            solution.Max(timeVar));
                        index = solution.Value(RoutingModel.NextVar(index));
                    }
                    var endTimeVar = timeDim.CumulVar(index);
                    stopInd = RoutingIndexManager.IndexToNode(index);
                    Console.WriteLine(DataModel.IndexToStop(stopInd) + "Time({0},{1})",
                        solution.Min(endTimeVar),
                        solution.Max(endTimeVar));
                    Console.WriteLine("Time of the route: {0}min", solution.Min(endTimeVar));
                    totalTime += solution.Min(endTimeVar);
                }
                Console.WriteLine("Total time of all routes: {0}min", totalTime);
            }
            else
            {
                throw new ArgumentNullException("Solution is null");
            }
        }
        //public override void Print(Assignment solution)
        //{
        //    if (solution != null)
        //    {
        //        RoutingDimension timeDimension = RoutingModel.GetMutableDimension("Time");
        //        // Inspect solution.
        //        long totalTime = 0;
        //        for (int i = 0; i < DataModel.VehicleNumber; ++i)
        //        {
        //            int stopInd = 0;
        //            Console.WriteLine("Route for Vehicle {0}:", i);
        //            var index = RoutingModel.Start(i);
        //            while (RoutingModel.IsEnd(index) == false)
        //            {
        //                var timeVar = timeDimension.CumulVar(index);
        //                stopInd = RoutingIndexManager.IndexToNode(index);
        //                Console.Write(DataModel.IndexToStop(stopInd) + " Time({0},{1}) -> ",
        //                    solution.Min(timeVar),
        //                    solution.Max(timeVar));
        //                index = solution.Value(RoutingModel.NextVar(index));
        //            }

        //            var endTimeVar = timeDimension.CumulVar(index);
        //            stopInd = RoutingIndexManager.IndexToNode(index);
        //            Console.WriteLine(DataModel.IndexToStop(stopInd) + "Time({0},{1})",
        //                solution.Min(endTimeVar),
        //                solution.Max(endTimeVar));
        //            Console.WriteLine("Time of the route: {0}min", solution.Min(endTimeVar));
        //            totalTime += solution.Min(endTimeVar);
        //        }

        //        Console.WriteLine("Total time of all routes: {0}min", totalTime);
        //    }
        //    else
        //    {
        //        throw new ArgumentNullException();
        //    }
        //}
    }
}
