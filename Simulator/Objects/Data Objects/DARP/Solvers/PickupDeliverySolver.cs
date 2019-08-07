using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP.DataModels;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP.Solvers
{
    public class PickupDeliverySolver:Solver
    {

        public override void Print(Assignment solution)
        {
            if (solution != null)
            {
                // Inspect solution.
                long maxRouteDistance = 0;
                long totalDistance = 0;
                Console.WriteLine("-----------------------------------------------------------------------");
                for (int i = 0; i < DataModel.VehicleNumber; ++i)
                {
                    Console.WriteLine(this.ToString() + "Route for Vehicle {0}:", i);
                    var stopInd = 0;
                    long routeDistance = 0;
                    var index = Routing.Start(i);
                    while (Routing.IsEnd(index) == false)
                    {
                        stopInd = Manager.IndexToNode((int) index);
                        Console.Write("{0} -> ", DataModel.GetStop(stopInd).Id);
                        var previousIndex = index;
                        index = solution.Value(Routing.NextVar(index));
                        var distance = Routing.GetArcCostForVehicle(previousIndex, index, 0);
                        routeDistance += distance;
                    }

                    stopInd = Manager.IndexToNode((int) index);
                    Console.WriteLine("{0}", DataModel.GetStop(stopInd).Id);
                    Console.WriteLine(this.ToString() + "Distance of the route: {0}m", routeDistance);
                    totalDistance = routeDistance + totalDistance;
                    maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
                }

                Console.WriteLine(this.ToString() + "Maximum distance of the routes: {0}m", maxRouteDistance);
                Console.WriteLine(this.ToString() + "Total distance traveled: {0}m", totalDistance);
            }
            else
            {
                throw new ArgumentNullException("solution is null");
            }
        }

        public Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>> GetVehicleStopSequenceCustomersDictionary(Assignment solution)
        {
            if (solution != null)
            {
                Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>> vehicleSolutionDictionary =
                    new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>>();
                List<Customer>
                    addedCustomers =
                        new List<Customer>(); //auxiliary list to make sure that the same customer isn't assigned to more than one vehicle
                for (int i = 0; i < DataModel.Vehicles.Count; ++i)
                {

                    List<Stop> stopSeq = new List<Stop>();
                    List<Customer> customers = new List<Customer>();
                    var stopInd = 0;
                    var index = Routing.Start(i);
                    while (Routing.IsEnd(index) == false)
                    {
                        stopInd = Manager.IndexToNode((int) index);
                        stopSeq.Add(DataModel.GetStop(stopInd));
                        index = solution.Value(Routing.NextVar(index));
                    }

                    stopInd = Manager.IndexToNode((int) index);
                    stopSeq.Add(DataModel.GetStop(stopInd));


                    foreach (var customer in DataModel.Customers)
                    {
                        var pickupIndex = stopSeq.FindIndex(s => s == customer.PickupDelivery[0]);
                        var deliveryIndex = stopSeq.FindIndex(s => s == customer.PickupDelivery[1]);
                        if (pickupIndex < deliveryIndex
                        ) //if pickup index is lower than the delivery index it means that this customer can be assigned to this vehicle.
                        {
                            if (!addedCustomers.Contains(customer)
                            ) //this check, makes sure that the same customer isn't assigned to more than one vehicle
                            {
                                customers.Add(customer);
                                addedCustomers.Add(customer);
                            }
                        }
                    }

                    var stopSeqCustomersTuple = Tuple.Create(stopSeq, customers);
                    vehicleSolutionDictionary.Add(DataModel.Vehicles[i], stopSeqCustomersTuple);
                }

                return vehicleSolutionDictionary;
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public override void InitHookMethod()
        {
            if (DataModel is PickupDeliveryDataModel pdDatamodel)
            {
                // Define cost of each arc.
                Routing.SetArcCostEvaluatorOfAllVehicles(TransitCallbackIndex);

                // Add Distance constraint.
                Routing.AddDimension(TransitCallbackIndex, 0, 99999999,
                    true, // start cumul to zero
                    "Distance");
                RoutingDimension = Routing.GetMutableDimension("Distance");
                RoutingDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                ConstraintSolver = Routing.solver();
                for (int i = 0; i < pdDatamodel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = Manager.NodeToIndex(pdDatamodel.PickupsDeliveries[i][0]);
                    long deliveryIndex = Manager.NodeToIndex(pdDatamodel.PickupsDeliveries[i][1]);
                    Routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    ConstraintSolver.Add(ConstraintSolver.MakeEquality(
                        Routing.VehicleVar(pickupIndex),
                        Routing.VehicleVar(deliveryIndex)));
                    ConstraintSolver.Add(ConstraintSolver.MakeLessOrEqual(
                        RoutingDimension.CumulVar(pickupIndex),
                        RoutingDimension.CumulVar(deliveryIndex)));
                }
            }
        }

    }
}
