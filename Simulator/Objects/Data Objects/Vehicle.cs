using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Simulator.Objects.Data_Objects
{
    public class Vehicle
    {
        private static int _nextId;

        public IEnumerator<Service> ServiceIterator;

        public Vehicle(int speed, int capacity, Dictionary<Tuple<Stop, Stop>, double> arcDictionary)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            Customers = new List<Customer>(Capacity);
            Services = new List<Service>();
            ArcDictionary = arcDictionary;
        }

        public int Id { get; internal set; }
        public int Speed { get; internal set; } // vehicle speed in km/h
        public int Capacity { get; internal set; }

        public string SeatsState => "[Vehicle " + Id + ", Seats:" + Customers.Count + "/" + Capacity + "] ";

        public Dictionary<Tuple<Stop,Stop>,double> ArcDictionary { get; internal set; }

        public bool IsFull => Customers.Count >= Capacity;

        public List<Service> Services { get; internal set; }

        public List<Customer> Customers { get; internal set; }


        public double TravelTime(double distance)
        {
            var vehicleSpeed = Speed / 3.6; //vehicle speed in m/s
            var timeToTravel =
                distance /
                vehicleSpeed; // time it takes for the vehicle to travel the distance = distance(m)/vehicleSpeed(m/s)
            return timeToTravel;
        }

        public bool AddCustomer(Customer customer)
        {
            if (customer == null) throw new ArgumentNullException();

            ServiceIterator.Current.TotalRequests++;
            if (IsFull) return false;
            if (Customers.Contains(customer)) return false;
            Customers.Add(customer);
                
            return true;
        }

        public bool AddService(Service service)
        {
            if (!Services.Contains(service))
            {
                Services.Add(service);
                Services = Services.OrderBy(s => s.StartTime).ToList(); //Orders services by service start_time
                ServiceIterator = Services.GetEnumerator();
                return true;
            }

            return false;
        }

        public bool Arrive(Stop stop, int time)
        {
            if (ServiceIterator.Current.StopsIterator.CurrentStop == stop)
            {
                if (ServiceIterator.Current.StopsIterator.CurrentIndex == 0)
                {
                    ServiceIterator.Current.Start(time);
                    Console.WriteLine(" ");
                    Console.WriteLine(ToString() + ServiceIterator.Current + " STARTED at " +
                                      TimeSpan.FromSeconds(time) + ".");
                    
                }

                Console.WriteLine(ToString() + "ARRIVED at " + stop + " at " + TimeSpan.FromSeconds(time) + ".");
                if (ServiceIterator.Current.StopsIterator.IsDone && Customers.Count == 0
                ) //this means that the service is complete
                {
                    ServiceIterator.Current.Finish(time); //Finishes the service
                    Console.WriteLine(ToString() + ServiceIterator.Current + " FINISHED at " +
                                      TimeSpan.FromSeconds(time) + ", Duration:" +
                                      Math.Round(TimeSpan.FromSeconds(ServiceIterator.Current.RouteDuration)
                                          .TotalMinutes) + " minutes.");
                    ServiceIterator.MoveNext();
                    if (ServiceIterator.Current == null)
                    {
                        ServiceIterator.Reset();
                        ServiceIterator.MoveNext();
                    }
                }

                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return "[Vehicle " + Id + "] ";
        }

        public bool Depart(Stop stop, int time)
        {
            if (ServiceIterator.Current.StopsIterator.CurrentStop == stop)
            {
                Console.WriteLine(ToString() + "DEPARTED from " + stop + "at " + TimeSpan.FromSeconds(time) + ".");
                var tuple = Tuple.Create(ServiceIterator.Current.StopsIterator.CurrentStop,
                    ServiceIterator.Current.StopsIterator.NextStop);
                ArcDictionary.TryGetValue(tuple, out var distance);
                TransverseToNextStop(distance, time);
                return true;
            }

            return false;
        }

        public bool RemoveCustomer(Customer customer)
        {
            if (customer == null) throw new ArgumentNullException();

            if (!Customers.Contains(customer)) return false;
            Customers.Remove(customer);
            ServiceIterator.Current.ServicedCustomers.Add(customer);
            return true;

        }

        public bool TransverseToNextStop(double distance, int startTime)
        {
            if (ServiceIterator.Current.StopsIterator != null && !ServiceIterator.Current.StopsIterator.IsDone)
            {
                var t = TimeSpan.FromSeconds(startTime);
                ServiceIterator.Current.TotalDistanceTraveled =
                    ServiceIterator.Current.TotalDistanceTraveled + distance;
                Console.WriteLine(ToString() + "started traveling to " +
                                  ServiceIterator.Current.StopsIterator.NextStop + " at " + t + ".");
                ServiceIterator.Current.StopsIterator.Next();
                return true;
            }

            return false;
        }
    }
}