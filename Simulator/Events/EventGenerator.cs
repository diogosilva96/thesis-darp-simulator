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

        public List<Event> GenerateCustomerEnterVehicleEvents(Vehicle vehicle, Stop stop, int time, int lambda, int expectedDemand)
        {
            List<Event> events = new List<Event>();
            //Lambda = lambda; // remove??

            if (vehicle.ServiceIterator.Current.Trip != null)
            { 
                var t = TimeSpan.FromSeconds(time);
                int currentHour = t.Hours;
                var currentRoute =vehicle.ServiceIterator.Current.Trip.Route;

                var sample = expectedDemand;
                //var sample = ((Poisson)_distribution).Sample();
                //check if distribution will be used or the linear use of the inputs
                
                
                int currentStopIndex = vehicle.ServiceIterator.Current.StopsIterator.CurrentIndex;

                if (sample > 0 && currentStopIndex < vehicle.ServiceIterator.Current.Trip.Stops.Count - 1 && vehicle.ServiceIterator.Current.StopsIterator.CurrentStop == stop) // generation of customers at each stop
                {
                    var enterTime = time;
                    for (int i = 1; i <= sample; i++)
                    {
                        var rnd = new Random();
                        int dropOffStopIndex = rnd.Next(currentStopIndex + 1,
                            vehicle.ServiceIterator.Current.Trip.Stops.Count - 1);
                        Stop dropOffStop = vehicle.ServiceIterator.Current.Trip.Stops[dropOffStopIndex];
                        Customer customer = new Customer(stop, dropOffStop);
                        if (!vehicle.IsFull)
                        { 
                            enterTime++;
                        }

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
            vehicle.ServiceIterator.Current.StopsIterator.Reset();
            if (vehicle.ServiceIterator.Current.Trip != null)
            {
                var ind = 0;
                while (!vehicle.ServiceIterator.Current.StopsIterator.IsDone)
                {
                    if (ind == 0)//If the index is 0, the evt time is the starttime
                    {
                        time = startTime;
                    }
                    else //otherwise adds the time to travel the distance from the the currentStop to the nextStop, which will give the next event startTime
                    {
                        var stopTuple = Tuple.Create(vehicle.ServiceIterator.Current.StopsIterator.CurrentStop,
                            vehicle.ServiceIterator.Current.StopsIterator.NextStop);
                        vehicle.ArcDictionary.TryGetValue(stopTuple,out var distance);
                        var travelTime = vehicle.TravelTime(distance);
                        time = Convert.ToInt32(time + travelTime);
                        vehicle.ServiceIterator.Current.StopsIterator.Next();                       
                    }
                    var stop = vehicle.ServiceIterator.Current.StopsIterator.CurrentStop;
                    var evtArrive = _eventFactory.CreateEvent(0, time, vehicle, stop, null);
                    events.Add(evtArrive);
                    var waitTime = 1;
                    time = time + waitTime;
                    if (!(vehicle.ServiceIterator.Current.StopsIterator.IsDone))//If stopIterator isn't done yet, creates new depart event
                    {
                        var evtDepart = _eventFactory.CreateEvent(1, time, vehicle, stop, null);
                        events.Add(evtDepart);
                    }

                    ind++;
                }
                vehicle.ServiceIterator.Current.StopsIterator.Reset();
            }

            return events;
        }


    }
}
