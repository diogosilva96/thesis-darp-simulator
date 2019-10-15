using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class DarpSolutionObject //pickup delivery with time windows solution object, contains the data to be used in the simulation such as the vehicles, stops and timeWindows
    {

        private readonly Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;

        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalLoad => GetTotalValue(_routeLoads);

        public long TotalDistanceInMeters => GetTotalValue(_routeDistancesInMeters);

        public long TotalTimeInSeconds => GetTotalValue(_routeTimesInSeconds);

        private readonly long[] _routeLoads; 

        private readonly long[] _routeDistancesInMeters;

        private readonly long[] _routeTimesInSeconds;


        public int CustomerNumber
        {
            get
            {
                var customerNumber = 0;
                if (_vehicleSolutionDictionary != null)
                {
                    
                    foreach (var vehicleTuples in _vehicleSolutionDictionary)
                    {
                        customerNumber += vehicleTuples.Value.Item2.Count;
                    }
                }
                return customerNumber;
            }
        }


        public DarpSolutionObject(Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> solutionDictionary,Dictionary<string,long[]> solutionMetricsDictionary)
        {
            _vehicleSolutionDictionary = solutionDictionary;
            solutionMetricsDictionary.TryGetValue("routeDistances", out long[] routeDistance);
            _routeDistancesInMeters = routeDistance;
            solutionMetricsDictionary.TryGetValue("routeTimes", out long[] routeTime);
            _routeTimesInSeconds = routeTime;
            solutionMetricsDictionary.TryGetValue("routeLoads", out long[] routeLoad);
            _routeLoads = routeLoad;

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
                foreach (var value in metricValues)
                {
                    totalValue += value;
                }
            }
            return totalValue;
        }
    }
}
