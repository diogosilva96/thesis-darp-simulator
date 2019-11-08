using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    class VehicleArriveEventAppender:EventAppender
    {

        public override void Append(Event evt)
        {
            if (evt.Category == 0)
            {
                Console.WriteLine("VehicleArriveEventAppender");
            } 
            else if (NextEventAppender != null)
            {
                NextEventAppender.Append(evt);
            }
        }

        public VehicleArriveEventAppender(AbstractSimulation simulation) : base(simulation)
        {
        }
    }
}
