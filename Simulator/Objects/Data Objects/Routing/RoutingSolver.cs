using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class RoutingSolver //pickup delivery with time windows solver
    {
        public RoutingDataModel DataModel;
        public RoutingIndexManager RoutingIndexManager;
        public RoutingModel RoutingModel;
        public bool DropNodesAllowed;

        public int MaximumDeliveryDelayTime; //the current upper bound limit of the timeWindows for the found solution (in seconds)

        public RoutingSolver(RoutingDataModel dataModel, bool dropNodesAllowed)
        {
            DropNodesAllowed = dropNodesAllowed;
            MaximumDeliveryDelayTime = 0; //default value
            if (dataModel.TimeWindows.GetLength(0) == dataModel.TravelTimes.GetLength(0))
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
                    RoutingIndexManager = new RoutingIndexManager(
                        DataModel.TravelTimes.GetLength(0),
                        DataModel.VehicleCapacities.Length,
                        DataModel.Starts, DataModel.Ends);
                }
                else
                {
                    throw new Exception("Starts or Ends in DataModel is null");
                }

                //Create routing model
                RoutingModel = new RoutingModel(RoutingIndexManager);
                // Create and register a transit callback.
                var transitCallbackIndex = RoutingModel.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                        var fromNode = RoutingIndexManager.IndexToNode(fromIndex);
                        var toNode = RoutingIndexManager.IndexToNode(toIndex);
                        return DataModel.TravelTimes[fromNode, toNode];
                    }
                );

                //Create and register demand callback
                var demandCallbackIndex = RoutingModel.RegisterUnaryTransitCallback(
                    (long fromIndex) => {
                        // Convert from routing variable Index to demand NodeIndex.
                        var fromNode = RoutingIndexManager.IndexToNode(fromIndex);
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
                    long penalty = 99999999;
                    for (int j = 0; j < DataModel.Starts.GetLength(0); j++)
                    {
                        var startIndex = DataModel.Starts[j];
                        for (int i = 0; i < DataModel.TravelTimes.GetLength(0); ++i)
                        {
                            if (startIndex != i)
                            {
                                RoutingModel.AddDisjunction(new long[] {RoutingIndexManager.NodeToIndex(i)}, penalty);//adds disjunction to all stop besides start stops
                            }
                        }
                    }
                }


                var vehicleCost = 10000;
                RoutingModel.SetFixedCostOfAllVehicles(vehicleCost);//adds a penalty for using each vehicle

                RoutingModel.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex); //Sets the cost function of the model such that the cost of a segment of a route between node 'from' and 'to' is evaluator(from, to), whatever the route or vehicle performing the route.

                //Adds capacity constraints
                RoutingModel.AddDimensionWithVehicleCapacity(
                    demandCallbackIndex, 0,  // null capacity slack
                    DataModel.VehicleCapacities,   // vehicle maximum capacities
                    false,     // start cumul to zero
                    "Capacity");
                RoutingDimension capacityDimension = RoutingModel.GetMutableDimension("Capacity");

                //Add Time window constraints
                RoutingModel.AddDimension(
                    transitCallbackIndex, // transit callback
                    86400, // allow waiting time (24 hours in seconds)
                    86400, // maximum travel time per vehicle (24 hours in seconds)
                    DataModel.ForceCumulToZero, // start cumul to zero
                    "Time");
                RoutingDimension timeDimension = RoutingModel.GetMutableDimension("Time");
                //timeDimension.SetGlobalSpanCostCoefficient(10);
                var solver = RoutingModel.solver();
                // Add time window constraints for each location except depot.
                for (int i = 0; i < DataModel.TimeWindows.GetLength(0); i++)
                {
                    long index = RoutingIndexManager.NodeToIndex(i); //gets the node index
                    if (index != -1)
                    {
                        var lowerBound = DataModel.TimeWindows[i, 0]; //minimum time to be at current index (lower bound for the timeWindow of current Index)
                        var softUpperBound = DataModel.TimeWindows[i, 1]; //soft maxUpperBound for the timeWindow at current index
                        var upperBound = softUpperBound + MaximumDeliveryDelayTime; //maxUpperBound to be at current index (upperbound for the timeWindow at current index)
                        //softupperbound and upperbound are different because the upperbound is usually bigger than the softuppberbound in order to soften the current timeWindows, enabling to generate a solution that accomodates more requests
                        timeDimension.CumulVar(index).SetRange(lowerBound, upperBound); //sets the maximum upper bound and lower bound limit for the timeWindow at the current index
                        timeDimension.SetCumulVarSoftUpperBound(index, softUpperBound, 10000); //adds soft upper bound limit which is the requested time window
                        RoutingModel.AddToAssignment(timeDimension.SlackVar(index)); //add timeDimension slack var for current index to the assignment
                        RoutingModel.AddToAssignment(timeDimension.TransitVar(index)); // add timeDimension transit var for current index to the assignment
                        RoutingModel.AddToAssignment(capacityDimension.TransitVar(index)); //add transit capacity var for current index to assignment
                    }
                }

                
                // Add time window constraints for each vehicle start node, and add to assignment the slack and transit vars for both dimensions
                for (int i = 0; i < DataModel.VehicleCapacities.Length; i++)
                {
                    long index = RoutingModel.Start(i);
                    var startDepotIndex = DataModel.Starts[i];
                    timeDimension.CumulVar(index).SetRange(DataModel.TimeWindows[startDepotIndex, 0], DataModel.TimeWindows[startDepotIndex, 1]); //this guarantees that a vehicle must visit the location during its time 
                    RoutingModel.AddToAssignment(timeDimension.SlackVar(index)); //add timeDimension slack var for depot index for vehicle i depotto assignment
                    RoutingModel.AddToAssignment(timeDimension.TransitVar(index));//add timeDimension  transit var for depot index for vehicle i depot to assignment
                    RoutingModel.AddToAssignment(capacityDimension.TransitVar(index));//add capacityDimension transit var for vehicle i depot
                }

                //Add client max ride time constraint, enabling better service quality
                for (int i = 0; i < DataModel.PickupsDeliveries.Length; i++) //iterates over each pickupDelivery pair
                {
                    int vehicleIndex = -1;
                    if (DataModel.PickupsDeliveries[i][0] == -1)//if the pickupDelivery is a customer inside a vehicle
                    {
                        vehicleIndex = DataModel.CustomersVehicle[i]; //gets the vehicle index
                    }
                    var pickupIndex = vehicleIndex == -1 ? RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][0]):RoutingModel.Start(vehicleIndex);//if is a customer inside a vehicle the pickupIndex will be the vehicle startIndex, otherwise its the customers real pickupIndex
                    var deliveryIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][1]);
                    var rideTime = DataModel.CustomersRideTimes[i];
                    var directRideTimeDuration = DataModel.TravelTimes[pickupIndex,DataModel.PickupsDeliveries[i][1]];
                    var realRideTimeDuration = rideTime+(timeDimension.CumulVar(deliveryIndex) - timeDimension.CumulVar(pickupIndex));//adds the currentRideTime of the customer and subtracts cumulative value of the ride time of the delivery index with the current one of the current index to get the real ride time duration
                    solver.Add(realRideTimeDuration < directRideTimeDuration + DataModel.MaxCustomerRideTime);//adds the constraint so that the current ride time duration does not exceed the directRideTimeDuration + maxCustomerRideTimeDuration
                }
                //Add precedence and same vehicle Constraints
                for (int i = 0; i < DataModel.PickupsDeliveries.GetLength(0); i++)
                {
                    if (DataModel.PickupsDeliveries[i][0] != -1)
                    {
                        long pickupIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][0]); //pickup index
                        long deliveryIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[i][1]); //delivery index
                        RoutingModel.AddPickupAndDelivery(pickupIndex, deliveryIndex); //Notifies that the pickupIndex and deliveryIndex form a pair of nodes which should belong to the same route.
                        solver.Add(solver.MakeEquality(RoutingModel.VehicleVar(pickupIndex), RoutingModel.VehicleVar(deliveryIndex))); //Adds a constraint to the solver, that defines that both these pickup and delivery pairs must be picked up and delivered by the same vehicle (same route)
                        solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex), timeDimension.CumulVar(deliveryIndex))); //Adds the precedence constraint to the solver, which defines that each item must be picked up at pickup index before it is delivered to the delivery index
                        //timeDimension.SlackVar(pickupIndex).SetMin(4);//mininimum slack will be 3 seconds (customer enter timer)
                        //timeDimension.SlackVar(deliveryIndex).SetMin(3); //minimum slack will be 3 seconds (customer leave time)
                    }
                }
                //Constraints to enforce that if there is a customer inside a vehicle, it has to be served by that vehicle
                for (int customerIndex = 0; customerIndex < DataModel.CustomersVehicle.GetLength(0); customerIndex++)
                {
                    var vehicleIndex = DataModel.CustomersVehicle[customerIndex];
                    if (vehicleIndex != -1)//if the current customer is inside a vehicle
                    {
                        var vehicleStartIndex = RoutingModel.Start(vehicleIndex); //vehicle start depot index
                        var deliveryIndex = RoutingIndexManager.NodeToIndex(DataModel.PickupsDeliveries[customerIndex][1]); //gets the deliveryIndex
                        solver.Add(solver.MakeEquality(RoutingModel.VehicleVar(vehicleStartIndex), RoutingModel.VehicleVar(deliveryIndex))); //vehicle with vehicleIndex has to be the one that delivers customer with nodeDeliveryIndex;
                        //this constraint enforces that the vehicle indexed by vehicleIndex has to be the vehicle which services (goes to) the nodeDeliveryIndex as well
                    }
                }

                for (int i = 0; i < DataModel.VehicleCapacities.Length;i++)
                {
                    RoutingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(RoutingModel.Start(i)));
                    RoutingModel.AddVariableMinimizedByFinalizer(
                        timeDimension.CumulVar(RoutingModel.End(i)));
                }
            }

           

        }

        public void PrintSolutionUsingRoutingVars(Assignment solution)
        {
            if (solution != null)
            {
                var capacityDimension = RoutingModel.GetMutableDimension("Capacity");
                var timeDimension = RoutingModel.GetMutableDimension("Time");
                // Inspect solution.
                long totalTime = 0;
                long totalLoad = 0;
                for (int i = 0; i < DataModel.VehicleCapacities.Length; ++i)
                {
                    Console.WriteLine("Route for Vehicle {0}:", i);
                    var index = RoutingModel.Start(i);
                    long routeLoad = 0;
                    while (RoutingModel.IsEnd(index) == false)
                    {
                        var capVar = capacityDimension.CumulVar(index);
                        var capTransit = capacityDimension.TransitVar(index);
                        var timeVar = timeDimension.CumulVar(index);
                        var slackVar = timeDimension.SlackVar(index);
                        var transitVar = timeDimension.TransitVar(index);
                        Console.Write("{0} Time: C({1},{2}) S({3},{4}) T({5}) Capacity: C({6}) T({7})-> ",
                            RoutingIndexManager.IndexToNode(index),
                            solution.Min(timeVar),
                            solution.Max(timeVar), solution.Min(slackVar), solution.Max(slackVar),
                            solution.Value(transitVar), solution.Value(capVar), solution.Value(capTransit));
                        index = solution.Value(RoutingModel.NextVar(index));
                    }
                    var endTimeVar = timeDimension.CumulVar(index);
                    var endCapVar = capacityDimension.CumulVar(index);
                    routeLoad += solution.Value(endCapVar);
                    totalLoad += routeLoad;
                    Console.WriteLine("{0} Time: C({1},{2}) Capacity: C({3})",
                        RoutingIndexManager.IndexToNode(index),
                        solution.Min(endTimeVar),
                        solution.Max(endTimeVar), solution.Value(endCapVar));
                    Console.WriteLine("Time of the route: {0}min", solution.Min(endTimeVar));
                    Console.WriteLine("Route load: " + routeLoad);
                    totalTime += solution.Min(endTimeVar);
                }
                Console.WriteLine("Total time of all routes: {0}min", totalTime);
                Console.WriteLine("Total Load of all routes: " + totalLoad + "min");
                Console.WriteLine("Avg route Total Time:{0}", totalTime / DataModel.VehicleCapacities.Length);
         
            }
            else
            {
                Console.WriteLine("No sol found upperbound:" + MaximumDeliveryDelayTime);
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
            if (searchParameters == null)
            {
                searchParameters = GetDefaultSearchParameters();
            }
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int currentMaximumDelayTime = 0; currentMaximumDelayTime < DataModel.MaxAllowedDeliveryDelayTime; currentMaximumDelayTime = currentMaximumDelayTime + 60) //iterates adding 1 minute to maximum allowed timeWindow (60 seconds) if a feasible solution isnt found for the current upperbound
            {
                MaximumDeliveryDelayTime = currentMaximumDelayTime;
                Init();
                //Get the solution of the problem
                try
                {
                    solution = RoutingModel.SolveWithParameters(searchParameters);
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
            int solverStatus = RoutingModel.GetStatus();
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
                routingSolutionObject = new RoutingSolutionObject(this,solution);
            }
            return routingSolutionObject;
        }


        public List<string> GetSolutionPrintableList(Assignment solution)
        {
            List<string> printableList = new List<string>();
            if (solution != null)
            {
                var timeDim = RoutingModel.GetMutableDimension("Time");
                var capacityDim = RoutingModel.GetMutableDimension("Capacity");
                long totalTime = 0;
                long totalDistance = 0;
                long totalLoad = 0;
                var solutionObject = GetSolutionObject(solution);
                for (int i = 0; i < DataModel.IndexManager.Vehicles.Count; ++i)
                {
                    int nodeIndex = 0;
                    long routeDistance = 0;
                    long currentLoad = 0;
                    long routeSlackTime = 0;
                    long routeTransitTime = 0;
                    printableList.Add("Vehicle "+DataModel.IndexManager.Vehicles[i].Id+" Route:");
                    var index = RoutingModel.Start(i);
                    string concatenatedString = "";
                    while (RoutingModel.IsEnd(index) == false)
                    {
                        var timeCumulVar = timeDim.CumulVar(index);
                        var timeSlackVar = timeDim.SlackVar(index);
                        var timeTransitVar = timeDim.TransitVar(index);
                        var capacityCumulVar = capacityDim.CumulVar(index);
                        var capacityTransitVar = capacityDim.TransitVar(index);
                        nodeIndex = RoutingIndexManager.IndexToNode(index);
                        //Console.WriteLine("Stop:"+DataModel.IndexManager.GetStop(nodeIndex) +" Demand: "+ DataModel.Demands[DataModel.IndexManager.GetStopIndex(DataModel.IndexManager.GetStop(nodeIndex))]);
                        //Console.WriteLine(DataModel.IndexManager.GetStop(nodeIndex) + " / Time Dimension - Cumul:(" + solution.Min(timeDim.CumulVar(index)) + "," + solution.Max(timeDim.CumulVar(index)) + ") - Slack: (" + solution.Min(timeDim.SlackVar(index)) + "," + solution.Max(timeDim.SlackVar(index)) + ") - Transit: (" + solution.Value(timeDim.TransitVar(index)) + ") / Capacity Dimension - Cumul:" + solution.Value(capacityDim.CumulVar(index)) + " Transit:" + solution.Value(capacityDim.TransitVar(index)));
                        var tw1 = solution.Min(timeDim.CumulVar(index));
                        var tw2 = solution.Max(timeDim.CumulVar(index));
                        var slack1 = solution.Min(timeDim.SlackVar(index));
                        var slack2 = solution.Max(timeDim.SlackVar(index));
                        var transit = solution.Value(timeDim.TransitVar(index));
                        if (nodeIndex > DataModel.TravelTimes.GetLength(0))
                        {
                            throw new Exception("Index out of bounds for nodeIndex");
                        }
                        var arcTransit = DataModel.TravelTimes[nodeIndex, RoutingIndexManager.IndexToNode(solution.Value(RoutingModel.NextVar(index)))];
                        if (arcTransit != transit)
                        {
                            tw2 = (tw1 == tw2 && slack1 != 0) ? tw1 + slack1 : tw2;
                            transit = (tw1 == tw2 && slack1 != 0) ? transit - slack1 : transit;

                        }
                        else
                        {
                            tw2 = (tw1 == tw2 && slack1 != 0) ? tw1 + transit + slack1:tw2;
                        }

                        //Console.WriteLine(DataModel.IndexManager.GetStop(nodeIndex) + " TimeWindow ("+tw1+","+tw2+") Transit("+transit+")");
                        double timeToTravel = solution.Value(timeTransitVar)-solution.Value(timeSlackVar);
                        routeSlackTime += solution.Value(timeSlackVar);
                        routeTransitTime += solution.Value(timeTransitVar);
                        currentLoad = solution.Value(capacityCumulVar) + solution.Value(capacityTransitVar);
                        
                        var distance = Calculator.TravelTimeToDistance((int)timeToTravel,DataModel.IndexManager.Vehicles[i].Speed);
                        if (DataModel.IndexManager.GetStop(nodeIndex) != null)
                        {
                            //concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" + tw1 + ";" + tw2 + "}; L:" +currentLoad+") --[" + Math.Round(distance) + "m = "+ timeToTravel+ " secs]--> ";
                            concatenatedString += nodeIndex + "(T:{" + tw1 + ";" + tw2 + "}; L:" + currentLoad + ") --[" + Math.Round(distance) + "m = " + timeToTravel + " secs]--> ";

                        }
                        if (DataModel.IndexManager.GetStop(RoutingIndexManager.IndexToNode(index)) == null) //if the next stop is null finish printing
                        {
                            //concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" +tw1 + ";" + tw2 + "}; L:" +currentLoad + ")";
                            concatenatedString += nodeIndex + "(T:{" +
                                                  tw1 + ";" + tw2 + "}; L:" +
                                                  currentLoad + ")";
                        }

                        routeDistance += (long)distance;
                        totalLoad += solution.Value(capacityTransitVar) > 0 ? solution.Value(capacityTransitVar):0;
                        index = solution.Value(RoutingModel.NextVar(index));
                    }
                    nodeIndex = RoutingIndexManager.IndexToNode(index);
                    Console.WriteLine(DataModel.IndexManager.GetStop(nodeIndex) + " / Time Dimension - Cumul:(" + solution.Min(timeDim.CumulVar(index)) + "," + solution.Max(timeDim.CumulVar(index)) + ") / Capacity Dimension - Cumul:" + solution.Value(capacityDim.CumulVar(index)));
                    var endTimeVar = timeDim.CumulVar(index);
                    currentLoad = solution.Value(capacityDim.CumulVar(index));
                    if (DataModel.IndexManager.GetStop(nodeIndex) != null)
                    {
                        //concatenatedString += DataModel.IndexManager.GetStop(nodeIndex).Id + "(T:{" + solution.Min(endTimeVar) + ";" + solution.Max(endTimeVar) + "}; L:" + currentLoad + ")";
                        concatenatedString += nodeIndex + "(T:{" + solution.Min(endTimeVar) + ";" + solution.Max(endTimeVar) + "}; L:" + currentLoad + ")";
                    }

                    var startTimeVar = timeDim.CumulVar(RoutingModel.Start(i));
                    printableList.Add(concatenatedString);
                    //long routeDistance = (long)Calculator.TravelTimeToDistance((int)solution.Min(endPickupDeliveryVar), DataModel.VehicleSpeed); //Gets the route distance which is the actual cumulative value of the distance dimension at the last stop of the route
                    var routeTime = solution.Max(endTimeVar) - solution.Min(startTimeVar);
                    printableList.Add("Route Total Time: "+ TimeSpan.FromSeconds(routeTime).TotalMinutes + " minutes");
                    printableList.Add("Route Distance: "+ routeDistance+" meters");
                    printableList.Add("Route distance (using cumul var):"+ Calculator.TravelTimeToDistance((int)solution.Min(endTimeVar), DataModel.IndexManager.Vehicles[i].Speed));//NEED TO CHANGE
                    printableList.Add("Route Total Load:" + totalLoad);
                    printableList.Add("Route customers served: " + solutionObject.GetVehicleCustomers(solutionObject.IndexToVehicle(i)).Count);
                    printableList.Add("Route Total Transit Time: "+routeTransitTime);
                    printableList.Add("Route Total Slack Time: "+routeSlackTime);
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
                printableList.Add("Total vehicles used: "+solutionObject.TotalVehiclesUsed + "/"+DataModel.IndexManager.Vehicles.Count);
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
                Console.WriteLine("Maximum Upper Bound limit:" + TimeSpan.FromSeconds(MaximumDeliveryDelayTime).TotalMinutes + " minutes");
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
    }
}
