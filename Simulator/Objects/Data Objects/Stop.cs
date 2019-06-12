using System;
using System.Collections.Generic;
using System.Data;

namespace Simulator.Objects.Data_Objects
{
    public class Stop
    {
        public int Id { get; }
        public string Code { get;}
        public string Name { get;}
        public double Latitude { get;}
        public double Longitude { get;}
        public bool IsUrban { get; set; }

        private Dictionary<Tuple<Route,int>, int> demands { get; }
        public Stop(int id, string code,string name, double lat, double lon)
        {
            Id = id;
            Code = code;
            Name = name;
            Latitude = lat;
            Longitude = lon;
            IsUrban = false;
            demands= new Dictionary<Tuple<Route, int>, int>();
        }

        public override string ToString()
        {
            return "Stop:"+Id+" ";
        }

        public bool AddToDemands(Route route, int hour, int demand)
        {
            if (route != null)
            {
                Tuple<Route, int> routeHourTuple = Tuple.Create(route, hour);
                if (!demands.ContainsKey(routeHourTuple))
                {
                    demands.Add(routeHourTuple, demand);
                    return true;
                }
            }
            return false;
        }

        public int GetDemand(Route route, int hour)
        {
            int demand = 0;
            if (route != null)
            {
                Tuple<Route, int> routeHourTuple = Tuple.Create(route, hour);
                demands.TryGetValue(routeHourTuple, out demand);
            }

            return demand;
        }

    }
}
