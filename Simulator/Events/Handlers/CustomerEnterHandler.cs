using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class CustomerEnterHandler:EventHandler
    {
        public override void Handle(Event evt)
        {
            if (evt.Category == 2 && evt is CustomerVehicleEvent customerEnterEvent)
            {
                Log(evt);
                //Customer entered vehicle i at stop x with destination y
                customerEnterEvent.OperationSuccess = customerEnterEvent.Customer.Enter(customerEnterEvent.Vehicle, evt.Time);
                evt.AlreadyHandled = true;
                Simulation.ValidationsLogger.Log(customerEnterEvent.GetValidationsMessage(Simulation.Stats.ValidationsCounter++));
            }
            else
            {
                Successor?.Handle(evt);
            }
        }

        public CustomerEnterHandler(Simulation simulation) : base(simulation)
        {
        }
    }
}
