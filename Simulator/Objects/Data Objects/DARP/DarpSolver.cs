using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.DARP;

namespace Simulator.Objects.Data_Objects
{
    public class DarpSolver
    {
        private PickupDeliveryDataModel _dataModel;
        private RoutingIndexManager _manager;
        private RoutingModel _routing;
        private Solver _solver;
        private int _type; //1 using distances, 2 using time windows

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public void Init(PickupDeliveryDataModel dataModel, int type)
        {
            _type = type;
            _dataModel = dataModel;
            if (_type == 1)//Uses distance matrix
            {
                // Create Routing Index Manager
                _manager = new RoutingIndexManager(
                    _dataModel.Matrix.GetLength(0),
                    _dataModel.VehicleNumber,
                    _dataModel.DepotIndex);

                // Create Routing Model.
                _routing = new RoutingModel(_manager);

                // Create and register a transit callback.
                int transitCallbackIndex = _routing.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        // Convert from routing variable Index to distance matrix NodeIndex.
                        var fromNode = _manager.IndexToNode(fromIndex);
                        var toNode = _manager.IndexToNode(toIndex);
                        return _dataModel.Matrix[fromNode, toNode];
                    }
                );

                // Define cost of each arc.
                _routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // Add Distance constraint.
                _routing.AddDimension(transitCallbackIndex, 0, 99999999,
                    true, // start cumul to zero
                    "Distance");
                RoutingDimension _distanceDimension = _routing.GetMutableDimension("Distance");
                _distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                _solver = _routing.solver();
                for (int i = 0; i < _dataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = _manager.NodeToIndex(_dataModel.PickupsDeliveries[i][0]);
                    long deliveryIndex = _manager.NodeToIndex(_dataModel.PickupsDeliveries[i][1]);
                    _routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                    _solver.Add(_solver.MakeEquality(
                        _routing.VehicleVar(pickupIndex),
                        _routing.VehicleVar(deliveryIndex)));
                    _solver.Add(_solver.MakeLessOrEqual(
                        _distanceDimension.CumulVar(pickupIndex),
                        _distanceDimension.CumulVar(deliveryIndex)));
                }
            }
            else //uses time matrix / time windows
            {

            }
        }
        public Assignment Solve()
        {
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
            //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            //searchParameters.TimeLimit = new Duration { Seconds = 10 };
            //searchParameters.LogSearch = true; //logs the search


            // Solve the problem.
            //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_dataModel.InitialRoutes, true);
            Assignment solution = _routing.SolveWithParameters(searchParameters);
            return solution;
        }

        public void Print(Assignment solution)
        {
            // Inspect solution.
            long maxRouteDistance = 0;
            long totalDistance = 0;
            Console.WriteLine("-----------------------------------------------------------------------");
            for (int i = 0; i < _dataModel.VehicleNumber; ++i)
            {
                Console.WriteLine(this.ToString()+"Route for Vehicle {0}:", i);
                var stopInd = 0;
                long routeDistance = 0;
                var index = _routing.Start(i);
                while (_routing.IsEnd(index) == false)
                {
                    stopInd = _manager.IndexToNode((int)index);
                    Console.Write("{0} -> ", _dataModel.GetStop(stopInd).Id);
                    var previousIndex = index;
                    index = solution.Value(_routing.NextVar(index));
                    var distance = _routing.GetArcCostForVehicle(previousIndex, index, 0);
                    routeDistance += distance;
                }

                stopInd = _manager.IndexToNode((int)index);
                Console.WriteLine("{0}", _dataModel.GetStop(stopInd).Id);
                Console.WriteLine(this.ToString() + "Distance of the route: {0}m", routeDistance);
                totalDistance = routeDistance + totalDistance;
                maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
            }
            Console.WriteLine(this.ToString()+"Maximum distance of the routes: {0}m", maxRouteDistance);
            Console.WriteLine(this.ToString()+"Total distance traveled: {0}m", totalDistance);
        }

        public Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>> SolutionToVehicleStopSequenceCustomersDictionary(Assignment solution)
        {
            Dictionary<Vehicle,Tuple<List<Stop>,List<Customer>>> vehicleSolutionDictionary = new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>>();
            List<Customer> addedCustomers = new List<Customer>(); //auxiliary list to make sure that the same customer isn't assigned to more than one vehicle
            for (int i = 0; i < _dataModel.Vehicles.Count; ++i)
            {

                List<Stop> stopSeq = new List<Stop>();
                List<Customer> customers = new List<Customer>();
                var stopInd = 0;
                var index = _routing.Start(i);
                while (_routing.IsEnd(index) == false)
                {
                    stopInd = _manager.IndexToNode((int) index);
                    stopSeq.Add(_dataModel.GetStop(stopInd));
                    index = solution.Value(_routing.NextVar(index));
                }

                stopInd = _manager.IndexToNode((int) index);
                stopSeq.Add(_dataModel.GetStop(stopInd));


                foreach (var customer in _dataModel.Customers)
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
                vehicleSolutionDictionary.Add(_dataModel.Vehicles[i], stopSeqCustomersTuple);
            }
                
            return vehicleSolutionDictionary;
        }

    }
}
