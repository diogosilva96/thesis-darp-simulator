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

                evt.AlreadyHandled = true;
                //Customer entered vehicle i at stop x with destination y
                //handle of customerLeave event
                if (customerLeaveEvent.Customer.IsInVehicle)
                {
                    var customerLeft = customerLeaveEvent.Vehicle.RemoveCustomer(customerLeaveEvent.Customer);
                    if (customerLeft)
                    {
                        TimeSpan t = TimeSpan.FromSeconds(evt.Time);
                        customerLeaveEvent.Customer.RealTimeWindow[1] = evt.Time; //assigns the real leave time of the time window
                        customerLeaveEvent.Customer.IsInVehicle = false;
                        customerLeaveEvent.Customer.AlreadyServed = true;
                        var delayTimeStr = "";
                        if (customerLeaveEvent.Customer.DesiredTimeWindow != null && customerLeaveEvent.Customer.RealTimeWindow != null)
                        {
                            delayTimeStr = " ; Delay time: " + customerLeaveEvent.Customer.DelayTime + " seconds";
                        }

                        _consoleLogger.Log(customerLeaveEvent.Vehicle.SeatsState + customerLeaveEvent.Customer.ToString() + "(Ride time:" + customerLeaveEvent.Customer.RideTime +
                                          " seconds" + delayTimeStr + ") LEFT at " + customerLeaveEvent.Customer.PickupDelivery[1] +
                                          " at " + t.ToString() + ".");
                        if (customerLeaveEvent.Vehicle.TripIterator.Current != null &&
                            (customerLeaveEvent.Vehicle.TripIterator.Current.StopsIterator.IsDone && customerLeaveEvent.Vehicle.Customers.Count == 0)
                        ) //this means that the trip is complete
                        {
                            customerLeaveEvent.Vehicle.TripIterator.Current.Finish(evt.Time); //Finishes the service
                            _consoleLogger.Log(customerLeaveEvent.Vehicle.ToString() + customerLeaveEvent.Vehicle.TripIterator.Current + " FINISHED at " +
                                              TimeSpan.FromSeconds(evt.Time).ToString() + ", Duration:" +
                                              Math.Round(TimeSpan
                                                  .FromSeconds(customerLeaveEvent.Vehicle.TripIterator.Current.RouteDuration)
                                                  .TotalMinutes) + " minutes.");
                            customerLeaveEvent.Vehicle.TripIterator.MoveNext();
                            if (customerLeaveEvent.Vehicle.TripIterator.Current == null)
                            {
                                customerLeaveEvent.Vehicle.TripIterator.Reset();
                                customerLeaveEvent.Vehicle.TripIterator.MoveNext();
                            }
                        }

                    }

                    customerLeaveEvent.OperationSuccess = customerLeft;
                }

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
