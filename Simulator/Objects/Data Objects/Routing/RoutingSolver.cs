using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class RoutingSolver //pickup delivery with time windows solver
    {
        public RoutingDataModel DataModel;
        private RoutingIndexManager _routingIndexManager;
        private RoutingModel _routingModel;
        private int _transitCallbackIndex;
        private int _demandCallbackIndex;
        public bool DropNodesAllowed;
        private long _maximumVehicleWaitTimeAtEachStop;
        private long _maximumVehicleRouteTime;
        public int MaxUpperBound; //the current upper bound limit of the timeWindows for the found solution (in seconds)


        public RoutingSolver(RoutingDataModel dataModel, bool dropNodesAllowed)
        {
            DropNodesAllowed = dropNodesAllowed;
            MaxUpperBound = 0; //default value
            _maximumVehicleWaitTimeAtEachStop = 60 * 30; //30mins
            _maximumVehicleRouteTime = 60 * 60 * 24; //24hours
            if (dataModel.TimeWindows.GetLength(0) == dataModel.TimeMatrix.GetLength(0))
            {
                DataModel = dataModel;
            }
            else
            {
                throw new ArgumentException("There is a problem in DataModel inputs");
            }


        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init()
        {
            if (DataModel != null)
            {
                // Create RoutingModel Index RoutingIndexManager
                if (DataModel.Starts != null && DataModel.Ends != null)
                {
                    _routingIndexManager = new RoutingIndexManager(
                        DataModel.TimeMatrix.GetLength(0),
                        DataModel.IndexManager.Vehicles.Count,
                        DataModel.Starts, DataModel.Ends);
                }
                else
                {
                    throw new Exception("Starts or Ends in DataModel is null");
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
                        return DataModel.TimeMatrix[fromNode, toNode];
                    }
                );

                //Create and register demand callback
                _demandCallbackIndex = _routingModel.RegisterUnaryTransitCallback(
                    (long fromIndex) => {
                        // Convert from routing variable Index to demand NodeIndex.
                        var fromNode = _routingIndexManager.IndexToNode(fromIndex);
                        return DataModel.Demands[fromNode];
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
                    for (int j = 0; j < DataModel.Starts.GetLength(0); j++)
                    {
                        var startIndex = DataModel.Starts[j];
                        for (int i = 0; i < DataModel.TimeMatrix.GetLength(0); ++i)
                        {
                            if (startIndex != i)
                            {
                                _routingModel.AddDisjunction(new long[] {_routingIndexManager.NodeToIndex(i)}, penalty);//adds disjunction to all stop besides start stops
                            }
                        }
                    }
                }

                _routingModel.SetArcCostEvaluatorOfAllVehicles(_transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.

            

                //Add Time window constraints
                _routingModel.AddDimension(
                    _transitCallbackIndex, // transit callback
                    _maximumVehicleWaitTimeAtEachStop, // allow waiting time 
                    _maximumVehicleRouteTime, // maximum travel time per vehicle
                    false, // start cumul to zero
                    "Time");
                RoutingDimension timeDimension = _routingModel.GetMutableDimension("Time");
                timeDimension.SetGlobalSpanCostCoefficient(100);
                // Add time window constraints for each location except depot.
                for (int i = 0; i < DataModel.TimeWindows.GetLength(0); i++)
                {
                    long index = _routingIndexManager.NodeToIndex(i); //gets the node index
                    if (index != -1)
                    {
                        var lowerBound = DataModel.TimeWindows[i, 0]; //minimum time to be at current index (lower bound for the timeWindow of current Index)
                        var softUpperBound = DataModel.TimeWindows[i, 1]; //soft maxUpperBound for the timeWindow at current index
                        var upperBound = softUpperBound + MaxUpperBound; //maxUpperBound to be at current index (upperbound for the timeWindow at current index)
                        //softupperbound and upperbound are different because the upperbound is usually bigger than the softuppberbound in order to soften the current timeWindows, enabling to generate a solution that accomodates more requests
                        timeDimension.CumulVar(index).SetRange(lowerBound, upperBound); //sets the maximum upper bound and lower bound limit for the timeWindow at the current index
                        timeDimension.SetCumulVarSoftUpperBound(index, softUpperBound, 1000); //adds soft upper bound limit which is the requested time window
                        _routingModel.AddToAssignment(timeDimension.SlackVar(index)); //add slack var for current index to the assignment
                        _routingModel.AddToAssignment(timeDimension.TransitVar(index)); // add transit var for current index to the assignment
                    }
                }

                // Add time window constraints for each vehicle start node.
                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; i++)
                {
                    long index = _routingModel.Start(i);
                    var startDepotIndex = DataModel.Starts[i];
                    timeDimension.CumulVar(index).SetRange(DataModel.TimeWindows[startDepotIndex, 0], DataModel.TimeWindows[startDepotIndex, 1]); //this guarantees that a vehicle must visit the location during its time 
                    //timeDimension.SlackVar(index).SetRange(0, _maximumVehicleWaitTimeAtEachStop);
                    _routingModel.AddToAssignment(timeDimension.SlackVar(index)); //add slack var for depot index for vehicle i to assignment
                    _routingModel.AddToAssignment(timeDimension.TransitVar(index));//add transit var for depot index for vehicle i to assignment
                }

                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; i++)
                {
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.Start(i)));
                    _routingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(_routingModel.End(i)));
                }

                var solver = _routingModel.solver();
                //Add client max ride time constraint, enabling better service quality
                for (int i = 0; i < _routingModel.Size(); i++)
                {
                    var pickupDeliveryPairs = Array.FindAll(DataModel.PickupsDeliveries, pickupDelivery => pickupDelivery[0] == i); //finds all the pickupdelivery pairs with pickup index i 
                    foreach (var pickupDelivery in pickupDeliveryPairs) //iterates over each deliverypair to ensure the maximum ride time constraint
                    {
                        var deliveryIndex = pickupDelivery[1];
                        var directRideTimeDuration = DataModel.TimeMatrix[pickupDelivery[0], pickupDelivery[1]];
                        var realRideTimeDuration = timeDimension.CumulVar(deliveryIndex) - timeDimension.CumulVar(i); //subtracts cumulative value of the ride time of the delivery index with the current one of the current index to get the real ride time duration
                        solver.Add(realRideTimeDuration < directRideTimeDuration+DataModel.MaxCustomerRideTime); //adds the constraint so that the current ride time duration does not exceed the directRideTimeDuration + maxCustomerRideTimeDuration
                    }
                }

                for (int i = 0; i < DataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    long pickupIndex = _routingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][0]); //pickup index
                    long deliveryIndex = _routingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][1]); //delivery index
                    _routingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                    solver.Add(solver.MakeEquality(_routingModel.VehicleVar(pickupIndex), _routingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                    solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex), timeDimension.CumulVar(deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                }
                //constraints to enforce that if there is a custumer inside a vehicle has to be served by that vehicle
                    foreach (var customer in DataModel.IndexManager.Customers)
                    {
                        if (customer.IsInVehicle)
                        {
                            var vehicleIndex = DataModel.IndexManager.Vehicles.FindIndex(v => v.Customers.Contains(customer));
                            long index = _routingModel.Start(vehicleIndex); //vehicle that starts at i
                            var deliveryIndex = _routingIndexManager.NodeToIndex(
                                DataModel.IndexManager.GetStopIndex(customer.PickupDelivery[1]));
                            solver.Add(solver.MakeEquality(_routingModel.VehicleVar(index),
                                _routingModel
                                    .VehicleVar(
                                        deliveryIndex))); //vehicle i has to be the one that delivers customer with deliveryIndex;
                            //this constraint enforces that the vehicle i has to be the vehicle which services (goes to) the deliveryIndex as well
                        }
                    }

                //Adds capacity constraints
                    _routingModel.AddDimensionWithVehicleCapacity(
                    _demandCallbackIndex, 0,  // null capacity slack
                    DataModel.VehicleCapacities,   // vehicle maximum capacities
                    false,     // start cumul to zero
                    "Capacity");
                RoutingDimension capacityDimension = _routingModel.GetMutableDimension("Capacity");
                //Add transit vars for Capacity Dimension to the Assignment
                for (int i = 0; i < DataModel.TimeWindows.GetLength(0); i++)
                {
                    long index = _routingIndexManager.NodeToIndex(i); //gets the node index
                    if (index != -1)
                    {
                        _routingModel.AddToAssignment(capacityDimension.TransitVar(index)); //add transit var for index i
                    }
                }

                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; i++)
                {
                    long index = _routingModel.Start(i);
                    _routingModel.AddToAssignment(capacityDimension.TransitVar(index));//add transit var for vehicle i depot
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



        public Assignment TryGetSolution(RoutingSearchParameters searchParameters)
        {
            Assignment solution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < DataModel.MaxAllowedUpperBoundTime; maxUpperBound = maxUpperBound + 60) //iterates adding 1 minute to maximum allowed timeWindow (60 seconds) if a feasible solution isnt found for the current upperbound
            {
                MaxUpperBound = maxUpperBound;
                Init();
                //Get the solution of the problem
                try
                {
                    if (searchParameters == null)
                    {
                        searchParameters = GetDefaultSearchParameters();
                    }
                    solution = _routingModel.SolveWithParameters(searchParameters);
                }
                catch (Exception)
                {
                    solution = null;

                }

                if (solution != null) //if true, solution was found, breaks the cycle
                {
                    break;
                }
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
                case 0: status = "ROUTING_NOT_SOLVED"; //Problem not solved yet
                    break;
                case 1: status = "ROUTING_SUCCESS"; //Problem solved successfully.
                    break;
                case 2: status = "ROUTING_FAIL"; //No solution found to the problem
                    break;
                case 3: status = "ROUTING_FAIL_TIMEOUT"; //Time limit reached before finding the solution
                    break;
                case 4: status = "ROUTING_INVALID"; //Model, parameter or flags are not valid
                    break;
            }
            return status;
        }
        
        public RoutingSolutionObject GetSolutionObject(Assignment solution)
        {
            RoutingSolutionObject routingSolutionObject = null;
            if (solution != null) { 

                var solutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(solution);
                if (solutionDictionary != null)
                {
                    var solutionMetricsDictionary = GetVehicleRouteData(solution);
                    routingSolutionObject = new RoutingSolutionObject(solutionDictionary,solutionMetricsDictionary);
                    
                }
            }
            return routingSolutionObject;
        }

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> SolutionToVehicleStopTimeWindowsDictionary(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            if (solution != null)
            {
               
                List<Customer> allCustomers = new List<Customer>(DataModel.IndexManager.Customers);
                vehicleStopCustomerTimeWindowsDictionary =
                    new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                var timeDim = _routingModel.GetMutableDimension("Time");
                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    int nodeIndex = 0;
                    List<Customer> routeCustomers = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    long[] timeWindow = null;
                    Stop currentStop = null;
                    var index = _routingModel.Start(i);
                    Stop previousStop = null;
                    while (_routingModel.IsEnd(index) == false) //while the iterator isn't done
                    {
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        //routeStops add
                        currentStop = DataModel.IndexManager.GetStop(nodeIndex);
                        var timeVar = timeDim.CumulVar(index);
                        if (currentStop != null && previousStop != null && currentStop.Id == previousStop.Id)
                        {
                            routeStops.Remove(previousStop); //removes previous stop
                            routeStops.Add(currentStop); //adds current stop
                            var joinedTimeWindow = new[] {timeWindow[0], solution.Max(timeVar)}; //adds the new timewindow the junction of the previous min time from the dummy stop
                            //with max timewindow value for the currentstop (the real stop)
                            routeTimeWindows.Remove(timeWindow); //removes previous time window
                            routeTimeWindows.Add(joinedTimeWindow);

                        }
                        else
                        {
                            if (currentStop != null && currentStop.IsDummy)
                            {
                                currentStop = TransportationNetwork.Stops.Find(s => s.Id == currentStop.Id); //finds the non dummy stop
                            }
                            routeStops.Add(currentStop); //adds the current stop
                            //timeWindow add       
                            timeWindow = new[] {solution.Min(timeVar), solution.Max(timeVar)};
                            routeTimeWindows.Add(timeWindow); //adds the timewindow to the list
                        }

                        index = solution.Value(_routingModel.NextVar(index)); //increments the iterator
                        previousStop = currentStop;
                    }
                    //timeWindow add
                    nodeIndex = _routingIndexManager.IndexToNode(index);
                    var endTimeVar = timeDim.CumulVar(index);
                    timeWindow = new[] { solution.Min(endTimeVar), solution.Max(endTimeVar) };
                    routeTimeWindows.Add(timeWindow);

                    //routeStops add
                    currentStop = DataModel.IndexManager.GetStop(nodeIndex);
                    routeStops.Add(currentStop); //adds the current stop
                    foreach (var customer in allCustomers) //loop to add the customers to the routecustomers
                    {
                        var pickupStop = customer.PickupDelivery[0];
                        var deliveryStop = customer.PickupDelivery[1];
                        if (!customer.IsInVehicle) //if the customer is not in the vehicle needs to check if the route contians the pickup and delivery stop and its precedence constraint
                        {
                            if (routeStops.Contains(pickupStop) && routeStops.Contains(deliveryStop)
                            ) //If the route contains the pickup and delivery stop
                            {
                                if (routeStops.IndexOf(pickupStop) < routeStops.IndexOf(deliveryStop) &&
                                    !routeCustomers.Contains(customer)
                                ) // if the pickup stop comes before the delivery stop (precedence constraint), adds it to the route customers list.
                                {
                                    routeCustomers.Add(customer);
                                }
                            }
                        }
                        else
                        {
                            if (routeStops.Contains(deliveryStop)) //for the cases where there is a customer already in the vehicle and therefore only needs to account for the deliveryStop
                            {
                                if (routeStops.IndexOf(deliveryStop) >= 0 && routeStops.IndexOf(pickupStop) == -1 &&
                                    !routeCustomers.Contains(customer))
                                {
                                    routeCustomers.Add(customer);
                                }
                            }
                        }
                    }
                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(DataModel.IndexManager.GetVehicle(i),
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
                var timeDim = _routingModel.GetMutableDimension("Time");
                var capacityDim = _routingModel.GetMutableDimension("Capacity");
                long totalTime = 0;
                long totalDistance = 0;
                long totalLoad = 0;

                var solutionObject = GetSolutionObject(solution);
                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; ++i)
                {
                    int nodeIndex = 0;
                    long routeLoad = 0;
                    long routeDistance = 0;
                    long previousRouteLoad = 0;
                    long routeWaitTime = 0;
                    long routeTransitTime = 0;
                    printableList.Add("Vehicle "+DataModel.IndexManager.Vehicles[i].Id+" Route:");
                    var index = _routingModel.Start(i);
                    string concatenatedString = "";
                    while (_routingModel.IsEnd(index) == false)
                    {
                        previousRouteLoad = routeLoad;
                        var timeCumulVar = timeDim.CumulVar(index);
                        var timeSlackVar = timeDim.SlackVar(index);
                        var timeTransitVar = timeDim.TransitVar(index);
                        var capacityCumulVar = capacityDim.CumulVar(index);
                        var capacityTransitVar = capacityDim.TransitVar(index);
                        nodeIndex = _routingIndexManager.IndexToNode(index);
                        routeLoad += DataModel.Demands[nodeIndex];

                        var previousIndex = index;
                        index = solution.Value(_routingModel.NextVar(index));
                        //printableList.Add(index+ " - "+solution.Max(capacityDim.CumulVar(index))); //current capacity
                        double timeToTravel = solution.Value(timeTransitVar);
                        routeWaitTime += solution.Value(timeSlackVar);
                        routeTransitTime += solution.Value(timeTransitVar);
                        var distance = DistanceCalculator.TravelTimeToDistance((int)timeToTravel,DataModel.IndexManager.Vehicles[i].Speed);
                        Console.WriteLine(DataModel.IndexManager.GetStop(nodeIndex)+" Time Dimension - Cumul: ("+solution.Min(timeCumulVar)+","+solution.Max(timeCumulVar)+") - Slack: ("+solution.Min(timeSlackVar)+","+solution.Max(timeSlackVar)+") - Transit: ("+solution.Value(timeTransitVar)+")");
                        //Console.WriteLine(DataModel.IndexManager.GetStop(nodeIndex) +" Capacity Dimension - Cumul:"+solution.Value(capacityCumulVar)+" Transit:"+solution.Value(capacityTransitVar));
                        if (DataModel.IndexManager.GetStop(nodeIndex) != null)
                        {
                            concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" +
                                                  solution.Min(timeCumulVar) + ";" + solution.Max(timeCumulVar) + "}; L:" +
                                                  routeLoad + ") --[" + Math.Round(distance) + "m = "+ solution.Value(timeTransitVar)+ " secs]--> ";

                        }
                        if (DataModel.IndexManager.GetStop(_routingIndexManager.IndexToNode(index)) == null) //if the next stop is null finish printing
                        {
                            concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" +
                                                  solution.Min(timeCumulVar) + ";" + solution.Max(timeCumulVar) + "}; L:" +
                                                  routeLoad + ")";
                        }

                        routeDistance += (long)distance;
                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad

                    }
                    var endTimeVar = timeDim.CumulVar(index);
                    nodeIndex = _routingIndexManager.IndexToNode(index);
                    routeLoad += DataModel.Demands[nodeIndex];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    if (DataModel.IndexManager.GetStop(nodeIndex) != null)
                    {
                        concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" +
                                              solution.Min(endTimeVar) + ";" + solution.Max(endTimeVar) + "}; L:" +
                                              routeLoad + ")";
                    }

                    var startTimeVar = timeDim.CumulVar(_routingModel.Start(i));
                    printableList.Add(concatenatedString);
                    //long routeDistance = (long)DistanceCalculator.TravelTimeToDistance((int)solution.Min(endPickupDeliveryVar), DataModel.VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    var routeTime = solution.Max(endTimeVar) - solution.Min(startTimeVar);
                    printableList.Add("Route Total Time: "+ TimeSpan.FromSeconds(routeTime).TotalMinutes + " minutes");
                    printableList.Add("Route Distance: "+ routeDistance+" meters");
                    printableList.Add("Route distance (using cumul var):"+ DistanceCalculator.TravelTimeToDistance((int)solution.Min(endTimeVar), DataModel.IndexManager.Vehicles[i].Speed));//NEED TO CHANGE
                    printableList.Add("Route Total Load:" + totalLoad);
                    printableList.Add("Route customers served: " + solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count);
                    printableList.Add("Route Transit Time: "+routeTransitTime);
                    printableList.Add("Route Vehicle Wait Time: "+routeWaitTime);
                    printableList.Add("Average Route Transit time: "+routeTransitTime/ solutionObject.GetVehicleStops(solutionObject.IndexToVehicle(i)).Count); //total route transit time/numberofstops visited
                    if (solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count > 0)
                    {
                        printableList.Add("Average distance traveled per Customer request: " +
                                          routeDistance / solutionObject
                                              .GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count +
                                          " meters.");
                    }

                    totalDistance += routeDistance;
                    totalTime += routeTime;
                    printableList.Add("------------------------------------------");
                }


                printableList.Add("Total time of all routes: "+ TimeSpan.FromSeconds(totalTime).TotalMinutes+" minutes");
                printableList.Add("Solution object time: " + solutionObject.TotalTimeInSeconds);
                printableList.Add("Total distance of all routes: "+ totalDistance+" meters");
                printableList.Add("Total Load of all routes: " + totalLoad + " customers");
                printableList.Add("Total customers served: "+ solutionObject.CustomerNumber+"/"+ DataModel.IndexManager.Customers.Count);
                printableList.Add("Solution Objective value: " + solution.ObjectiveValue());
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
                Console.WriteLine("Maximum Upper Bound limit:" + TimeSpan.FromSeconds(MaxUpperBound).TotalMinutes + " minutes");
                Console.WriteLine("Maximum Customer Ride Time Duration: "+TimeSpan.FromSeconds(DataModel.MaxCustomerRideTime).TotalMinutes + " minutes");
                var printableList = GetSolutionPrintableList(solution);
                foreach (var stringToBePrinted in printableList)
                {
                    Console.WriteLine(stringToBePrinted);
                }
            }
            else
            {
                throw new ArgumentNullException("Solution is null");
            }
        }

        public Dictionary<string,long[]> GetVehicleRouteData(Assignment solution) //computes the metrics for each vehicle route
        {
            
            Dictionary<string, long[]> vehicleMetricsDictionary = new Dictionary<string, long[]>();
            if (solution != null)
            {
                
                var timeDim = _routingModel.GetMutableDimension("Time");
                var vehicleNumber = DataModel.IndexManager.Vehicles.Count;
                //route metrics each index is the vehicle index
                long[] routeTimes = new long[vehicleNumber];
                long[] routeDistances = new long[vehicleNumber];
                long[] routeLoads = new long[vehicleNumber];
                for (int i = 0; i < vehicleNumber; ++i)
                {
                    long routeLoad = 0;
                    long totalLoad = 0;
                    long routeDistance = 0;
                    long previousRouteLoad = 0;
                    var index = _routingModel.Start(i);
                    while (_routingModel.IsEnd(index) == false)
                    {
                        var previousIndex = index;                      
                        previousRouteLoad = routeLoad;
                        routeLoad += DataModel.Demands[_routingIndexManager.IndexToNode(index)];
                        var timeTransitVar = timeDim.TransitVar(index);
                        index = solution.Value(_routingModel.NextVar(index));
                        double timeToTravel = solution.Value(timeTransitVar);
                        var distance = DistanceCalculator.TravelTimeToDistance((int)timeToTravel, DataModel.IndexManager.Vehicles[i].Speed);
                        routeDistance += (long)distance;
                        totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    }
                    routeLoad += DataModel.Demands[_routingIndexManager.IndexToNode(index)];
                    totalLoad += previousRouteLoad != routeLoad && routeLoad > previousRouteLoad ? routeLoad - previousRouteLoad : 0; //if the current route load is greater than previous routeload and its value has changed, adds the difference to the totalLoad
                    var endTimeVar = timeDim.CumulVar(index);
                    var startTimeVar = timeDim.CumulVar(_routingModel.Start(i));
                    routeLoads[i] = totalLoad;
                    routeDistances[i] = routeDistance;
                    routeTimes[i] = solution.Max(endTimeVar)-solution.Min(startTimeVar);
                }
                vehicleMetricsDictionary.Add("routeLoads", routeLoads);
                vehicleMetricsDictionary.Add("routeDistances", routeDistances);
                vehicleMetricsDictionary.Add("routeTimes", routeTimes);
            }
            return vehicleMetricsDictionary;
        }
    }
}
