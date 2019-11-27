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

        private readonly Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;

        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalLoad => GetTotalValue(_routeLoads);

        private readonly RoutingSolver _routingSolver;

        private readonly Assignment _solution;

        public long TotalDistanceInMeters => GetTotalValue(_routeDistancesInMeters);

        public long TotalTimeInSeconds => GetTotalValue(_routeTimesInSeconds);

        private long[] _routeLoads; 

        private long[] _routeDistancesInMeters;

        private long[] _routeTimesInSeconds;


        public int TotalVehiclesUsed
        {
            get
            {
                var vehiclesUsed = 0;
                foreach (var vehicle in _vehicleSolutionDictionary.Keys)
                {
                    var vehicleStops = GetVehicleStops(vehicle);
                    if (vehicleStops.Count > 2 && vehicleStops[0] != vehicleStops[1]) //this check means that the vehicle is  used because there are 2 more than 2 stops
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
            _vehicleSolutionDictionary = SolutionToVehicleStopTimeWindowsDictionary(_solution);
            SolutionToVehicleRouteMetrics(_solution);
        }

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> SolutionToVehicleStopTimeWindowsDictionary(Assignment solution)
        {
            Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>
                vehicleStopCustomerTimeWindowsDictionary = null;
            if (solution != null)
            {

                List<Customer> allCustomers = new List<Customer>(_routingSolver.DataModel.IndexManager.Customers);
                vehicleStopCustomerTimeWindowsDictionary = new Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>>();
                var timeDim = _routingSolver.RoutingModel.GetMutableDimension("Time");
                var capacityDim = _routingSolver.RoutingModel.GetMutableDimension("Capacity");
                for (int i = 0; i < _routingSolver.DataModel.IndexManager.Vehicles.Count; ++i)
                {
                    List<Stop> routeStops = new List<Stop>();
                    int nodeIndex = 0;
                    List<Customer> routeCustomers = new List<Customer>();
                    List<long[]> routeTimeWindows = new List<long[]>();
                    long[] timeWindow = null;
                    Stop currentStop = null;
                    var index = _routingSolver.RoutingModel.Start(i);
                    Stop previousStop = null;
                    while (_routingSolver.RoutingModel.IsEnd(index) == false) //while the iterator isn't done
                    {
                        nodeIndex = _routingSolver.RoutingIndexManager.IndexToNode(index);
                        //routeStops add
                        currentStop = _routingSolver.DataModel.IndexManager.GetStop(nodeIndex);
                        var timeVar = timeDim.CumulVar(index);
                        if (currentStop != null && previousStop != null && currentStop.Id == previousStop.Id)
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

                        if (capacityDim.TransitVar(index) == -1)
                        {
                            var customersCurrentStopAsDelivery = allCustomers.FindAll(c => c.PickupDelivery[1] == currentStop);
                            foreach (var customer in customersCurrentStopAsDelivery)
                            {
                                if (routeStops.Contains(customer.PickupDelivery[0]))
                                {
                                    var pickupIndex = routeStops.FindIndex(s=>s == customer.PickupDelivery[0]);
                                    var rideTime = routeTimeWindows[routeStops.FindLastIndex(s => s == currentStop)][1] - routeTimeWindows[pickupIndex][1];
                                    Console.WriteLine(customer.ToString()+" ride time:"+rideTime);//need to fix this
                                }
                            }

                        }
                        index = solution.Value(_routingSolver.RoutingModel.NextVar(index)); //increments the iterator
                        previousStop = currentStop;
                    }
                    //timeWindow add
                    nodeIndex = _routingSolver.RoutingIndexManager.IndexToNode(index);
                    var endTimeVar = timeDim.CumulVar(index);
                    timeWindow = new[] { solution.Min(endTimeVar), solution.Max(endTimeVar) };
                    routeTimeWindows.Add(timeWindow);

                    //routeStops add
                    currentStop = _routingSolver.DataModel.IndexManager.GetStop(nodeIndex);
                    routeStops.Add(currentStop); //adds the current stop
                    foreach (var customer in allCustomers) //loop to add the customers to the routecustomers
                    {
                        var pickupStop = customer.PickupDelivery[0];
                        var deliveryStop = customer.PickupDelivery[1];
                        if ((!customer.IsInVehicle && routeStops.Contains(pickupStop) && routeStops.Contains(deliveryStop) && routeStops.IndexOf(pickupStop) < routeStops.IndexOf(deliveryStop)) || (customer.IsInVehicle && routeStops.Contains(deliveryStop) && routeStops.IndexOf(deliveryStop) >= 0 && routeStops.IndexOf(pickupStop) == -1
                            )) // For the case were a customer is not inside a vehicle, if the pickup stop and delivery stops are contained in the routeStops and the pickup stop comes before the delivery stop (precedence constraint)
                               //Or for the case were the customer is already inside a vehicle, if that deliveryStop is contained in the route stops and its pickupStop is not in the routeStops
                        {
                            if (!routeCustomers.Contains(customer))
                            {
                                routeCustomers.Add(customer); //if the above checks are confirmed and the customer is not in the list, adds it
                            }
                        }

                    }
                    var tuple = Tuple.Create(routeStops, routeCustomers, routeTimeWindows);
                    vehicleStopCustomerTimeWindowsDictionary.Add(_routingSolver.DataModel.IndexManager.GetVehicle(i),
                        tuple); //adds the vehicle index + tuple with the customer and routestop list
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

                foreach (var cust in allCustomers)
                {
                    Console.WriteLine("Not served Customers: ");
                    if (!allrouteCustomers.Contains(cust))
                    {
                        cust.PrintPickupDelivery();
                    }
                }
                //end of debug
            }

            return vehicleStopCustomerTimeWindowsDictionary;

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
