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
                Routing.SetArcCostEvaluatorOfAllVehicles(TransitCallbackIndex);

                Routing.AddDimension(
                    TransitCallbackIndex, // transit callback
                    30, // allow waiting time
                    999999, // maximum travel time per vehicle
                    false, // start cumul to zero
                    "Time");
                RoutingDimension timeDimension = Routing.GetMutableDimension("Time");
                // Add time window constraints for each location except depot.
                for (int i = 1; i < twDataModel.TimeWindows.GetLength(0); ++i)
                {
                    long index = Manager.NodeToIndex(i);
                    timeDimension.CumulVar(index).SetRange(
                        twDataModel.TimeWindows[i, 0],
                        twDataModel.TimeWindows[i, 1]);
                }

                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    long index = Routing.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        twDataModel.TimeWindows[0, 0],
                        twDataModel.TimeWindows[0,
                            1]); //this guarantees that a vehicle must visit the location during its time window
                }

                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    Routing.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(Routing.Start(i)));
                    Routing.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(Routing.End(i)));
                }
            }
        }


        public override void Print(Assignment solution)
        {
            if (solution != null)
            {
                RoutingDimension timeDimension = Routing.GetMutableDimension("Time");
                // Inspect solution.
                long totalTime = 0;
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    int stopInd = 0;
                    Console.WriteLine("Route for Vehicle {0}:", i);
                    var index = Routing.Start(i);
                    while (Routing.IsEnd(index) == false)
                    {
                        var timeVar = timeDimension.CumulVar(index);
                        stopInd = Manager.IndexToNode(index);
                        Console.Write(DataModel.GetStop(stopInd) + " Time({0},{1}) -> ",
                            solution.Min(timeVar),
                            solution.Max(timeVar));
                        index = solution.Value(Routing.NextVar(index));
                    }

                    var endTimeVar = timeDimension.CumulVar(index);
                    stopInd = Manager.IndexToNode(index);
                    Console.WriteLine(DataModel.GetStop(stopInd) + "Time({0},{1})",
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
