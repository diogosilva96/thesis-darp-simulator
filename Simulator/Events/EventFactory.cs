using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.Objects;
using Simulator.Objects;



namespace Simulator.Events
{
    class EventFactory
    {
        private Dictionary<int, Event> _events;

        public EventFactory()
        {
            _events = new Dictionary<int, Event>();
        }
        public Event CreateEvent(int category, int time, Vehicle vehicle, Stop stop, Customer customer)
        {
            
            Event evt = null;
            switch (category)
            {
                    case 0:
                        evt = new VehicleStopEvent(category,time,vehicle,stop);
                        //Vehicle arrived at stop y from origin x
                        break;
                    case 1:
                        //Vehicle left stop x to destination y
                        evt = new VehicleStopEvent(category,time,vehicle,stop);
                        break;
                    case 2:
                        //Customer entered vehicle i at stop x with destination y
                        evt = new CustomerVehicleEvent(category, time, customer, vehicle);
                    break;
                    case 3:
                        //Customer left vehicle i at stop y
                        evt = new CustomerVehicleEvent(category, time, customer, vehicle);
                    break;
                    case 4:                        
                        //Customer arrived at stop x removed!!
                    break;
            }

            return evt;
        }

        public Event CreateEvent(int category) //following flyweight pattern
        {
            //uses lazy init
            Event evt = null;
            if (_events.ContainsKey(category))
            {
                evt = _events[category];
            }
            else
            {
                switch (category)
                {
                    case 0:
                        evt = new VehicleStopEvent(category);
                        //Vehicle arrived at stop y from origin x
                        break;
                    case 1:
                        //Vehicle left stop x to destination y
                        evt = new VehicleStopEvent(category);
                        break;
                    case 2:
                        //Customer entered vehicle i at stop x with destination y
                        evt = new CustomerVehicleEvent(category);
                        break;
                    case 3:
                        //Customer left vehicle i at stop y
                        evt = new CustomerVehicleEvent(category);
                        break;
                    case 4:
                        //Customer arrived at stop x
                        break;
                }
                _events.Add(category,evt);
            }

            return evt;
        }
    }
}
