using System.Collections.Generic;
using System.Linq;
using Simulator.Events;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Simulation
{
    public abstract class AbstractSimulation
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;



        protected AbstractSimulation()
        {
            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
            EventGenerator = EventGenerator.Instance();
        }


        public abstract void MainLoop();

        public abstract void OnSimulationStart();

        public IEventHandler FirstEventHandler;
        public void Simulate()
        {

                if (VehicleFleet.FindAll(v=>v.ServiceTrips.Count>0).Count>0)
                {
                    OnSimulationStart();
                    var handler = FirstEventHandler;
                    for (int i = 0; i < Events.Count ;i++)
                    {
                        var currentEvent = Events[i];
                        handler.Handle(currentEvent);
                        SortEvents();
                    }
                    OnSimulationEnd();
                }
            
        }

        public abstract void OnSimulationEnd();

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public void SortEvents()
        {
            Events = Events.OrderBy(evt => evt.Time).ThenBy(evt => evt.Category).ToList();
        }
        public bool AddEvent(Event evt)
        {
            if (evt != null)
            {
                if (!Events.Contains(evt))
                {
                    Events.Add(evt);
                    return true;
                }
            }
            return false;
        }

        public bool AddEvent(IEnumerable<Event> eventSet)
        {
            if (eventSet == null)
            {
                return false;
            }

            foreach (var evt in eventSet)
            {
               Events.Add(evt);
            }
            return true;
        }
    }
}
