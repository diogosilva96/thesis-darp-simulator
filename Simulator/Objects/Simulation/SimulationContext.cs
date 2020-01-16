using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Simulation
{
    public class SimulationContext
    {
        public List<Vehicle> VehicleFleet;

        public SimulationContext()
        {
            VehicleFleet = new List<Vehicle>();
            var transportationNetworkDataLoader = new TransportationNetworkDataLoader(true);
            Routes = transportationNetworkDataLoader.Routes;
            Stops = transportationNetworkDataLoader.Stops;
            DemandsDataObject = transportationNetworkDataLoader.DemandsDataObject;
        }


        public List<Route> Routes;

        public List<Stop> Stops;
        public Stop Depot => Stops.Find(s => s.Id == 2183);

        public  Dictionary<Tuple<Stop, Stop>, double> ArcDictionary
        {
            get
            {
                if (_arcDictionary == null)
                {
                    _arcDictionary = new Dictionary<Tuple<Stop, Stop>, double>();
                    foreach (var stopSource in Stops)
                    {
                        foreach (var stopDestination in Stops)
                        {
                            var distance = Calculator.CalculateHaversineDistance(stopSource.Latitude,
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

        private  Dictionary<Tuple<Stop, Stop>, double> _arcDictionary;
        public DemandsDataObject DemandsDataObject;
    }
}
