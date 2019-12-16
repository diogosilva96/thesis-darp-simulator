using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics.Mcmc;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;


namespace Simulator.Events
{
    public class EventGenerator
    {
        private static EventGenerator _instance; //Singleton pattern

        private EventFactory _eventFactory;


        //Lock syncronization object for multithreading (might not be needed)
        private static object syncLock = new object();

        public static EventGenerator Instance() //Singleton
        {
            // Support multithreaded apps through Double checked locking pattern which (once the instance exists) avoids locking each time the method is invoked

            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new EventGenerator();
                    }
                }
            }
            return _instance;
        }
        public EventGenerator()
        {
            _eventFactory= new EventFactory();
        }


        public List<Event> GenerateCustomerLeaveVehicleEvents(Vehicle vehicle, Stop stop, int time)
        {

            List<Event> events = new List<Event>();

            var customersToLeaveVehicleAtCurrentStop = vehicle.Customers.FindAll(c => c.PickupDelivery[1] == stop);
            var leaveTime = 0;

            if (customersToLeaveVehicleAtCurrentStop.Count > 0) //assigns the dropoffstop event
            {
                int ind = 0;
                foreach (var customer in customersToLeaveVehicleAtCurrentStop)
                {
                    ind++;
                    leaveTime = time + ind+1;
                    var customerLeaveVehicleEvent =
                        _eventFactory.CreateEvent(3, leaveTime, vehicle, null, customer);
                    events.Add(customerLeaveVehicleEvent);
                }

            }

            return events;
        }
        
        public Event GenerateCustomerEnterVehicleEvent(Vehicle vehicle, int time, Customer customer)
        {
            Event evt = _eventFactory.CreateEvent(2, time, vehicle, null, customer);
            return evt;
        }
        public List<Event> GenerateCustomersEnterVehicleEvents(Vehicle vehicle, Stop stop, int time, int expectedDemand)
        {
            List<Event> events = new List<Event>();
            if (vehicle.TripIterator.Current != null)
            { 
                var t = TimeSpan.FromSeconds(time);
                var sample = expectedDemand;
                int currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;

                if (sample > 0 && currentStopIndex < vehicle.TripIterator.Current.StopsIterator.TotalStops - 1 && vehicle.CurrentStop == stop) // generation of customers at each stop
                {
                    var enterTime = time;
                    for (int i = 1; i <= sample; i++)
                    {
                        var rng = RandomNumberGenerator.Random;
                        int dropOffStopIndex = rng.Next(currentStopIndex + 1,
                            vehicle.TripIterator.Current.StopsIterator.TotalStops - 1);
                        Stop dropOffStop = vehicle.TripIterator.Current.Stops[dropOffStopIndex];
                        Customer customer = new Customer(new Stop[] { stop, dropOffStop},time);
                        if (!vehicle.IsFull)
                        { 
                            enterTime+=2;
                        }
                        var customerEnterVehicleEvent =
                            GenerateCustomerEnterVehicleEvent(vehicle, enterTime, customer);
                        events.Add(customerEnterVehicleEvent);
                    }
                }
            }
            return events;
        }

        public Event GenerateCustomerRequestEvent(int time, Customer customer)
        {     
                Event evt = _eventFactory.CreateEvent(4, time, null, null, customer);
                return evt;
        }


        public Event GenerateVehicleArriveEvent(Vehicle vehicle, int time)
        {
            Event evt = null;
            if (vehicle.TripIterator.Current != null)
            {
                var stop = vehicle.CurrentStop;
                evt = _eventFactory.CreateEvent(0,time,vehicle,stop,null);
            }
            return evt;
        }

        public Event GenerateVehicleDepartEvent(Vehicle vehicle, int time)
        {
            if (vehicle.TripIterator.Current != null && vehicle.TripIterator.Current.StopsIterator.IsDone)
            {
                return null;
            }
            else
            {
                if (vehicle.TripIterator.Current != null)
                {
                    var stop = vehicle.CurrentStop;
                    var evtDepart = _eventFactory.CreateEvent(1, time, vehicle, stop, null);
                    return evtDepart;
                }

                return null;
            }
        }


    }
}
