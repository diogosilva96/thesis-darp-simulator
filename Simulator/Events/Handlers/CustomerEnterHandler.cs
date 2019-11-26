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
                evt.AlreadyHandled = true;
                //Customer enter vehicle handle
                if (!customerEnterEvent.Customer.IsInVehicle)
                {
                    var customerAdded = customerEnterEvent.Vehicle.AddCustomer(customerEnterEvent.Customer);
                    TimeSpan t = TimeSpan.FromSeconds(evt.Time);
                    if (customerAdded)
                    {
                        if (customerEnterEvent.Vehicle.TripIterator.Current != null && customerEnterEvent.Vehicle.TripIterator.Current.ExpectedCustomers.Contains(customerEnterEvent.Customer))
                        {
                            customerEnterEvent.Vehicle.TripIterator.Current.ExpectedCustomers.Remove(customerEnterEvent.Customer);
                        }
                        customerEnterEvent.Customer.RealTimeWindow[0] = evt.Time; //assigns the real enter time of the timewindow
                        customerEnterEvent.Customer.IsInVehicle = true;
                        var waitTimeStr = "";
                        if (customerEnterEvent.Customer.DesiredTimeWindow != null && customerEnterEvent.Customer.RealTimeWindow != null)
                        {
                            waitTimeStr = "(Wait time: " + customerEnterEvent.Customer.WaitTime + " seconds)";
                        }
                        _consoleLogger.Log(customerEnterEvent.Vehicle.SeatsState + customerEnterEvent.Customer.ToString() + waitTimeStr + " ENTERED at " + customerEnterEvent.Customer.PickupDelivery[0] +
                                           " at " + t.ToString() + ".");

                    }
                    else
                    {
                        _consoleLogger.Log(customerEnterEvent.Vehicle.SeatsState + customerEnterEvent.Customer.ToString() + "was not serviced at " + customerEnterEvent.Customer.PickupDelivery[0] + " at " + t.ToString() + ", because vehicle is FULL!");
                        customerEnterEvent.Customer.IsInVehicle = false;
                    }

                    customerEnterEvent.OperationSuccess = customerAdded; //returns true if vehicle is not full and false if it is full
                }
                //end of customer enter handle

          
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
