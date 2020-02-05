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
            ArcDistanceDictionary = transportationNetworkDataLoader.ArcDistanceDictionary;
            DynamicCustomers = new List<Customer>();
        }

        public Dictionary<Tuple<Stop, Stop>,double> ArcDistanceDictionary;
        public SimulationContext(TransportationNetworkDataLoader transportationNetworkDataLoader)
        {
            VehicleFleet = new List<Vehicle>();
            Routes = transportationNetworkDataLoader.Routes;
            Stops = transportationNetworkDataLoader.Stops;
            DemandsDataObject = transportationNetworkDataLoader.DemandsDataObject;
            DynamicCustomers = new List<Customer>();
            ArcDistanceDictionary = transportationNetworkDataLoader.ArcDistanceDictionary;
        }

        public List<Route> Routes;

        public List<Stop> Stops;

        public List<Customer> DynamicCustomers;
        public Stop Depot => Stops.Find(s => s.Id == 2183);
            
        public DemandsDataObject DemandsDataObject;
    }
}
