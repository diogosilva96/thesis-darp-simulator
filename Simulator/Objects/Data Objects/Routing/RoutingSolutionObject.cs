using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class RoutingSolutionObject //pickup delivery with time windows solution object, contains the data to be used in the simulation such as the vehicles, stops and timeWindows
    {

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;

        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalLoad => GetTotalValue(_routeLoads);

        private readonly RoutingSolver _routingSolver;

        private readonly Assignment _solution;

        public long TotalDistanceInMeters => GetTotalValue(_routeDistancesInMeters);

        public long TotalTimeInSeconds => GetTotalValue(_routeTimesInSeconds);

        private long[] _routeLoads; 

        private long[] _routeDistancesInMeters;

        private long[] _routeTimesInSeconds;

        private Dictionary<Customer, int> CustomerRideTimes;


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
         

        private int _customerNumber = -1;


        public RoutingSolutionObject(RoutingSolver routingSolver, Assignment solution)
        {
            _routingSolver = routingSolver;
            _solution = solution;
            ComputeSolutionData(_solution);
            //SolutionToVehicleRouteMetrics(_solution);
        }

        private void ComputeSolutionData(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            Dictionary<Customer, int> customerRideTimes = new Dictionary<Customer, int>();

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
                        var timeVar = timeDim.CumulVar(index);

                        
                        //calculate routeDistance
                        double timeToTravel = solution.Value(timeDim.TransitVar(index));
                        var distance = DistanceCalculator.TravelTimeToDistance((int)timeToTravel, _routingSolver.DataModel.IndexManager.Vehicles[i].Speed);
                        routeDistance += (long)distance;
                        //add currentLoad
                        routeLoad += solution.Value(capacityDim.TransitVar(index)) > 0 ? solution.Value(capacityDim.TransitVar(index)) : 0; //adds the load if it is greater than 0
                        //auxiliary data structures add
                        auxiliaryTimeWindows.Add(new long[]{solution.Min(timeVar),solution.Max(timeVar)}); //adds current timeWindow
                        routeStopsIndex.Add(nodeIndex); //adds current stopIndex
                        if (currentStop != null && previousStop != null && currentStop.Id == previousStop.Id)//if current stop is the same as the previous one
                        {
                            routeStops.Remove(previousStop); //removes previous stop
                            routeStops.Add(currentStop); //adds current stop
                            var joinedTimeWindow = new[] { timeWindow[0], solution.Max(timeVar) }; //adds the new timewindow the junction of the previous min time from the dummy stop
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
                            timeWindow = new[] { solution.Min(timeVar), solution.Max(timeVar) };
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
                                    
                                    var customerRideTime = auxiliaryTimeWindows[routeDeliveryIndex][1] -auxiliaryTimeWindows[routePickupIndex][0];//customer ride time = tw[deliveryIndex][1] - tw[pickupIndex][0]
                                    var customer = _routingSolver.DataModel.IndexManager.GetCustomer(customerIndex);
                                    if (!customerRideTimes.ContainsKey(customer))
                                    {
                                        customerRideTimes.Add(customer, (int)customerRideTime);
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

                if (allrouteCustomers.Count != allCustomers.Count)
                {
                    throw new Exception("Routing solution is not serving all the customers");
                }
                //end of debug
                _routeDistancesInMeters = routeDistances;//assigns the routeDistance metric
                _routeTimesInSeconds = routeTimes;//assigns the routeDistance metric
                _routeLoads = routeLoads;//assigns the routeDistance metric
                CustomerRideTimes = customerRideTimes;//assigns customerRideTimes
            }
            _vehicleSolutionDictionary = vehicleStopCustomerTimeWindowsDictionary;

        }

        private void SolutionToVehicleRouteMetrics(Assignment solution) //computes the metrics for each vehicle route
        {
            if (_solution != null)
            {

                var timeDim = _routingSolver.RoutingModel.GetMutableDimension("Time");
                var capacityDim = _routingSolver.RoutingModel.GetMutableDimension("Capacity");
                var vehicleNumber = _routingSolver.DataModel.IndexManager.Vehicles.Count;
                //route metrics each index is the vehicle index
                long[] routeTimes = new long[vehicleNumber];
                long[] routeDistances = new long[vehicleNumber];
                long[] routeLoads = new long[vehicleNumber];
                for (int i = 0; i < vehicleNumber; ++i)
                {
                    long routeDistance = 0;
                    var index = _routingSolver.RoutingModel.Start(i);
                    long routeLoad = solution.Value(capacityDim.CumulVar(index)); //initial route load
                    while (_routingSolver.RoutingModel.IsEnd(index) == false)
                    {
                        var timeTransitVar = timeDim.TransitVar(index);
                        var capacityTransitVar = capacityDim.TransitVar(index);
                        index = solution.Value(_routingSolver.RoutingModel.NextVar(index));
                        double timeToTravel = solution.Value(timeTransitVar);
                        var distance = DistanceCalculator.TravelTimeToDistance((int)timeToTravel, _routingSolver.DataModel.IndexManager.Vehicles[i].Speed);
                        routeDistance += (long)distance;
                        routeLoad += solution.Value(capacityTransitVar) > 0 ? solution.Value(capacityTransitVar) : 0;
                    }

                    var endTimeVar = timeDim.CumulVar(index);
                    var startTimeVar = timeDim.CumulVar(_routingSolver.RoutingModel.Start(i));
                    routeLoads[i] = routeLoad;
                    routeDistances[i] = routeDistance;
                    routeTimes[i] = solution.Max(endTimeVar) - solution.Min(startTimeVar);
                }

                _routeDistancesInMeters = routeDistances;//assigns the routeDistance metric
                _routeTimesInSeconds = routeTimes;//assigns the routeDistance metric
                _routeLoads = routeLoads;//assigns the routeDistance metric
            }
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
