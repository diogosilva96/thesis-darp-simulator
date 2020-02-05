using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
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
                _consoleLogger.Log("New Customer (dynamic) request:" + customerRequestEvent.Customer + " - " + customerRequestEvent.Customer.PickupDelivery[0] + " -> " + customerRequestEvent.Customer.PickupDelivery[1] + ", TimeWindows: {" + customerRequestEvent.Customer.DesiredTimeWindow[0] + "," + customerRequestEvent.Customer.DesiredTimeWindow[1] + "} at " + TimeSpan.FromSeconds(evt.Time).ToString());

                //Check if request can be served, if so the 
                Simulation.Stats.TotalDynamicRequests++;
                var newCustomer = customerRequestEvent.Customer;
                RoutingSolutionObject solutionObject = null;
                if (Simulation.Context.VehicleFleet.FindAll(v => v.FlexibleRouting).Count > 0 && newCustomer != null)
                {
                    var dataModel = DataModelFactory.Instance().CreateCurrentSimulationDataModel(Simulation, newCustomer, evt.Time);
                    var solver = new RoutingSolver(dataModel, false);
                    RoutingSearchParameters searchParameters =
                        operations_research_constraint_solver.DefaultRoutingSearchParameters();
                    searchParameters.FirstSolutionStrategy =
                        FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
                    searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.TabuSearch;
                    searchParameters.TimeLimit = new Duration { Seconds = 5 };//change
                    var solution = solver.TryGetSolution(searchParameters);
                    if (solution != null)
                    {
                        //dataModel.PrintTravelTimes();
                        //dataModel.PrintPickupDeliveries();
                        //dataModel.PrintTimeWindows();
                        solver.PrintSolution(solution);
                        Simulation.Stats.TotalServedDynamicRequests++;
                        _consoleLogger.Log(newCustomer.ToString() + " request was inserted into a vehicle route at " + TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());

                        solutionObject = solver.GetSolutionObject(solution);
                    }
                    else
                    {
                        _consoleLogger.Log(newCustomer.ToString() + " will not be able to be served by any of the available vehicles at " + TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());
                    }
                }

                if (!Simulation.Context.DynamicCustomers.Contains(newCustomer))
                {
                    Simulation.Context.DynamicCustomers.Add(newCustomer);
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
                        var solutionCustomers = solutionObject.GetVehicleCustomers(vehicle);               
                        if (solutionRoute.Count >= 2 && solutionRoute[0] != solutionRoute[1])//check if current vehicle route is valid
                        {
                            if (vehicle.VisitedStops.Count > 1 && vehicle.CurrentStop == Simulation.Context.Depot)
                            {
                                _consoleLogger.Log("Vehicle " + vehicle.Id + " already performed a route and is currently idle at depot.");//debug
                            }
                            if (vehicle.TripIterator?.Current != null)
                            {
                                var currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                                var customers = solutionObject.GetVehicleCustomers(vehicle); //contains all customers (already inside and not yet in vehicle)
                                List<Stop> visitedStops = new List<Stop>(vehicle.VisitedStops);
                                List<long[]> visitedTimeWindows = new List<long[]>(vehicle.StopsTimeWindows);
   
                               
                                if (currentStopIndex < vehicle.VisitedStops.Count)
                                {
                                    visitedStops.RemoveAt(currentStopIndex); //remove current stop from the visitedStops list
                                    visitedTimeWindows.RemoveAt(currentStopIndex); //remove current timeWindow from the visitedTimeWindows list
                                }

                                //inserts the already visited stops at the beginning of the solutionRoute list
                                for (int e = visitedStops.Count - 1; e >= 0; e--)
                                {
                                    solutionRoute.Insert(0, visitedStops[e]);
                                    solutionTimeWindows.Insert(0, visitedTimeWindows[e]);
                                }

                                vehicle.TripIterator.Current.AssignStops(solutionRoute, solutionTimeWindows, currentStopIndex);
                                vehicle.TripIterator.Current.ExpectedCustomers = customers.FindAll(c=> !c.IsInVehicle);//the expected customers for the current vehicle are the ones that are not in that vehicle

                                var vehicleEvents = Simulation.Events.FindAll(e => (e is VehicleStopEvent vse && vse.Vehicle == vehicle && e.Time >= evt.Time) || (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle && e.Time >= evt.Time)).OrderBy(e => e.Time).ThenBy(e => e.Category).ToList(); //gets all next vehicle depart or arrive events
                                if (vehicleEvents.Count > 0)
                                {
                                    foreach (var vEvent in vehicleEvents)
                                    {
                                        if (vEvent is VehicleStopEvent vehicleStopArriveEvent && vEvent.Category == 0
                                        ) //vehicle arrive stop event
                                        {
                                            //_consoleLogger.Log(vehicleStopArriveEvent.GetTraceMessage());
                                        }

                                        if (vEvent is VehicleStopEvent vehicleStopDepartEvent && vEvent.Category == 1
                                        ) //vehicle depart stop event
                                        {
                                            var departTime = vEvent.Time;
                                            //_consoleLogger.Log(vehicleStopDepartEvent.GetTraceMessage());
                                            if (vehicleStopDepartEvent.Stop == vehicle.CurrentStop)
                                            {
                                                departTime =
                                                    (int) vehicle.TripIterator.Current.ScheduledTimeWindows[
                                                        vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1] +
                                                    2; //recalculates new event depart time;
                                                _consoleLogger.Log("New event depart: " + departTime);

                                                vEvent.Time = departTime;
                                            }

                                            var allEnterLeaveEventsForCurrentVehicle = Simulation.Events
                                                .FindAll(e =>
                                                    e.Time >= evt.Time &&
                                                    (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle))
                                                .OrderBy(e => e.Time).ThenBy(e => e.Category).ToList();

                                            foreach (var enterOrLeaveEvt in allEnterLeaveEventsForCurrentVehicle)
                                            {
                                                if (enterOrLeaveEvt.Time > departTime
                                                ) //if the event time is greater than the depart time removes those events, otherwise maintain the enter and leave events for current stop
                                                {
                                                    Simulation.Events.Remove(enterOrLeaveEvt);
                                                    //_consoleLogger.Log("Enter Leave event removed (depart time:"+departTime+"; evt time:"+enterOrLeaveEvt.Time+"): "+enterOrLeaveEvt.GetTraceMessage());
                                                }

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //if vehicle events is 0 it means the vehicle has no more generated events for it, need to generate a new depart event for the vehicle
                                    vehicle.TripIterator.Current.IsDone = false;//current trip is no longer completed
                                    var currentDepartureTime = (int) solutionTimeWindows[currentStopIndex][1];
                                    if (currentDepartureTime == evt.Time)//if departure time is the same time as the event being handled adds 1 to its time
                                    {
                                        currentDepartureTime++;
                                    }
                                    var departEvt = Simulation.EventGenerator.GenerateVehicleDepartEvent(vehicle, currentDepartureTime);
                                    Simulation.Events.Add(departEvt);
                                    //Simulation.InitializeVehicleFirstArriveEvent(vehicle,currentArriveTime);
                                }

                            }
                            else
                            {
                                //trip assignment for vehicles that are have not yet performed a route                                                
                                Simulation.InitializeVehicleFlexibleRoute(vehicle, solutionRoute, solutionCustomers, solutionTimeWindows);
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
