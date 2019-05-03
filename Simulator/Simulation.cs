using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Google.OrTools.ConstraintSolver;
using GraphLibrary;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;


namespace Simulator
{
    public class Simulation:AbstractSimulation
    {
        public List<Route> Routes;
       

        public Simulation()
        {
            Routes = TsDataObject.Routes; 
            GenerateVehicleFleet(1); // Generates a vehicle for each route
        }

        public override void AssignVehicleServices()
        {
            var ind = 0;
            foreach (var route in Routes) // Each vehicle is responsible for a route
            {
                if (ind > VehicleFleet.Count - 1) //if it reaches the last vehicle breaks the loop
                {
                    break;
                }
                
                var v = VehicleFleet[ind];
                var allRouteServices = new List<Service>(); //ADD this to Route object instead of here!
                foreach (var trip in route.Trips)
                {
                    foreach (var startTime in trip.StartTimes)
                    {
                        var service = new Service(trip, startTime);
                        allRouteServices.Add(service);
                    }

                }

                allRouteServices = allRouteServices.OrderBy(s => s.StartTime).ToList(); //Orders services by start_time
                
                    foreach (var service in allRouteServices)
                    {
                        if (v.Services.FindAll(s => Math.Abs(s.StartTime - service.StartTime) < 60 * 30).Count == 0
                        ) //if there is no service where the start time is lower than 30mins (1800seconds)
                        {
                            v.AddService(service); //Adds the service
                            ConsoleLogger.Log(service + "- start_time:" + TimeSpan.FromSeconds(service.StartTime));
                        }
                    }

                if (v.Services.Count > 0)
                {
                    ConsoleLogger.Log(this.ToString() + v.Services.Count + " Services ("+Routes.Find(r=>r.Trips.Contains(v.Services[0].Trip))+") were assigned to Vehicle " +
                                      v.Id + ".");
                }

                ind++;

            }
        }

        public override void GenerateVehicleServiceEvents()
        {
            foreach (var vehicle in VehicleFleet)
            {
                if (vehicle.Services.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.ServiceIterator.Reset();
                    while (vehicle.ServiceIterator.MoveNext()) //Iterates over each service
                    {
                        var events =
                            EventGenerator.GenerateRouteEvents(vehicle, vehicle.ServiceIterator.Current.StartTime); //Generates the arrive/depart events for that service
                        AddEvent(events);
                    }

                    vehicle.ServiceIterator.Reset(); //resets iterator
                    vehicle.ServiceIterator.MoveNext(); //initializes the iterator at the first service
                }
            }
            SortEvents();
        }
 
        public override void PrintSolution()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(Environment.CurrentDirectory, @"Logger/sim_solution.txt"));
            Logger.Logger myFileLogger = new Logger.Logger(fileRecorder);
            List<string> toPrintList = new List<string>();
            toPrintList.Add(this.ToString()+"Total number of events handled: "+Events.FindAll(e=>e.AlreadyHandled == true).Count+" out of "+Events.Count+".");
            toPrintList.Add(this.ToString()+"Vehicle Fleet Size: "+VehicleFleet.Count+" vehicle(s).");

