using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;


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
                    //Customer service request
                    evt = new CustomerRequestEvent(category,time,customer);
                    break;     
            }

            return evt;
        }

    }
}
