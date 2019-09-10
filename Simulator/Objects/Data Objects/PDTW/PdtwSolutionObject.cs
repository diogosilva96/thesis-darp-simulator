using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.PDTW
{
    public class PdtwSolutionObject //pickup delivery with time windows solution object, contains the data to be used in the simulation such as the vehicles, stops and timeWindows
    {

        private readonly Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> _vehicleSolutionDictionary;

        public int VehicleNumber => _vehicleSolutionDictionary.Count;

        public long TotalLoad;

        public long TotalDistanceInMeters;

        public long TotalTimeInSeconds;

        public int CustomerNumber
        {
            get
            {
                var customerNumber = 0;
                if (_vehicleSolutionDictionary != null)
                {
                    
                    foreach (var dictTuple in _vehicleSolutionDictionary)
                    {
                        customerNumber += dictTuple.Value.Item2.Count;
                    }
                }
                return customerNumber;
            }
        }

        public PdtwSolutionObject(Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>, List<long[]>>> solutionDictionary)
        {
            _vehicleSolutionDictionary = solutionDictionary;
        }

        public Vehicle GetVehicle(int index)
        {
            return _vehicleSolutionDictionary.Keys.ElementAt(index);
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
                    var stopIndex = stops.FindIndex(s => s.Id == stop.Id); //Gets the stopIndex
                    stopTimeWindow = GetVehicleTimeWindows(vehicle)[stopIndex]; //gets the timewindow for the stop received as the function parameter
                }
            }
            return stopTimeWindow;
        }
        public bool ContainsVehicle(Vehicle vehicle)
        {
            return _vehicleSolutionDictionary.Keys.Contains(vehicle);
        }

        public void AddMetrics(Dictionary<string, long> metricsDictionary)
        {
            metricsDictionary.TryGetValue("totalDistance", out long totalDistance);
            TotalDistanceInMeters = totalDistance;
            metricsDictionary.TryGetValue("totalTime", out long totalTime);
            TotalTimeInSeconds = totalTime;
            metricsDictionary.TryGetValue("totalLoad", out long totalLoad);
            TotalLoad = totalLoad;
        }
    }
}
