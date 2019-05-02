using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GraphLibrary.Objects
{
    public class Route
    {
        public int Id { get; }

        public bool UrbanRoute { get; }

        public string DisplayName { get; }

        public string LongName { get; }

        public string Description { get; }

        public List<Trip> Trips { get; set; }

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

        
    }
}
