using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.Logger;

namespace Simulator.Objects
{
    public class Vehicle
    {
        private static int _nextId;
        public int Id { get; internal set; }
        public int Speed { get; internal set; } // vehicle speed in km/h
        public int Capacity { get; internal set; }

        public string State
        {
            get { return "[Vehicle " + Id + ", Seats:" + Customers.Count + "/" + Capacity + "] "; }
        }

        public int TotalRequests { get; internal set; }
        public int TotalServicedRequests => ServicedCustomers.Count;

        public int TotalDeniedRequests => (TotalRequests - TotalServicedRequests);

        public DirectedGraph<Stop,double> StopsGraph { get; set; }

        private readonly Logger.Logger _consoleLogger;
        public bool IsFull => Customers.Count >= Capacity;

        private double _totalDistanceTraveled;

        public Router Router { get; set; }

        public List<Customer> Customers { get; internal set; }

        public List<Customer> ServicedCustomers { get; internal set; }

        public Vehicle(int speed,int capacity)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            Customers = new List<Customer>(Capacity);
            Router = new Router();
            TotalRequests = 0;
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
            ServicedCustomers = new List<Customer>();
        }

        public double TravelTime(double distance)
        {
            var vehicleSpeed = Speed / 3.6; //vehicle speed in m/s
            var timeToTravel = distance / vehicleSpeed; // time it takes for the vehicle to transverse the distance
            return timeToTravel;
        }

        public List<Customer> GetCustomersToLeaveVehicle(Stop dropOffStop)
        {
            List<Customer> customers = new List<Customer>();
            foreach (var customer in Customers)
            {
                if (customer.PickupDelivery[1] == dropOffStop && !customers.Contains(customer))
                {
                    customers.Add(customer);
                }
            }
            return customers;
        }
        public bool AddCustomer(Customer customer)
        {
            if (customer == null)
            {
                throw new ArgumentNullException();
            }

            if (!IsFull)
            {
                if (Customers.Contains(customer)) return false;
                Customers.Add(customer);
                TotalRequests++;
                return true;
            }
            TotalRequests++;  
            return false;
        }

        public bool Arrive(Stop stop, int time)
        {
           
            if (Router.CurrentStop == stop)
            {
                TimeSpan t = TimeSpan.FromSeconds(time);
                if (Router.CurrentStop == Router.CurrentTrip.Stops[0])
                {
                    _consoleLogger.Log(this.ToString() + "CurrentTrip " + this.Router.CurrentTrip.Id + " started at "+t.ToString()+".");
                    this.Router.StartEndTimeWindow[0] = time;
                }
                _consoleLogger.Log(this.ToString() + "ARRIVED at " + stop+" at "+t.ToString()+".");
                if (Router.NextStop == null)
                {
                    _consoleLogger.Log(this.ToString()+"CurrentTrip " + this.Router.CurrentTrip.Id + " finished at "+t.ToString()+".");
                    this.Router.StartEndTimeWindow[1] = time;

                }
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return "[Vehicle "+Id+"] ";
        }

        public bool Depart(Stop stop, int time)
        {
            if (Router.CurrentStop == stop)
            {
                TimeSpan t = TimeSpan.FromSeconds(time);
                _consoleLogger.Log(this.ToString() +"DEPARTED from "+ stop+"at "+t.ToString()+".");  
                TransverseToNextStop(StopsGraph.GetWeight(Router.CurrentStop, Router.NextStop),time);

                return true;

            }

            return false;
        }
        public bool RemoveCustomer(Customer customer)
        {
            if (customer == null)
            {
                throw new ArgumentNullException();
            }

            if (Customers.Contains(customer))
            { 
                Customers.Remove(customer);
                ServicedCustomers.Add(customer);
                return true;
            }
            return false;
        }

        public bool TransverseToNextStop(double distance, int startTime)
        {
            if (Router != null && Router.NextStop != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(startTime);
                var timeToTravel = TravelTime(distance);
                _totalDistanceTraveled = _totalDistanceTraveled + distance;
                var watch = Stopwatch.StartNew();
                _consoleLogger.Log(this.ToString() + "started traveling to "+Router.NextStop+" at "+t.ToString()+".");
                //using (new MinimumSeconds(timeToTravel))
                //{
                //    Console.WriteLine(_classDescriptor + "Traveling from stop " + Router.CurrentStop + " to stop " + Router.NextStop +
                //                      " distance:" + distance);
                //}

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Router.GoToNextStop();
                return true;
            } 
            else
            {
                return false;
            }

        }

    }
}
