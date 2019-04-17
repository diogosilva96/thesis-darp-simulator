using System;
using System.Collections.Generic;
using System.IO;
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
                if (c >= 4)
                {
                    break;
                }
                if (route.UrbanRoute)
                {
                    var v = new Vehicle(35, 20);
                    v.StopsGraph = _stopsGraph;
                    foreach (var routeTrip in route.Trips)
                    {
                        v.Router.AddTrip(routeTrip);
                    }

                    VehicleFleet.Add(v);
                    c++;
                }

               
            }

            foreach (var vehicle in VehicleFleet)
            {
                
                if (vehicle.Router.NextTrip())
                {
                    var events = _eventGenerator.GenerateRouteEvents(vehicle, vehicle.Router.CurrentTrip.StartTime);
                    AddEvent(events);
                }

                vehicle.Router.InitCurrentTrip();


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
            var i = 0;
            foreach (var vehicle in VehicleFleet)
            {
              
                toPrintList.Add(" ");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Current " + Routes.Find(r=>r.Trips.Contains(vehicle.Router.CurrentTrip)).ToString() + " - "+vehicle.Router.CurrentTrip);
                //var startTime = TimeSpan.FromSeconds(vehicle.Router.StartEndTimeWindows[0,0]);
                //var endTime = TimeSpan.FromSeconds(vehicle.Router.StartEndTimeWindows[0,1]);
                //toPrintList.Add("Service start time:" + startTime.ToString());
                //toPrintList.Add("Service end time:"+endTime.ToString());
                //adicionar service end time?
                toPrintList.Add("Number of route trips:"+vehicle.Router.Trips.Count);
                //foreach (var trip in vehicle.Router.Trips)
                //{
                //   toPrintList.Add(trip.ToString());
                //}
                toPrintList.Add("Number of customers inside:"+vehicle.Customers.Count);
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickupDelivery[0]+"delivery:"+cust.PickupDelivery[1]);
                }
                toPrintList.Add("Metrics:");
                toPrintList.Add("Number of serviced trips:"+vehicle.Router.ServicedTrips.Count);
                toPrintList.Add( "Total number of requests:" + vehicle.TotalRequests);
                toPrintList.Add("Total number of serviced requests:" + vehicle.TotalServicedRequests);
                toPrintList.Add( "Total number of denied requests:" + (vehicle.TotalDeniedRequests));
                double ratioServicedRequest = Convert.ToDouble(vehicle.TotalServicedRequests) / Convert.ToDouble(vehicle.TotalRequests);
                toPrintList.Add("Percentage of serviced requests: " + ratioServicedRequest * 100 + "%");
                double ratioDeniedRequests = 1 - (ratioServicedRequest);
                toPrintList.Add("Percentage of denied request: " + ratioDeniedRequests * 100 + "%");
                var totalServiceTime = 0;
                foreach (var customer in vehicle.ServicedCustomers)
                {
                    totalServiceTime = totalServiceTime + customer.ServiceTime;
                }

                var avgServiceTime = totalServiceTime / vehicle.TotalServicedRequests;
                toPrintList.Add("Average service time (per customer):" + avgServiceTime+" seconds.");
                i++;
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

                Random rnd = new Random();
                var pickup = Routes[0].Trips[0].Stops[rnd.Next(0, Routes[0].Trips[0].Stops.Count)];
                var delivery = pickup;
                while (pickup == delivery)
                {
                    delivery = Routes[0].Trips[0].Stops[rnd.Next(0, Routes[0].Trips[0].Stops.Count)];
                }
                
                var eventReq = _eventGenerator.GenerateCustomerRequestEvent(evt.Time+1,pickup ,delivery); //Mudar
                if (eventReq != null)
                {
                    AddEvent(eventReq);
                    SortEvents();
                }
        }
            
        
  
          

        public override void Handle(Event evt)
        { 
            evt.Treat();
            _fileLogger.Log(evt.GetMessage());
            _totalEventsHandled++;
        }
    }
}
