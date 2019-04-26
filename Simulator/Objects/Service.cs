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

        private readonly Logger.Logger _consoleLogger;

        public int RouteDuration => EndTime - StartTime;

        public StopsIterator StopsIterator { get; internal set; }

        public bool IsDone { get; set; }

        public int TotalRequests { get; set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public List<Customer> ServicedCustomers { get; internal set; }

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public Service(Trip trip,int startTime)
        {
            Trip = trip;
            StartTime = startTime;
            IsDone = false;
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
            StopsIterator = new StopsIterator(Trip.Stops);
            ServicedCustomers = new List<Customer>();
        }

        public bool Start(int time)
        {
            if (!IsDone)
            {
                _consoleLogger.Log(this.ToString()+ " STARTED at " + TimeSpan.FromSeconds(time).ToString() + ".");
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
            if (!IsDone)
            {
                EndTime = time;
                IsDone = true;
                _consoleLogger.Log("["+this.ToString() + "] FINISHED at " +
                                   TimeSpan.FromSeconds(time).ToString() + ".");
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
