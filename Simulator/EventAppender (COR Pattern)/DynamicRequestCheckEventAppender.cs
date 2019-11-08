using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    class DynamicRequestCheckEventAppender:EventAppender
    {
        public override void Append(Event evt)
        {
            if (evt.Category == 5)
            {
                Console.WriteLine("DynamicRequestEventAppender");
            }
            else if (NextEventAppender != null)
            {
                NextEventAppender.Append(evt);
            }
        }

        public DynamicRequestCheckEventAppender(AbstractSimulation simulation) : base(simulation)
        {
        }
    }
}
