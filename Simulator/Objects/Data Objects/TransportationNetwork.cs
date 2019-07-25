using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class TransportationNetwork//Class that contains all data for the transportation network such as arc distances, all the routes, all the stops, the demands for each stop
    {
        public List<Route> Routes;

        public List<Stop> Stops;

        public Dictionary<Tuple<Stop, Stop>, double> ArcDictionary; //Dictionary with tuples of stops and its respective distance

        public DemandsDataObject DemandsDataObject;

        public TransportationNetwork()
        {
            var routesDataObject = new RoutesDataObject(true);
            Routes = routesDataObject.Routes;
            Stops = routesDataObject.Stops;
            DemandsDataObject = routesDataObject.DemandsDataObject;
            ArcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
            LoadArcDictionary();
        }

        private void LoadArcDictionary()
        {
            ArcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
            HaversineDistanceCalculator haversineDistanceCalculator = new HaversineDistanceCalculator();
            foreach (var stopO in Stops)
            {
                foreach (var stopD in Stops)
                {
                    var distance = haversineDistanceCalculator.Calculate(stopO.Latitude, stopO.Longitude,
                        stopD.Latitude, stopD.Longitude);
                    var tuple = Tuple.Create(stopO, stopD);
                    if (!ArcDictionary.ContainsKey(tuple))
                    {
                        ArcDictionary.Add(tuple,distance);
                    }
                }
            }
        }
        
    }
}
