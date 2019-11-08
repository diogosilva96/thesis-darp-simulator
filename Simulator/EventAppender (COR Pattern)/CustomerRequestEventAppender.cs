using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    class CustomerRequestEventAppender:EventAppender
    {
        public override void Append(Event evt)
        {
            if (evt.Category == 4)
            {
                Console.WriteLine("CustomerRequestEventAppender");
            }
            else if (NextEventAppender != null)
            {
                NextEventAppender.Append(evt);
            }
        }

        public CustomerRequestEventAppender(AbstractSimulation simulation) : base(simulation)
        {
        }
    }
}
