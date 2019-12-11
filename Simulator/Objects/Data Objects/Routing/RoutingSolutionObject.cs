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
    public class RoutingSolutionObject //pickup delivery with time windows solution object, contains the data to be used in the simulation such as the vehicles, stops and timeWindows
    {

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;

        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalCustomers => GetTotalValue(_routeLoads);

        private readonly RoutingSolver _routingSolver;

        private readonly Assignment _solution;

        public Dictionary<string, int> MetricsDictionary;

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

        private long[] _routeLoads; 

        private long[] _routeDistancesInMeters;

        private long[] _routeTimesInSeconds;

        public int TotalCustomerRideTimesInSeconds
        {
            get
            {
                var totalRideTime = 0;
                if (_customerRideTimes != null)
                {
                    
                    foreach (var customer in _customerRideTimes)
                    {
                        totalRideTime += customer.Value;
                    }
                }

                return totalRideTime;
            }
        }

        private Dictionary<Customer, int> _customerRideTimes;

        private Dictionary<Customer,int> _customerDelayTimes;

        public int TotalCustomerDelayTimeInSeconds
        {
            get
            {
                var totalDelayTime = 0;
                foreach (var customer in _customerDelayTimes)
                {
                    totalDelayTime += customer.Value;
                }

                return totalDelayTime;
            }
        }

        public int TotalCustomersEarlier
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
                    if (vehicleStops.Count > 2 && vehicleStops[0] != vehicleStops[1]) //this check means that the vehicle is used because there are 2 more than 2 stops
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

        public int AvgCustomerRideTime => TotalCustomerRideTimesInSeconds / CustomerNumber;

        private int _customerNumber = -1;


        public RoutingSolutionObject(RoutingSolver routingSolver, Assignment solution)
        {
            _routingSolver = routingSolver;
            _solution = solution;
            ComputeSolutionData(_solution);
            //SolutionToVehicleRouteMetrics(_solution);
            MetricsDictionary = new Dictionary<string, int>();
            ComputeAllMetrics();
        }

        public void AddSolutionMetrics(string nameOfMetric,int value)
        {
            if (!MetricsDictionary.ContainsKey(nameOfMetric))
            {
                MetricsDictionary.Add(nameOfMetric,value);
            }
        }

        public void ComputeAllMetrics()
        {
            AddSolutionMetrics(nameof(TotalCustomers), (int)TotalCustomers);
            AddSolutionMetrics(nameof(TotalCustomersEarlier), TotalCustomersEarlier);
            AddSolutionMetrics(nameof(TotalCustomersDelayed), TotalCustomersDelayed);
            AddSolutionMetrics(nameof(TotalVehiclesUsed), TotalVehiclesUsed);
            AddSolutionMetrics(nameof(ObjectiveValue),(int)ObjectiveValue);
            AddSolutionMetrics(nameof(TotalDistanceInMeters),(int)TotalDistanceInMeters);
            AddSolutionMetrics(nameof(TotalCustomerDelayTimeInSeconds),TotalCustomerDelayTimeInSeconds);
            AddSolutionMetrics(nameof(TotalCustomerRideTimesInSeconds),TotalCustomerRideTimesInSeconds);
            AddSolutionMetrics(nameof(TotalStops),(int)TotalStops);
            AddSolutionMetrics(nameof(TotalTimeInSeconds),(int)TotalTimeInSeconds);
            AddSolutionMetrics(nameof(AvgCustomerRideTime),AvgCustomerRideTime);


        }
        private void ComputeSolutionData(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            Dictionary<Customer, int> customerRideTimes = new Dictionary<Customer, int>();
            Dictionary<Customer,int> customerDelayTimes = new Dictionary<Customer, int>();

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
                        double timeToTravel = solution.Value(timeDim.TransitVar(index));
                        var distance = DistanceCalculator.TravelTimeToDistance((int)timeToTravel, _routingSolver.DataModel.IndexManager.Vehicles[i].Speed);
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
                                    }

                                    var customerDelayTime = auxiliaryTimeWindows[routeDeliveryIndex][0] - customer.DesiredTimeWindow[1];
                                    if (!customerDelayTimes.ContainsKey(customer))
                                    {
                                        customerDelayTimes.Add(customer,(int) customerDelayTime);
                                    }
                                 
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
                    vehicleStopCustomerTimeWindowsDictionary.Add(_routingSolver.DataModel.IndexManager.GetVehicle(i), tuple); //adds the vehicle index + tuple with the customer and routestop list
                    
                   
                }
                //debug
                var allrouteCustomers = new List<Customer>();
                foreach (var dict in vehicleStopCustomerTimeWindowsDictionary)
                {
                    foreach (var customer in dict.Value.Item2)
                    {
                        allrouteCustomers.Add(customer);
                    }
                }

                foreach (var customer in allCustomers)
                {
                    if (!allrouteCustomers.Contains(customer))
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

                if (allrouteCustomers.Count != allCustomers.Count && _routingSolver.DropNodesAllowed == false)
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
