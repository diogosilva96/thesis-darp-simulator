using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator
{
    public abstract class AbstractSimulation
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;

        public int ComputationTime;

        protected AbstractSimulation()
        {
            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
            EventGenerator = EventGenerator.Instance();
            TotalEventsHandled = 0;
            ComputationTime = 0;
        }


        public abstract void MainLoop();
        public void Simulate()
        {
            if (Events.Count > 0)
            {                              
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < Events.Count ;i++)
                {
                            var currentEvent = Events[i];
                            Handle(currentEvent);
                            Append(currentEvent);                  
                            SortEvents();
                }
                watch.Stop();
                ComputationTime = (int)TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds;

            }
            
        }

        public abstract void Append(Event evt);
        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public abstract void Handle(Event evt);

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
