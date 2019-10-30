using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public static class TransportationNetwork//Class that contains all data for the transportation network such as arc distances, all the routes, all the stops, the demands for each stop
    {
        private static SimulationDataLoader _simulationDataLoader = new SimulationDataLoader(true);
        public static List<Route> Routes => _simulationDataLoader.Routes;

        public static List<Stop> Stops => _simulationDataLoader.Stops;

        public static Dictionary<Tuple<Stop, Stop>, double> ArcDictionary
        {
            get {
                if (_arcDictionary == null)
                {
                    _arcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
                    foreach (var stopSource in Stops)
                    {
                        foreach (var stopDestination in Stops)
                        {
                            var distance = DistanceCalculator.CalculateHaversineDistance(stopSource.Latitude,
                                stopSource.Longitude,
                                stopDestination.Latitude, stopDestination.Longitude);
                            var tuple = Tuple.Create(stopSource, stopDestination);
                            if (!_arcDictionary.ContainsKey(tuple))
                            {
                                _arcDictionary.Add(tuple, distance);
                            }
                        }
                    }
                }

                return _arcDictionary;
            }
        } //Dictionary with tuples of stops and its respective distances

        private static Dictionary<Tuple<Stop, Stop>, double> _arcDictionary;
        public static DemandsDataObject DemandsDataObject => _simulationDataLoader.DemandsDataObject;

    }
}
