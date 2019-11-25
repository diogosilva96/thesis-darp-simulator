using System.Collections.Generic;

namespace Simulator.Objects.Data_Objects.Simulation_Objects
{
    public class Route //Contains a set of trips (with different start_times and different stop sequences (routes))
    {
        public int Id { get; }

        public bool UrbanRoute { get; }

        public string DisplayName { get; }

        public string LongName { get; }

        public string Name;

        public string Description { get; }

        public List<Trip> Trips { get; set; }

        public Route(int id, string displayName, string longName, string description, int type)
        {
            Id = id;
            DisplayName = displayName;
            LongName = longName;
            Description = description;
            UrbanRoute = type != 5;
            Name = DisplayName + " - " + LongName;
            Trips = new List<Trip>();
        }

        public override string ToString()
        {
            return "Route: " + Name;
        }

    }
}
