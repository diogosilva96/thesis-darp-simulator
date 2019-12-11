using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class DynamicRequestHandler:EventHandler
    {
        public override void Handle(Event evt)
        {  //INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE ( after a dynamic request has been accepted)
            if (evt.Category == 4 && evt is CustomerRequestEvent customerRequestEvent)
            {
                Log(evt);
                evt.AlreadyHandled = true;
                _consoleLogger.Log("New request:" + customerRequestEvent.Customer + " - " + customerRequestEvent.Customer.PickupDelivery[0] + " -> " + customerRequestEvent.Customer.PickupDelivery[1] + ", TimeWindows: {" + customerRequestEvent.Customer.DesiredTimeWindow[0] + "," + customerRequestEvent.Customer.DesiredTimeWindow[1] + "} at " + TimeSpan.FromSeconds(evt.Time).ToString());

                //Check if request can be served, if so the 
                Simulation.Stats.TotalDynamicRequests++;
                var newCustomer = customerRequestEvent.Customer;
                RoutingSolutionObject solutionObject = null;
                if (Simulation.Context.VehicleFleet.FindAll(v => v.FlexibleRouting).Count > 0 && newCustomer != null && Simulation.Context.VehicleFleet.FindAll(v => v.TripIterator.Current != null && !v.TripIterator.Current.IsDone).Count > 0)
                {
                    var dataModel = DataModelFactory.Instance().CreateCurrentSimulationDataModel(Simulation, newCustomer, evt.Time);
                    var solver = new RoutingSolver(dataModel, false);
                    var solution = solver.TryGetSolution(null);
                    if (solution != null)
                    {
                        //dataModel.PrintTimeMatrix();
                        //dataModel.PrintPickupDeliveries();
                        //dataModel.PrintTimeWindows();
                        solver.PrintSolution(solution);
                        Simulation.Stats.TotalServedDynamicRequests++;
                        _consoleLogger.Log(newCustomer.ToString() + " was inserted into a vehicle service at " + TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());

                        solutionObject = solver.GetSolutionObject(solution);
                    }
                    else
                    {
                        _consoleLogger.Log(newCustomer.ToString() + " was not possible to be served at " + TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());
                    }
                }
                if (solutionObject != null)
                {
                    solutionObject.MetricsContainer.PrintMetrics();
                    var vehicleFlexibleRouting = Simulation.Context.VehicleFleet.FindAll(v => v.FlexibleRouting);
                    //_consoleLogger.Log("Flexible routing vehicles count: " + vehicleFlexibleRouting.Count);
                    foreach (var vehicle in vehicleFlexibleRouting)
                    {
                        var solutionRoute = solutionObject.GetVehicleStops(vehicle);
                        var solutionTimeWindows = solutionObject.GetVehicleTimeWindows(vehicle);

                        if (vehicle.TripIterator.Current != null)
                        {
                            var currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                            var currentStopList = new List<Stop>(vehicle.TripIterator.Current.Stops); //current stoplist for vehicle (before adding the new request)
                            var currentTimeWindows = new List<long[]>(vehicle.TripIterator.Current.ScheduledTimeWindows);
                            var customers = solutionObject.GetVehicleCustomers(vehicle); //contains all customers (already inside and not yet in vehicle)
                            List<Stop> visitedStops = new List<Stop>();
                            List<long[]> visitedTimeWindows = new List<long[]>();
                            //_consoleLogger.Log("Vehicle " + vehicle.Id + ":");
                            //_consoleLogger.Log("Current stop: " + currentStopList[currentStopIndex].ToString());
                            //construction of already visited stops list
                            if (currentStopIndex > 0)
                            {
                                //_consoleLogger.Log("Visited stops:");
                                for (int index = 0; index < currentStopIndex; index++)
                                {
                                    visitedStops.Add(vehicle.TripIterator.Current.VisitedStops[index]);
                                    visitedTimeWindows.Add(vehicle.TripIterator.Current.StopsTimeWindows[index]);
                                    //_consoleLogger.Log(currentStopList[index].ToString() + " - " + vehicle.TripIterator.Current.VisitedStops[index].ToString());
                                    //ConsoleLogger.Log(currentStopList[index].ToString()+ " - TW:{" + currentTimeWindows[index][0] + "," + currentTimeWindows[index][1] + "}");
                                }
                            }

                            //end of visited stops list construction
                            //inserts the already visited stops at the beginning of the  solutionRoute list
                            for (int e = visitedStops.Count - 1; e >= 0; e--)
                            {

                                solutionRoute.Insert(0, visitedStops[e]);
                                solutionTimeWindows.Insert(0, visitedTimeWindows[e]);
                            }

                            vehicle.TripIterator.Current.AssignStops(solutionRoute, solutionTimeWindows, currentStopIndex);
                            vehicle.TripIterator.Current.ExpectedCustomers = customers.FindAll(c => !c.IsInVehicle); //the expected customers for the current vehicle are the ones that are not in that vehicle

                            var vehicleEvents = Simulation.Events.FindAll(e => (e is VehicleStopEvent vse && vse.Vehicle == vehicle && e.Time >= evt.Time) || (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle && e.Time >= evt.Time)).OrderBy(e => e.Time).ThenBy(e => e.Category).ToList(); //gets all next vehicle depart or arrive events
                            //_consoleLogger.Log("ALL NEXT VEHICLE " + vehicle.Id + " EVENTS (COUNT:" +vehicleEvents.Count + ") (TIME >=" + evt.Time + "):");
                            foreach (var vEvent in vehicleEvents)
                            {
                                if (vEvent is VehicleStopEvent vehicleStopArriveEvent && vEvent.Category == 0
                                ) //vehicle arrive stop event
                                {
                                    _consoleLogger.Log(vehicleStopArriveEvent.GetTraceMessage());
                                }

                                if (vEvent is VehicleStopEvent vehicleStopDepartEvent && vEvent.Category == 1) //vehicle depart stop event
                                {
                                    var departTime = vEvent.Time;
                                    _consoleLogger.Log(vehicleStopDepartEvent.GetTraceMessage());
                                    if (vehicleStopDepartEvent.Stop == vehicle.TripIterator.Current.StopsIterator.CurrentStop)
                                    {
                                        departTime = (int)vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1] + 2; //recalculates new event depart time;
                                        _consoleLogger.Log("New event depart: " + departTime);
                                        
                                        vEvent.Time = departTime;
                                    }

                                    var allEnterLeaveEventsForCurrentVehicle = Simulation.Events.FindAll(e => e.Time >= evt.Time && (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle)).OrderBy(e => e.Time).ThenBy(e => e.Category).ToList();

                                    foreach (var enterOrLeaveEvt in allEnterLeaveEventsForCurrentVehicle)
                                    {
                                        if (enterOrLeaveEvt.Time > departTime) //if the event time is greater than the depart time removes those events, otherwise maintain the enter and leave events for current stop
                                        {
                                            Simulation.Events.Remove(enterOrLeaveEvt);
                                            //_consoleLogger.Log("Enter Leave event removed (depart time:"+departTime+"; evt time:"+enterOrLeaveEvt.Time+"): "+enterOrLeaveEvt.GetTraceMessage());
                                        }
                                        
                                    }
                                }
                            }

                        }
                    }
                }
            }
            else
            {
                Successor?.Handle(evt);
                
            }
        }

        public DynamicRequestHandler(Simulation simulation) : base(simulation)
        {
        }
    }
}
