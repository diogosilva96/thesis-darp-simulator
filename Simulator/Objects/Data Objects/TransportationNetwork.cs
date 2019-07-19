using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class TransportationNetwork
    {
        public List<Route> Routes;

        public List<Stop> Stops;

        public Dictionary<Tuple<Stop, Stop>, double> ArcDictionary; //Dictionary with tuples of stops and its respective distance

        public DemandsDataObject DemandsDataObject;
        public TransportationNetwork( List<Stop> stops, List<Route> routes, DemandsDataObject demandsDataObject)
        {
            Routes = routes;
            Stops = stops;
            DemandsDataObject = demandsDataObject;
            ArcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
            LoadArcDictionary();
        }

        private void LoadArcDictionary()
        {
            ArcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
            HaversineDistanceCalculator haversineDistanceCalculator = new HaversineDistanceCalculator();
            foreach (var r in Routes)
            foreach (var t in r.Trips)
            {
                var i = 0;
                foreach (var stop in t.Stops)
                {
                    if (i < t.Stops.Count - 1)
                    {
                        var stopOrigin = stop;
                        var stopDestination = t.Stops[i + 1];
                        if (stopOrigin != stopDestination && stopDestination != null && stopOrigin != null)
                        {
                            var distance = haversineDistanceCalculator.Calculate(stopOrigin.Latitude, stopOrigin.Longitude,
                                stopDestination.Latitude, stopDestination.Longitude);
                            var tuple = Tuple.Create(stopOrigin, stopDestination);
                            if (!ArcDictionary.ContainsKey(tuple))
                            {
                                ArcDictionary.Add(tuple, distance);
                            }
                        }
                    }

                    i++;
                }
            }
        }
        
    }
}
