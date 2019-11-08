using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    public abstract class EventAppender:IEventAppender
    {
        protected IEventAppender NextEventAppender;

        protected Simulation Simulation;

        protected EventAppender(AbstractSimulation simulation)
        {
            Simulation = (Simulation)simulation;
        }

        public void SetNext(IEventAppender nextEventAppender)
        {
            NextEventAppender = nextEventAppender;
        }
        public abstract void Append(Event evt);
    }
}
