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

        public bool IsDummy { get; set; }

        public Stop(int id, string code,string name, double lat, double lon)
        {
            Id = id;
            Code = code;
            Name = name;
            Latitude = lat;
            Longitude = lon;
            IsUrban = false;
            IsDummy = false;
        }

        public override string ToString()
        {
            string toString = "Stop(" + Id + ")";
            if (IsDummy)
            {
                toString = "Dummy " + toString;
            }

            return toString;
        }

    }
}
