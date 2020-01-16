using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class
        RoutingSolutionObject //pickup delivery with time windows solution object, contains the data to be used in the simulation such as the vehicles, stops and timeWindows
    {

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;


        private readonly RoutingSolver _routingSolver;

        private readonly Assignment _solution;

        public MetricsContainer MetricsContainer;

        private long[] _routeLoads;

        private long[] _routeDistancesInMeters;

        private long[] _routeTimesInSeconds;

        private Dictionary<Customer, int> _customerRideTimes;

        private Dictionary<Customer, int> _customerDelayTimes;

        private Dictionary<Customer, int> _customerWaitTimes; //only working for static routing atm

        //METRICS
        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalCustomers => GetTotalValue(_routeLoads);
        public int TotalCustomerRideTimesInSeconds => _customerRideTimes.Values.Sum();

        public long TotalDistanceInMeters => GetTotalValue(_routeDistancesInMeters);

        public long TotalTimeInSeconds => GetTotalValue(_routeTimesInSeconds);

        public long ObjectiveValue => _solution.ObjectiveValue();

        public long TotalStops
        {
            get
            {
                var totalStops = 0;
                foreach (var vehicleDict in _vehicleSolutionDictionary)
                {
                    var stops = GetVehicleStops(vehicleDict.Key);
                    if (stops.Count > 2)
                    {
                        totalStops += stops.Count; // this means the vehicle doesnt have a unassigned route
                    }
                }

                return totalStops;
            }
        }

        public int TotalCustomerDelayTimeInSeconds => _customerDelayTimes.Values.Sum();

        public int TotalCustomersEarly
        {
            get
            {
                var totalCustomers = 0;
                foreach (var customerDict in _customerDelayTimes)
                {
                    if (customerDict.Value <= 0)
                    {
                        totalCustomers++;
                    }
                }

                return totalCustomers++;
            }
        }
        public int MaximumCustomerWaitTimeInSeconds
        {
            get
            {
                var maxCustomerWaitTime = 0;
                foreach (var customerWaitTime in _customerWaitTimes)
                {
                    maxCustomerWaitTime = Math.Max(maxCustomerWaitTime, customerWaitTime.Value);
                }

                return maxCustomerWaitTime;
            }
        }

        public int MinimumCustomerRideTimeInSeconds
        {
            get
            {
                var minCustomerRideTime = 0;
                foreach (var customerRideTimes in _customerRideTimes)
                {
                    minCustomerRideTime = Math.Min(minCustomerRideTime, customerRideTimes.Value);
                }

                return minCustomerRideTime;
            }
        }

        public int MinimumCustomerDelayTimeInSeconds
        {
            get
            {
                var minCustomerDelay = 0;
                foreach (var customerDelay in _customerDelayTimes)
                {
                    minCustomerDelay = Math.Min(minCustomerDelay, customerDelay.Value);
                }

                return minCustomerDelay;
            }
        }
        public int MinimumCustomerWaitTimeInSeconds
        {
            get
            {
                var minCustomerWaitTime = 0;
                foreach (var customerWaitTime in _customerWaitTimes)
                {
                    minCustomerWaitTime = Math.Min(minCustomerWaitTime, customerWaitTime.Value);
                }

                return minCustomerWaitTime;
            }
        }

        public int MaximumCustomerRideTimeInSeconds
        {
            get
            {
                var maxCustomerRideTime = 0;
                foreach (var customerRideTimes in _customerRideTimes)
                {
                    maxCustomerRideTime = Math.Max(maxCustomerRideTime, customerRideTimes.Value);
                }

                return maxCustomerRideTime;
            }
        }

        public int MaximumCustomerDelayTimeInSeconds
        {
            get
            {
                var maxCustomerDelay = 0;
                foreach (var customerDelay in _customerDelayTimes)
                {
                    maxCustomerDelay = Math.Max(maxCustomerDelay, customerDelay.Value);
                }

                return maxCustomerDelay;
            }
        }

        public int TotalCustomersDelayed
        {
            get
            {
                var totalCustomers = 0;
                foreach (var customerDict in _customerDelayTimes)
                {
                    if (customerDict.Value > 0)
                    {
                        totalCustomers++;
                    }
                }

                return totalCustomers++;
            }
        }

        public int TotalVehiclesUsed
        {
            get
            {
                var vehiclesUsed = 0;
                foreach (var vehicle in _vehicleSolutionDictionary.Keys)
                {
                    var vehicleStops = GetVehicleStops(vehicle);
                    if (vehicleStops.Count > 2 && vehicleStops[0] != vehicleStops[1]
                    ) //this check means that the vehicle is used because there are 2 more than 2 stops
                    {
                        vehiclesUsed++;
                    }
                }

                return vehiclesUsed;
            }
        }

        public int CustomerNumber
        {
            get
            {
                if (_customerNumber == -1)
                {
                    _customerNumber = 0;
                    if (_vehicleSolutionDictionary != null)
                    {

                        foreach (var vehicleTuples in _vehicleSolutionDictionary)
                        {
                            _customerNumber += vehicleTuples.Value.Item2.Count;
                        }
                    }
                }

                return _customerNumber;
            }
        }

        public int AvgCustomerRideTimeInSeconds => (TotalCustomerRideTimesInSeconds / CustomerNumber);

        public int AvgCustomerWaitTimeInSeconds => (TotalCustomersWaitTimeInSeconds / CustomerNumber);

        public int TotalCustomersWaitTimeInSeconds => _customerWaitTimes.Values.Sum();

        public int AvgCustomerDelayTimeInSeconds
        {
            get
            {

                var avgCustomerDelayTime = 0;
                if (TotalCustomersDelayed > 0)
                {
                    var totalDelayTimes = 0;
                    foreach (var customerDelayTime in _customerDelayTimes)
                    {
                        if (customerDelayTime.Value > 0)
                        {
                            totalDelayTimes += customerDelayTime.Value;
                        }

                    }

                    avgCustomerDelayTime = totalDelayTimes / TotalCustomersDelayed;

                }

                return (int) avgCustomerDelayTime;
            }
        }

        public int MaximumRouteDistanceInMeters => (int)_routeDistancesInMeters.Max();
        public int MaximumRouteDurationInSeconds => (int)_routeTimesInSeconds.Max();

        public int MinimumRouteDurationInSeconds => (int) _routeTimesInSeconds.Min();

        public int MinimumRouteDistanceInMeters => (int) _routeDistancesInMeters.Min();

        public int AvgCustomerEarlyTimeInSeconds
        {
            get
            {
              
                var avgCustomerEarlyTime = 0;
                if (TotalCustomersDelayed > 0)
                {
                    foreach (var customerDelayTime in _customerDelayTimes)
                    {
                        var totalEarlyTimes = 0;
                        if (customerDelayTime.Value <= 0)
                        {
                            totalEarlyTimes += customerDelayTime.Value;
                        }

                        avgCustomerEarlyTime = (int) (Math.Abs(totalEarlyTimes) / TotalCustomersEarly);
                    }
                }

                return avgCustomerEarlyTime;
            }
        }

        private int _customerNumber = -1;
        // end of metrics

        public RoutingSolutionObject(RoutingSolver routingSolver, Assignment solution)
        {
            _routingSolver = routingSolver;
            _solution = solution;
            ComputeSolutionData(_solution);
            //SolutionToVehicleRouteMetrics(_solution);
            MetricsContainer = new MetricsContainer();
            RegisterAllMetrics();
        }

 

        public void RegisterAllMetrics()
        {
            MetricsContainer.AddMetric(nameof(TotalCustomers), (int)TotalCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomersEarly), TotalCustomersEarly);
            MetricsContainer.AddMetric(nameof(TotalCustomersDelayed), TotalCustomersDelayed);
            MetricsContainer.AddMetric(nameof(TotalVehiclesUsed), TotalVehiclesUsed);
            MetricsContainer.AddMetric(nameof(ObjectiveValue),(int)ObjectiveValue);
            MetricsContainer.AddMetric(nameof(MaximumCustomerWaitTimeInSeconds),(int)MaximumCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumCustomerRideTimeInSeconds),(int)MaximumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumCustomerDelayTimeInSeconds),(int)MaximumCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumRouteDistanceInMeters),MaximumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MaximumRouteDurationInSeconds),MaximumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumCustomerWaitTimeInSeconds), (int)MinimumCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumCustomerRideTimeInSeconds), (int)MinimumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumCustomerDelayTimeInSeconds), (int)MinimumCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumRouteDistanceInMeters), MinimumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MinimumRouteDurationInSeconds), MinimumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(TotalDistanceInMeters),(int)TotalDistanceInMeters);
            MetricsContainer.AddMetric(nameof(TotalCustomerDelayTimeInSeconds),TotalCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(TotalCustomerRideTimesInSeconds),TotalCustomerRideTimesInSeconds);
            MetricsContainer.AddMetric(nameof(TotalCustomersWaitTimeInSeconds),TotalCustomersWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(TotalStops),(int)TotalStops);
            MetricsContainer.AddMetric(nameof(TotalTimeInSeconds),(int)TotalTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AvgCustomerRideTimeInSeconds),AvgCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AvgCustomerDelayTimeInSeconds),AvgCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AvgCustomerEarlyTimeInSeconds),AvgCustomerEarlyTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AvgCustomerWaitTimeInSeconds),AvgCustomerWaitTimeInSeconds);
            

        }
        private void ComputeSolutionData(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> vehicleStopCustomerTimeWindowsDictionary = null;
            Dictionary<Customer, int> customerRideTimes = new Dictionary<Customer, int>();
            Dictionary<Customer,int> customerDelayTimes = new Dictionary<Customer, int>();
            Dictionary<Customer,int> customerWaitTimes = new Dictionary<Customer, int>();

            var vehicleNumber = _routingSolver.DataModel.IndexManager.Vehicles.Count;
            //route metrics each index is the vehicle index
            long[] routeTimes = new long[vehicleNumber];
            long[] routeDistances = new long[vehicleNumber];
            long[] routeLoads = new long[vehicleNumber];
            if (solution != null)
            {

                List<Customer> allCustomers = new List<Customer>(_routingSolver.DataModel.IndexManager.Customers);
                vehicleStopCustomerTimeWindowsDictionary = new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                var timeDim = _routingSolver.RoutingModel.GetMutableDimension("Time");
                var capacityDim = _routingSolver.RoutingModel.GetMutableDimension("Capacity");
                for (int i = 0; i < vehicleNumber; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    int nodeIndex = 0;
                    List<Customer> routeCustomers = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    long routeDistance = 0;
                    long[] timeWindow = null;
                    Stop currentStop = null;
                    var index = _routingSolver.RoutingModel.Start(i);
                    long routeLoad = solution.Value(capacityDim.CumulVar(index)); //initial route load
                    Stop previousStop = null;
                    List<long[]> auxiliaryTimeWindows = new List<long[]>(); //auxiliary timeWindows list to help calculate customer timewindows using unique indices for each pickup and delivery stop (multi indexing matrix)
                    List<int> routeStopsIndex = new List<int>(); //has the list of stop indices that need to be visited by the vehicle, used as an auxiliary list to mainly check for precedence constraints
                    while (_routingSolver.RoutingModel.IsEnd(index) == false) //while the iterator isn't done
                    {
                        nodeIndex = _routingSolver.RoutingIndexManager.IndexToNode(index);
                        currentStop = _routingSolver.DataModel.IndexManager.GetStop(nodeIndex);
                        var tw1 = solution.Min(timeDim.CumulVar(index));
                        var tw2 = solution.Max(timeDim.CumulVar(index));
                        var slack1 = solution.Min(timeDim.SlackVar(index));
                        var transit = solution.Value(timeDim.TransitVar(index));
                        var arcTransit = _routingSolver.DataModel.TravelTimes[_routingSolver.DataModel.Starts[i], _routingSolver.RoutingIndexManager.IndexToNode(solution.Value(_routingSolver.RoutingModel.NextVar(index)))]; //arc transit
                        if (arcTransit != transit)
                        {
                            tw2 = (tw1 == tw2 && slack1 != 0) ? tw1 + slack1 : tw2;
                        }
                        else
                        {
                            tw2 = (tw1 == tw2 && slack1 != 0) ? tw1 + transit + slack1 : tw2;
                        }

                        //calculate routeDistance
                        double timeToTravel = _routingSolver.DataModel.TravelTimes[_routingSolver.DataModel.Starts[i], _routingSolver.RoutingIndexManager.IndexToNode(solution.Value(_routingSolver.RoutingModel.NextVar(index)))];
                        var distance = Calculator.TravelTimeToDistance((int)timeToTravel, _routingSolver.DataModel.IndexManager.Vehicles[i].Speed);
                        routeDistance += (long)distance;
                        //add currentLoad
                        routeLoad += solution.Value(capacityDim.TransitVar(index)) > 0 ? solution.Value(capacityDim.TransitVar(index)) : 0; //adds the load if it is greater than 0
                        //auxiliary data structures add
                        auxiliaryTimeWindows.Add(new long[]{tw1,tw2}); //adds current timeWindow
                        routeStopsIndex.Add(nodeIndex); //adds current stopIndex
                        if (currentStop.Id == previousStop?.Id)//if current stop is the same as the previous one
                        {
                            routeStops.Remove(previousStop); //removes previous stop
                            routeStops.Add(currentStop); //adds current stop
                            var joinedTimeWindow = new[] { timeWindow[0], tw2 }; //adds the new timewindow the junction of the previous min time from the dummy stop
                            //with max timewindow value for the currentstop (the real stop)
                            routeTimeWindows.Remove(timeWindow); //removes previous time window
                            routeTimeWindows.Add(joinedTimeWindow);
                        }
                        else
                        {                       
                            routeStops.Add(currentStop); //adds the current stop
                            //timeWindow add       
                            timeWindow = new[] { tw1, tw2 };
                            routeTimeWindows.Add(timeWindow); //adds the timewindow to the list
                        }
                        //Check if vehicle serves any customer, if so, adds the ride time for that client for the routing solution
                      
                        if (capacityDim.TransitVar(index) == -1)
                        {
                            var pickupDelivery = Array.Find(_routingSolver.DataModel.PickupsDeliveries, pd => pd[1] == nodeIndex);
                            var customerIndex = Array.FindIndex(_routingSolver.DataModel.PickupsDeliveries, pd => pd[1] == nodeIndex);

                            if (pickupDelivery != null)
                            {
                                if (pickupDelivery.Length > 2)
                                {
                                    throw new Exception("error more than 1 customer with same stop indices");
                                }

                                var routePickupIndex = routeStopsIndex.IndexOf(pickupDelivery[0]);
                                var routeDeliveryIndex = routeStopsIndex.IndexOf(pickupDelivery[1]);
                                if (routeStopsIndex.Contains(pickupDelivery[0]) && routeStopsIndex.Contains(pickupDelivery[1]) &&  routePickupIndex <= routeDeliveryIndex)
                                {
                                    
                                    var customerRideTime = auxiliaryTimeWindows[routeDeliveryIndex][1] - auxiliaryTimeWindows[routePickupIndex][0];//customer ride time = tw[deliveryIndex][1] - tw[pickupIndex][0]
                                    var customer = _routingSolver.DataModel.IndexManager.GetCustomer(customerIndex);
                                    if (!customerRideTimes.ContainsKey(customer))
                                    {
                                        customerRideTimes.Add(customer, (int)customerRideTime);
                                        //Console.WriteLine(customer.ToString()+"ride time:"+customerRideTime);
                                    }

                                    var customerDelayTime = auxiliaryTimeWindows[routeDeliveryIndex][0] - customer.DesiredTimeWindow[1];
                                    if (!customerDelayTimes.ContainsKey(customer))
                                    {
                                        customerDelayTimes.Add(customer,(int) customerDelayTime);
                                    }
                                 
                                }
                            }
                        }
                        if (capacityDim.TransitVar(index) == 1)
                        {
                            var customerIndex = Array.FindIndex(_routingSolver.DataModel.PickupsDeliveries, pd => pd[0] == nodeIndex);
                            if (customerIndex != -1)
                            {
                                var customer = _routingSolver.DataModel.IndexManager.GetCustomer(customerIndex);
                                var waitTime = routeTimeWindows[routeTimeWindows.Count - 1][0] - customer.DesiredTimeWindow[0];
                                if (!customerWaitTimes.ContainsKey(customer))
                                {
                                    customerWaitTimes.Add(customer,(int)waitTime);
                                    //Console.WriteLine(customer.ToString()+" wait time:"+waitTime);
                                }
                                else
                                {
                                    throw new Exception("More than one index for this customer");
                                }

                                
                            }
                        }
                        index = solution.Value(_routingSolver.RoutingModel.NextVar(index)); //increments the iterator
                        previousStop = currentStop;
                    }
                    //timeWindow add
                    nodeIndex = _routingSolver.RoutingIndexManager.IndexToNode(index);
                    var startTimeVar = timeDim.CumulVar(_routingSolver.RoutingModel.Start(i));
                    routeStopsIndex.Add(nodeIndex);
                    var endTimeVar = timeDim.CumulVar(index);
                    timeWindow = new[] { solution.Min(endTimeVar), solution.Max(endTimeVar) };
                    routeTimeWindows.Add(timeWindow);
                    routeLoads[i] = routeLoad; //assigns routeLoad for vehicle i
                    routeDistances[i] = routeDistance; //assigns routeDistance for vehicle i
                    routeTimes[i] = solution.Max(endTimeVar) - solution.Min(startTimeVar); // assigns routeTime for vehicle i (routeTime = EndTime - StartTime)
                    //routeStops add
                    currentStop = _routingSolver.DataModel.IndexManager.GetStop(nodeIndex);
                    routeStops.Add(currentStop); //adds the current stop
                    //adds each customer that will be served by the current vehicle route
                    for (int j = 0; j < _routingSolver.DataModel.PickupsDeliveries.Length; j++)
                    {
                        var pickupDelivery = _routingSolver.DataModel.PickupsDeliveries[j];
                        var customer = _routingSolver.DataModel.IndexManager.GetCustomer(j);
                        if ((routeStopsIndex.Contains(pickupDelivery[0]) && routeStopsIndex.Contains(pickupDelivery[1]) && routeStopsIndex.IndexOf(pickupDelivery[0]) <= routeStopsIndex.IndexOf(pickupDelivery[1]))|| pickupDelivery[0] == -1)
                            //check for precedence constraints or if the current client is already inside the vehicle
                        {
                            if (!routeCustomers.Contains(customer)) //if routeCustomers doesn't contain the customer, adds it
                            {
                                routeCustomers.Add(customer);
                            }
                        }
                    }
                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(_routingSolver.DataModel.IndexManager.GetVehicle(i), tuple); //adds the vehicle index + tuple with the customer and routeStop list
                }
                //debug
                var allRouteCustomers = new List<Customer>();
                foreach (var dict in vehicleStopCustomerTimeWindowsDictionary)
                {
                    foreach (var customer in dict.Value.Item2)
                    {
                        allRouteCustomers.Add(customer);
                    }
                }
                foreach (var customer in allCustomers)
                {
                    if (!allRouteCustomers.Contains(customer))
                    {
                            Console.Write("Not in vehicle :");
                            customer.PrintPickupDelivery();
                    }
                }
                //end of debug
                _routeDistancesInMeters = routeDistances;//assigns the routeDistance value
                _routeTimesInSeconds = routeTimes;//assigns the routeDistance value
                _routeLoads = routeLoads;//assigns the routeDistance value
                _customerRideTimes = customerRideTimes;//assigns customerRideTimes
                _customerDelayTimes = customerDelayTimes;
                _customerWaitTimes = customerWaitTimes;

                if (allRouteCustomers.Count != allCustomers.Count && _routingSolver.DropNodesAllowed == false)
                {
                    //throw new Exception("Routing solution is not serving all the customers");
                }
            }
            _vehicleSolutionDictionary = vehicleStopCustomerTimeWindowsDictionary;

        }

        public Vehicle IndexToVehicle(int index)
        {
            return _vehicleSolutionDictionary.Keys.ElementAt(index);
        }

        public int VehicleToIndex(Vehicle vehicle)
        {
            int index = 0;
            foreach (var vehicleTuple in _vehicleSolutionDictionary)
            {
                if (vehicleTuple.Key == vehicle)//index found, breaks loop
                {
                    break;
                }
                index++;
            }
            return index;
        }

        public long GetVehicleRouteLoad(Vehicle vehicle)
        {
            return _vehicleSolutionDictionary.ContainsKey(vehicle) ? _routeLoads[VehicleToIndex(vehicle)] : 0;
        }

        public long GetVehicleRouteDistance(Vehicle vehicle)
        {
            return _vehicleSolutionDictionary.ContainsKey(vehicle) ? _routeDistancesInMeters[VehicleToIndex(vehicle)] : 0;
        }

        public long GetVehicleRouteTime(Vehicle vehicle)
        {
            return _vehicleSolutionDictionary.ContainsKey(vehicle) ? _routeTimesInSeconds[VehicleToIndex(vehicle)] : 0;
        }
        public List<Stop> GetVehicleStops(Vehicle vehicle)
        {
            return GetTupleData(vehicle) != null ? GetTupleData(vehicle).Item1 : null;
        }
        public List<Customer> GetVehicleCustomers(Vehicle vehicle)
        {
            return GetTupleData(vehicle) != null ? GetTupleData(vehicle).Item2 : null;
        }

        public List<long[]> GetVehicleTimeWindows(Vehicle vehicle)
        {
            return GetTupleData(vehicle) != null ? GetTupleData(vehicle).Item3 : null;
        }

        private Tuple<List<Stop>, List<Customer>, List<long[]>> GetTupleData(Vehicle vehicle)
        {
            Tuple<List<Stop>, List<Customer>, List<long[]>> tupleData;
            if (_vehicleSolutionDictionary.ContainsKey(vehicle))
            {

                _vehicleSolutionDictionary.TryGetValue(vehicle, out tupleData);
                if (tupleData != null)
                {
                    return tupleData; //returns the tupleData
                }
            }
            return null;
        }

        public long[] GetVehicleStopTimeWindow(Vehicle vehicle, Stop stop)
        {
            long[] stopTimeWindow = null;
            var stops = GetVehicleStops(vehicle);
            if (stops != null)
            {
                if (stops.Contains(stop))
                {
                    var stopIndex = stops.FindIndex(s => s == stop); //Gets the stopIndex
                    stopTimeWindow = GetVehicleTimeWindows(vehicle)[stopIndex]; //gets the timewindow for the stop received as the function parameter
                }
            }
            return stopTimeWindow;
        }
        public bool ContainsVehicle(Vehicle vehicle)
        {
            return _vehicleSolutionDictionary.Keys.Contains(vehicle);
        }

        private long GetTotalValue(long[] metricValues)
        {
            long totalValue = 0;
            if (metricValues.Length > 0)
            {
                for (int i = 0; i < metricValues.Length; i++)
                {
                    totalValue += metricValues[i];
                }

            }
            return totalValue;
        }
    }
}
