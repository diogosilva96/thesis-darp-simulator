using System.Collections.Generic;
using System.Linq;

namespace Simulator.Objects.Data_Objects
{
    public class Route //Contains a set of trips (with different start_times and different stop sequences (routes))
    {
        public int Id { get; }

        public bool UrbanRoute { get; }

        public string DisplayName { get; }

        public string LongName { get; }

        public string Description { get; }

        public List<Trip> Trips { get; set; }

        public List<Service> AllRouteServices { get; private set; } //All the services of the route

        public Route(int id, string displayName, string longName, string description, int type)
        {
            Id = id;
            DisplayName = displayName;
            LongName = longName;
            Description = description;
            UrbanRoute = type != 5;
            Trips = new List<Trip>();
        }

        public override string ToString()
        {
            return "Route: "+DisplayName + " - "+LongName;
        }

        public void LoadRouteServices()
        {
            AllRouteServices = new List<Service>();
            foreach (var trip in Trips)
            {
                foreach (var startTime in trip.StartTimes) //Generates a service for each trip and each start_time
                {
                    var service = new Service(trip, startTime);
                    AllRouteServices.Add(service);
                }
            }
            AllRouteServices = AllRouteServices.OrderBy(s => s.StartTime).ToList(); //Orders services by start_time
        }
    }
}
