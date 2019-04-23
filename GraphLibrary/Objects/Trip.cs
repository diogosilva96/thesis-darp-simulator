using System.Collections.Generic;

namespace GraphLibrary.Objects
{
    public class Trip
    {
        public int Id { get; }
        public string Headsign { get; }

        public List<Stop> Stops { get; set; }

        public List<int> StartTimes { get; set; }

        public Trip(int id, string headsign)
        {
            Id = id;
            Headsign = headsign;
            Stops = new List<Stop>();
            StartTimes = new List<int>();
        }

        public override string ToString()
        {
            return "Trip:"+Id + "-"+ Headsign;
        }

        public bool AddStartTime(int startTime)
        {
            if (!StartTimes.Contains(startTime))
            {
                StartTimes.Add(startTime);
                StartTimes.Sort();
                return true;
            }
            return false;
        }
    }
}
