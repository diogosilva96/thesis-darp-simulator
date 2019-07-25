using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.OrTools.Graph;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private readonly Logger.Logger _eventLogger;

        private readonly Logger.Logger _validationsLogger;

        private int _validationsCounter;

        private Dictionary<int, Tuple<List<Stop>, List<Customer>>> solutionVehicleCustomersDictionary;


        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            VehicleCapacity = 53;
            VehicleSpeed = 30;
            _validationsCounter = 1;
            SimulationOptions();
        }

        public override void InitializeVehicleEvents()
        {
            foreach (var vehicle in VehicleFleet)
                if (vehicle.Services.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.ServiceIterator.Reset();
                    vehicle.ServiceIterator.MoveNext();//initializes the serviceIterator
                    var arriveEvt = EventGenerator.GenerateVehicleArriveEvent(vehicle, vehicle.ServiceIterator.Current.StartTime); //Generates the first event for every vehicle (arrival at the first stop of the route)
                    Events.Add(arriveEvt);
                }

            SortEvents();
        }

        public override void SimulationOptions()
        {
            DarpDataModel = new DarpDataModel(TransportationNetwork.Stops.Find(s => s.Id == 2183), 2);
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
                goto wrongKeyLabel;
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
            Simulate();
        }

        public void SingleBusRouteFlexibleOption()
        {
            //ConsoleLogger.Log("Please select the route that you wish to simulate:");
            //int ind = 0;
            //Route route = null;
            //foreach (var r in TransportationNetwork.Routes)
            //{
            //    ConsoleLogger.Log(ind+"-"+r.ToString());
            //    ind++;
            //}

            //bool canAdvance = false;
            //while (!canAdvance)
            //{
                
            //    try
            //    {
            //        route = TransportationNetwork.Routes[int.Parse(Console.ReadLine())];
            //        canAdvance = true;
            //    }
            //    catch (Exception)
            //    {
            //        ConsoleLogger.Log(this.ToString() + "Error Wrong input, please insert a route index number.");
                    
            //        canAdvance = false;
            //    }
            //}
            var route = TransportationNetwork.Routes[0];
            Random rand = new Random();
            var service = route.AllRouteServices[0];
            var v = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary);
            v.AddService(service); //Adds the service to the vehicle
            VehicleFleet.Add(v);
            
            // Pickup and deliveries definition using static generated stops (to make the route flexible)
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430),0);
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359),0);
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018), 0);
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523), 0);
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884), 0);
            DarpDataModel.AddCustomer(TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555), 0);

            //var serviceStops = service.Trip.Stops;
            //DarpDataModel.AddInitialRoute(serviceStops);

            ConsoleLogger.Log("Initial solution:");
            DarpDataModel.PrintPickupDeliveries();
            DarpSolver darpSolver = new DarpSolver(DarpDataModel);
            var solution = darpSolver.Solve();
            darpSolver.Print(solution);
            var vehicleStopSequence = darpSolver.SolutionToVehicleStopSequenceCustomersDictionary(solution);
            solutionVehicleCustomersDictionary = darpSolver.SolutionToVehicleStopSequenceCustomersDictionary(solution);

            foreach (var dictionary in vehicleStopSequence)
            {
                var trip = new Trip(10000 + dictionary.Key, "Flexible trip " + dictionary.Key);
                trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                trip.Stops = dictionary.Value.Item1;
                var s = new Service(trip, new Random().Next(1, 10000));
                var vehicle = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary);
                vehicle.AddService(s);
                VehicleFleet.Add(vehicle);
            }
        
        }


        public void StandardBusRouteOption()
        {
            bool canAdvance = false;
            while (!canAdvance)
            {
                try
                {
                    ConsoleLogger.Log(this.ToString() + "Insert the start hour of the simulation (inclusive).");
                    SimulationStartHour = int.Parse(Console.ReadLine());
                    ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation (exclusive).");
                    SimulationEndHour = int.Parse(Console.ReadLine());
                    canAdvance = true;
                }
                catch (Exception)
                {
                    ConsoleLogger.Log(this.ToString() +
                                      "Error Wrong input, please insert integer numbers for the start and end hour.");
                    canAdvance = false;
                }
            }

            foreach (var route in TransportationNetwork.Routes) 
            {
                var allRouteServices = route.AllRouteServices.FindAll(s => TimeSpan.FromSeconds(s.StartTime).Hours >= SimulationStartHour && TimeSpan.FromSeconds(s.StartTime).Hours < SimulationEndHour);
                if (allRouteServices.Count > 0)
                {
                    var serviceCount = 0;
                    foreach (var service in allRouteServices) //Generates a new vehicle for each service, meaning that the number of services will be equal to the number of vehicles
                    {
                        var v = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary);
                        v.AddService(service); //Adds the service
                        VehicleFleet.Add(v);
                        serviceCount++;
                    }
                    ConsoleLogger.Log(this.ToString() + route.ToString() + " - total of services added:" + serviceCount);
                }
            }
        }

        public override void PrintSimulationSettings()
        {
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("|     Simulation Settings     |");
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("Number of vehicles:" + VehicleFleet.Count);
            ConsoleLogger.Log("Vehicle average speed: " + VehicleSpeed + " km/h.");
            ConsoleLogger.Log("Vehicle capacity: " + VehicleCapacity + " seats.");
            List<Route> distinctRoutes = new List<Route>();
            foreach (var vehicle in VehicleFleet)
            {
                vehicle.ServiceIterator.Reset();

                while (vehicle.ServiceIterator.MoveNext())//iterates over each vehicle service
                {
                    var route = vehicle.ServiceIterator.Current.Trip.Route;
                    if (!distinctRoutes.Contains(route)) //if the route isn't in distinct routes list adds it
                    {
                        distinctRoutes.Add(route);
                    }
                }
            }
            ConsoleLogger.Log("Number of distinct routes:"+distinctRoutes.Count);
            if (SimulationStartHour == 0 && SimulationEndHour == 0) return;
            ConsoleLogger.Log("Start hour:" + SimulationStartHour);
            ConsoleLogger.Log(  "End Hour:" + SimulationEndHour);

        }



        public override void PrintSolution()
        {
            //start of darp solution
            if (DarpDataModel != null)
            {
                ConsoleLogger.Log("Final DARP  solution:");
                DarpDataModel.PrintPickupDeliveries();
                DarpSolver darpSolver = new DarpSolver(DarpDataModel);
                var solution = darpSolver.Solve();
                darpSolver.Print(solution);
                var solutionStops = darpSolver.SolutionToVehicleStopSequenceCustomersDictionary(solution);
            }
            //end of darp solution

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
           
            foreach (var route in TransportationNetwork.Routes)
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
            SimulationOptions();

        }
        public override void Append(Event evt)
        {
            var eventsCount = Events.Count;
            var rnd = new Random();

            //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS AND GENERATION OF THE DEPART EVENT FROM THE CURRENT STOP---------------------------------------
            if (evt.Category == 0 && evt is VehicleStopEvent eventArrive)
            {
                var arrivalTime = evt.Time;
                var custLeaveVehicleEvents = EventGenerator.GenerateCustomerLeaveVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, arrivalTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = custLeaveVehicleEvents.Count > 0 ? custLeaveVehicleEvents[custLeaveVehicleEvents.Count - 1].Time : arrivalTime;

                List<Event> custEnterVehicleEvents = null;
                if (eventArrive.Vehicle.ServiceIterator.Current != null && eventArrive.Vehicle.ServiceIterator.Current.HasStarted)
                {
                    int expectedDemand = 0;
                    try
                    {
                        expectedDemand = TransportationNetwork.DemandsDataObject.GetDemand(eventArrive.Stop.Id, eventArrive.Vehicle.ServiceIterator.Current.Trip.Route.Id, TimeSpan.FromSeconds(eventArrive.Time).Hours);
                    }
                    catch (Exception e)
                    {
                        expectedDemand = 0;
                    }

                    custEnterVehicleEvents = EventGenerator.GenerateCustomersEnterVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, lastInsertedLeaveTime, rnd.Next(1, 7), expectedDemand);
                    if (custEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = custEnterVehicleEvents[custEnterVehicleEvents.Count - 1].Time;
                }
       
                AddEvent(custEnterVehicleEvents);
                AddEvent(custLeaveVehicleEvents);


                var maxInsertedTime = Math.Max(lastInsertedEnterTime, lastInsertedLeaveTime); ; //gets the highest value of the last insertion in order to maintain precedence constraints for the depart evt, meaning that the stop depart only happens after every customer has already entered and left the vehicle on that stop location

                //INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS
                int vehicleInd = eventArrive.Vehicle.Id - 2; //change this in order to use the id of the vehicle instead of the index
     
                    solutionVehicleCustomersDictionary.TryGetValue(vehicleInd, out var solutionDictionaryValue); //Tries to get the customer dictionary for the current vehicle;
                    if (solutionDictionaryValue != null)
                    {
                        var customersToEnterAtCurrentStop =
                            solutionDictionaryValue.Item2?.FindAll(c =>
                                c.PickupDelivery[0] ==
                                eventArrive
                                    .Stop); //gets all the customers that have the current stop as the pickup stop
                        if (customersToEnterAtCurrentStop != null)
                        {
                            var count = 1;
                            foreach (var customer in customersToEnterAtCurrentStop
                            ) //iterates over every customer that has the actual stop as the pickup stop, in order to make them enter the vehicle
                            {
                                var customerEnterVehicleEvt =
                                    EventGenerator.GenerateCustomerEnterVehicleEvent(eventArrive.Vehicle,
                                        maxInsertedTime + count, customer); //generates the enter event
                                AddEvent(customerEnterVehicleEvt); //adds to the event list
                                count++;
                            }
                        }
                    }

                    // END OF INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS

                //VEHICLE DEPART STOP EVENT
                var departEvent = EventGenerator.GenerateVehicleDepartEvent(eventArrive.Vehicle, maxInsertedTime + 1);
                AddEvent(departEvent);


            }
            //END OF INSERTION OF CUSTOMER ENTER, LEAVE VEHICLE EVENTS AND OF VEHICLE DEPART EVENT--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION (APPEND) OF VEHICLE NEXT STOP ARRIVE EVENT
            if (evt.Category == 1 && evt is VehicleStopEvent eventDepart)
            {
                    var departTime = eventDepart.Time; //the time the vehicle departed on the previous depart event
                  
                    var stopTuple = Tuple.Create(eventDepart.Vehicle.ServiceIterator.Current.StopsIterator.CurrentStop,
                        eventDepart.Vehicle.ServiceIterator.Current.StopsIterator.NextStop);
                    eventDepart.Vehicle.ArcDictionary.TryGetValue(stopTuple, out var distance);
                    var travelTime = eventDepart.Vehicle.TravelTime(distance); //Gets the time it takes to travel from the currentStop to the nextStop
                    var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                    eventDepart.Vehicle.ServiceIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
                    var arriveEvent = EventGenerator.GenerateVehicleArriveEvent(eventDepart.Vehicle, nextArrivalTime); //generates the arrive event
                    AddEvent(arriveEvent);

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            var pickup = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];
            var delivery = pickup;
            while (pickup == delivery) delivery = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];

            var eventReq =
                EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickup,
                    delivery); //Generates a pickup and delivery customer request
            if (eventReq != null) //if eventReq isn't null, add the event 
                AddEvent(eventReq);
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            //--------------------------------------------------------------------------------------------------------
            if (eventsCount != Events.Count) //If the size of the events list has changed, the event list has to be sorted
                SortEvents();
        }


        public override void Handle(Event evt)
        {
            evt.Treat();
            TotalEventsHandled++;
            _eventLogger.Log(evt.GetTraceMessage());
            switch (evt)
            {
                case CustomerVehicleEvent customerVehicleEvent:
                    _validationsLogger.Log(customerVehicleEvent.GetValidationsMessage(_validationsCounter));
                    _validationsCounter++;
                    break;
                case CustomerRequestEvent customerRequestEvent:
                    DarpDataModel.AddCustomer(customerRequestEvent.Customer);
                    break;
            }
        }
    }
}