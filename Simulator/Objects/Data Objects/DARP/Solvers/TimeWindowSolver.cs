using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP.DataModels;

namespace Simulator.Objects.Data_Objects.DARP.Solvers
{
    public class TimeWindowSolver:Solver

    {

        public override void InitHookMethod()
        {
            if (DataModel is TimeWindowDataModel twDataModel)
            {

                RoutingModel.SetArcCostEvaluatorOfAllVehicles(TransitCallbackIndex);

                // Add Distance constraint.
                RoutingModel.AddDimension(TransitCallbackIndex, 0, 99999999,
                    true, // start cumul to zero
                    "Distance");
                RoutingDimension distanceDimension = RoutingModel.GetMutableDimension("Distance");
                distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                ConstraintSolver = RoutingModel.solver();
                for (int i = 0; i < DataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][0]);
                    long deliveryIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][1]);
                    RoutingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    ConstraintSolver.Add(ConstraintSolver.MakeEquality(
                        RoutingModel.VehicleVar(pickupIndex),
                        RoutingModel.VehicleVar(deliveryIndex)));
                    ConstraintSolver.Add(ConstraintSolver.MakeLessOrEqual(
                        distanceDimension.CumulVar(pickupIndex),
                        distanceDimension.CumulVar(deliveryIndex)));
                }

                RoutingModel.AddDimension(
                    TransitCallbackIndex, // transit callback
                    30, // allow waiting time
                    999999, // maximum travel time per vehicle
                    false, // start cumul to zero
                    "Time");
                RoutingDimension timeDimension = RoutingModel.GetMutableDimension("Time");
                // Add time window constraints for each location except depot.
                for (int i = 1; i < twDataModel.TimeWindows.GetLength(0); ++i)
                {
                    long index = RoutingIndexManager.NodeToIndex(i);
                    timeDimension.CumulVar(index).SetRange(
                        twDataModel.TimeWindows[i, 0],
                        twDataModel.TimeWindows[i, 1]);
                }

                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    long index = RoutingModel.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        twDataModel.TimeWindows[0, 0],
                        twDataModel.TimeWindows[0,
                            1]); //this guarantees that a vehicle must visit the location during its time window
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

        public void PrintSol(Assignment solution)
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
        }
        public override void Print(Assignment solution)
        {
            if (solution != null)
            {
                RoutingDimension timeDimension = RoutingModel.GetMutableDimension("Time");
                // Inspect solution.
                long totalTime = 0;
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    int stopInd = 0;
                    Console.WriteLine("Route for Vehicle {0}:", i);
                    var index = RoutingModel.Start(i);
                    while (RoutingModel.IsEnd(index) == false)
                    {
                        var timeVar = timeDimension.CumulVar(index);
                        stopInd = RoutingIndexManager.IndexToNode(index);
                        Console.Write(DataModel.IndexToStop(stopInd) + " Time({0},{1}) -> ",
                            solution.Min(timeVar),
                            solution.Max(timeVar));
                        index = solution.Value(RoutingModel.NextVar(index));
                    }

                    var endTimeVar = timeDimension.CumulVar(index);
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
                throw new ArgumentNullException();
            }
        }
    }
}
