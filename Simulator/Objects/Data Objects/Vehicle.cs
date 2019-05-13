﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Simulator.GraphLibrary;

namespace Simulator.Objects.Data_Objects
{
    public class Vehicle
    {
        private static int _nextId;
        public int Id { get; internal set; }
        public int Speed { get; internal set; } // vehicle speed in km/h
        public int Capacity { get; internal set; }

        public IEnumerator<Service> ServiceIterator;

        public string SeatsState
        {
            get { return "[Vehicle " + Id + ", Seats:" + Customers.Count + "/" + Capacity + "] "; }
        }

        public DirectedGraph<Stop,double> StopsGraph { get; set; }
       
        public bool IsFull => Customers.Count >= Capacity;

        public List<Service> Services { get; internal set; }

        public List<Customer> Customers { get; internal set; }

        public Vehicle(int speed,int capacity, DirectedGraph<Stop,double> stopsGraph)
        {
            Id = Interlocked.Increment(ref _nextId);
            Speed = speed;
            Capacity = capacity;
            StopsGraph = stopsGraph;
            Customers = new List<Customer>(Capacity);
            Services = new List<Service>();
        }
     

        public double TravelTime(double distance)
        {
            var vehicleSpeed = Speed / 3.6; //vehicle speed in m/s
            var timeToTravel = distance / vehicleSpeed; // time it takes for the vehicle to travel the distance = distance(m)/vehicleSpeed(m/s)
            return timeToTravel;
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
                    Console.WriteLine(this.ToString()+ServiceIterator.Current+" STARTED at " + TimeSpan.FromSeconds(time).ToString() + ".");
                }

                Console.WriteLine(this.ToString() + "ARRIVED at " + stop+" at "+TimeSpan.FromSeconds(time).ToString()+".");
                if (ServiceIterator.Current.StopsIterator.IsDone && Customers.Count == 0) //this means that the service is complete
                {
                    ServiceIterator.Current.Finish(time); //Finishes the service
                    Console.WriteLine(this.ToString() + ServiceIterator.Current + " FINISHED at " +
                                       TimeSpan.FromSeconds(time).ToString() + ", Duration:" + Math.Round(TimeSpan.FromSeconds(ServiceIterator.Current.RouteDuration).TotalMinutes) + " minutes.");
                    ServiceIterator.MoveNext();   
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
                Console.WriteLine(this.ToString() +"DEPARTED from "+ stop+"at "+TimeSpan.FromSeconds(time).ToString()+".");
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
            if (ServiceIterator.Current.StopsIterator != null && !ServiceIterator.Current.StopsIterator.IsDone)
            {
                TimeSpan t = TimeSpan.FromSeconds(startTime);
                ServiceIterator.Current.TotalDistanceTraveled =
                    ServiceIterator.Current.TotalDistanceTraveled + distance;
                Console.WriteLine(this.ToString() + "started traveling to "+ServiceIterator.Current.StopsIterator.NextStop+" at "+t.ToString()+".");
                ServiceIterator.Current.StopsIterator.Next();
                return true;
            } 
        return false;
            
        }

    }
}