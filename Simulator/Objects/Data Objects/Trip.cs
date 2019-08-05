using System;
using System.Collections.Generic;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects
{
    public class Trip
    {
        public Route Route { get; set; } //route associated to the trip

        public int Id { get; }
        public string Headsign { get; }

        public List<Stop> Stops { get; set; }

        public int StartTime { get; internal set; }

        public int EndTime { get; internal set; }

        public int RouteDuration => EndTime - StartTime;

        public StopsIterator StopsIterator
        {
            get
            {
                if (_stopsIterator == null || _stopsIterator.TotalStops != Stops.Count) //if stopsIterator is not yet initialized or the current stopiterator has a different size than the stops list, reinitialized the StopsIterator
                {
                    _stopsIterator = new StopsIterator(Stops);
                }
                return _stopsIterator;

            }
            internal set { _stopsIterator = value; } }

        private StopsIterator _stopsIterator;
        public bool IsDone { get; set; } // true if the service has already been completed

        public int TotalRequests { get; set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public List<Customer> ServicedCustomers { get; internal set; }

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public double TotalDistanceTraveled;

        public bool HasStarted { get; set; } // true if the service has already started or been completed

        public Trip(int id, string headsign)
        {
            Id = id;
            Headsign = headsign;
            Stops = new List<Stop>();
            Reset();
        }

        public void Reset()
        {
            EndTime = 0;
            IsDone = false;
            ServicedCustomers = new List<Customer>();
            TotalDistanceTraveled = 0;
            HasStarted = false;
        }
        public bool Start(int time)
        {
            if (!IsDone)
            {
                TotalRequests = 0;
                ServicedCustomers = new List<Customer>();
                StartTime = time;
                StopsIterator.Reset();
                HasStarted = true;
                return true;
            }
            else
            {
                HasStarted = false;
                return false;
            }
        }

        public bool Finish(int time)
        {
            if (!IsDone)
            {
                EndTime = time;
                IsDone = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            if (EndTime != 0)
            {
                return Id + "-" + Headsign + " - Start/End time:[" + TimeSpan.FromSeconds(StartTime).ToString() + "," +
                       TimeSpan.FromSeconds(EndTime).ToString()+"]";
            }
            else
            {
                return Id + "-" + Headsign + " - Start time:" + TimeSpan.FromSeconds(StartTime).ToString();
            }
            ;
        }


    }
}