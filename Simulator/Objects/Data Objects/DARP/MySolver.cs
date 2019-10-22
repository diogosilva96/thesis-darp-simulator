using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class MySolver
    {
        private DarpDataModel _darpDataModel;
        private RoutingIndexManager _routingIndexManager;
        private RoutingModel _routingModel;
        private int _transitCallbackIndex;
        private int _demandCallbackIndex;
        public bool DropNodesAllowed;


        public MySolver(bool dropNodesAllowed)
        {
            DropNodesAllowed = dropNodesAllowed;

        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init()
        {
            // Create RoutingModel Index RoutingIndexManager
            if (_darpDataModel.Starts != null && _darpDataModel != null)
            {
                _routingIndexManager = new RoutingIndexManager(
                    _darpDataModel.DistanceMatrix.GetLength(0),
                    _darpDataModel.IndexManager.Vehicles.Count,
                    _darpDataModel.Starts, _darpDataModel.Ends);
            }
            else
            {
                _routingIndexManager = new RoutingIndexManager(
                    _darpDataModel.TimeMatrix.GetLength(0),
                    _darpDataModel.IndexManager.Vehicles.Count,
                    0);
            }

            //Create routing model
            _routingModel = new RoutingModel(_routingIndexManager);
            // Create and register a transit callback.
            _transitCallbackIndex = _routingModel.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                    var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                    var toNode = _routingIndexManager.IndexToNode(toIndex);
                    return _darpDataModel.DistanceMatrix[fromNode, toNode];
                }
            );

            //Create and register demand callback
            _demandCallbackIndex = _routingModel.RegisterUnaryTransitCallback(
                (long fromIndex) => {
                    // Convert from routing variable Index to demand NodeIndex.
                    var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                    return _darpDataModel.Demands[fromNode];
                }
            );


            if (DropNodesAllowed)
            {
                // Allow to drop nodes.
                //The penalty should be larger than the sum of all travel times locations (excluding the depot).
                //As a result, after dropping one location to make the problem feasible, the solver won't drop any additional locations,
                //because the penalty for doing so would exceed any further reduction in travel time.
                //If we want to make as many deliveries as possible, penalty value should be larger than the sum of all travel times between locations
                long penalty = 9999999;
                for (int i = 1; i < _darpDataModel.TimeMatrix.GetLength(0); ++i)
                {
                    _routingModel.AddDisjunction(new long[] { _routingIndexManager.NodeToIndex(i) }, penalty);
                }
            }

            _routingModel.SetArcCostEvaluatorOfAllVehicles(_transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.
            AddPickupDeliveryDimension(); //Adds the pickup delivery dimension, which contains the pickup and delivery constraints
            AddCapacityDimension();

        }

        private void AddCapacityDimension()
        {
            if (_darpDataModel != null)
            {
                //Adds capacity constraints
                _routingModel.AddDimensionWithVehicleCapacity(
                    _demandCallbackIndex, 0,  // null capacity slack
                    _darpDataModel.VehicleCapacities,   // vehicle maximum capacities
                    true,                      // start cumul to zero
                    "Capacity");

            }
        }

        private void AddPickupDeliveryDimension()
        {
            if (_darpDataModel != null)
            {
                // Add Distance constraints
                _routingModel.AddDimension(_transitCallbackIndex, 9999999, 99999999,
                    true, // start cumul to zero
                    "PickupDelivery");
                RoutingDimension pickupDeliveryDimension = _routingModel.GetMutableDimension("PickupDelivery");
                pickupDeliveryDimension.SetGlobalSpanCostCoefficient(100);
                //SetGlobalSpanCostCoefficient sets a large coefficient(100) for the global span of the routes, which in this example is the maximum of the distances of the routes.
                //This makes the global span the predominant factor in the objective function, so the program minimizes the length of the longest route.

                // Define Transportation Requests (pickup and delivery) and its respective constraints.
                var solver = _routingModel.solver(); //Gets the underlying constraint solver
                for (int i = 0; i < _darpDataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex =
                        _routingIndexManager.NodeToIndex(_darpDataModel.PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex =
                        _routingIndexManager.NodeToIndex(_darpDataModel.PickupsDeliveries[i][1]); //delivery index
                    _routingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    solver.Add(solver.MakeEquality(_routingModel.VehicleVar(pickupIndex), _routingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    solver.Add(solver.MakeLessOrEqual(pickupDeliveryDimension.CumulVar(pickupIndex), pickupDeliveryDimension.CumulVar(deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                }

            }
        }

        private RoutingSearchParameters GetDefaultSearchParameters()
        {
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;

            return searchParameters;
        }

        public Assignment TryGetFastSolution(DarpDataModel darpDataModel)
        {
            _darpDataModel = darpDataModel;
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
           
                Init();
                //Get the solution of the problem
                try
                {
                    var searchParameters = GetDefaultSearchParameters();
                    solution = _routingModel.SolveWithParameters(searchParameters);
                }
                catch (Exception)
                {
                    solution = null;

                }


            Console.WriteLine("Solver status:" + GetSolverStatus());
            return solution; //retuns null if no solution is found, otherwise returns the solution
        }


        public string GetSolverStatus()
        {
            string status = "";
            int solverStatus = _routingModel.GetStatus();
            switch (solverStatus)
            {
                case 0:
                    status = "ROUTING_NOT_SOLVED"; //Problem not solved yet
                    break;
                case 1:
                    status = "ROUTING_SUCCESS"; //Problem solved successfully.
                    break;
                case 2:
                    status = "ROUTING_FAIL"; //No solution found to the problem
                    break;
                case 3:
                    status = "ROUTING_FAIL_TIMEOUT"; //Time limit reached before finding the solution
                    break;
                case 4:
                    status = "ROUTING_INVALID"; //Model, parameter or flags are not valid
                    break;
            }
            return status;
        }
        public Assignment TryGetSolutionWithSearchStrategy(DarpDataModel darpDataModel, int searchTimeLimitInSeconds, LocalSearchMetaheuristic.Types.Value searchAlgorithm)
        {
            _darpDataModel = darpDataModel;
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
                Init();
                var searchParameters = GetSearchParametersWithSearchStrategy(searchTimeLimitInSeconds, searchAlgorithm);
                //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
                //Get the solution of the problem
                solution = _routingModel.SolveWithParameters(searchParameters); //solves the problem
            return solution; //retuns null if no solution is found, otherwise returns the solution
        }
        private RoutingSearchParameters GetSearchParametersWithSearchStrategy(int searchTimeLimit, LocalSearchMetaheuristic.Types.Value searchAlgorithm)
        {
            var searchParam = GetDefaultSearchParameters();
            searchParam.LocalSearchMetaheuristic = searchAlgorithm;
            searchParam.TimeLimit = new Duration { Seconds = searchTimeLimit };
            searchParam.LogSearch = false; //logs the search if true
            return searchParam;

        }

        public DarpSolutionObject GetSolutionObject(Assignment solution)
        {
            DarpSolutionObject darpSolutionObject = null;
            if (solution != null)
            {

                var solutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(solution);
                if (solutionDictionary != null)
                {
                    var solutionMetricsDictionary = GetVehicleRouteMetrics(solution);
                    darpSolutionObject = new DarpSolutionObject(solutionDictionary, solutionMetricsDictionary);

                }
            }
            return darpSolutionObject;
        }

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> SolutionToVehicleStopTimeWindowsDictionary(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            if (solution != null)
            {

                List<Customer> allCustomers = _darpDataModel.IndexManager.Customers;
                vehicleStopCustomerTimeWindowsDictionary =
                    new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                for (int i = 0; i < _darpDataModel.IndexManager.Vehicles.Count; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    int nodeIndex = 0;
                    List<Customer> routeCustomers = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    Stop currentStop = null;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false) //while the iterator isn't done
                    {
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        //routeStops add
                        currentStop = _darpDataModel.IndexManager.GetStop(nodeIndex);
                        routeStops.Add(currentStop); //adds the current stop
                        //timeWindow add
                        index = solution.Value(_routingModel.NextVar(index)); //increments the iterator
                    }
                    //timeWindow add
                    nodeIndex = _routingIndexManager.IndexToNode(index);

                    //routeStops add
                    currentStop = _darpDataModel.IndexManager.GetStop(nodeIndex);
                    routeStops.Add(currentStop); //adds the current stop
                    foreach (var customer in allCustomers) //loop to add the customers to the routecustomers
                    {
                        var pickupStop = customer.PickupDelivery[0];
                        var deliveryStop = customer.PickupDelivery[1];
                        if (routeStops.Contains(pickupStop) &&
                            routeStops.Contains(deliveryStop)) //If the route contains the pickup and delivery stop
                        {
                            if (routeStops.IndexOf(pickupStop) < routeStops.IndexOf(deliveryStop)) // if the pickup stop comes before the delivery stop (precedence constraint), adds it to the route customers list.
                            {
                                routeCustomers.Add(customer);
                            }
                        }
                    }
                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(_darpDataModel.IndexManager.GetVehicle(i),
                        tuple); //adds the vehicle index + tuple with the customer and routestop list
                }
            }

            return vehicleStopCustomerTimeWindowsDictionary;

        }

        public List<string> GetSolutionPrintableList(Assignment solution)
        {
            List<string> printableList = new List<string>();
            if (solution != null)
            {
                var pickupDeliveryDim = _routingModel.GetMutableDimension("PickupDelivery");
                var capacityDim = _routingModel.GetMutableDimension("Capacity");
                long totalTime = 0;
                long totalDistance = 0;
                long totalLoad = 0;
                var solutionObject = GetSolutionObject(solution);
                for (int i = 0; i < _darpDataModel.IndexManager.Vehicles.Count; ++i)
                {
                    int nodeIndex = 0;
                    long routeLoad = 0;
                    long previousRouteLoad = 0;
                    printableList.Add("Vehicle " + i + " Route:");
                    var index = _routingModel.Start(i);
                    string concatenatedString = "";
                    while (_routingModel.IsEnd(index) == false)
                    {
                        previousRouteLoad = routeLoad;
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        routeLoad += _darpDataModel.Demands[nodeIndex];

                        var previousIndex = index;
                        index = solution.Value(_routingModel.NextVar(index));
                        //printableList.Add(index+ " - "+solution.Max(capacityDim.CumulVar(index))); //current capacity
                        var distance =
                            _routingModel.GetArcCostForVehicle(previousIndex, index,
                                0); //Gets the distance
                        if (_darpDataModel.IndexManager.GetStop(nodeIndex) != null)
                        {
                            concatenatedString += _darpDataModel.IndexManager.GetStop(nodeIndex) + ": L(" +
                                                  routeLoad + ") --[" + distance + "m]--> ";
                        }
                        if (_darpDataModel.IndexManager.GetStop(_routingIndexManager.IndexToNode(index)) == null) //if the next stop is null finish printing
                        {
                            concatenatedString += _darpDataModel.IndexManager.GetStop(nodeIndex) + ": L(" +
                                                  routeLoad + ")";
                        }

                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad

                    }

                    var endPickupDeliveryVar = pickupDeliveryDim.CumulVar(index);
                    nodeIndex = _routingIndexManager.IndexToNode(index);
                    routeLoad += _darpDataModel.Demands[nodeIndex];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    if (_darpDataModel.IndexManager.GetStop(nodeIndex) != null)
                    {
                        concatenatedString += _darpDataModel.IndexManager.GetStop(nodeIndex) + ": L(" +
                                              routeLoad + ")";
                    }

                    printableList.Add(concatenatedString);
                    long routeDistance = (int)solution.Min(endPickupDeliveryVar); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    printableList.Add("Route Distance: " + routeDistance + " meters");
                    printableList.Add("Route Total Load:" + totalLoad);
                    printableList.Add("Route customers served: " + solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count);
                    printableList.Add("Average distance traveled per request: " + routeDistance / solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count + " meters.");
                    totalDistance += routeDistance;
                    printableList.Add("------------------------------------------");
                }


                printableList.Add("Total time of all routes: " + TimeSpan.FromSeconds(totalTime).TotalMinutes + " minutes");
                printableList.Add("Total distance of all routes: " + totalDistance + " meters");
                printableList.Add("Total Load of all routes: " + totalLoad + " customers");
                printableList.Add("Total customers served: " + solutionObject.CustomerNumber + "/" + _darpDataModel.IndexManager.Customers.Count);
            }
            else
            {
                throw new ArgumentNullException("solution = null");
            }

            return printableList;
        }
        public void PrintSolution(Assignment solution)
        {

            if (solution != null)
            {

                Console.WriteLine("--------------------------------");
                Console.WriteLine("| PDTW Solver Solution Printer |");
                Console.WriteLine("--------------------------------");
                Console.WriteLine("T - Time Windows");
                Console.WriteLine("L - Load of the vehicle");
                var printableList = GetSolutionPrintableList(solution);
                foreach (var stringToBePrinted in printableList)
                {
                    Console.WriteLine(stringToBePrinted);
                }
            }
            else
            {
                throw new ArgumentNullException("solution = null");
            }
        }

        public Dictionary<string, long[]> GetVehicleRouteMetrics(Assignment solution) //computes the metrics for each vehicle route
        {

            Dictionary<string, long[]> vehicleMetricsDictionary = new Dictionary<string, long[]>();
            if (solution != null)
            {

                var pickupDeliveryDim = _routingModel.GetMutableDimension("PickupDelivery");
                var vehicleNumber = _darpDataModel.IndexManager.Vehicles.Count;
                //route metrics each index is the vehicle index
                long[] routeTimes = new long[vehicleNumber];
                long[] routeDistances = new long[vehicleNumber];
                long[] routeLoads = new long[vehicleNumber];
                for (int i = 0; i < vehicleNumber; ++i)
                {
                    long routeLoad = 0;
                    long totalLoad = 0;
                    long previousRouteLoad = 0;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false)
                    {
                        previousRouteLoad = routeLoad;
                        routeLoad += _darpDataModel.Demands[_routingIndexManager.IndexToNode(index)];
                        index = solution.Value(_routingModel.NextVar(index));
                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    }
                    routeLoad += _darpDataModel.Demands[_routingIndexManager.IndexToNode(index)];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    var endPickupDeliveryVar = pickupDeliveryDim.CumulVar(index);
                    routeLoads[i] = totalLoad;
                    routeDistances[i] = (int)solution.Min(endPickupDeliveryVar); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    routeTimes[i] = 0;
                }
                vehicleMetricsDictionary.Add("routeLoads", routeLoads);
                vehicleMetricsDictionary.Add("routeDistances", routeDistances);
                vehicleMetricsDictionary.Add("routeTimes", routeTimes);
            }
            return vehicleMetricsDictionary;
        }
    }
}
