using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.Objects;

namespace Simulator.Objects
{
    public class Service
    {
        public Trip Trip { get; }

        public int StartTime { get; internal set; }

        public int EndTime { get; internal set; }

        public StopsIterator StopsIterator { get; internal set; }


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
            StopsIterator = new StopsIterator(Trip.Stops);
            ServicedCustomers = new List<Customer>();
        }

        public bool Start(int time)
        {
            if (!HasBeenServiced)
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

        public bool End(int time)
        {
            if (!HasBeenServiced)
            {
                EndTime = time;
                HasBeenServiced = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {

            string baseString = "Service: " + Trip + " - Start_Time:" + TimeSpan.FromSeconds(StartTime).ToString();
            if (EndTime == 0)
            {
                return baseString;
            }
            else
            {
                return baseString + " - End_Time:" + TimeSpan.FromSeconds(EndTime).ToString();
            }
        }
    }
}
