using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Type = System.Type;

namespace Simulator.Objects.Data_Objects.PDTW
{
    public class PdtwSolver //pickup delivery with time windows solver
    {
        private PdtwDataModel _pdtwDataModel;
        private RoutingIndexManager _routingIndexManager;
        private RoutingModel _routingModel;
        private int _transitCallbackIndex;
        private int _demandCallbackIndex;
        public bool DropNodesAllowed;
        public int MaxUpperBound; //the current upper bound limit of the found solution, which is lesser or equal than _maxUpperBoundLimit
        public int MaxAllowedUpperBound;

        public PdtwSolver(bool dropNodesAllowed)
        {
            DropNodesAllowed = dropNodesAllowed;
            MaxAllowedUpperBound = 30;
            MaxUpperBound = 0; //default value
            
        }
        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init()
        {
            // Create RoutingModel Index RoutingIndexManager
            _routingIndexManager = new RoutingIndexManager(
                _pdtwDataModel.TimeMatrix.GetLength(0),
                _pdtwDataModel.Vehicles.Count,
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
            if (DropNodesAllowed)
            {
                // Allow to drop nodes.
                //The penalty should be larger than the sum of all travel times locations (excluding the depot).
                //As a result, after dropping one location to make the problem feasible, the solver won't drop any additional locations,
                //because the penalty for doing so would exceed any further reduction in travel time.
                //If we want to make as many deliveries as possible, penalty value should be larger than the sum of all travel times between locations
                long penalty = 9999999;
                for (int i = 1; i < _pdtwDataModel.TimeMatrix.GetLength(0); ++i)
                {
                    _routingModel.AddDisjunction(new long[] {_routingIndexManager.NodeToIndex(i)}, penalty);
                }
            }

            _routingModel.SetArcCostEvaluatorOfAllVehicles(_transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.
            AddPickupDeliveryDimension(); //Adds the pickup delivery dimension, which contains the pickup and delivery constraints
            AddTimeWindowDimension(MaxUpperBound*60); //Adds the time window dimension, which contains the timewindow constraints, upper bound limit = maxupperbound*60seconds
            AddCapacityDimension();

        }

        private void AddCapacityDimension()
        {
            if (_pdtwDataModel != null)
            {
                //Adds capacity constraints
                _routingModel.AddDimensionWithVehicleCapacity(
                    _demandCallbackIndex, 0,  // null capacity slack
                    _pdtwDataModel.VehicleCapacities,   // vehicle maximum capacities
                    true,                      // start cumul to zero
                    "Capacity");
                RoutingDimension capacityDimension = _routingModel.GetMutableDimension("Capacity");
                //RoutingDimension distanceDimension = _routingModel.GetMutableDimension("Distance");
                var solver = _routingModel.solver();

                for (int i = 0; i < _routingModel.Size(); i++)
                {
                 
                    if (_routingModel.IsStart(i))
                    {
                        capacityDimension.CumulVar(i).SetValue(0);

                    }
                    //Console.WriteLine("Is pickup node ind:"+i+" - "+_pdtwDataModel.IndexToStop(i)+":"+_pdtwDataModel.IsPickupStop(i));
                    //Console.WriteLine("Is delivery node ind:"+i+" - " + _pdtwDataModel.IndexToStop(i) + ":" + _pdtwDataModel.IsDeliveryStop(i));
                    //if (_pdtwDataModel.IsDeliveryStop(i))
                    //{
                    //    var deliveryStop= _pdtwDataModel.IndexToStop(i);
                    //    var deliveryIndex = i;
                    //    var foundCustomers = _pdtwDataModel.Customers.FindAll(c => c.PickupDelivery[1] == deliveryStop);
                    //    var numDeliveries = 0;
                    //    foreach (var customer in foundCustomers)
                    //    {
                    //        //put this in the demand callback!
                    //        var pickupIndex = _pdtwDataModel.StopToIndex(customer.PickupDelivery[0]);
                    //        var checkPrecedenceConstraint = solver.CheckConstraint(solver.MakeLessOrEqual(
                    //            distanceDimension.CumulVar(pickupIndex), distanceDimension.CumulVar(deliveryIndex)));
                    //        var checkSameVehicleConstraint = solver.CheckConstraint(
                    //            solver.MakeEquality(_routingModel.VehicleVar(pickupIndex),
                    //                _routingModel.VehicleVar(deliveryIndex)));
                    //        Console.WriteLine("Pairs:" + customer.PickupDelivery[0] + "->" + deliveryStop);
                    //        Console.WriteLine("Vehicle Prec const (pickup / delivery):" + _routingModel.VehicleVar(pickupIndex).Index()+ " <= "+ _routingModel.VehicleVar(deliveryIndex).Index()+" = "+checkPrecedenceConstraint);
                    //        Console.WriteLine("Same Vehicle const (pickup / delivery):"+_routingModel.VehicleIndex(pickupIndex)+"=="+_routingModel.VehicleIndex(deliveryIndex)+" = "+checkSameVehicleConstraint);
                    //        if (checkPrecedenceConstraint && checkSameVehicleConstraint)
                    //        {
                    //            numDeliveries++;
                                
                    //        }
                    //    }

                    //    var demands = _pdtwDataModel.Demands;
                    //    var currentDemand = demands[i];
                    //    //_pdtwDataModel.UpdateDemands(i, currentDemand-numDeliveries);
                    //    demands = _pdtwDataModel.Demands;
                    //}

                    //_routingModel.AddVariableMinimizedByFinalizer(capacityDimension.CumulVar(i));
                }
              

              
            }
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
                var solver = _routingModel.solver(); //Gets the underlying constraint solver
                for (int i = 0; i < _pdtwDataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = _routingIndexManager.NodeToIndex(_pdtwDataModel.PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex = _routingIndexManager.NodeToIndex(_pdtwDataModel.PickupsDeliveries[i][1]); //delivery index
                    _routingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    solver.Add(solver.MakeEquality(_routingModel.VehicleVar(pickupIndex), _routingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    solver.Add(solver.MakeLessOrEqual(distanceDimension.CumulVar(pickupIndex), distanceDimension.CumulVar(deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
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
                for (int i = 0; i < _pdtwDataModel.Vehicles.Count; ++i)
                {
                    long index = _routingModel.Start(i);
                    timeDimension.CumulVar(index).SetRange(
                        _pdtwDataModel.TimeWindows[0, 0],
                        _pdtwDataModel.TimeWindows[0, 1]); //this guarantees that a vehicle must visit the location during its time window
                }

                for (int i = 0; i < _pdtwDataModel.Vehicles.Count; ++i)
                {
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.Start(i)));
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.End(i)));
                }
            }
        }

        private RoutingSearchParameters GetDefaultSearchParameters()
        {
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.Automatic; //automatically finds best first solution strategy

            return searchParameters;
        }

        public Assignment TryGetFastSolution(PdtwDataModel pdtwDataModel)
        {
            _pdtwDataModel = pdtwDataModel;
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < MaxAllowedUpperBound; maxUpperBound++)
            {
                MaxUpperBound = maxUpperBound;
                Init();
                var searchParameters = GetDefaultSearchParameters();
                //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
                //Get the solution of the problem
                solution = _routingModel.SolveWithParameters(searchParameters);
                if (solution != null) //if true, solution was found, breaks the cycle
                {
                    break;
                }
            }

            return solution; //retuns null if no solution is found, otherwise returns the solution
        }

        public Assignment TryGetSolutionWithSearchStrategy(PdtwDataModel pdtwDataModel, int searchTimeLimitInSeconds,LocalSearchMetaheuristic.Types.Value searchAlgorithm)
        {
            _pdtwDataModel = pdtwDataModel;
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < MaxAllowedUpperBound; maxUpperBound++)
            {
                MaxUpperBound = maxUpperBound;
                Init();
                var searchParameters = GetSearchParametersWithSearchStrategy(searchTimeLimitInSeconds, searchAlgorithm);
                //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_pickupDeliveryDataModel.InitialRoutes, true);
                //Get the solution of the problem
                solution = _routingModel.SolveWithParameters(searchParameters); //solves the problem
                if (solution != null) //if true, solution was found, breaks the cycle
                {
                    break;
                }
            }
            return solution; //retuns null if no solution is found, otherwise returns the solution
        }
        private RoutingSearchParameters GetSearchParametersWithSearchStrategy(int searchTimeLimit,LocalSearchMetaheuristic.Types.Value searchAlgorithm)
        {
            var searchParam = GetDefaultSearchParameters();
            searchParam.LocalSearchMetaheuristic = searchAlgorithm;
            searchParam.TimeLimit = new Duration { Seconds = searchTimeLimit };
            searchParam.LogSearch = false; //logs the search if true
            return searchParam;

        }

        public PdtwSolutionObject GetSolutionObject(Assignment solution)
        {
            PdtwSolutionObject pdtwSolutionObject = null;
            if (solution != null) { 

                var solutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(solution);
                if (solutionDictionary != null)
                {
                    var solutionMetricsDictionary = GetVehicleRouteMetrics(solution);
                    pdtwSolutionObject = new PdtwSolutionObject(solutionDictionary,solutionMetricsDictionary);
                    
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
                for (int i = 0; i < _pdtwDataModel.Vehicles.Count; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    List<Customer> routeCustomers = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    long[] timeWindow;
                    Stop currentStop = null;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false) //while the iterator isn't done
                    {
                        
                        //routeStops add
                        currentStop = _pdtwDataModel.IndexToStop((int)index);
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
                    //routeStops add
                    currentStop = _pdtwDataModel.IndexToStop((int)index);
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
                            }
                        }
                    }
                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(_pdtwDataModel.Vehicles[i],
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
                Calculator calculator = new Calculator();
                var timeDim = _routingModel.GetMutableDimension("Time");
                var distanceDim = _routingModel.GetMutableDimension("Distance");
                long totalTime = 0;
                long totalDistance = 0;
                long totalLoad = 0;
                var solutionObject = GetSolutionObject(solution);
                for (int i = 0; i < _pdtwDataModel.Vehicles.Count; ++i)
                {
                    int nodeIndex = 0;
                    long routeLoad = 0;
                    long previousRouteLoad = 0;
                    printableList.Add("Vehicle "+i+" Route:");
                    var index = _routingModel.Start(i);
                    string concatenatedString = "";
                    while (_routingModel.IsEnd(index) == false)
                    {
                        previousRouteLoad = routeLoad;
                        var timeVar = timeDim.CumulVar(index);
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        routeLoad += _pdtwDataModel.Demands[nodeIndex];

                        var previousIndex = index;
                        index = solution.Value(_routingModel.NextVar(index));
                        var timeToTravel =
                            _routingModel.GetArcCostForVehicle(previousIndex, index,
                                0); //Gets the travel time between the previousNode and the NextNode
                        var distance =
                            calculator.TravelTimeToDistance((int)timeToTravel,
                                _pdtwDataModel
                                    .VehicleSpeed); //Calculates the distance based on the travel time and vehicle speed
                        concatenatedString += _pdtwDataModel.IndexToStop(nodeIndex) + ":T("+ solution.Min(timeVar) + ","+solution.Max(timeVar)+"), L("+ routeLoad+") --["+distance+"m]--> ";
                        //need to fix totalLoad
                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad

                    }

                    var endDistanceVar = distanceDim.CumulVar(index);
                    var endTimeVar = timeDim.CumulVar(index);
                    nodeIndex = _routingIndexManager.IndexToNode(index);
                    routeLoad += _pdtwDataModel.Demands[nodeIndex];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    concatenatedString+=_pdtwDataModel.IndexToStop(nodeIndex) + ":T("+ solution.Min(endTimeVar) + ","+ solution.Max(endTimeVar) + "), L("+routeLoad+")";
                    printableList.Add(concatenatedString);
                    long routeDistance = (long)calculator.TravelTimeToDistance((int)solution.Min(endDistanceVar),
                        _pdtwDataModel
                            .VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    printableList.Add("Route time: "+ TimeSpan.FromSeconds(solution.Min(endTimeVar)).TotalMinutes + " minutes");
                    printableList.Add("Route Distance: "+ routeDistance+" meters");
                    printableList.Add("Route Total Load:" + totalLoad); 
                    printableList.Add("Route customers served: " + solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count);
                    printableList.Add("Avg time cost:" + solution.Min(endTimeVar) / index); //debug
                    totalDistance += routeDistance;
                    totalTime += solution.Min(endTimeVar);
                    printableList.Add("------------------------------------------");
                }


                printableList.Add("Total time of all routes: "+ TimeSpan.FromSeconds(totalTime).TotalMinutes+" minutes");
                printableList.Add("Total distance of all routes: "+ totalDistance+" meters");
                printableList.Add("Total Load of all routes: " + totalLoad + " customers");
                printableList.Add("Total customers served: "+ solutionObject.CustomerNumber+"/"+ _pdtwDataModel.Customers.Count);
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
                Console.WriteLine("Max Upper Bound limit:" + MaxUpperBound + " minutes");
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

        public Dictionary<string,long[]> GetVehicleRouteMetrics(Assignment solution) //computes the metrics for each vehicle route
        {
       
            var calculator = new Calculator();
            Dictionary<string, long[]> vehicleMetricsDictionary = new Dictionary<string, long[]>();
            if (solution != null)
            {
                
                var timeDim = _routingModel.GetMutableDimension("Time");
                var distanceDim = _routingModel.GetMutableDimension("Distance");
                //route metrics each index is the vehicle index
                long[] routeTimes = new long[_pdtwDataModel.Vehicles.Count];
                long[] routeDistances = new long[_pdtwDataModel.Vehicles.Count];
                long[] routeLoads = new long[_pdtwDataModel.Vehicles.Count];
                for (int i = 0; i < _pdtwDataModel.Vehicles.Count; ++i)
                {
                    long routeLoad = 0;
                    long totalLoad = 0;
                    long previousRouteLoad = 0;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false)
                    {
                        previousRouteLoad = routeLoad;
                        routeLoad += _pdtwDataModel.Demands[_routingIndexManager.IndexToNode(index)];
                        index = solution.Value(_routingModel.NextVar(index));
                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    }
                    routeLoad += _pdtwDataModel.Demands[_routingIndexManager.IndexToNode(index)];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    var endTimeVar = timeDim.CumulVar(index);
                    var endDistanceVar = distanceDim.CumulVar(index);
                    routeLoads[i] = totalLoad;
                    routeDistances[i] = (long)calculator.TravelTimeToDistance((int)solution.Min(endDistanceVar), _pdtwDataModel.VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    routeTimes[i] = solution.Min(endTimeVar);
                }
                vehicleMetricsDictionary.Add("routeLoads", routeLoads);
                vehicleMetricsDictionary.Add("routeDistances", routeDistances);
                vehicleMetricsDictionary.Add("routeTimes", routeTimes);
            }
            return vehicleMetricsDictionary;
        }
    }
}
