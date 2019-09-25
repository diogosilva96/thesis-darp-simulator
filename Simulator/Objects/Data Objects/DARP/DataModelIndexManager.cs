using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class DataModelIndexManager
    {
        private List<Stop> _stops;
        private List<Vehicle> _vehicles;

        public DataModelIndexManager(List<Stop> stops, List<Vehicle> vehicles)
        {
            _stops = stops;
            _vehicles = vehicles;
        }

        public int GetVehicleIndex(Vehicle vehicle)
        {
            return _vehicles.FindIndex(v => v == vehicle);      
        }

        public Vehicle GetVehicle(int index)
        {
            if (index >= _vehicles.Count)
            {
                return null;
            }
            else
            {
                return _vehicles[index];
            }
        }

        public Stop GetStop(int index)
        {
            if (index >= _stops.Count)
            {
                //index = 0; //the depot
                return null;
            }
            return _stops[index];
        }
        public int GetStopIndex(Stop stop)
        {
            return _stops.FindIndex(s => s == stop);
        }
    }
}
