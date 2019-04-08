using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Simulator.Events;
using Simulator.Objects;

namespace Simulator
{
    public abstract class AbstractSimulation
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        public void Simulate()
        {
            if (Events != null)
            {
                    
                    for (int i = 0; i < Events.Count ;i++)
                    {
                            Console.WriteLine(Events[i].ToString());
                            Handle(Events[i]);
                            Append(Events[i]);
                            Events.Sort();

                    }
                    PrintMetrics();
            }
            
        }

        public abstract void Handle(Event evt);

        public abstract void Append(Event evt);

        public void AddEvent(Event evt)
        {
            if (evt != null)
            {
                Events.Add(evt);
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public void AddEvent(IEnumerable<Event> eventSet)
        {
            if (eventSet == null)
                throw new ArgumentNullException();
            foreach (var evt in eventSet)
            {
               Events.Add(evt);                
            }
        }
        public abstract void PrintMetrics();
    }
}
