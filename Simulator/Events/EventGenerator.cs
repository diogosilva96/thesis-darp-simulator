using System;
using System.Collections.Generic;
using GraphLibrary.Objects;
using MathNet.Numerics.Distributions;
using Simulator.Objects;


namespace Simulator.Events
{
    class EventGenerator
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

        public List<Event> GenerateCustomerEnterVehicleEvents(Vehicle vehicle, Stop stop, int time, int lambda)
        {
            List<Event> events = new List<Event>();
            Lambda = lambda;

            if (vehicle.ServiceIterator.Current.Trip != null)
            {

                var sample = ((Poisson) _distribution).Sample();
                int currentStopIndex = vehicle.ServiceIterator.Current.Trip.Stops.IndexOf(stop);
                if (sample > 0 && currentStopIndex < vehicle.ServiceIterator.Current.Trip.Stops.Count - 1
                ) // generation of customers at each stop
                {
                    for (int i = 1; i <= sample; i++)
                    {
                        var rnd = new Random();
                        int dropOffStopIndex = rnd.Next(currentStopIndex + 1,
                            vehicle.ServiceIterator.Current.Trip.Stops.Count - 1);
                        Stop dropOffStop = vehicle.ServiceIterator.Current.Trip.Stops[dropOffStopIndex];
                        Customer customer = new Customer(stop, dropOffStop);
                        var enterTime = time + i;
                        var customerEnterVehicleEvent =
                            _eventFactory.CreateEvent(2, enterTime, vehicle, null, customer);
            
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
            if (prob <= 0.01)
            {
                Customer customer = new Customer(pickup, dropoff);
                evt = _eventFactory.CreateEvent(4, time, null, null, customer);
            }

            return evt;
        }
        public List<Event> GenerateRouteEvents(Vehicle vehicle, int startTime)
        {
            Lambda = 1;
            var events = new List<Event>();
            var time = 0;
            if (vehicle.ServiceIterator.Current.Trip != null)
            {

                foreach (var stop in vehicle.ServiceIterator.Current.Trip.Stops)
                {
                    if (stop == vehicle.ServiceIterator.Current.Trip.Stops[0])
                    {
                        time = startTime;
                    }
                    else
                    {
                            var distance =
                                vehicle.StopsGraph.GetWeight(vehicle.ServiceIterator.Current.StopsIterator.CurrentStop, vehicle.ServiceIterator.Current.StopsIterator.NextStop);

                        var travelTime = vehicle.TravelTime(distance);
                        time = Convert.ToInt32(time + travelTime);
                    }

                    var evtArrive = _eventFactory.CreateEvent(0, time, vehicle, stop, null);
                    events.Add(evtArrive);
                    var waitTime = 2;
                    time = time + waitTime;
                    if (!(vehicle.ServiceIterator.Current.Trip.Stops.IndexOf(stop) == vehicle.ServiceIterator.Current.Trip.Stops.Count - 1))
                    {
                        var evtDepart = _eventFactory.CreateEvent(1, time, vehicle, stop, null);
                        events.Add(evtDepart);
                    }


                }
            }

            return events;
        }


    }
}
