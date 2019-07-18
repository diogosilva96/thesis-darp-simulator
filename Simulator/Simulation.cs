using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Simulator.Events;
using Simulator.GraphLibrary;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private readonly List<Route> _routes;

        private readonly Logger.Logger _eventLogger;

        private readonly Logger.Logger _validationsLogger;

        private int _validationsCounter;

        private int _vehicleSpeed = 30;

        private int _vehicleCapacity = 53;




        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            _routes = RoutesDataObject.Routes;
            ConfigSimulation(); // configures the simulation
            _validationsCounter = 1;
        }

        public void ConfigSimulation()
        {
            var numOptions = 2;
            ConsoleLogger.Log("Please Select one of the options:");
            ConsoleLogger.Log("1 - Standard Bus route simulation (static routing)");
            ConsoleLogger.Log("2 - Single Bus route flexible simulation");
            int key = 0;
            wrongKeyLabel:
            try
            {
                key = int.Parse(Console.ReadLine());
                if (key <= 0 && key > numOptions)
                {
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {

            }

            switch (key)
            {
                case 1: StandardBusRouteOption();
                    break;
                case 2:SingleBusRouteFlexibleOption();
                    break;
                default: StandardBusRouteOption();
                    break;
            }
        }

        public void SingleBusRouteFlexibleOption()
        {
            ConsoleLogger.Log("Please select the route that you wish to simulate:");
            int ind = 0;
            Route route;
            foreach (var r in _routes)
            {
                ConsoleLogger.Log(ind+"-"+r.ToString());
                ind++;
            }
            insertLabel:
            try
            {
                route = _routes[int.Parse(Console.ReadLine())];
            }
            catch (Exception)
            {
                ConsoleLogger.Log(this.ToString() + "Error Wrong input, please insert integer numbers for the start and end hour.");
                goto insertLabel;
            }
            Random rand = new Random();
            var stopsNetworkGraph = new StopsNetworkGraphLoader(RoutesDataObject.Stops, RoutesDataObject.Routes);
            stopsNetworkGraph.LoadGraph();
            var stopsGraph = stopsNetworkGraph.StopsGraph;
            var service = route.AllRouteServices[rand.Next(0,route.AllRouteServices.Count-1)];
            var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, stopsGraph);
            v.AddService(service); //Adds the service to the vehicle
            VehicleFleet.Add(v);
            GenerateVehicleServiceEvents();

            var serviceStops = v.ServiceIterator.Current.Trip.Stops;
            var stopList = new List<Stop>();
            var depot = RoutesDataObject.Stops.Find(s => s.Id == 2183);
            stopList.Add(depot);
            // Initial route creation using long array instead of stop list
            long[] initialRoute = new long[serviceStops.Count];
            int index = 0;
            foreach (var stop in serviceStops)
            {
                if (!stopList.Contains(stop))
                {
                    stopList.Add(stop);
                }
                initialRoute[index] =stopList.IndexOf(stop);
                index++;
            }
            // Pickup and deliveries definition (to make the route flexible)
            var numExecutions = 3;

            int[][] pickupsDeliveriesStopId =
            {
                new int[] {438, 2430},
                new int[] {1106, 1359},
                new int[] {2270, 2018},
                new int[] {2319, 1523},
                new int[] {430, 1884},
                new int[] {399, 555},
            };
            int[][] pickupsDeliveries = new int[pickupsDeliveriesStopId.Length][];
            // transforms from stop id into index of distance matrix
            int insertCounter = 0;
            //Build stopList based on pickup and delivery in order to later build the distance matrix for only the necessary stops
            foreach (var pickupDelivery in pickupsDeliveriesStopId)
            {
                for (int i = 0; i < 2; i++)
                {
                    var stop = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[i]);

                    if (!stopList.Contains(stop))
                    {
                        stopList.Add(stop);
                    }
                }

                var pickup = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[0]);
                var delivery = RoutesDataObject.Stops.Find(s => s.Id == pickupDelivery[1]);
                var pickupDeliveryInd = new int[] { stopList.IndexOf(pickup), stopList.IndexOf(delivery) };
                pickupsDeliveries[insertCounter] = pickupDeliveryInd;
                insertCounter++;
            }
           
            ConsoleLogger.Log("Total stops:" + stopList.Count);

            //GOOGLE OR TOOLS
            //long[][] initialRoutes = {initialRoute};

        executeLabel:
            DarpDataModel dataM = new DarpDataModel(numExecutions, depot.Id, pickupsDeliveries, stopList);
            dataM.PrintPickupDeliveries();
            DarpSolver darpSolver = new DarpSolver(dataM);
            darpSolver.Solve();

            numExecutions--;
            if (numExecutions != 0)
            {
                goto executeLabel;
            }

            Console.ReadKey();
     
        }
        public void StandardBusRouteOption()
        {
            insertLabel:
            try
            {
                ConsoleLogger.Log(this.ToString() + "Insert the start hour of the simulation (inclusive).");
                SimulationStartHour = int.Parse(Console.ReadLine());
                ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation (exclusive).");
                SimulationEndHour = int.Parse(Console.ReadLine());
            }
            catch (Exception)
            {
                ConsoleLogger.Log(this.ToString() + "Error Wrong input, please insert integer numbers for the start and end hour.");
                goto insertLabel;
            }

            var stopsNetworkGraph = new StopsNetworkGraphLoader(RoutesDataObject.Stops, RoutesDataObject.Routes);
            foreach (var route in _routes) // Each vehicle is responsible for a route
            {
                stopsNetworkGraph.LoadGraph();
                var stopsGraph = stopsNetworkGraph.StopsGraph;
                var allRouteServices = route.AllRouteServices.FindAll(s => TimeSpan.FromSeconds(s.StartTime).Hours >= SimulationStartHour && TimeSpan.FromSeconds(s.StartTime).Hours < SimulationEndHour);
                if (allRouteServices.Count > 0)
                {
                    int serviceCount = 0;
                    foreach (var service in allRouteServices) //Generates a new vehicle for each service
                    {
                        var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, stopsGraph);
                        v.AddService(service); //Adds the service
                        VehicleFleet.Add(v);
                        serviceCount++;

                    }

                    ConsoleLogger.Log(this.ToString() + route.ToString() + " - total of services added:" + serviceCount);
                }

            }
            ConsoleLogger.Log(ToString() + "Vehicle average speed: " + _vehicleSpeed + " km/h.");
            ConsoleLogger.Log(ToString() + "Vehicle capacity: " + _vehicleCapacity + " seats.");
            GenerateVehicleServiceEvents();
        }

        public void GenerateVehicleServiceEvents()
        {
            foreach (var vehicle in VehicleFleet)
                if (vehicle.Services.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.ServiceIterator.Reset();
                    while (vehicle.ServiceIterator.MoveNext()) //Iterates over each service
                    {
                        var events =
                            EventGenerator.GenerateRouteEvents(vehicle,
                                vehicle.ServiceIterator.Current.StartTime); //Generates the arrive/depart events for that service
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
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");

            foreach (var route in _routes)
            {

                var allRouteVehicles = VehicleFleet.FindAll(v => v.ServiceIterator.Current.Trip.Route == route);
            
                if (allRouteVehicles.Count > 0)
                {
                    
                    toPrintList.Add(route.ToString());
                    toPrintList.Add("Number of services:" + allRouteVehicles.Count);
                    foreach (var v in allRouteVehicles)
                    {
                        //For debug purposes---------------------------------------------------------------------------
                        if (v.Services.Count != v.Services.FindAll(s => s.IsDone).Count)
                        {
                            toPrintList.Add("Services Completed:");
                            foreach (var service in v.Services)
                                if (service.IsDone)
                                    toPrintList.Add(" - " + service + " - [" +
                                                    TimeSpan.FromSeconds(service.StartTime) + " - " +
                                                    TimeSpan.FromSeconds(service.EndTime) + "]");
                        }

                        if (v.Customers.Count > 0)
                        {
                            toPrintList.Add("Number of customers inside:" + v.Customers.Count);
                            foreach (var cust in v.Customers)
                                toPrintList.Add(
                                    cust + "Pickup:" + cust.PickupDelivery[0] + "Delivery:" + cust.PickupDelivery[1]);
                        }

                        //End of debug purposes---------------------------------------------------------------------------
                     
                    }
                 
                    var routeServicesStatistics = new RouteServicesStatistics(allRouteVehicles);
                    var list = routeServicesStatistics.GetOverallStatsPrintableList();
                    var logList = routeServicesStatistics.GetPerServiceStatsPrintableList();

                    foreach (var log in logList) myFileLogger.Log(log);

                    foreach (var toPrint in list)
                    {
                        toPrintList.Add(toPrint);
                    }

                    toPrintList.Add("------------------------------------------");
                }
            }

            if (toPrintList.Count > 0)
            {
                foreach (var print in toPrintList)
                {
                    myFileLogger.Log(print);
                    ConsoleLogger.Log(print);
                }
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
                if (vseEvt.Vehicle.ServiceIterator.Current != null && vseEvt.Vehicle.ServiceIterator.Current.HasStarted)
                {
                    int expectedDemand = RoutesDataObject.DemandsDataObject.GetDemand(vseEvt.Stop.Id,
                        vseEvt.Vehicle.ServiceIterator.Current.Trip.Route.Id,
                        TimeSpan.FromSeconds(vseEvt.Time).Hours);
                    customerEnterVehicleEvents =
                        EventGenerator.GenerateCustomerEnterVehicleEvents(vseEvt.Vehicle, vseEvt.Stop,
                            lastInsertedLeaveTime, rnd.Next(1, 7),expectedDemand);
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
            _eventLogger.Log(evt.GetTraceMessage());
            if (evt is CustomerVehicleEvent cve)
            {
                _validationsLogger.Log(cve.GetValidationsMessage(_validationsCounter));
                _validationsCounter++;
            }
        }
    }
}