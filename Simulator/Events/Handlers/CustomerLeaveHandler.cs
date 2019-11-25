using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class CustomerLeaveHandler:EventHandler
    {
        public override void Handle(Event evt)
        {
            if (evt.Category == 3 && evt is CustomerVehicleEvent customerLeaveEvent)
            {
                Log(evt);
                //Customer entered vehicle i at stop x with destination y
                customerLeaveEvent.OperationSuccess = customerLeaveEvent.Customer.Leave(customerLeaveEvent.Vehicle, evt.Time);
                evt.AlreadyHandled = true;
                Simulation.ValidationsLogger.Log(customerLeaveEvent.GetValidationsMessage(Simulation.Stats.ValidationsCounter++));
            }
            else
            {
                Successor?.Handle(evt);
            }
        }

        public CustomerLeaveHandler(Simulation simulation) : base(simulation)
        {
        }
    }
}
