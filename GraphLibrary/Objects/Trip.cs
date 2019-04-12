using System.Collections.Generic;

namespace GraphLibrary.Objects
{
    public class Trip
    {
        public int Id { get; }
        public string Headsign { get; }

        public List<Stop> Stops { get; set; }


        public Trip(int id, string headsign)
        {
            Id = id;
            Headsign = headsign;
            Stops = new List<Stop>();
        }

        public override string ToString()
        {
            return "Trip:"+Id + "-"+ Headsign;
        }
    }
}
