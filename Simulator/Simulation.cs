using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
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
        

        private int _totalEventsHandled;

        private EventGenerator _eventGenerator;

        public Simulation()
        {
            Routes = _tsDataObject.Routes;
            _eventGenerator = new EventGenerator();   
            _totalEventsHandled = 0;
        }

        public override void GenerateVehicleFleet()
        {
            var c = 0;
            foreach (var route in Routes) // Generates a vehicle for each urban route
            {
                if (c >= 2)
                {
                    break;
                }
                    var v = new Vehicle(35, 20);
                    v.StopsGraph = _stopsGraph;
                    Service service = new Service(route.Trips[0],route.Trips[0].StartTimes[0]);
                    v.AddService(service);
                    Service service1 = new Service(route.Trips[0],route.Trips[0].StartTimes[1]);
                    v.AddService(service1);
                    //foreach (var routeTrip in route.Trips)
                    //{
                    //    v.StopsIterator.AddTrip(routeTrip);
                    //}
                    VehicleFleet.Add(v);
                    c++;
                         
            }

            foreach (var vehicle in VehicleFleet)
            {
                while (vehicle.NextService())
                {
                    Console.WriteLine(vehicle.CurrentService.StopsIterator.Trip +" start_Time:"+vehicle.CurrentService.StartTime);
                    var events = _eventGenerator.GenerateRouteEvents(vehicle, vehicle.CurrentService.StartTime);
                    AddEvent(events);
                }
                vehicle.ResetService();


            } 
            SortEvents();
        }
 
        public override void PrintMetrics()
        {
            IRecorder fileRecorder = new FileRecorder(@"C:\Users\Diogo Silva\Desktop\Simulation\sim_metrics.txt");
            Logger.Logger myFileLogger = new Logger.Logger(fileRecorder);
            List<string> toPrintList = new List<string>();
            toPrintList.Add("-----------------------------------------------------");
            toPrintList.Add("Simulation finished");
            toPrintList.Add("Total number of events handled:"+_totalEventsHandled+" out of "+Events.Count);
            toPrintList.Add("Vehicle Fleet Size:"+VehicleFleet.Count+" vehicle(s).");

            foreach (var vehicle in VehicleFleet)
            {
              
                toPrintList.Add(" ");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Current " + Routes.Find(r=>r.Trips.Contains(vehicle.CurrentService.StopsIterator.Trip)).ToString() + " - "+vehicle.CurrentService.StopsIterator.Trip);
                toPrintList.Add("Number of services:"+vehicle.Services.Count);
            
                toPrintList.Add("Number of customers inside:"+vehicle.Customers.Count);
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickupDelivery[0]+"delivery:"+cust.PickupDelivery[1]);
                }
                toPrintList.Add("Metrics:");
                toPrintList.Add("Number of serviced trips:" + vehicle.Services.FindAll(s=>s.HasBeenServiced).Count);
                foreach (var service in vehicle.Services)
                {
                    if (service.HasBeenServiced)
                    {
                        TimeSpan startTime = TimeSpan.FromSeconds(service.StartTime);
                        TimeSpan endTime = TimeSpan.FromSeconds(service.EndTime);
                        toPrintList.Add("Service " + service.StopsIterator.Trip + " - Start_time: " + startTime.ToString()+ " - End_time: " + endTime);
                        toPrintList.Add("Total number of requests:" + service.TotalRequests);
                        toPrintList.Add("Total number of serviced requests:" + service.TotalServicedRequests);
                        toPrintList.Add("Total number of denied requests:" + (service.TotalDeniedRequests));
                        double ratioServicedRequest = Convert.ToDouble(service.TotalServicedRequests) / Convert.ToDouble(service.TotalRequests);
                        toPrintList.Add("Percentage of serviced requests: " + ratioServicedRequest * 100 + "%");
                        double ratioDeniedRequests = 1 - (ratioServicedRequest);
                        toPrintList.Add("Percentage of denied request: " + ratioDeniedRequests * 100 + "%");
                        var totalServiceTime = 0;
                        foreach (var customer in service.ServicedCustomers)
                        {
                            totalServiceTime = totalServiceTime + customer.ServiceTime;
                        }
                        var avgServiceTime = totalServiceTime / vehicle.CurrentService.TotalServicedRequests;
                        toPrintList.Add("Average service time (per customer):" + avgServiceTime + " seconds.");
                    }
                }
            }

            foreach (var metric in toPrintList)
            {
                 myFileLogger.Log(metric);
                _consoleLogger.Log(metric);
            }
        }


        public override void Append(Event evt)
        {
            if (evt.Category == 0 && evt is VehicleStopEvent vseEvt)
            {
                    var baseTime = evt.Time;
                    List<Event> customerLeaveEvents = _eventGenerator.GenerateCustomerLeaveVehicleEvents(vseEvt.Vehicle,vseEvt.Stop, baseTime);
                    var lastInsertedLeaveTime = 0;
                    var lastInsertedEnterTime = 0;
                    if (customerLeaveEvents.Count > 0)
                    {     
                        lastInsertedLeaveTime = customerLeaveEvents[customerLeaveEvents.Count - 1].Time;
                    }
                    else
                    {
                        lastInsertedLeaveTime = baseTime;
                    }
                    
                    List<Event> customerEnterEvents = _eventGenerator.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle,vseEvt.Stop, lastInsertedLeaveTime, 1);
                    if (customerEnterEvents.Count > 0)
                    {
                        lastInsertedEnterTime = customerEnterEvents[customerEnterEvents.Count - 1].Time;
                    }

                    var biggestInsertedTime = 0;
                    var timeAdded = 0;
                    if (lastInsertedEnterTime < lastInsertedLeaveTime)
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
                    var count = 0;
                    foreach (var ev in Events)
                    {
                        if (ev.Time > baseTime) 
                        {
                            if (ev is VehicleStopEvent vseEv)
                            {
                                if (vseEv.Vehicle == vseEvt.Vehicle)
                                {
                                    ev.Time = ev.Time + timeAdded;//adds the added time to all the next events for that vehicle
                                    count++;
                                }
                            }
                        }
                    }
                }

                var evtEnterAdded = AddEvent(customerEnterEvents);
                var evtLeaveAdded = AddEvent(customerLeaveEvents);
                if (evtLeaveAdded || evtEnterAdded)
                {
                    SortEvents();
                }
            }

            //NEED TO CHANGE THIS!-----------------
            Random rnd = new Random();
            var pickup = Routes[0].Trips[0].Stops[rnd.Next(0, Routes[0].Trips[0].Stops.Count)];
            var delivery = pickup;
            while (pickup == delivery)
            {
                delivery = Routes[0].Trips[0].Stops[rnd.Next(0, Routes[0].Trips[0].Stops.Count)];
            }

            var eventReq = _eventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickup, delivery); 
            if (eventReq != null)
            {
                AddEvent(eventReq);
                SortEvents();
            }
            // -------------------------------

        }
            
        
        
          

        public override void Handle(Event evt)
        { 
            evt.Treat();
            _fileLogger.Log(evt.GetMessage());
            _totalEventsHandled++;
        }
    }
}
