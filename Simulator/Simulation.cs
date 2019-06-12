using System;
using System.Collections.Generic;
using System.IO;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        public static List<Route> Routes;


        public Simulation()
        {
            Routes = RoutesDataObject.Routes;
            GenerateVehicleFleet(1); // Generates a vehicle for each route
        }

        public override void AssignVehicleServices(int startHour, int endHour, int routeIndex)
        {
            var ind = 0;
            foreach (var route in Routes) // Each vehicle is responsible for a route
            {
                if (ind == routeIndex)
                {
                    //if (ind > VehicleFleet.Count - 1) //if it reaches the last vehicle breaks the loop
                    //    break;

                    var v = VehicleFleet[0];
                    var allRouteServices = route.AllRouteServices.FindAll(s => TimeSpan.FromSeconds(s.StartTime).Hours >= startHour && TimeSpan.FromSeconds(s.StartTime).Hours <= endHour);
                    if (allRouteServices.Count > 0)
                    {
                        foreach (var service in allRouteServices)
                            if (v.Services.FindAll(s => Math.Abs(s.StartTime - service.StartTime) < 60 * 30).Count == 0
                            ) //if there is no service where the start time is lower than 30mins (1800seconds)
                            {
                                v.AddService(service); //Adds the service
                            }
                    }


                    if (v.Services.Count > 0)
                        ConsoleLogger.Log(ToString() + v.Services.Count + " Services (" +
                                          v.Services[0].Trip.Route +
                                          ") were assigned to Vehicle " +
                                          v.Id + ".");

                }

                ind++;
            }
        }

        public override void GenerateVehicleServiceEvents()
        {
            foreach (var vehicle in VehicleFleet)
                if (vehicle.Services.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.ServiceIterator.Reset();
                    while (vehicle.ServiceIterator.MoveNext()) //Iterates over each service
                    {
                        var events =
                            EventGenerator.GenerateRouteEvents(vehicle,
                                vehicle.ServiceIterator.Current
                                    .StartTime); //Generates the arrive/depart events for that service
                        AddEvent(events);
                    }

                    vehicle.ServiceIterator.Reset(); //resets iterator
                    vehicle.ServiceIterator.MoveNext(); //initializes the iterator at the first service
                }

            SortEvents();
        }

        public override void PrintSolution()
        {
            IRecorder fileRecorder =
                new FileRecorder(Path.Combine(Environment.CurrentDirectory, @"Logger/stats_logs.txt"));
            var myFileLogger = new Logger.Logger(fileRecorder);
            var toPrintList = new List<string>();
            toPrintList.Add(ToString() + "Total number of events handled: " +
                            Events.FindAll(e => e.AlreadyHandled).Count + " out of " + Events.Count + ".");
            toPrintList.Add(ToString() + "Vehicle Fleet Size: " + VehicleFleet.Count + " vehicle(s).");

            foreach (var vehicle in VehicleFleet)
            {
                toPrintList.Add("-----------------------------------------------------");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Service route:" + vehicle.Services[0].Trip.Route);

                //For debug purposes---------------------------------------------------------------------------
                if (vehicle.Services.Count != vehicle.Services.FindAll(s => s.IsDone).Count)
                {
                    toPrintList.Add("Services Completed:");
                    foreach (var service in vehicle.Services)
                        if (service.IsDone)
                            toPrintList.Add(" - " + service + " - [" +
                                            TimeSpan.FromSeconds(service.StartTime) + " - " +
                                            TimeSpan.FromSeconds(service.EndTime) + "]");
                }

                if (vehicle.Customers.Count > 0)
                {
                    toPrintList.Add("Number of customers inside:" + vehicle.Customers.Count);
                    foreach (var cust in vehicle.Customers)
                        toPrintList.Add(
                            cust + "Pickup:" + cust.PickupDelivery[0] + "Delivery:" + cust.PickupDelivery[1]);
                }

                //End of debug purposes---------------------------------------------------------------------------

                if (vehicle.ServiceIterator != null)
                {
                    var servicesMetricsObject = new VehicleServicesStatistics(vehicle);
                    var list = servicesMetricsObject.GetOverallStatsPrintableList();
                    var logList = servicesMetricsObject.GetPerServiceStatsPrintableList();

                    foreach (var log in logList) myFileLogger.Log(log);
                    foreach (var toPrint in list) toPrintList.Add(toPrint);
                }
            }

            foreach (var printableMessage in toPrintList)
            {
                myFileLogger.Log(printableMessage);
                ConsoleLogger.Log(printableMessage);
            }
        }


        public override void Append(Event evt)
        {
            var eventsCount = Events.Count;
            var rnd = new Random();
            //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS---------------------------------------
            if (evt.Category == 0 && evt is VehicleStopEvent vseEvt)
            {
                var baseTime = evt.Time;
                var customerLeaveVehicleEvents =
                    EventGenerator.GenerateCustomerLeaveVehicleEvents(vseEvt.Vehicle, vseEvt.Stop,
                        baseTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = customerLeaveVehicleEvents.Count > 0
                    ? customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time
                    : baseTime;

                List<Event> customerEnterVehicleEvents = null;
                if (vseEvt.Vehicle.ServiceIterator.Current != null)
                {
                    customerEnterVehicleEvents =
                        EventGenerator.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle, vseEvt.Stop,
                            lastInsertedLeaveTime, rnd.Next(1, 7));
                    if (customerEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = customerEnterVehicleEvents[customerEnterVehicleEvents.Count - 1].Time;
                }

                var biggestInsertedTime = 0;
                var timeAdded = 0;
                if (lastInsertedEnterTime < lastInsertedLeaveTime
                ) //Verifies which of the inserted time is bigger and calculates the time added
                {
                    timeAdded = lastInsertedLeaveTime - baseTime;
                    biggestInsertedTime = lastInsertedLeaveTime;
                }
                else
                {
                    timeAdded = lastInsertedEnterTime - baseTime;
                    biggestInsertedTime = lastInsertedEnterTime;
                }


                if (timeAdded != 0 && biggestInsertedTime != 0)
                    foreach (var ev in Events)
                        if (ev.Time > baseTime && ev is VehicleStopEvent vseEv)
                            if (vseEv.Vehicle == vseEvt.Vehicle && vseEv.Service == vseEvt.Service)
                                ev.Time =
                                    ev.Time +
                                    timeAdded; //adds the added time to all the next events of that service for that vehicle
                AddEvent(customerEnterVehicleEvents);
                AddEvent(customerLeaveVehicleEvents);
            }

            //END OF INSERTION OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            var pickup = RoutesDataObject.Stops[rnd.Next(0, RoutesDataObject.Stops.Count)];
            var delivery = pickup;
            while (pickup == delivery) delivery = RoutesDataObject.Stops[rnd.Next(0, RoutesDataObject.Stops.Count)];

            var eventReq =
                EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickup,
                    delivery); //Generates a pickup and delivery customer request
            if (eventReq != null) //if eventReq isn't null, add the event and then sorts the events list
                AddEvent(eventReq);
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------

            if (eventsCount != Events.Count) //If the size of the events list has changed, Sorts Events
                SortEvents();
        }

        public override void Handle(Event evt)
        {
            evt.Treat();
            TotalEventsHandled++;
            EventLogger.Log(evt.GetTraceMessage());
            if (evt is CustomerVehicleEvent cve)
            {
                ValidationsLogger.Log(cve.GetValidationsMessage());
            }
        }
    }
}