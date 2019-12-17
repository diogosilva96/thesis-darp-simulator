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

        public Vehicle(int speed, int capacity)
        {
            Initialize(speed,capacity);
            FlexibleRouting = false;
        }

        public Vehicle(int speed, int capacity,Stop startStop,Stop endStop)
        {
            Initialize(speed,capacity);
            FlexibleRouting = true;
            StartStop = startStop;
            EndStop = endStop;
        }
        public bool FlexibleRouting; // true if the vehicle does flexible routing
        public int Id { get; internal set; }
        public int Speed { get; internal set; } // vehicle speed in km/h
        public int Capacity { get; internal set; }

        public Stop StartStop;

        public Stop EndStop;

        public string SeatsState => "[Vehicle " + Id + ", Seats:" + Customers.Count + "/" + Capacity + "] ";

        public bool IsFull => Customers.Count >= Capacity;

        public List<Trip> ServiceTrips { get; internal set; }

        public List<Customer> Customers { get; internal set; }//customers inside the vehicle

        public bool IsIdle;

        public double TotalDistanceTraveled;
        public List<Stop> VisitedStops { get; set; }

        public List<long[]> StopsTimeWindows { get; set; }

        public List<Customer> ServedCustomers { get; set; }

        public Stop CurrentStop => TripIterator?.Current?.StopsIterator?.CurrentStop;

        public Stop NextStop => TripIterator?.Current?.StopsIterator?.NextStop;

        public int StartTime => StopsTimeWindows != null && StopsTimeWindows.Count >0 ? (int)StopsTimeWindows[0][0]: 0;

        public int EndTime => StopsTimeWindows != null && StopsTimeWindows.Count > 0 ? (int) StopsTimeWindows[StopsTimeWindows.Count - 1][1] : 0;

        public int TotalDeniedRequests => TotalRequests - TotalServedRequests;

        public int TotalServedRequests => ServedCustomers.Count;
        public int RouteDuration => EndTime - StartTime;

        public int TotalRequests { get; set; }

        public void Initialize(int speed, int capacity)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            Customers = new List<Customer>(Capacity);
            ServiceTrips = new List<Trip>();
            VisitedStops = new List<Stop>();
            ServedCustomers = new List<Customer>();
            StopsTimeWindows = new List<long[]>();
            IsIdle = true;
            TotalDistanceTraveled = 0;
            TotalRequests = 0;
        }
        public bool AddCustomer(Customer customer)
        {
            TotalRequests++;
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

        public void PrintRoute() //debug purposes
        {
            Console.WriteLine("Route for "+this.ToString());
            List<Customer> pickupCustomers = new List<Customer>();
            for (int i= 0;i<VisitedStops.Count;i++)
            {
                var currentStop = VisitedStops[i];
                var currentTimeWindow = StopsTimeWindows[i];
                var message = currentStop.ToString() + " T:(" + currentTimeWindow[0] + "," + currentTimeWindow[1] + ") ";
                var customersToEnter = ServedCustomers.FindAll(c => c.PickupDelivery[0] == currentStop);
                var customersToLeave = ServedCustomers.FindAll(c => c.PickupDelivery[1] == currentStop);
                foreach (var customer in customersToEnter)
                {
                    var pickup = customer.PickupDelivery[0];
                    var pickupTime = customer.DesiredTimeWindow[0];
                    var pickupTimeIsRespected = currentTimeWindow[0] >= pickupTime;
                    message += "Enter: " + customer.ToString() + ", Pickup: " + pickup.ToString() + ", PickupTime: " + pickupTime+", PickupTimeIsRespected: "+pickupTimeIsRespected;
                    pickupCustomers.Add(customer);
                }

                foreach (var customer in customersToLeave)
                {
                    var delivery = customer.PickupDelivery[1];
                    var deliveryTime = customer.DesiredTimeWindow[1];
                    var deliveryTimeIsRespected = customer.DelayTime<=0;
                    var precedenceConstraint = pickupCustomers.Contains(customer);
                    message += "Leave: " + customer.ToString() + ", Delivery: " +delivery.ToString() + ", DeliveryTime: " + deliveryTime+", DeliveryTimeIsRespected: "+deliveryTimeIsRespected+", PrecedenceIsRespected: "+precedenceConstraint;

                }

                Console.WriteLine(message);
            }
            Console.WriteLine("Total customers: "+ServedCustomers.Count);
        }


        public override string ToString()
        {
            return "[Vehicle " + Id + "] ";
        }


        public bool RemoveCustomer(Customer customer)
        {
            if (customer == null) throw new ArgumentNullException();

            if (!Customers.Contains(customer)) return false;
            Customers.Remove(customer);
            if (TripIterator.Current == null) return false;
            TripIterator.Current.ServicedCustomers.Add(customer);
            ServedCustomers.Add(customer);
            return true;

        }

    }
}