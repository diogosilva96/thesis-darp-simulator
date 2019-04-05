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


        private int _totalEventsHandled;

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
            Trips = trips;
            VehicleFleet = new List<Vehicle>();
            Events = new List<Event>();
            _stopsGraph = stopsGraph;
            GenerateVehicleFleet(1);
            EventGenerator eg = new EventGenerator();

            Dictionary<Vehicle,int> vehicleStartTimeDict = new Dictionary<Vehicle, int>(); 
            vehicleStartTimeDict.Add(VehicleFleet[0],0);
            //vehicleStartTimeDict.Add(VehicleFleet[1],300);//Problema com 1 ou mais veiculos verificar!!

            foreach (var vehicleStartTime in vehicleStartTimeDict) //Mudar!
            {
                    var events = eg.GenerateRouteEvents(vehicleStartTime.Key, vehicleStartTime.Value);
                    AddEvent(events);
            }
            Events.Sort();
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
                v.StopsGraph = _stopsGraph;
                v.Router.Trip = Trips[0]; //mudar
                VehicleFleet.Add(v);
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
            foreach (var vehicle in VehicleFleet)
            {
              
                toPrintList.Add(" ");
                toPrintList.Add("Vehicle " + vehicle.Id + ":");
                toPrintList.Add("Average speed:" + vehicle.Speed + " km/h.");
                toPrintList.Add("Capacity:" + vehicle.Capacity + " seats.");
                toPrintList.Add("Current " + vehicle.Router.Trip);
                toPrintList.Add("number of customers inside:");
                foreach (var cust in vehicle.Customers)
                {
                    toPrintList.Add(cust+"pickup:"+cust.PickUpStop+"dropoff:"+cust.DropOffStop);
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
                    EventGenerator eg = new EventGenerator();
                    var baseTime = evt.Time;
                    List<Event> customerLeaveEvents = eg.GenerateCustomerLeaveVehicleEvents(vseEvt.Vehicle,vseEvt.Stop, baseTime);
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
                    
                    List<Event> customerEnterEvents = eg.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle,vseEvt.Stop, lastInsertedLeaveTime, 1);
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
                        if (ev.Time > baseTime) //adds the added event times to all the next events
                        {
                            if (ev is VehicleStopEvent vseEv)
                            {
                                if (vseEv.Vehicle == vseEvt.Vehicle)
                                {
                                    ev.Time = ev.Time + timeAdded;
                                    count++;
                                }
                            }
                            //ev.Time = ev.Time + timeAdded;
                            //count++;
                        }
                    }
                }
                AddEvent(customerEnterEvents);
                AddEvent(customerLeaveEvents);

            }


            Events.Sort();
            }
            
        
  
          

        public override void Handle(Event evt)
        { 
            _fileLogger.Log(evt.GetMessage());
            evt.Treat();
            _totalEventsHandled++;
        }
    }
}
