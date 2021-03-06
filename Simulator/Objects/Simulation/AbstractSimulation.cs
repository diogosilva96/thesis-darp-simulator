﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Simulator.Events;
using Simulator.Events.Handlers;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Simulation
{
    public abstract class AbstractSimulation
    {
        private static int nextId;
        public int Id { get; internal set; }

        public List<Event> Events;

        public EventGenerator EventGenerator;

        public bool Finished => Events.Count == Events.FindAll(e => e.AlreadyHandled).Count;

        protected AbstractSimulation()
        {
            Events = new List<Event>();
            EventGenerator = EventGenerator.Instance();
            Id = Interlocked.Increment(ref nextId);
        }

        public abstract void OnSimulationStart();

        public IEventHandler FirstEventHandler;
        public void Simulate()
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
