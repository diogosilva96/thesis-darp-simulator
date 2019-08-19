using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class PickupDeliveryTimeWindowSolver
    {
        private PickupDeliveryDataModel _pickupDeliveryDataModel;
        private RoutingIndexManager _routingIndexManager;
        private RoutingModel _routingModel;
        private int _transitCallbackIndex;

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init()
        {
            // Create RoutingModel Index RoutingIndexManager
            _routingIndexManager = new RoutingIndexManager(
                _pickupDeliveryDataModel.TimeMatrix.GetLength(0),
                _pickupDeliveryDataModel.VehicleNumber,
                _pickupDeliveryDataModel.DepotIndex);

            //Create routing model
            _routingModel = new RoutingModel(_routingIndexManager);

            // Create and register a transit callback.
            _transitCallbackIndex = _routingModel.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                    var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                    var toNode = _routingIndexManager.IndexToNode(toIndex);
                    return _pickupDeliveryDataModel.TimeMatrix[fromNode, toNode];
                }
            );


            _routingModel.SetArcCostEvaluatorOfAllVehicles(_transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.
            AddPickupDeliveryDimension(); //Adds the pickup delivery dimension, which contains the pickup and delivery constraints
            AddTimeWindowDimension(5*60); //Adds the time window dimension, which contains the timewindow constraints, 5min upper bound limit
        }

        private void AddPickupDeliveryDimension()
        {
            if (_pickupDeliveryDataModel != null)
            {
                // Add Distance constraints
                _routingModel.AddDimension(_transitCallbackIndex, 9999999, 99999999,
                    true, // start cumul to zero
                    "Distance");
                RoutingDimension distanceDimension = _routingModel.GetMutableDimension("Distance");
                distanceDimension.SetGlobalSpanCostCoefficient(100);

                // Define Transportation Requests.
                var constraintSolver = _routingModel.solver(); //Gets the underlying constraint solver
                for (int i = 0; i < _pickupDeliveryDataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex =
                        _routingIndexManager.NodeToIndex(_pickupDeliveryDataModel
                            .PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex =
                        _routingIndexManager.NodeToIndex(_pickupDeliveryDataModel
                            .PickupsDeliveries[i][1]); //delivery index
                    _routingModel.AddPickupAndDelivery(pickupIndex,
                        deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    constraintSolver.Add(constraintSolver.MakeEquality(
                        _routingModel.VehicleVar(pickupIndex),
                        _routingModel
                            .VehicleVar(
                                deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    constraintSolver.Add(constraintSolver.MakeLessOrEqual(
                        distanceDimension.CumulVar(pickupIndex),
                        distanceDimension
                            .CumulVar(
                                deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                }
            }
        }

        private void AddTimeWindowDimension(int maxUpperBoundLimitInSeconds)
        {
            //Max upper bound limit gives defines the maximum arrival time at the delivery location (e.g a request with {10,20} the maximum arrival time at the delivery location will be 20 + maxUpperBoundLimitInSeconds)
            // this is used to relax the problem, if needed in cases such as if the problem isn't possible to be solved with the current timewindow requests.
            if (_pickupDeliveryDataModel != null)
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
                for (int i = 1; i < _pickupDeliveryDataModel.TimeWindows.GetLength(0); ++i)
                {
                    long index = _routingIndexManager.NodeToIndex(i); //gets the node index
                    timeDimension.CumulVar(index)
                        .SetMin(_pickupDeliveryDataModel.TimeWindows[i, 0]); //Sets the minimum upper bound limit
                    timeDimension.CumulVar(index)
                        .SetMax(_pickupDeliveryDataModel.TimeWindows[i, 1] +
                                maxUpperBoundLimitInSeconds); //Sets the maximum upper bound limit
                    timeDimension.SetCumulVarSoftUpperBound(index, _pickupDeliveryDataModel.TimeWindows[i, 1],
                        1); //adds soft upper bound limit which is the requested time window

                }

                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < _pickupDeliveryDataModel.VehicleNumber; ++i)
                {
                    long index = _routingModel.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        _pickupDeliveryDataModel.TimeWindows[0, 0],
                        _pickupDeliveryDataModel.TimeWindows[0,
                            1]); //this guarantees that a vehicle must visit the location during its time window
                }

                for (int i = 0; i < _pickupDeliveryDataModel.VehicleNumber; ++i)
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

        public Assignment GetSolution(PickupDeliveryDataModel pickupDeliveryDataModel)
        {
            _pickupDeliveryDataModel = pickupDeliveryDataModel;
            Init();
            var searchParameters = GetSearchParameters();
            //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
            //Get the solution of the problem
            Assignment solution = _routingModel.SolveWithParameters(searchParameters);
            return solution;
        }

        public Assignment GetSolution(PickupDeliveryDataModel pickupDeliveryDataModel,int searchTimeLimit)
        {
            _pickupDeliveryDataModel = pickupDeliveryDataModel;
            Init();
            var searchParameters = GetSearchParameters();
            //Get the solution of the problem
            SetSearchStrategy(searchParameters, 20);
            Assignment solution = _routingModel.SolveWithParameters(searchParameters); //solves the problem
            return solution;
        }
        public void SetSearchStrategy(RoutingSearchParameters searchParam,int searchTimeLimit)
        {
            searchParam.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            searchParam.TimeLimit = new Duration { Seconds = searchTimeLimit };
            searchParam.LogSearch = true; //logs the search if true

        }

        public PickupDeliverySolutionObject GetSolutionObject(Assignment solution)
        {
            PickupDeliverySolutionObject pickupDeliverySolutionObject = null;
            if (solution != null) { 

                var solutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(solution);
                if (solutionDictionary != null)
                {
                    pickupDeliverySolutionObject = new PickupDeliverySolutionObject(solutionDictionary);
                }
            }
            return pickupDeliverySolutionObject;
        }

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> SolutionToVehicleStopTimeWindowsDictionary(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            if (solution != null)
            {
                List<Customer> allCustomers = _pickupDeliveryDataModel.Customers;
                vehicleStopCustomerTimeWindowsDictionary =
                    new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                var timeDim = _routingModel.GetMutableDimension("Time");
                var distanceDim = _routingModel.GetMutableDimension("Distance");
                for (int i = 0; i < _pickupDeliveryDataModel.VehicleNumber; ++i)
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
                        currentStop = _pickupDeliveryDataModel.IndexToStop(stopIndex);
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
                    currentStop = _pickupDeliveryDataModel.IndexToStop(stopIndex);
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
                    vehicleStopCustomerTimeWindowsDictionary.Add(_pickupDeliveryDataModel.Vehicles[i],
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
                for (int i = 0; i < _pickupDeliveryDataModel.VehicleNumber; ++i)
                {
                    int stopInd = 0;
                    Console.WriteLine("Vehicle {0} Route:", i);
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false)
                    {
                        var timeVar = timeDim.CumulVar(index);
                        
                        stopInd = _routingIndexManager.IndexToNode(index);
                        var previousIndex = index;
                        index = solution.Value(_routingModel.NextVar(index));
                        var timeToTravel = _routingModel.GetArcCostForVehicle(previousIndex, index, 0); //Gets the travel time between the previousNode and the NextNode
                        var distance =
                            calculator.TravelTimeToDistance((int)timeToTravel, _pickupDeliveryDataModel.VehicleSpeed); //Calculates the distance based on the travel time and vehicle speed
                        Console.Write(_pickupDeliveryDataModel.IndexToStop(stopInd) + " Time({0},{1}) -[{2}m]-> ",
                            solution.Min(timeVar),
                            solution.Max(timeVar),
                            (int)distance);

                    }
                    var endDistanceVar = distanceDim.CumulVar(index);
                    var endTimeVar = timeDim.CumulVar(index);
                    stopInd = _routingIndexManager.IndexToNode(index);
                    Console.WriteLine(_pickupDeliveryDataModel.IndexToStop(stopInd) + "Time({0},{1})",
                        solution.Min(endTimeVar),
                        solution.Max(endTimeVar));
                    Console.WriteLine("Time of the route: {0}min", TimeSpan.FromSeconds(solution.Min(endTimeVar)).TotalMinutes);
                    long routeDistance = (long)calculator.TravelTimeToDistance((int)solution.Min(endDistanceVar), _pickupDeliveryDataModel.VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    Console.WriteLine("Distance of the route:"+routeDistance); 
                    totalDistance += solution.Min(endDistanceVar);
                    totalTime += solution.Min(endTimeVar);
                    Console.WriteLine("------------------------------------------");
                }
                Console.WriteLine("Total time of all routes: {0}min", TimeSpan.FromSeconds(totalTime).TotalMinutes);
                Console.WriteLine("Total distance of all routes: {0}meters",totalDistance);
                
            }
            else
            {
                throw new ArgumentNullException("Solution is null");
            }
        }
    }
}
