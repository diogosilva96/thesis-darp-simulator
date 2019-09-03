using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.PDTW
{
    public class PdtwSolver //pickup delivery with time windows solver
    {
        private PdtwDataModel _pdtwDataModel;
        private RoutingIndexManager _routingIndexManager;
        private RoutingModel _routingModel;
        private int _transitCallbackIndex;
        private int _demandCallbackIndex;
        public int MaxUpperBound;

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init(int maxUpperBoundLimitInMinutes)
        {
            // Create RoutingModel Index RoutingIndexManager
            _routingIndexManager = new RoutingIndexManager(
                _pdtwDataModel.TimeMatrix.GetLength(0),
                _pdtwDataModel.VehicleNumber,
                _pdtwDataModel.DepotIndex);

            //Create routing model
            _routingModel = new RoutingModel(_routingIndexManager);
            // Create and register a transit callback.
            _transitCallbackIndex = _routingModel.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                    var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                    var toNode = _routingIndexManager.IndexToNode(toIndex);
                    return _pdtwDataModel.TimeMatrix[fromNode, toNode];
                }
            );

            //Create and register demand callback
            _demandCallbackIndex = _routingModel.RegisterUnaryTransitCallback(
                (long fromIndex) => {
                    // Convert from routing variable Index to demand NodeIndex.
                    var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                    return _pdtwDataModel.Demands[fromNode];
                }
            );
            //// Allow to drop nodes.
            //long penalty = 999999;
            //for (int i = 1; i < _pdtwDataModel.TimeMatrix.GetLength(0); ++i)
            //{
            //    _routingModel.AddDisjunction(new long[] { _routingIndexManager.NodeToIndex(i) }, penalty);
            //}

            _routingModel.SetArcCostEvaluatorOfAllVehicles(_transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.
            //Adds capacity constraints
            _routingModel.AddDimensionWithVehicleCapacity(
                _demandCallbackIndex, 0,  // null capacity slack
                _pdtwDataModel.VehicleCapacities,   // vehicle maximum capacities
                true,                      // start cumul to zero
                "Capacity");
            AddPickupDeliveryDimension(); //Adds the pickup delivery dimension, which contains the pickup and delivery constraints
            AddTimeWindowDimension(maxUpperBoundLimitInMinutes*60); //Adds the time window dimension, which contains the timewindow constraints, 5min upper bound limit
        
        }

        private void AddPickupDeliveryDimension()
        {
            if (_pdtwDataModel != null)
            {
                // Add Distance constraints
                _routingModel.AddDimension(_transitCallbackIndex, 9999999, 99999999,
                    true, // start cumul to zero
                    "Distance");
                RoutingDimension distanceDimension = _routingModel.GetMutableDimension("Distance");
                distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                var constraintSolver = _routingModel.solver(); //Gets the underlying constraint solver
                for (int i = 0; i < _pdtwDataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = _routingIndexManager.NodeToIndex(_pdtwDataModel.PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex = _routingIndexManager.NodeToIndex(_pdtwDataModel.PickupsDeliveries[i][1]); //delivery index
                    _routingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    constraintSolver.Add(constraintSolver.MakeEquality(_routingModel.VehicleVar(pickupIndex), _routingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    constraintSolver.Add(constraintSolver.MakeLessOrEqual(distanceDimension.CumulVar(pickupIndex), distanceDimension.CumulVar(deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                }
            }
        }

        private void AddTimeWindowDimension(int maxUpperBoundLimitInSeconds)
        {
            //Max upper bound limit received as parameter, defines the maximum arrival time at the delivery location (e.g a request with {10,20} the maximum arrival time at the delivery location will be 20 + maxUpperBoundLimitInSeconds)
            // this is used to relax the problem, if needed in cases such as if the problem isn't possible to be solved with the current timewindow requests.
            if (_pdtwDataModel != null)
            {

                //Add Time window constraints
                _routingModel.AddDimension(
                    _transitCallbackIndex, // transit callback
                    999999, // allow waiting time
                    999999, // maximum travel time per vehicle
                    true, // start cumul to zero
                    "Time");
                RoutingDimension timeDimension = _routingModel.GetMutableDimension("Time");
                // Add time window constraints for each location except depot.
                for (int i = 1; i < _pdtwDataModel.TimeWindows.GetLength(0); ++i)
                {
                    long index = _routingIndexManager.NodeToIndex(i); //gets the node index
                    timeDimension.CumulVar(index).SetMin(_pdtwDataModel.TimeWindows[i, 0]); //Sets the minimum upper bound limit
                    timeDimension.CumulVar(index).SetMax(_pdtwDataModel.TimeWindows[i, 1] + maxUpperBoundLimitInSeconds); //Sets the maximum upper bound limit
                    timeDimension.SetCumulVarSoftUpperBound(index, _pdtwDataModel.TimeWindows[i, 1], 1); //adds soft upper bound limit which is the requested time window
                }

                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < _pdtwDataModel.VehicleNumber; ++i)
                {
                    long index = _routingModel.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        _pdtwDataModel.TimeWindows[0, 0],
                        _pdtwDataModel.TimeWindows[0, 1]); //this guarantees that a vehicle must visit the location during its time window
                }

                for (int i = 0; i < _pdtwDataModel.VehicleNumber; ++i)
                {
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.Start(i)));
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.End(i)));
                }
            }
        }

        public RoutingSearchParameters GetSearchParameters()
        {
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;

            return searchParameters;
        }

        public Assignment GetSolution(PdtwDataModel pdtwDataModel)
        {
            _pdtwDataModel = pdtwDataModel;
            Assignment solution = null;

            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < 20; maxUpperBound++)
            {
                MaxUpperBound = maxUpperBound;
                Init(MaxUpperBound);
                var searchParameters = GetSearchParameters();
                //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
                //Get the solution of the problem
                solution = _routingModel.SolveWithParameters(searchParameters);
                if (solution != null) //if the solution isn't null, this means the problem is feasible with the inserted maxUpperBound, break the for loop
                {
                    break;
                }
            }
            return solution;
        }

        public Assignment GetSolution(PdtwDataModel pdtwDataModel,int searchTimeLimit)
        {
            _pdtwDataModel = pdtwDataModel;
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < 20; maxUpperBound++)
            {
                MaxUpperBound = maxUpperBound;
                Init(MaxUpperBound);
                var searchParameters = GetSearchParameters();
                //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
                //Get the solution of the problem
                SetSearchStrategy(searchParameters, 20);
                solution = _routingModel.SolveWithParameters(searchParameters); //solves the problem
                if (solution != null) //if the solution isn't null, this means the problem is feasible with the inserted maxUpperBound, break the for loop
                {
                    break;
                }
            }
            return solution;
        }
        public void SetSearchStrategy(RoutingSearchParameters searchParam,int searchTimeLimit)
        {
            searchParam.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            searchParam.TimeLimit = new Duration { Seconds = searchTimeLimit };
            searchParam.LogSearch = true; //logs the search if true

        }

        public PdtwSolutionObject GetSolutionObject(Assignment solution)
        {
            PdtwSolutionObject pdtwSolutionObject = null;
            if (solution != null) { 

                var solutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(solution);
                if (solutionDictionary != null)
                {
                    pdtwSolutionObject = new PdtwSolutionObject(solutionDictionary);
                }
            }
            return pdtwSolutionObject;
        }

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> SolutionToVehicleStopTimeWindowsDictionary(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            if (solution != null)
            {
                List<Customer> allCustomers = _pdtwDataModel.Customers;
                vehicleStopCustomerTimeWindowsDictionary =
                    new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                var timeDim = _routingModel.GetMutableDimension("Time");
                var distanceDim = _routingModel.GetMutableDimension("Distance");
                for (int i = 0; i < _pdtwDataModel.VehicleNumber; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    List<Customer> routeCustomers = new List<Customer>();
                    List<Customer> customersToBeRemove = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    int stopIndex = 0;
                    long[] timeWindow;
                    Stop currentStop = null;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false) //while the iterator isn't done
                    {

                        stopIndex = _routingIndexManager.IndexToNode(index); //Gets current stop index
                        //routeStops add
                        currentStop = _pdtwDataModel.IndexToStop(stopIndex);
                        routeStops.Add(currentStop); //adds the current stop
                        //timeWindow add
                        var timeVar = timeDim.CumulVar(index);
                        timeWindow = new[] { solution.Min(timeVar), solution.Max(timeVar) };
                        routeTimeWindows.Add(timeWindow); //adds the timewindow to the list

                        index = solution.Value(_routingModel.NextVar(index)); //increments the iterator
                    }
                    //timeWindow add
                    var endTimeVar = timeDim.CumulVar(index);
                    timeWindow = new[] { solution.Min(endTimeVar), solution.Max(endTimeVar) };
                    routeTimeWindows.Add(timeWindow);

                    stopIndex = _routingIndexManager.IndexToNode(index); //Gets current stop index
                    //routeStops add
                    currentStop = _pdtwDataModel.IndexToStop(stopIndex);
                    routeStops.Add(currentStop); //adds the current stop
                    foreach (var customer in allCustomers) //loop to add the customers to the routecustomers
                    {
                        var pickupStop = customer.PickupDelivery[0];
                        var deliveryStop = customer.PickupDelivery[1];
                        if (routeStops.Contains(pickupStop) &&
                            routeStops.Contains(deliveryStop)) //If the route contains the pickup and delivery stop
                        {
                            if (routeStops.IndexOf(pickupStop) < routeStops.IndexOf(deliveryStop)
                            ) // if the pickup stop comes before the delivery stop (precedence constraint), adds it to the route customers list.
                            {
                                routeCustomers.Add(customer);
                                customersToBeRemove.Add(customer);
                            }
                        }
                    }
                    //customer removal (for the already added to the solution object)
                    foreach (var customer in customersToBeRemove)
                    {
                        allCustomers.Remove(customer); //removes the already added customers from the list
                    }

                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(_pdtwDataModel.Vehicles[i],
                        tuple); //adds the vehicle index + tuple with the customer and routestop list
                }
            }

            return vehicleStopCustomerTimeWindowsDictionary;

        }
        public void PrintSolution(Assignment solution)
        {

            if (solution != null)
            {
                var timeDim = _routingModel.GetMutableDimension("Time");
                var distanceDim = _routingModel.GetMutableDimension("Distance");
                long totalTime = 0;
                long totalDistance = 0;
                Calculator calculator = new Calculator();
                Console.WriteLine("--------------------------------");
                Console.WriteLine("| PDTW Solver Solution Printer |");
                Console.WriteLine("--------------------------------");
                Console.WriteLine("T - Time Windows");
                Console.WriteLine("L - Load of the vehicle");
                for (int i = 0; i < _pdtwDataModel.VehicleNumber; ++i)
                {
                    int nodeIndex = 0;
                    long routeLoad = 0;
                    Console.WriteLine("Vehicle {0} Route:", i);
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false)
                    {
                        var timeVar = timeDim.CumulVar(index);
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        routeLoad += _pdtwDataModel.Demands[nodeIndex];
                        var previousIndex = index;
                        index = solution.Value(_routingModel.NextVar(index));
                        var timeToTravel = _routingModel.GetArcCostForVehicle(previousIndex, index, 0); //Gets the travel time between the previousNode and the NextNode
                        var distance =
                            calculator.TravelTimeToDistance((int)timeToTravel, _pdtwDataModel.VehicleSpeed); //Calculates the distance based on the travel time and vehicle speed
                        Console.Write(_pdtwDataModel.IndexToStop(nodeIndex) + ":T({0},{1}), L({2}) --[{3}m]--> ",
                            solution.Min(timeVar),
                            solution.Max(timeVar), routeLoad,
                            (int)distance);

                    }
                    var endDistanceVar = distanceDim.CumulVar(index);
                    var endTimeVar = timeDim.CumulVar(index);
                    nodeIndex = _routingIndexManager.IndexToNode(index);
                    routeLoad += _pdtwDataModel.Demands[nodeIndex];
                    Console.WriteLine(_pdtwDataModel.IndexToStop(nodeIndex) + ":T({0},{1}), L({2})",
                        solution.Min(endTimeVar),
                        solution.Max(endTimeVar),routeLoad);
                    Console.WriteLine("Time of the route: {0} minutes", TimeSpan.FromSeconds(solution.Min(endTimeVar)).TotalMinutes);
                    long routeDistance = (long)calculator.TravelTimeToDistance((int)solution.Min(endDistanceVar), _pdtwDataModel.VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    Console.WriteLine("Distance of the route: {0} meters",routeDistance); 
                    Console.WriteLine("Avg time cost:"+solution.Min(endTimeVar)/index); //debug
                    totalDistance += routeDistance;
                    totalTime += solution.Min(endTimeVar);
                    Console.WriteLine("------------------------------------------");
                }
                Console.WriteLine("Total time of all routes: {0} minutes", TimeSpan.FromSeconds(totalTime).TotalMinutes);
                Console.WriteLine("Total distance of all routes: {0} meters",totalDistance);
                
            }
            else
            {
                throw new ArgumentNullException("solution = null");
            }
        }
    }
}
