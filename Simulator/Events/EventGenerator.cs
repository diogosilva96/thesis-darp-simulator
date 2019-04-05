using System;
using System.Collections.Generic;
using GraphLibrary.Objects;
using MathNet.Numerics.Distributions;
using Simulator.Objects;


namespace Simulator.Events
{
    class EventGenerator
    {
      
        private IDistribution _distribution;


        public int Lambda
        {
            get => Lambda;
            set
            {
                _distribution = new Poisson(value);
            }
        }

        public List<Event> GenerateCustomerEvents(Event evt) // generates the customer events to enter the vehicle of evt at a certain stop
        {
                List<Event> _events = new List<Event>();
                Stop stop = null;

                if (evt is VehicleStopEvent vseEvt)
                {
                    EventFactory eventFactory = new EventFactory();
                    stop = vseEvt.Stop;
                    if (vseEvt.Vehicle.Router.Trip != null)
                    {
                        var sample = ((Poisson) _distribution).Sample();
                        int currentStopIndex = vseEvt.Vehicle.Router.Trip.Stops.IndexOf(stop);
                        if (sample > 0 && currentStopIndex < vseEvt.Vehicle.Router.Trip.Stops.Count - 1)
                        {
                            for (int i = 0; i < sample; i++)
                            {
                                var rnd = new Random();
                                int dropOffStopIndex = rnd.Next(currentStopIndex + 1,
                                    vseEvt.Vehicle.Router.Trip.Stops.Count - 1);
                                Stop dropOffStop = vseEvt.Vehicle.Router.Trip.Stops[dropOffStopIndex];
                                Customer c = new Customer(stop, dropOffStop);
                                var enterTime = vseEvt.Time + 2;
                                var customerEnterVehicleEvent = eventFactory.CreateEvent(2, vseEvt.Time + 2, vseEvt.Vehicle, null, c);
                                var leaveTime = (enterTime-1) + 4*(dropOffStopIndex - currentStopIndex);
                                var customerLeaveVehicleEvent =
                                    eventFactory.CreateEvent(3,leaveTime, vseEvt.Vehicle, null, c);
                                _events.Add(customerEnterVehicleEvent);
                                _events.Add(customerLeaveVehicleEvent);
                            }
                        }
                    }
                }
                return _events;
        }

        public List<Event> GenerateCustomerLeaveVehicleEvents(Vehicle vehicle,Stop stop, int time)
        {

            List<Event> events = new List<Event>();


                EventFactory eventFactory = new EventFactory();
                var customersToLeaveVehicleAtCurrentStop = vehicle.GetCustomersToLeaveVehicle(stop);
                var leaveTime = 0;
                if (customersToLeaveVehicleAtCurrentStop.Count > 0) //assigns the dropoffstop event
                {
                    int ind = 0;
                    foreach (var customer in customersToLeaveVehicleAtCurrentStop)
                    {
                        ind++;
                        leaveTime = time + ind;
                        var customerLeaveVehicleEvent =
                            eventFactory.CreateEvent(3, leaveTime, vehicle, null, customer);
                        events.Add(customerLeaveVehicleEvent);
                    }

                }

            return events;
        } 
        public List<Event> GenerateCustomerEnterVehicleEvents(Vehicle vehicle,Stop stop,int time, int lambda)
        {
            List<Event> events = new List<Event>();
            Lambda = lambda;
                if (vehicle.Router.Trip != null)
                {

                    var sample = ((Poisson) _distribution).Sample();
                    int currentStopIndex = vehicle.Router.Trip.Stops.IndexOf(stop);
                    if (sample > 0 && currentStopIndex < vehicle.Router.Trip.Stops.Count - 1) // generation of customers at each stop
                    {

                        EventFactory eventFactory = new EventFactory();
                        for (int i = 1; i <= sample; i++)
                        {
                            var rnd = new Random();
                            int dropOffStopIndex = rnd.Next(currentStopIndex + 1,
                                vehicle.Router.Trip.Stops.Count - 1);
                            Stop dropOffStop = vehicle.Router.Trip.Stops[dropOffStopIndex];
                            Customer c = new Customer(stop, dropOffStop);
                            var enterTime = time + i;
                            var customerEnterVehicleEvent =
                                eventFactory.CreateEvent(2, enterTime, vehicle, null, c);
                            events.Add(customerEnterVehicleEvent);
                        }
                    }
                }

            return events;
        }

        public List<Event> GenerateRouteEvents(Vehicle vehicle, int startTime)
        {
            var events = new List<Event>();
            var time = 0;
            if (vehicle.Router.Trip != null)
            {
                var eventFactory = new EventFactory();

                foreach (var stop in vehicle.Router.Trip.Stops)
                {
                    if (stop == vehicle.Router.Trip.Stops[0])
                    {
                        time = startTime;
                    }
                    else
                    {
                        var distance =
                            vehicle.StopsGraph.GetWeight(vehicle.Router.CurrentStop, vehicle.Router.NextStop);
                        var travelTime = vehicle.TravelTime(distance);
                        time = Convert.ToInt32(time + travelTime * 2);
                    }

                    var evtArrive = eventFactory.CreateEvent(0, time, vehicle, stop, null);
                    events.Add(evtArrive);
                    var waitTime = 2;//rever isto
                    time = time + waitTime;
                    if (!(vehicle.Router.Trip.Stops.IndexOf(stop) == vehicle.Router.Trip.Stops.Count - 1))
                    {
                        var evtDepart = eventFactory.CreateEvent(1, time, vehicle, stop, null);
                        events.Add(evtDepart);
                    }


                }
            }

            return events;
        }

        

    }
}
