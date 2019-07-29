using System;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics.Mcmc;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;


namespace Simulator.Events
{
    public class EventGenerator
    {
        private EventFactory _eventFactory;
        private IDistribution _distribution;

        public EventGenerator()
        {
            _eventFactory= new EventFactory();
        }
        public int Lambda
        {
            get => Lambda;
            set { _distribution = new Poisson(value); }
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
        public List<Event> GenerateCustomersEnterVehicleEvents(Vehicle vehicle, Stop stop, int time, int lambda, int expectedDemand)
        {
            List<Event> events = new List<Event>();
            //Lambda = lambda; // remove??

            if (vehicle.TripIterator.Current != null)
            { 
                var t = TimeSpan.FromSeconds(time);
                int currentHour = t.Hours;
                var currentRoute =vehicle.TripIterator.Current.Route;

                var sample = expectedDemand;
                //var sample = ((Poisson)_distribution).Sample();
                //check if distribution will be used or the linear use of the inputs
                
                
                int currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;

                if (sample > 0 && currentStopIndex < vehicle.TripIterator.Current.StopsIterator.TotalStops - 1 && vehicle.TripIterator.Current.StopsIterator.CurrentStop == stop) // generation of customers at each stop
                {
                    var enterTime = time;
                    for (int i = 1; i <= sample; i++)
                    {
                        var rnd = new Random();
                        int dropOffStopIndex = rnd.Next(currentStopIndex + 1,
                            vehicle.TripIterator.Current.StopsIterator.TotalStops - 1);
                        Stop dropOffStop = vehicle.TripIterator.Current.Stops[dropOffStopIndex];
                        Customer customer = new Customer(stop, dropOffStop,time);
                        if (!vehicle.IsFull)
                        { 
                            enterTime++;
                        }

                        var customerEnterVehicleEvent =
                            GenerateCustomerEnterVehicleEvent(vehicle, enterTime, customer);
                        events.Add(customerEnterVehicleEvent);
                    }
                }
            }

            return events;
        }

        public Event GenerateCustomerRequestEvent(int time,Stop pickup, Stop dropoff)
        {
            Random rnd = new Random();
            var prob = rnd.NextDouble();
            Event evt = null;
            if (prob <= 0.03)
            {
                Customer customer = new Customer(pickup, dropoff, time);
                evt = _eventFactory.CreateEvent(4, time, null, null, customer);
            }

            return evt;
        }

        public Event GenerateVehicleArriveEvent(Vehicle vehicle, int time)
        {
            var stop = vehicle.TripIterator.Current.StopsIterator.CurrentStop;
            Event evt = _eventFactory.CreateEvent(0,time,vehicle,stop,null);
            return evt;
        }

        public Event GenerateVehicleDepartEvent(Vehicle vehicle, int time)
        {
            if (vehicle.TripIterator.Current.StopsIterator.IsDone)
            {
                return null;
            }
            else
            {
                var stop = vehicle.TripIterator.Current.StopsIterator.CurrentStop;
                var evtDepart = _eventFactory.CreateEvent(1, time, vehicle, stop, null);
                return evtDepart;
            }
        }


    }
}