            foreach (var vehicle in VehicleFleet)
            {
                toPrintList.Add("-----------------------------------------------------");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                var totalServices = vehicle.Services.FindAll(s => s.IsDone).Count;
                toPrintList.Add("Total number of completed services:" + totalServices +" out of "+vehicle.Services.Count);
                if (vehicle.Services.Count > 0)
                {
                    toPrintList.Add("Service route:" + Routes.Find(r => r.Trips.Contains(vehicle.Services[0].Trip)));
                    toPrintList.Add("Service Trips:");
                    List<Trip> serviceRoutes = new List<Trip>();
                    foreach (var service in vehicle.Services)
                    {
                        if (!serviceRoutes.Contains(service.Trip))
                        {
                            serviceRoutes.Add(service.Trip);
                            var completedServices = vehicle.Services.FindAll(s => s.Trip == service.Trip && s.IsDone);
                            toPrintList.Add(" - " + service.Trip.ToString() + ", Number of services completed:" +
                                            completedServices.Count);

                        }
                    }
                }

                //For debug purposes---------------------------------------------------------------------------
                if (vehicle.Services.Count != vehicle.Services.FindAll(s => s.IsDone).Count)
                {
                    toPrintList.Add("Services Completed:");
                    foreach (var service in vehicle.Services)
                    {
                        if (service.IsDone)
                        {
                            toPrintList.Add(" - " + service + " - [" +
                                            TimeSpan.FromSeconds(service.StartTime).ToString() + " - " +
                                            TimeSpan.FromSeconds(service.EndTime) + "]");
                        }
                    }
                }

                if (vehicle.Customers.Count > 0)
                {
                    toPrintList.Add("Number of customers inside:" + vehicle.Customers.Count);
                    foreach (var cust in vehicle.Customers)
                    {
                        toPrintList.Add(
                            cust + "Pickup:" + cust.PickupDelivery[0] + "Delivery:" + cust.PickupDelivery[1]);
                    }
                }

                //End of debug purposes---------------------------------------------------------------------------
                
                if (vehicle.ServiceIterator != null)
                {
 
                    var completedServices = vehicle.Services.FindAll(s => s.IsDone);//Finds all the completed services
                    var totalCustomerRideTime = completedServices.Sum(s => s.ServicedCustomers.Sum(c => c.RideTime));
                    var avgCustomerRideTime =
                        totalCustomerRideTime / completedServices.Sum(s => s.TotalServicedRequests);

                    toPrintList.Add("Total Distance Traveled: "+Math.Round(completedServices.Sum(s=>s.TotalDistanceTraveled))+" meters.");
                    toPrintList.Add(" ");
                    toPrintList.Add("Metrics (per service):");
                    toPrintList.Add("Average route duration:" + Math.Round(TimeSpan.FromSeconds(completedServices.Average(s=>s.RouteDuration)).TotalMinutes) + " minutes.");
                    toPrintList.Add("Average number of requests:" + completedServices.Average(s=>s.TotalRequests));
                    var avgServicedRequests = completedServices.Average(s => s.TotalServicedRequests);
                    toPrintList.Add("Average number of serviced requests:" + avgServicedRequests);
                    toPrintList.Add("Average number of denied requests:" + completedServices.Average(s=>s.TotalDeniedRequests));
                    double avgServicedRequestRatio = Convert.ToDouble(avgServicedRequests) /
                                                     Convert.ToDouble(completedServices.Average(s=>s.TotalRequests));
                    double avgDeniedRequestRatio = 1 - (avgServicedRequestRatio);
                    toPrintList.Add("Average percentage of serviced requests:" + avgServicedRequestRatio * 100 + "%");
                    toPrintList.Add("Average percentage of denied requests:" + avgDeniedRequestRatio * 100 + "%");
                    toPrintList.Add("Average service time (per customer):" + Math.Round((decimal) avgCustomerRideTime) +
                                    " seconds");
                    toPrintList.Add("Average distance traveled: "+Math.Round(completedServices.Average(s=>s.TotalDistanceTraveled))+" meters.");
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
            Random rnd = new Random();
            //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS---------------------------------------
            if (evt.Category == 0 && evt is VehicleStopEvent vseEvt)
            {
                    var baseTime = evt.Time;
                    List<Event> customerLeaveVehicleEvents = EventGenerator.GenerateCustomerLeaveVehicleEvents(vseEvt.Vehicle,vseEvt.Stop, baseTime); //Generates customer leave vehicle event
                    var lastInsertedLeaveTime = 0;
                    var lastInsertedEnterTime = 0;
                    if (customerLeaveVehicleEvents.Count > 0)
                    {     
                        lastInsertedLeaveTime = customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time;
                    }
                    else
                    {
                        lastInsertedLeaveTime = baseTime;
                    }

                    List<Event> customerEnterVehicleEvents = null;
                    if (vseEvt.Vehicle.ServiceIterator.Current != null)
                    {
                            customerEnterVehicleEvents =
                            EventGenerator.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle, vseEvt.Stop,
                                lastInsertedLeaveTime, rnd.Next(1,7));
                        if (customerEnterVehicleEvents.Count > 0)
                        {
                            lastInsertedEnterTime = customerEnterVehicleEvents[customerEnterVehicleEvents.Count - 1].Time;
                        }
                    }

                    var biggestInsertedTime = 0;
                    var timeAdded = 0;
                    if (lastInsertedEnterTime < lastInsertedLeaveTime) //Verifies which of the inserted time is bigger and calculates the time added
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
                {
                    foreach (var ev in Events)
                    {
                        if (ev.Time > baseTime) 
                        {
                            if (ev is VehicleStopEvent vseEv)
                            {
                                if (vseEv.Vehicle == vseEvt.Vehicle && vseEv.Service == vseEvt.Service) 
                                {
                                        ev.Time = ev.Time + timeAdded; //adds the added time to all the next events of that service for that vehicle
                                }
                            }
                        }
                    }
                }
                var evtEnterAdded = AddEvent(customerEnterVehicleEvents);
                var evtLeaveAdded = AddEvent(customerLeaveVehicleEvents);
                if (evtLeaveAdded || evtEnterAdded)//if any of these events were added (true), it sorts the event list
                {
                    SortEvents();
                }
            }
            //END OF INSERTION OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            var pickup = TsDataObject.Stops[rnd.Next(0, TsDataObject.Stops.Count)];
            var delivery = pickup;
            while (pickup == delivery)
            {
                delivery = TsDataObject.Stops[rnd.Next(0, TsDataObject.Stops.Count)];
            }

            var eventReq = EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickup, delivery); //Generates a pickup and delivery customer request
            if (eventReq != null) //if eventReq isn't null, add the event and then sorts the events list
            {
                AddEvent(eventReq);
                SortEvents();
            }
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------

        }





        public override void Handle(Event evt)
        { 
            evt.Treat();
            TotalEventsHandled++;
            FileLogger.Log(evt.GetMessage());
        }
    }
}
