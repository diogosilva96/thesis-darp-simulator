using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.OrTools.Graph;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.DARP;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private readonly Logger.Logger _eventLogger;

        private readonly Logger.Logger _validationsLogger;

        private int _validationsCounter;

        private Dictionary<Vehicle, Tuple<List<Stop>, List<Customer>>> solutionVehicleCustomersDictionary; //CHANGE THIS!

        public DarpSolver Solver = new DarpSolver();


        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            VehicleCapacity = 53;
            VehicleSpeed = 30;
            _validationsCounter = 1;
            PickupDeliveryDataModel = new PickupDeliveryDataModel(TransportationNetwork.Stops.Find(s => s.Id == 2183));
            PrintSimulationOptions();

        }

        public override void InitializeVehicleEvents()
        {
            foreach (var vehicle in VehicleFleet)
                if (vehicle.ServiceTrips.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.TripIterator.Reset();
                    vehicle.TripIterator.MoveNext();//initializes the serviceIterator
                    var arriveEvt = EventGenerator.GenerateVehicleArriveEvent(vehicle, vehicle.TripIterator.Current.StartTime); //Generates the first event for every vehicle (arrival at the first stop of the route)
                    Events.Add(arriveEvt);
                }

            SortEvents();
        }

        public override void PrintSimulationOptions()
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
            var serviceTrip = route.Trips[0]; //ADD assignvehicletrip function here!
            var v = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary,false);
            v.AddTrip(serviceTrip); //Adds the service to the vehicle
            VehicleFleet.Add(v);
            TimeWindowsDataModel twDM = new TimeWindowsDataModel(TransportationNetwork.Stops.Find(s => s.Id == 2183),VehicleSpeed);

            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new int[] { 3500, 4000 }, 0));
            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359) }, new int[] { 3400, 3600 }, 0));
            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018) }, new int[] { 3250, 3550 }, 0));
            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523) }, new int[] { 3220, 3700 }, 0));
            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884) }, new int[] { 3100, 3900 }, 0));
            twDM.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555) }, new int[] { 2900, 3300 }, 0));
            // Pickup and deliveries definition using static generated stops (to make the route flexible)

            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[]{ TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430)},new int[]{3500,4000}, 0));
            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[]{TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359)}, new int[] { 3400, 3600 }, 0));
            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[]{TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018)}, new int[] { 3250, 3550 }, 0));
            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523)}, new int[] { 3220, 3700 }, 0));
            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884)}, new int[] { 3100, 3900 }, 0));
            PickupDeliveryDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555)}, new int[] { 2900, 3300 },0));

            //var serviceStops = service.Trip.Stops;
            //PickupDeliveryDataModel.AddInitialRoute(serviceStops);

            //Creates two available vehicles to be able to perform flexible routing
            for (int i = 0; i < 2; i++)
            {
                var vehicle = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary, true);
                PickupDeliveryDataModel.AddVehicle(vehicle);
                twDM.AddVehicle(vehicle);
                VehicleFleet.Add(vehicle);
            }
            twDM.PrintMatrix();
            twDM.PrintTimeWindows();
            ConsoleLogger.Log("Initial solution:");
            PickupDeliveryDataModel.PrintPickupDeliveries();
            Solver.Init(PickupDeliveryDataModel, 1);
            var solution = Solver.Solve();
            Solver.Print(solution);
            solutionVehicleCustomersDictionary = Solver.SolutionToVehicleStopSequenceCustomersDictionary(solution);

            foreach (var dictionary in solutionVehicleCustomersDictionary)
            {
                var trip = new Trip(20000 + dictionary.Key.Id, "Flexible trip " + dictionary.Key.Id);
                trip.StartTime = new Random().Next(0, 60 * 60 * 24);//random start time between hour 0 and 24
                trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                trip.Stops = dictionary.Value.Item1;
                var vehicle = VehicleFleet.Find(v1 => v1.Id == dictionary.Key.Id); //finds the vehicle in the vehiclefleet
                vehicle.AddTrip(trip); //adds the new flexible service to the vehicle
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
            AssignVehicleTrips();
        }

        private void AssignVehicleTrips()
        {
            foreach (var route in TransportationNetwork.Routes)
            {
                var allRouteTrips = route.Trips.FindAll(t => TimeSpan.FromSeconds(t.StartTime).Hours >= SimulationStartHour && TimeSpan.FromSeconds(t.StartTime).Hours < SimulationEndHour);
                if (allRouteTrips.Count > 0)
                {
                    var tripCount = 0;
                    foreach (var trip in allRouteTrips) //Generates a new vehicle for each service, meaning that the number of services will be equal to the number of vehicles
                    {
                        if (trip.IsDone == true)
                        {
                            trip.Reset();
                        }
                        var v = new Vehicle(VehicleSpeed, VehicleCapacity, TransportationNetwork.ArcDictionary, false);
                        v.AddTrip(trip); //Adds the service
                        VehicleFleet.Add(v);
                        ConsoleLogger.Log(trip.ToString() + " added.");
                        tripCount++;
                    }
                    ConsoleLogger.Log(this.ToString() + route.ToString() + " - total of trips added:" + tripCount);
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
                vehicle.TripIterator.Reset();

                while (vehicle.TripIterator.MoveNext())//iterates over each vehicle service
                {
                    var route = vehicle.TripIterator.Current.Route;
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
            //if (PickupDeliveryDataModel != null)
            //{
            //    ConsoleLogger.Log("Final DARP  solution:");
            //    PickupDeliveryDataModel.PrintPickupDeliveries();
            //    Solver.Init(PickupDeliveryDataModel, 1);
            //    var solution = Solver.Solve();
            //    Solver.Print(solution);
            //}
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

                var allRouteVehicles = VehicleFleet.FindAll(v => v.TripIterator.Current.Route == route);
            
                if (allRouteVehicles.Count > 0)
                {
                    
                    toPrintList.Add(route.ToString());
                    toPrintList.Add("Number of services:" + allRouteVehicles.Count);
                    foreach (var v in allRouteVehicles)
                    {
                        //For debug purposes---------------------------------------------------------------------------
                        if (v.ServiceTrips.Count != v.ServiceTrips.FindAll(s => s.IsDone).Count)
                        {
                            toPrintList.Add("ServiceTrips Completed:");
                            foreach (var service in v.ServiceTrips)
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
            PrintSimulationOptions();

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
                if (eventArrive.Vehicle.TripIterator.Current != null && eventArrive.Vehicle.TripIterator.Current.HasStarted)
                {
                    int expectedDemand = 0;
                    try
                    {
                        expectedDemand = TransportationNetwork.DemandsDataObject.GetDemand(eventArrive.Stop.Id, eventArrive.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(eventArrive.Time).Hours);
                    }
                    catch (Exception)
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
                if (solutionVehicleCustomersDictionary != null)
                {
                    solutionVehicleCustomersDictionary.TryGetValue(eventArrive.Vehicle,
                        out var solutionDictionaryValue); //Tries to get the customer dictionary for the current vehicle;
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
                  
                    var stopTuple = Tuple.Create(eventDepart.Vehicle.TripIterator.Current.StopsIterator.CurrentStop,
                        eventDepart.Vehicle.TripIterator.Current.StopsIterator.NextStop);
                    eventDepart.Vehicle.ArcDictionary.TryGetValue(stopTuple, out var distance);
                    var travelTime = new Calculator().CalculateTravelTime(eventDepart.Vehicle.Speed,distance);//Gets the time it takes to travel from the currentStop to the nextStop
                    var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                    eventDepart.Vehicle.TripIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
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
                    PickupDeliveryDataModel.AddCustomer(customerRequestEvent.Customer);
                    break;
            }
        }
    }
}