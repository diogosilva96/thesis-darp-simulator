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
                if (c >= 1)
                {
                    break;
                }
                    
                                var v = new Vehicle(17, 20);
                                v.StopsGraph = _stopsGraph;
                                for (int i = 0; i < 3; i++)
                                {     
                                    v.AddService(new Service(route.Trips[0], route.Trips[0].StartTimes[i]));
                                    v.AddService(new Service(route.Trips[1], route.Trips[1].StartTimes[i]));
                                }

                                //foreach (var routeTrip in route.Trips)
                                //{
                                //   Console.WriteLine(routeTrip);
                                //   foreach (var startTime in routeTrip.StartTimes)
                                //   {
                                //       Console.WriteLine(TimeSpan.FromSeconds(startTime));
                                //   }
                                   
                                //}
                                VehicleFleet.Add(v);
                                c++;                                   
            }

            foreach (var vehicle in VehicleFleet)
            {
                vehicle.ServiceIterator.Reset();
                while (vehicle.ServiceIterator.MoveNext())
                {
                    var events = _eventGenerator.GenerateRouteEvents(vehicle, vehicle.ServiceIterator.Current.StartTime);
                    AddEvent(events);
                }

                vehicle.ServiceIterator.Reset();
                vehicle.ServiceIterator.MoveNext();


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
                vehicle.ServiceIterator.Reset();
                toPrintList.Add("-----------------------------------------------------");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Number of services:"+vehicle.Services.Count);
                toPrintList.Add("Number of serviced services:"+vehicle.Services.FindAll(s=>s.IsDone).Count);
            
                toPrintList.Add("Number of customers inside:"+vehicle.Customers.Count);
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickupDelivery[0]+"delivery:"+cust.PickupDelivery[1]);
                }
                toPrintList.Add(" ");
                vehicle.ServiceIterator.Reset();
                while(vehicle.ServiceIterator.MoveNext())
                {
                    if (vehicle.ServiceIterator.Current.IsDone)
                    {
                        toPrintList.Add("Current " + Routes.Find(r => r.Trips.Contains(vehicle.ServiceIterator.Current.Trip)).ToString());
                        toPrintList.Add(vehicle.ServiceIterator.Current.ToString()+", Start_Time:" + TimeSpan.FromSeconds(vehicle.ServiceIterator.Current.StartTime).ToString()+ ", End_Time:" + TimeSpan.FromSeconds(vehicle.ServiceIterator.Current.EndTime).ToString());
                        toPrintList.Add("Metrics:");
                        toPrintList.Add("Total service duration:"+vehicle.ServiceIterator.Current.RouteDuration+" seconds");
                        toPrintList.Add("Total number of requests:" + vehicle.ServiceIterator.Current.TotalRequests);
                        toPrintList.Add("Total number of serviced requests:" + vehicle.ServiceIterator.Current.TotalServicedRequests);
                        toPrintList.Add("Total number of denied requests:" + (vehicle.ServiceIterator.Current.TotalDeniedRequests));
                        double ratioServicedRequest = Convert.ToDouble(vehicle.ServiceIterator.Current.TotalServicedRequests) / Convert.ToDouble(vehicle.ServiceIterator.Current.TotalRequests);
                        toPrintList.Add("Percentage of serviced requests: " + ratioServicedRequest * 100 + "%");
                        double ratioDeniedRequests = 1 - (ratioServicedRequest);
                        toPrintList.Add("Percentage of denied request: " + ratioDeniedRequests * 100 + "%");
                        var totalServiceTime = 0;
                        foreach (var customer in vehicle.ServiceIterator.Current.ServicedCustomers)
                        {
                            totalServiceTime = totalServiceTime + customer.RideTime;
                        }
                        var avgServiceTime = totalServiceTime / vehicle.ServiceIterator.Current.TotalServicedRequests;
                        toPrintList.Add("Average service time (per customer):" + avgServiceTime + " seconds.");
                        toPrintList.Add("");
                    }

                    
                }
                vehicle.ServiceIterator.Reset();
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

                    List<Event> customerEnterEvents = null;
                    if (vseEvt.Vehicle.ServiceIterator.Current != null)
                    {
                            customerEnterEvents =
                            _eventGenerator.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle, vseEvt.Stop,
                                lastInsertedLeaveTime, 2);
                        if (customerEnterEvents.Count > 0)
                        {
                            lastInsertedEnterTime = customerEnterEvents[customerEnterEvents.Count - 1].Time;
                        }
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
                                        ev.Time =
                                            ev.Time +
                                            timeAdded; //adds the added time to all the next events of that service for that vehicle
                                    }
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
