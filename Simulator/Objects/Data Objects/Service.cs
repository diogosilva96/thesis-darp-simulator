using System.Collections.Generic;

namespace Simulator.Objects.Data_Objects
{
    public class Service //Service is basically a trip to be served at a certain start_time
    {
        public int Id { get; }
        public Trip Trip { get; }

        public int StartTime { get; internal set; }

        public int EndTime { get; internal set; }

        public int RouteDuration => EndTime - StartTime;

        public StopsIterator StopsIterator { get; internal set; }

        public bool IsDone { get; set; } // true if the service has already been completed

        public int TotalRequests { get; set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public List<Customer> ServicedCustomers { get; internal set; }

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public double TotalDistanceTraveled;

        public bool HasStarted { get; set; } // true if the service has already started or been completed

        public Service(int id,Trip trip,int startTime)
        {
            Id = id;
            Trip = trip;
            StartTime = startTime;
            IsDone = false;
            StopsIterator = new StopsIterator(Trip.Stops);
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

            return "Service: " + Trip;
        }
    }
}
