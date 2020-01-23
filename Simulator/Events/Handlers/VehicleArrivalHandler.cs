using System;
using System.Collections.Generic;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class VehicleArrivalHandler:EventHandler
    {
        public override void Handle(Event evt)
        {
            if (evt.Category == 0 && evt is VehicleStopEvent arriveEvent)
            {
                Log(evt);
                evt.AlreadyHandled = true;
                var arrivalTime = evt.Time;

                //Handle arrival evt
                if (arriveEvent.Vehicle.TripIterator.Current != null && arriveEvent.Vehicle.CurrentStop == arriveEvent.Stop)
                {

                    arriveEvent.Vehicle.IsIdle = true;
                    if (arriveEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex == 0) //check if the trip has started
                    {
                        arriveEvent.Vehicle.TripIterator.Current.Start(arrivalTime);
                        _consoleLogger.Log(" ");
                        _consoleLogger.Log(arriveEvent.Vehicle.ToString() + arriveEvent.Vehicle.TripIterator.Current + " STARTED at " +
                                          TimeSpan.FromSeconds(arrivalTime) + ".");

                    }

                    _consoleLogger.Log(arriveEvent.Vehicle.ToString() + "ARRIVED at " + arriveEvent.Stop + " at " + TimeSpan.FromSeconds(arrivalTime) + ".");
                    arriveEvent.Vehicle.VisitedStops.Add(arriveEvent.Stop); //adds the current stop to the visited stops
                    arriveEvent.Vehicle.StopsTimeWindows.Add(new long[] { arrivalTime, arrivalTime }); //adds the current time window

                    if (arriveEvent.Vehicle.TripIterator.Current.StopsIterator.IsDone && arriveEvent.Vehicle.Customers.Count == 0) //this means that the trip is complete
                    {
                        arriveEvent.Vehicle.TripIterator.Current.Finish(arrivalTime); //Finishes the trip
                        _consoleLogger.Log(arriveEvent.Vehicle.ToString() + arriveEvent.Vehicle.TripIterator.Current + " FINISHED at " +
                                          TimeSpan.FromSeconds(arrivalTime) + ", Duration:" +
                                          Math.Round(TimeSpan.FromSeconds(arriveEvent.Vehicle.TripIterator.Current.RouteDuration)
                                              .TotalMinutes) + " minutes.");

                        arriveEvent.Vehicle.TripIterator.MoveNext();
                        if (arriveEvent.Vehicle.TripIterator.Current == null)
                        {
                            arriveEvent.Vehicle.TripIterator.Reset();
                            arriveEvent.Vehicle.TripIterator.MoveNext();
                        }
                    }

                }
                //end of arrival event handle

                //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS AND GENERATION OF THE DEPART EVENT FROM THE CURRENT STOP---------------------------------------
                var customerLeaveVehicleEvents = EventGenerator.Instance().GenerateCustomerLeaveVehicleEvents(arriveEvent.Vehicle, arriveEvent.Stop, arrivalTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = customerLeaveVehicleEvents.Count > 0 ? customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time : arrivalTime;

                List<Event> customersEnterVehicleEvents = null;
                if (arriveEvent.Vehicle.TripIterator.Current != null && arriveEvent.Vehicle.TripIterator.Current.HasStarted)
                {
                    int expectedDemand = 0;
                    try
                    {
                        expectedDemand = !arriveEvent.Vehicle.FlexibleRouting ? Simulation.Context.DemandsDataObject.GetDemand(arriveEvent.Stop.Id, arriveEvent.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(arriveEvent.Time).Hours) : 0;

                    }
                    catch (Exception)
                    {
                        expectedDemand = 0;
                    }

                    customersEnterVehicleEvents = EventGenerator.Instance().GenerateCustomersEnterVehicleEvents(arriveEvent.Vehicle, arriveEvent.Stop, lastInsertedLeaveTime, expectedDemand);
                    if (customersEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = customersEnterVehicleEvents[customersEnterVehicleEvents.Count - 1].Time;
                }

                Simulation.AddEvent(customersEnterVehicleEvents);
                Simulation.AddEvent(customerLeaveVehicleEvents);


                var maxInsertedTime = Math.Max(lastInsertedEnterTime, lastInsertedLeaveTime); ; //gets the highest value of the last insertion in order to maintain precedence constraints for the depart evt, meaning that the stop depart only happens after every customer has already entered and left the vehicle on that stop location

                //INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS!


                if (arriveEvent.Vehicle.TripIterator.Current != null && arriveEvent.Vehicle.FlexibleRouting)
                {
                    var currentVehicleTrip = arriveEvent.Vehicle.TripIterator.Current;
                    var customersToEnterAtCurrentStop = currentVehicleTrip.ExpectedCustomers.FindAll(c => c.PickupDelivery[0] == arriveEvent.Stop && !c.IsInVehicle); //gets all the customers that have the current stop as the pickup stop

                    if (customersToEnterAtCurrentStop.Count > 0) //check if there is customers to enter at current stop
                    {
                        foreach (var customer in customersToEnterAtCurrentStop) //iterates over every customer that has the actual stop as the pickup stop, in order to make them enter the vehicle
                        {
                            if (currentVehicleTrip.ScheduledTimeWindows[currentVehicleTrip.StopsIterator.CurrentIndex][1] >= customer.DesiredTimeWindow[0]) //if current stop expected depart time is greater or equal than the customer arrival time adds the customer
                            {
                                var enterTime = maxInsertedTime > customer.DesiredTimeWindow[0] ? maxInsertedTime + 1 : customer.DesiredTimeWindow[0] + 1; //case maxinserted time is greather than desired time window the maxinserted time +1 will be the new enterTime of the customer, othersie it is the customer's desiredtimewindow
                                var customerEnterVehicleEvt =
                                    EventGenerator.Instance().GenerateCustomerEnterVehicleEvent(arriveEvent.Vehicle, (int)enterTime, customer); //generates the enter event
                                Simulation.AddEvent(customerEnterVehicleEvt); //adds to the event list
                                maxInsertedTime = (int)enterTime; //updates the maxInsertedTime
                            }

                        }
                    }
                }


                // END OF INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS

                //VEHICLE DEPART STOP EVENT

                if (arriveEvent.Vehicle.TripIterator.Current?.ScheduledTimeWindows != null)
                {
                    var currentStopIndex = arriveEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                    var newDepartTime = arriveEvent.Vehicle.TripIterator.Current.ScheduledTimeWindows[currentStopIndex][1]; //gets the expected latest depart time
                    maxInsertedTime = newDepartTime != 0 ? (int)Math.Max(maxInsertedTime, newDepartTime) : maxInsertedTime; //if new depart time != 0,new maxInsertedTime will be the max between maxInsertedtime and the newDepartTime, else the value stays the same.
                                                                                                                            //If maxInsertedTime is still max value between the previous maxInsertedTime and newDepartTime, this means that there has been a delay in the flexible trip (compared to the model generated by the solver)
                }

                var nextDepartEvent = EventGenerator.Instance().GenerateVehicleDepartEvent(arriveEvent.Vehicle, maxInsertedTime + 2);
                Simulation.AddEvent(nextDepartEvent);
            }
            else
            {
                Successor?.Handle(evt);
            }
        }



        public VehicleArrivalHandler(Simulation simulation) : base(simulation)
        {
        }
    }
}
