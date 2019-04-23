using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.Objects;

namespace Simulator.Objects
{
    public class Service
    {
        private Trip Trip { get; }

        public int StartTime { get; set; }

        public int EndTime { get; set; }

        public StopsIterator StopsIterator { get; set; }

        public bool HasBeenServiced { get; set; }

        public int TotalRequests { get; set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public List<Customer> ServicedCustomers { get; internal set; }

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public Service(Trip trip,int startTime)
        {
            Trip = trip;
            StartTime = startTime;
            HasBeenServiced = false;
            StopsIterator = new StopsIterator(Trip);
            ServicedCustomers = new List<Customer>();
        }

    }
}
