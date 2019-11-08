using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    public class VehicleDepartEventAppender:EventAppender
    {
        public override void Append(Event evt)
        {
            if (evt.Category == 1)
            {
                Console.WriteLine("VehicleDepartEventAppender");
            }
            else if (NextEventAppender != null)
            {
                NextEventAppender.Append(evt);
            }
        }

        public VehicleDepartEventAppender(AbstractSimulation simulation) : base(simulation)
        {
        }
    }
}
