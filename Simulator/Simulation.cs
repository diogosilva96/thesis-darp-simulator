using System;
using System.Collections.Generic;
using System.IO;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;


namespace Simulator
{
    public class Simulation:AbstractSimulation
    {


        public DirectedGraph<Stop,double> StopsGraph { get; internal set; }


        private Logger.Logger _consoleLogger;

        private Logger.Logger _fileLogger;

        private List<Trip> Trips { get; }

        private DirectedGraph<Stop, double> _stopsGraph;

        private List<int> _startTimes;

        private int _totalEventsHandled;

        private EventGenerator _eventGenerator;

        public Simulation(List<Trip> trips, DirectedGraph<Stop,double> stopsGraph)
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
            string loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }

            IRecorder fileRecorder = new FileRecorder(Path.Combine(loggerPath,@"sim.txt"));
            _fileLogger = new Logger.Logger(fileRecorder);
            _eventGenerator = new EventGenerator();
            Trips = trips;
            Events = new List<Event>();
            _stopsGraph = stopsGraph;
            VehicleFleet = new List<Vehicle>();
            _startTimes = new List<int>();
            GenerateVehicleFleet(2);
            SortEvents();
            _totalEventsHandled = 0;
        }

        public void GenerateVehicleFleet(int numVehicles)
        {

            Random rand = new Random();

            for (int i = 0; i < numVehicles; i++)
            {
                var speed = rand.Next(30, 51);
                var capacity = rand.Next(10,26);
                var v = new Vehicle(speed, capacity);
                int serviceStartTime = rand.Next(0,300);
                _startTimes.Add(serviceStartTime);
                v.StopsGraph = _stopsGraph;
                v.Router.Trip = Trips[0];
                VehicleFleet.Add(v);
            }


            for (int ind = 0; ind < VehicleFleet.Count; ind++)
            {
                var events = _eventGenerator.GenerateRouteEvents(VehicleFleet[ind], _startTimes[ind]);
                AddEvent(events);
            }
        
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
                toPrintList.Add("Current " + vehicle.Router.Trip);
                var t = TimeSpan.FromSeconds(_startTimes[i]);
                toPrintList.Add("Service start time:" + t.ToString());
                //adicionar service end time?
                toPrintList.Add("number of customers inside:"+vehicle.Customers.Count);
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickupDelivery[0]+"dropoff:"+cust.PickupDelivery[1]);
                }
                toPrintList.Add("Metrics:");
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
                AddEvent(customerEnterEvents);
                AddEvent(customerLeaveEvents);
                SortEvents();
            }

                var eventReq = _eventGenerator.GenerateCustomerRequestEvent(evt.Time+1, Trips[0].Stops[1],Trips[0].Stops[2]); //Mudar
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
