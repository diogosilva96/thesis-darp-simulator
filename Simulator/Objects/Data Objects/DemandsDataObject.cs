using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class DemandsDataObject
    {
        private readonly Dictionary<string, int> _demandsDictionary;
        public DemandsDataObject()
        {
           _demandsDictionary = new Dictionary<string, int>();
        }

        public bool AddDemand(int stopId,int routeId, int hour, int demand)
        {
            if (routeId != 0 && stopId != 0)
            {
                string key = BuildKeyString(stopId, routeId, hour);
                if (!_demandsDictionary.ContainsKey(key))
                {
                    _demandsDictionary.Add(key, demand);
                    return true;
                }
            }
            return false;
        }

        public int GetDemand(int stopId, int routeId, int hour)
        {
            int demand = 0;
            if (stopId != 0 && routeId != 0)
            {
                string lookupKey = BuildKeyString(stopId, routeId, hour);
                _demandsDictionary.TryGetValue(lookupKey, out demand);
            }
            return demand;
        }

        private string BuildKeyString(int stopId, int routeId, int hour)
        {
            string keyString = stopId + "-" + routeId + "-" + hour;
            return keyString;
        }
    }
}
