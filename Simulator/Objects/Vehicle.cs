using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.Iterator;
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

        public DirectedGraph<Stop,double> StopsGraph { get; set; }

        public IServiceIterator ServiceIterator { get; internal set; }

        public int ServicedServices;

        private readonly Logger.Logger _consoleLogger;
        public bool IsFull => Customers.Count >= Capacity;

        public List<Service> Services { get; internal set; }

        private double _totalDistanceTraveled;

        public List<Customer> Customers { get; internal set; }


        public Vehicle(int speed,int capacity)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            Customers = new List<Customer>(Capacity);
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
            Services = new List<Service>();
            ServicedServices = 0;
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
                ServiceIterator.Current.TotalRequests++;
                return true;
            }

            ServiceIterator.Current.TotalRequests++; 
            return false;
        }
        public bool AddService(Service service)
        {
            if (!Services.Contains(service))
            {
                Services.Add(service);
                Services = Services.OrderBy(s => s.StartTime).ToList(); //Orders services by service starttime
                return true;
            }
            return false;
        }

        public void InitServiceIterator()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            for (int i = 0; i < Services.Count; i++)
            {
                serviceCollection[i] = Services[i];
            }

            ServiceIterator = serviceCollection.CreateIterator();
        }
        public bool Arrive(Stop stop, int time)
        {
           
            if (ServiceIterator.Current.StopsIterator.CurrentStop == stop)
            {
                TimeSpan t = TimeSpan.FromSeconds(time);
                if (ServiceIterator.Current.StopsIterator.CurrentStop ==ServiceIterator.Current.Trip.Stops[0])
                {
                    _consoleLogger.Log(this.ToString() + ServiceIterator.Current + ServiceIterator.Current.Trip.Id + " STARTED at "+t.ToString()+".");
                    ServiceIterator.Current.Start(time);
                }
                _consoleLogger.Log(this.ToString() + "ARRIVED at " + stop+" at "+t.ToString()+".");
                if (ServiceIterator.Current.StopsIterator.NextStop == null)
                {
                    _consoleLogger.Log(this.ToString()+ ServiceIterator.Current + " FINISHED at "+t.ToString()+".");
                    ServicedServices++;
                    ServiceIterator.Current.End(time);
                    ServiceIterator.Next();
                    if (ServiceIterator.IsDone)
                    {
                        ServiceIterator.First();
                    }
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
            if (ServiceIterator.Current.StopsIterator.CurrentStop == stop)
            {
                TimeSpan t = TimeSpan.FromSeconds(time);
                _consoleLogger.Log(this.ToString() +"DEPARTED from "+ stop+"at "+t.ToString()+".");
                TransverseToNextStop(StopsGraph.GetWeight(ServiceIterator.Current.StopsIterator.CurrentStop, ServiceIterator.Current.StopsIterator.NextStop),time);
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
                ServiceIterator.Current.ServicedCustomers.Add(customer);
                return true;
            }
            return false;
        }

        public bool TransverseToNextStop(double distance, int startTime)
        {
            if (ServiceIterator.Current.StopsIterator != null && ServiceIterator.Current.StopsIterator.NextStop != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(startTime);
                var timeToTravel = TravelTime(distance);
                _totalDistanceTraveled = _totalDistanceTraveled + distance;
                _consoleLogger.Log(this.ToString() + "started traveling to "+ServiceIterator.Current.StopsIterator.NextStop+" at "+t.ToString()+".");
                //using (new MinimumSeconds(timeToTravel))
                //{
                //    Console.WriteLine(_classDescriptor + "Traveling from stop " + StopsIterator.CurrentStop + " to stop " + StopsIterator.NextStop +
                //                      " distance:" + distance);
                //}

                ServiceIterator.Current.StopsIterator.Next();
                return true;
            } 
        return false;
            

        }

    }
}
