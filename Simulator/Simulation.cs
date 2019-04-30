using System;
using System.Collections.Generic;
using System.IO;
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
            GenerateVehicleFleet(3); // Generates a vehicle for each route
        }

        public override void AssignVehicleServices()
        {
            var ind = 0;
            foreach (var route in Routes) // Each vehicle is responsible for a route
            {
                if (ind > VehicleFleet.Count - 1)
                {
                    break;
                }

                var v = VehicleFleet[ind];

                foreach (var trip in route.Trips)
                {
                    foreach (var startTime in trip.StartTimes)
                    {
                        var service = new Service(trip, startTime);
                        if (v.Services.FindAll(s => Math.Abs(s.StartTime - service.StartTime)<1800).Count == 0) //if there is no service where the start time is lower than 30mins (1800seconds)
                        {
                            v.AddService(service); //adds the service
                        }
                    }
                }

                ind++;

            }
        }

        public override void GenerateVehicleServiceEvents()
        {
            foreach (var vehicle in VehicleFleet)
            {
                vehicle.ServiceIterator.Reset();
                while (vehicle.ServiceIterator.MoveNext())
                {
                    var events = EventGenerator.GenerateRouteEvents(vehicle, vehicle.ServiceIterator.Current.StartTime);
                    AddEvent(events);
                }
                vehicle.ServiceIterator.Reset();
                vehicle.ServiceIterator.MoveNext();
            }
            SortEvents();
        }
 
        public override void PrintSolution()
        {
            IRecorder fileRecorder = new FileRecorder(@"C:\Users\Diogo Silva\Desktop\Simulation\sim_metrics.txt");
            Logger.Logger myFileLogger = new Logger.Logger(fileRecorder);
            List<string> toPrintList = new List<string>();
            toPrintList.Add("-----------------------------------------------------");
            toPrintList.Add("Simulation finished");
            toPrintList.Add("Total number of events handled:"+TotalEventsHandled+" out of "+Events.Count);
            toPrintList.Add("Vehicle Fleet Size:"+VehicleFleet.Count+" vehicle(s).");

            foreach (var vehicle in VehicleFleet)
            {
                vehicle.ServiceIterator.Reset();
                toPrintList.Add("-----------------------------------------------------");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Number of services:"+vehicle.Services.Count);
                var totalServices = vehicle.Services.FindAll(s => s.IsDone).Count;
                toPrintList.Add("Number of serviced services:"+totalServices);
                toPrintList.Add("Service route:"+Routes.Find(r=>r.Trips.Contains(vehicle.Services[0].Trip)));
                toPrintList.Add("Service Trips:");
                List<Trip> serviceRoutes = new List<Trip>();
                foreach (var service in vehicle.Services)
                {
                    if (!serviceRoutes.Contains(service.Trip))
                    {
                        serviceRoutes.Add(service.Trip);
                        toPrintList.Add(service.Trip.ToString()+", Number of services completed:"+vehicle.Services.FindAll(s=>s.Trip == service.Trip && s.IsDone).Count);
                    }
                }
            
                toPrintList.Add("Number of customers inside:"+vehicle.Customers.Count);
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickupDelivery[0]+"delivery:"+cust.PickupDelivery[1]);
                }
                toPrintList.Add(" ");
                vehicle.ServiceIterator.Reset();
                var totalRouteDuration = 0;
                var totalRequests = 0;
                var totalServicedRequests = 0;
                var totalDeniedRequests = 0;
                var totalAverageServiceTime = 0;
                
                while(vehicle.ServiceIterator.MoveNext())
                {
                    if (vehicle.ServiceIterator.Current.IsDone)
                    {
                        totalRouteDuration = totalRouteDuration + vehicle.ServiceIterator.Current.RouteDuration;
                        totalRequests = totalRequests + vehicle.ServiceIterator.Current.TotalRequests;
                        totalServicedRequests =
                            totalServicedRequests + vehicle.ServiceIterator.Current.TotalServicedRequests;
                        totalDeniedRequests = totalDeniedRequests + vehicle.ServiceIterator.Current.TotalDeniedRequests;
                        var totalServiceTime = 0;
                        foreach (var customer in vehicle.ServiceIterator.Current.ServicedCustomers)
                        {
                            totalServiceTime = totalServiceTime + customer.RideTime;
                        }
                        var avgServiceTime = totalServiceTime / vehicle.ServiceIterator.Current.TotalServicedRequests;
                        totalAverageServiceTime = totalAverageServiceTime + avgServiceTime;
                    }          
                }
                vehicle.ServiceIterator.Reset();
                toPrintList.Add("Metrics (averages per service):");
                toPrintList.Add("Average route duration:"+totalRouteDuration/totalServices+" seconds");
                toPrintList.Add("Average number of requests:" + totalRequests / totalServices);
                toPrintList.Add("Average number of serviced requests:"+totalServicedRequests / totalServices);                
                toPrintList.Add("Average number of denied requests:"+totalDeniedRequests / totalServices);
                double avgServicedRequestRatio = Convert.ToDouble(totalServicedRequests / totalServices) /
                                            Convert.ToDouble(totalRequests / totalServices);
                double avgDeniedRequestRatio = 1 - (avgServicedRequestRatio);
                toPrintList.Add("Average percentage of serviced requests:"+avgServicedRequestRatio *100+"%");
                toPrintList.Add("Average percentage of denied requests:" + avgDeniedRequestRatio * 100 + "%");
                toPrintList.Add("Average service time (per customer):"+totalAverageServiceTime/totalServices+" seconds");
            }

            foreach (var metric in toPrintList)
            {
                 myFileLogger.Log(metric);
                ConsoleLogger.Log(metric);
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
                                if (vseEv.Vehicle == vseEvt.Vehicle) 
                                {
                                    if (vseEv.Service == vseEvt.Service)
                                    {
                                        ev.Time = ev.Time + timeAdded; //adds the added time to all the next events of that service for that vehicle
                                    }
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
