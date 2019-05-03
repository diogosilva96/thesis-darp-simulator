using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.Objects;
using Simulator.Logger;

namespace Simulator.Objects
{
    public class Service
    {
        public Trip Trip { get; }

        public int StartTime { get; internal set; }

        public int EndTime { get; internal set; }

        public int RouteDuration => EndTime - StartTime;

        public StopsIterator StopsIterator { get; internal set; }

        public bool IsDone { get; set; }

        public int TotalRequests { get; set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public List<Customer> ServicedCustomers { get; internal set; }

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public double TotalDistanceTraveled;

        public Service(Trip trip,int startTime)
        {
            Trip = trip;
            StartTime = startTime;
            IsDone = false;
            StopsIterator = new StopsIterator(Trip.Stops);
            ServicedCustomers = new List<Customer>();
            TotalDistanceTraveled = 0;
        }

        public bool Start(int time)
        {
            if (!IsDone)
            {
                TotalRequests = 0;
                ServicedCustomers = new List<Customer>();
                StartTime = time;
                StopsIterator.Reset();
                return true;
            }
            else
            {
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

            return "Service: " + Trip;
        }
    }
}
