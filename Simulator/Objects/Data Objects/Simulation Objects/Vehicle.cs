using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Simulator.Objects.Data_Objects.Simulation_Objects
{
    public class Vehicle
    {
        private static int _nextId;

        public IEnumerator<Trip> TripIterator;

        public Vehicle(int speed, int capacity, bool flexibleRouting)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            Customers = new List<Customer>(Capacity);
            ServiceTrips = new List<Trip>();
            FlexibleRouting = flexibleRouting;
            IsIdle = true;
        }

        public bool FlexibleRouting; // true if the vehicle does flexible routing
        public int Id { get; internal set; }
        public int Speed { get; internal set; } // vehicle speed in km/h
        public int Capacity { get; internal set; }

        public string SeatsState => "[Vehicle " + Id + ", Seats:" + Customers.Count + "/" + Capacity + "] ";

        public bool IsFull => Customers.Count >= Capacity;

        public List<Trip> ServiceTrips { get; internal set; }

        public List<Customer> Customers { get; internal set; }//customers inside the vehicle

        public bool IsIdle;

        public bool AddCustomer(Customer customer)
        {
            if (customer == null) throw new ArgumentNullException();

            if (TripIterator.Current != null) TripIterator.Current.TotalRequests++;
            if (IsFull) return false;
            if (Customers.Contains(customer)) return false;
            Customers.Add(customer);
                
            return true;
        }

        public bool AddTrip(Trip trip)
        {
            if (!ServiceTrips.Contains(trip))
            {
                ServiceTrips.Add(trip);
                ServiceTrips = ServiceTrips.OrderBy(s => s.StartTime).ToList(); //Orders services by trip start_time
                TripIterator = ServiceTrips.GetEnumerator();
                return true;
            }

            return false;
        }

        public bool Arrive(Stop stop, int time)
        {
            IsIdle = true;
            if (TripIterator.Current != null && TripIterator.Current.StopsIterator.CurrentStop == stop)
            {
                if (TripIterator.Current.StopsIterator.CurrentIndex == 0)
                {
                    TripIterator.Current.Start(time);
                    Console.WriteLine(" ");
                    Console.WriteLine(ToString() + TripIterator.Current + " STARTED at " +
                                      TimeSpan.FromSeconds(time) + ".");

                }

                Console.WriteLine(ToString() + "ARRIVED at " + stop + " at " + TimeSpan.FromSeconds(time) + ".");
                TripIterator.Current.VisitedStops.Add(stop); //adds the current stop to the visited stops
                TripIterator.Current.StopsTimeWindows.Add(new long[] {time,time}); //adds the current time window
                
                if (TripIterator.Current.StopsIterator.IsDone && Customers.Count == 0
                ) //this means that the trip is complete
                {
                    TripIterator.Current.Finish(time); //Finishes the trip
                    Console.WriteLine(ToString() + TripIterator.Current + " FINISHED at " +
                                      TimeSpan.FromSeconds(time) + ", Duration:" +
                                      Math.Round(TimeSpan.FromSeconds(TripIterator.Current.RouteDuration)
                                          .TotalMinutes) + " minutes.");
                    TripIterator.MoveNext();
                    if (TripIterator.Current == null)
                    {
                        TripIterator.Reset();
                        TripIterator.MoveNext();
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
            if (TripIterator.Current != null && TripIterator.Current.StopsIterator.CurrentStop == stop)
            {
                Console.WriteLine(ToString() + "DEPARTED from " + stop + " at " + TimeSpan.FromSeconds(time) + ".");
                var tuple = Tuple.Create(TripIterator.Current.StopsIterator.CurrentStop,
                    TripIterator.Current.StopsIterator.NextStop);
                var currentStopIndex = TripIterator.Current.StopsIterator.CurrentIndex;
                //TripIterator.Current.StopsTimeWindows[currentStopIndex][1]=time;
                TransportationNetwork.ArcDictionary.TryGetValue(tuple, out var distance);
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
            if (TripIterator.Current == null) return true;
            TripIterator.Current.ServicedCustomers.Add(customer);
            return true;

        }

        public bool TransverseToNextStop(double distance, int startTime)
        {
            if (TripIterator.Current?.StopsIterator != null && !TripIterator.Current.StopsIterator.IsDone)
            {
                IsIdle = false;
                var t = TimeSpan.FromSeconds(startTime);
                TripIterator.Current.TotalDistanceTraveled =
                    TripIterator.Current.TotalDistanceTraveled + distance;
                Console.WriteLine(ToString() + "started traveling to " +
                                  TripIterator.Current.StopsIterator.NextStop + " at " + t + ".");
                
                return true;
            }

            return false;
        }
    }
}