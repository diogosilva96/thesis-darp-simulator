﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Algorithms;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private Logger.Logger _eventLogger;

        private Logger.Logger _validationsLogger;

        private int _validationsCounter;

        private readonly int _vehicleSpeed;

        private readonly int _vehicleCapacity;

        public int TotalDynamicRequests;

        public int TotalServedDynamicRequests;

        public int[] SimulationTimeWindow;

        public int MaxCustomerRideTime;//max customer ride time 

        public int MaxAllowedUpperBoundTime;//max delay of timeWindows upperBound (relative to desired timeWindows)

        public Stop Depot; //the stop that every flexible service vehicle starts and ends at 

        public int TotalSimulationTime => SimulationTimeWindow[0] >= 0 && SimulationTimeWindow[1] >= 0 && SimulationTimeWindow[1] != 0 ? SimulationTimeWindow[1] - SimulationTimeWindow[0] : 0; //in seconds

        public double DynamicRequestProbabilityThreshold; //randomly generated value has to be
                                                                     //than this in order to generate a dynamic request
        private readonly string _loggerBasePath;
        private string _currentSimulationLoggerPath;


        public Simulation(int maxCustomerRideTimeSeconds, int maxAllowedUpperBoundTimeSeconds, double dynamicRequestProbability)
        {
            var loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }
            _loggerBasePath = Path.Combine(loggerPath, DateTime.Now.ToString("MMMM dd"));
            if (!Directory.Exists(_loggerBasePath))
            {
                Directory.CreateDirectory(_loggerBasePath);
            }
            _vehicleCapacity = 20;
            _vehicleSpeed = 30;
            DynamicRequestProbabilityThreshold = dynamicRequestProbability;
            SimulationTimeWindow = new int[2];
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 24 * 24 * 60; // 24hours in seconds
            MaxCustomerRideTime = maxCustomerRideTimeSeconds;
            MaxAllowedUpperBoundTime = maxAllowedUpperBoundTimeSeconds;
            Depot = TransportationNetwork.Stops.Find(s => s.Id == 2183);
        }

        public void Init()
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var auxTime = currentTime.Split(":");
            currentTime = auxTime[0] + auxTime[1] +auxTime[2];
            _currentSimulationLoggerPath = Path.Combine(_loggerBasePath, currentTime);
            if (!Directory.Exists(_currentSimulationLoggerPath))
            {
                Directory.CreateDirectory(_currentSimulationLoggerPath);
            }
            RandomNumberGenerator.Seed = new Random().Next(int.MaxValue); //initiates the random number generator seed
            TotalEventsHandled = 0;
            TotalDynamicRequests = 0;
            TotalServedDynamicRequests = 0;
            _validationsCounter = 1;
            Events.Clear(); //clears all events 
            VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
        }

        public void InitSimulationLoggers()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(_currentSimulationLoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(_currentSimulationLoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
        }

        public void InitVehicleEvents()
        {
            var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(SimulationTimeWindow[0], DynamicRequestProbabilityThreshold);//initializes dynamic requests
            AddEvent(eventDynamicRequestCheck);
            foreach (var vehicle in VehicleFleet)
                if (vehicle.ServiceTrips.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.TripIterator.Reset();
                    vehicle.TripIterator.MoveNext();//initializes the serviceIterator
                    if (vehicle.TripIterator.Current != null)
                    {
                        var arriveEvt = EventGenerator.GenerateVehicleArriveEvent(vehicle, vehicle.TripIterator.Current.StartTime); //Generates the first event for every vehicle (arrival at the first stop of the route)
                        Events.Add(arriveEvt);
                    }
                }
            SortEvents();
        }
        public override void MainLoop()
        {
            while (true)
            {
                Init(); //initializes simulation variables
                DisplayOptionsMenu();
                if (Events.Count > 0) //it means there is the need to simulate
                {
                    PrintSimulationSettings();
                    Simulate();
                    PrintSimulationStatistics();
                }
            }
        }
        public void DisplayOptionsMenu()
        {
            var numOptions = 4;
            ConsoleLogger.Log("Please Select one of the options:");
            ConsoleLogger.Log("1 - Standard Bus route simulation");
            ConsoleLogger.Log("2 - Flexible Bus route simulation");
            ConsoleLogger.Log("3 - Algorithms Test & Results");
            ConsoleLogger.Log("4 - Exit");
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
                case 1:
                    StandardBusRouteOption();
                    break;
                case 2:
                    FlexibleBusRouteOption();
                    break;
                case 3:
                    AlgorithmComparisonOption();
                    break;
                case 4:
                    Environment.Exit(0);
                    break;
                default:
                    StandardBusRouteOption();
                    break;
            }

        }

        public RoutingDataModel GenerateRandomInitialDataModel()
        {
            ConsoleLogger.Log("Please insert the number of vehicles to be considered:");
            var vehicleNumber = GetIntInput(1, int.MaxValue);
            ConsoleLogger.Log("Please insert the number of customers to be randomly generated:");
            var numberCustomers = GetIntInput(1, int.MaxValue);
            GenerateNewDataModelLabel:
            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            List<Stop> startDepots = new List<Stop>(); //array with the start depot for each vehicle, each index is a vehicle
            List<Stop> endDepots = new List<Stop>();//array with the end depot for each vehicle, each index is a vehicle
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < vehicleNumber; i++)
            {
                dataModelVehicles.Add(new Vehicle(_vehicleSpeed, 20, true));
                startDepots.Add(Depot);
               // startDepots.Add(null); //dummy start depot
                endDepots.Add(Depot);
               // endDepots.Add(null);//dummy end depot
            }

            var customersToBeServed = new List<Customer>();
            // Pickup and deliveries definition using static generated stop requests
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new long[] { 2250, 3500 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359) }, new long[] { 1000, 2700 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018) }, new long[] { 2200, 4000 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523) }, new long[] { 2000, 2900 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884) }, new long[] { 2300, 2900 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555) }, new long[] { 1900, 2300 }, 0));
            //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2200) }, new long[] { 1500, 3000 }, 0));
            for (int i = 0; i < numberCustomers; i++) //generate 5 customers with random timeWindows and random pickup and delivery stops
            {
                var rng = RandomNumberGenerator.Random;
                var pickup = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                while (pickup == Depot) //if the pickup is the depot has to generate another pickup stop
                {
                    pickup = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                }

                var delivery = pickup;

                var requestTime = 0;
                while (delivery == pickup || delivery == Depot) //if the delivery stop is equal to the pickup stop or depot stop, it needs to generate a different delivery stop
                {

                    delivery = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                }

                var pickupTime =
                    rng.Next(requestTime, requestTime + 90 * 60); //the minimum pickup time is 0 minutes above the requestTime and maximum pickup 90 minutes after the request time, 
                var deliveryTime = rng.Next(pickupTime + 15 * 60, pickupTime + 45 * 60); //delivery time will be at minimum 15 minutes above the pickuptime and at max 45 minutes from the pickup time
                Stop[] pickupDelivery = new[] {pickup, delivery};
                long[] desiredTimeWindow = new[] {(long) pickupTime, (long) deliveryTime};
                customersToBeServed.Add(new Customer(pickupDelivery,desiredTimeWindow,requestTime));
            }


            long[] startDepotsArrivalTime = new long[dataModelVehicles.Count];
            var routingDataModel = new RoutingDataModel(startDepots,endDepots, dataModelVehicles, customersToBeServed,startDepotsArrivalTime,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
            var solver = new RoutingSolver(routingDataModel,false);
            var solution = solver.TryGetFastSolution();
            if (solution == null)
            {
                goto GenerateNewDataModelLabel;
            }
            return routingDataModel;
        }

        public void FlexibleBusRouteOption()
        {
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = (int)TimeSpan.FromHours(3).TotalSeconds;
            var dataModel=GenerateRandomInitialDataModel();
            if (dataModel != null)
            {
                RoutingSolver routingSolver = new RoutingSolver(dataModel, false);

                var timeWindowSolution = routingSolver.TryGetFastSolution();
                RoutingSolutionObject routingSolutionObject = null;
;
                if (timeWindowSolution != null)
                {
                    routingSolver.PrintSolution(timeWindowSolution);
                    routingSolutionObject = routingSolver.GetSolutionObject(timeWindowSolution);
                }
                AssignVehicleFlexibleTrips(routingSolutionObject, SimulationTimeWindow[0]);
            }
            InitSimulationLoggers(); //simulation loggers init
            InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)
        }

        public void AlgorithmComparisonOption()
        {
            bool allowDropNodes = AllowDropNodesMenu();
            var dataModel = GenerateRandomInitialDataModel();
            dataModel.PrintDataModelSettings();
            AlgorithmStatistics algorithmStatistics = new AlgorithmStatistics(dataModel);
            var algorithmStatList = algorithmStatistics.GetSearchAlgorithmsResultsList(10,allowDropNodes);
            var printList = algorithmStatistics.GetPrintableStatisticsList(algorithmStatList);
            IRecorder algorithmsRecorder = new FileRecorder(Path.Combine(_currentSimulationLoggerPath, @"algorithms.txt"));
            var algorithmsLogger = new Logger.Logger(algorithmsRecorder);
            foreach (var printableItem in printList)
            {
                ConsoleLogger.Log(printableItem);
                algorithmsLogger.Log(printableItem);
            }
        }

        public int GetIntInput(int minVal, int maxVal)
        {
            wrongKeyLabel:
            int key = 0;
            try
            {
                key = int.Parse(Console.ReadLine());
                if (key < minVal || key > maxVal)
                {
                    ConsoleLogger.Log("Wrong input, please retype using a valid integer number!");
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {
                ConsoleLogger.Log("Wrong input, please retype using a valid integer number!");
                goto wrongKeyLabel;
            }

            return key;
        }

        public bool AllowDropNodesMenu()
        {
            ConsoleLogger.Log("Allow drop nodes penalties?");
            ConsoleLogger.Log("1 - Yes");
            ConsoleLogger.Log("2 - No");
            var key = GetIntInput(1, 2);
            var allowDropNodes = key == 1;
            return allowDropNodes;
        }
        public void StandardBusRouteOption()
        {
            DisplayStartEndHourMenu();
            AssignAllConventionalTripsToVehicles();
            InitSimulationLoggers();
            InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)
        }

        public void DisplayStartEndHourMenu()
        {
            int startTimeHour = 0;
            int endTimeHour = 0;
            bool canAdvance = false;
            while (!canAdvance)
            {
                try
                {
                    ConsoleLogger.Log(this.ToString() + "Insert the start hour of the simulation (inclusive).");
                    startTimeHour = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
                    ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation (exclusive).");
                    endTimeHour = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
                    if (startTimeHour >= endTimeHour)
                    {
                        throw new InvalidOperationException();
                    }
                    canAdvance = true;
                }
                catch (Exception)
                {
                    ConsoleLogger.Log(this.ToString() +
                                      "Error Wrong input, please insert integer numbers for the start and end hour.");
                    canAdvance = false;
                }
            }

            SimulationTimeWindow[0] = (int)TimeSpan.FromHours(startTimeHour).TotalSeconds;//hours in seconds
            SimulationTimeWindow[1] = (int)TimeSpan.FromHours(endTimeHour).TotalSeconds;//hours in seconds
    
        }
        private void AssignVehicleFlexibleTrips(RoutingSolutionObject routingSolutionObject,int time)
        {
            if (routingSolutionObject != null)
            {
                //Adds the flexible trip vehicles to the vehicleFleet
                for (int j = 0; j < routingSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                {
                    var solutionVehicle = routingSolutionObject.IndexToVehicle(j);
                    var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                    trip.StartTime =
                       time+(int)routingSolutionObject.GetVehicleTimeWindows(solutionVehicle)[0][0]; //start time, might need to change!
                    trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                    trip.Stops = routingSolutionObject.GetVehicleStops(solutionVehicle);
                    trip.ExpectedCustomers = routingSolutionObject.GetVehicleCustomers(solutionVehicle);
                    trip.ScheduledTimeWindows = routingSolutionObject.GetVehicleTimeWindows(solutionVehicle);
                    solutionVehicle.AddTrip(trip); //adds the new flexible trip to the vehicle
                    
                   
                    VehicleFleet.Add(solutionVehicle); //adds the vehicle to the vehicle fleet
                }
            }
            else
            {
                ConsoleLogger.Log("routingSolutionObject is null.");
            }
        }

        private void AssignAllConventionalTripsToVehicles() //assigns all the conventional trips to n vehicles where n = the number of trips, conventional trip is an already defined trip with fixed routes
        {
            foreach (var route in TransportationNetwork.Routes)
            {
                var allRouteTrips = route.Trips.FindAll(t => t.StartTime >= SimulationTimeWindow[0] && t.StartTime < SimulationTimeWindow[1]);
                if (allRouteTrips.Count > 0)
                {
                    List<int> startTimes = new List<int>();
                    var tripCount = 0;
                    foreach (var trip in allRouteTrips) //Generates a new vehicle for each trip, meaning that the number of services will be equal to the number of vehicles
                    {
                        if (!startTimes.Contains(trip.StartTime))
                        {
                            startTimes.Add(trip.StartTime);

                            if (trip.IsDone == true)
                            {
                                trip.Reset();
                            }

                            var v = new Vehicle(_vehicleSpeed, _vehicleCapacity,false);
                            v.AddTrip(trip); //Adds the service
                            VehicleFleet.Add(v);
                            ConsoleLogger.Log(trip.ToString() + " added.");
                            tripCount++;
                        }
                    }
                    ConsoleLogger.Log(this.ToString() + route.ToString() + " - total of trips added:" + tripCount);
                }
            }
        }

        public void PrintSimulationSettings()
        {
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("|     Simulation Settings     |");
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("Random Number Generator Seed: "+RandomNumberGenerator.Seed);
            ConsoleLogger.Log("Maximum Allowed UpperBound Time: "+TimeSpan.FromSeconds(MaxAllowedUpperBoundTime).TotalMinutes + " minutes");
            ConsoleLogger.Log("Maximum Customer ride time: "+TimeSpan.FromSeconds(MaxCustomerRideTime).TotalMinutes +" minutes");
            ConsoleLogger.Log("Dynamic request check probability threshold: "+DynamicRequestProbabilityThreshold);
            ConsoleLogger.Log("Simulation Start Time: "+TimeSpan.FromSeconds(SimulationTimeWindow[0]).ToString());
            ConsoleLogger.Log("Simulation End Time: "+ TimeSpan.FromSeconds(SimulationTimeWindow[1]).ToString());
            ConsoleLogger.Log("Simulation Duration: " + TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours");
            ConsoleLogger.Log("Number of vehicles:" + VehicleFleet.Count);
            ConsoleLogger.Log("Vehicle average speed: " + _vehicleSpeed + " km/h.");
            ConsoleLogger.Log("Vehicle capacity: " + _vehicleCapacity + " seats.");

            //logs into a file the settings
            IRecorder settingsFileRecorder = new FileRecorder(Path.Combine(_currentSimulationLoggerPath, @"settings.txt"));
            var settingsLogger = new Logger.Logger(settingsFileRecorder);
            settingsLogger.Log(nameof(RandomNumberGenerator.Seed)+ ": "+RandomNumberGenerator.Seed);
            settingsLogger.Log(nameof(MaxAllowedUpperBoundTime)+": "+MaxAllowedUpperBoundTime);
            settingsLogger.Log(nameof(MaxCustomerRideTime)+": "+MaxCustomerRideTime);
            settingsLogger.Log(nameof(DynamicRequestProbabilityThreshold)+": "+DynamicRequestProbabilityThreshold);
            settingsLogger.Log(nameof(SimulationTimeWindow)+"[0]: "+SimulationTimeWindow[0]);
            settingsLogger.Log(nameof(SimulationTimeWindow) + "[1]: " + SimulationTimeWindow[1]);
            settingsLogger.Log(nameof(VehicleFleet)+nameof(VehicleFleet.Count)+": "+VehicleFleet.Count);
            settingsLogger.Log(nameof(_vehicleSpeed)+": "+_vehicleSpeed);
            settingsLogger.Log(nameof(_vehicleCapacity)+": "+_vehicleCapacity);
        }



        public override void PrintSimulationStatistics()
        {
            IRecorder fileRecorder =
                new FileRecorder(Path.Combine(_currentSimulationLoggerPath, @"stats_logs.txt"));
            var myFileLogger = new Logger.Logger(fileRecorder);
            var toPrintList = new List<string>();
            var alreadyHandledEvents = Events.FindAll(e => e.AlreadyHandled);
            toPrintList.Add(ToString() + "Total number of events handled: " +
                            alreadyHandledEvents.Count + " out of " + Events.Count + ".");
            if (alreadyHandledEvents.Count <= Events.Count)
            {
                var notHandledEvents = Events.FindAll(e => !e.AlreadyHandled);
                foreach (var notHandledEvent in notHandledEvents)
                {
                    ConsoleLogger.Log(notHandledEvent.ToString());
                }
            }
            toPrintList.Add(ToString() + "Vehicle Fleet Size: " + VehicleFleet.Count + " vehicle(s).");
            toPrintList.Add(ToString()+ "Average Dynamic requests per hour: "+TotalDynamicRequests / TimeSpan.FromSeconds(TotalSimulationTime).TotalHours);
            toPrintList.Add(ToString() + "Total simulation time: "+TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours.");
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");
            foreach (var vehicle in VehicleFleet.FindAll(v=>v.FlexibleRouting))
            {
                vehicle.PrintRoute(vehicle.TripIterator.Current.Stops, vehicle.TripIterator.Current.ScheduledTimeWindows,vehicle.TripIterator.Current.ServicedCustomers); //scheduled route
                vehicle.PrintRoute(vehicle.TripIterator.Current.VisitedStops, vehicle.TripIterator.Current.StopsTimeWindows, vehicle.TripIterator.Current.ServicedCustomers); //simulation route
            }
            foreach (var route in TransportationNetwork.Routes)
            {

                var allRouteVehicles = VehicleFleet.FindAll(v => v.TripIterator != null && v.TripIterator.Current.Route == route );
            
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
        }
        public override void Append(Event evt)
        {
            var currentNumberOfEvents = Events.Count;
        

            //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS AND GENERATION OF THE DEPART EVENT FROM THE CURRENT STOP---------------------------------------
            if (evt.Category == 0 && evt is VehicleStopEvent arriveEvent)
            {
                var arrivalTime = evt.Time;
                var customerLeaveVehicleEvents = EventGenerator.GenerateCustomerLeaveVehicleEvents(arriveEvent.Vehicle, arriveEvent.Stop, arrivalTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = customerLeaveVehicleEvents.Count > 0 ? customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time : arrivalTime;

                List<Event> customersEnterVehicleEvents = null;
                if (arriveEvent.Vehicle.TripIterator.Current != null && arriveEvent.Vehicle.TripIterator.Current.HasStarted)
                {
                    int expectedDemand = 0;
                    try
                    {
                        expectedDemand = !arriveEvent.Vehicle.FlexibleRouting ? TransportationNetwork.DemandsDataObject.GetDemand(arriveEvent.Stop.Id, arriveEvent.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(arriveEvent.Time).Hours) : 0;
                 
                    }
                    catch (Exception)
                    {
                        expectedDemand = 0;
                    }

                    customersEnterVehicleEvents = EventGenerator.GenerateCustomersEnterVehicleEvents(arriveEvent.Vehicle, arriveEvent.Stop, lastInsertedLeaveTime, expectedDemand);
                    if (customersEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = customersEnterVehicleEvents[customersEnterVehicleEvents.Count - 1].Time;
                }
       
                AddEvent(customersEnterVehicleEvents);
                AddEvent(customerLeaveVehicleEvents);


                var maxInsertedTime = Math.Max(lastInsertedEnterTime, lastInsertedLeaveTime); ; //gets the highest value of the last insertion in order to maintain precedence constraints for the depart evt, meaning that the stop depart only happens after every customer has already entered and left the vehicle on that stop location

                //INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS!
        
                   
                        if (arriveEvent.Vehicle.TripIterator.Current != null && arriveEvent.Vehicle.FlexibleRouting)
                        {
                            var currentVehicleTrip = arriveEvent.Vehicle.TripIterator.Current;
                            var customersToEnterAtCurrentStop = currentVehicleTrip.ExpectedCustomers.FindAll(c => c.PickupDelivery[0] == arriveEvent.Stop && !c.IsInVehicle); //gets all the customers that have the current stop as the pickup stop

                            if (customersToEnterAtCurrentStop.Count > 0) //check if there is customers to enter at current stop
                            {
                                var sameStops = currentVehicleTrip.Stops.FindAll(s => s == arriveEvent.Stop && currentVehicleTrip.Stops.IndexOf(s) >= currentVehicleTrip.StopsIterator.CurrentIndex);
                                foreach (var customer in customersToEnterAtCurrentStop) //iterates over every customer that has the actual stop as the pickup stop, in order to make them enter the vehicle
                                {
                                    ConsoleLogger.Log("Vehicle expected depart time" + currentVehicleTrip.ScheduledTimeWindows[currentVehicleTrip.StopsIterator.CurrentIndex][1] + " customer arrival time:" + customer.DesiredTimeWindow[0]);
                                    if (sameStops.Count > 1)
                                    {
                                        ConsoleLogger.Log("SameStops");

                                    }
                                    if (currentVehicleTrip.ScheduledTimeWindows[currentVehicleTrip.StopsIterator.CurrentIndex][1] >= customer.DesiredTimeWindow[0]) //if current stop expected depart time is greater or equal than the customer arrival time adds the customer
                                    {
                                        var enterTime = maxInsertedTime > customer.DesiredTimeWindow[0] ? maxInsertedTime + 1 : customer.DesiredTimeWindow[0]+1; //case maxinserted time is greather than desired time window the maxinserted time +1 will be the new enterTime of the customer, othersie it is the customer's desiredtimewindow
                                        var customerEnterVehicleEvt =
                                            EventGenerator.GenerateCustomerEnterVehicleEvent(arriveEvent.Vehicle, (int)enterTime, customer); //generates the enter event
                                        AddEvent(customerEnterVehicleEvt); //adds to the event list
                                        maxInsertedTime = (int)enterTime; //updates the maxInsertedTime
                                    }

                                }
                            }
                }
                

                // END OF INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS

                //VEHICLE DEPART STOP EVENT
               
                        if (arriveEvent.Vehicle.TripIterator.Current?.ScheduledTimeWindows != null)
                        {
                            var currentStopIndex = arriveEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                            var newDepartTime = arriveEvent.Vehicle.TripIterator.Current.ScheduledTimeWindows[currentStopIndex][1]; //gets the expected depart time
                            maxInsertedTime = newDepartTime != 0 ? (int)Math.Max(maxInsertedTime, newDepartTime) : maxInsertedTime; //if new depart time != 0,new maxInsertedTime will be the max between maxInsertedtime and the newDepartTime, else the value stays the same.
                            //If maxInsertedTime is still max value between the previous maxInsertedTime and newDepartTime, this means that there has been a delay in the flexible trip (compared to the model generated by the solver)
                        }

                var nextDepartEvent = EventGenerator.GenerateVehicleDepartEvent(arriveEvent.Vehicle, maxInsertedTime + 2);
                AddEvent(nextDepartEvent);


            }
            //END OF INSERTION OF CUSTOMER ENTER, LEAVE VEHICLE EVENTS AND OF VEHICLE DEPART EVENT--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION (APPEND) OF VEHICLE NEXT STOP ARRIVE EVENT
            if (evt.Category == 1 && evt is VehicleStopEvent departEvent)
            {
                    var departTime = departEvent.Time; //the time the vehicle departed on the previous depart event

                    if (departEvent.Vehicle.TripIterator.Current != null)
                    {
                        var  currentStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.IsDummy ? TransportationNetwork.Stops.Find(s => s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.Id) : departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop;//if it is a dummy stop gets the real object in TransportationNetwork stops list
                        var  nextStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.IsDummy ? TransportationNetwork.Stops.Find(s => s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.Id) : departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop;
                        var stopTuple = Tuple.Create(currentStop,nextStop);
                        TransportationNetwork.ArcDictionary.TryGetValue(stopTuple, out var distance);

                        if (distance == 0)
                        {
                            distance = DistanceCalculator.CalculateHaversineDistance(currentStop.Latitude,currentStop.Longitude,nextStop.Latitude,nextStop.Longitude);
                        }
                        var travelTime = DistanceCalculator.DistanceToTravelTime(departEvent.Vehicle.Speed, distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                        departEvent.Vehicle.TripIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
                        var nextArriveEvent = EventGenerator.GenerateVehicleArriveEvent(departEvent.Vehicle, nextArrivalTime); //generates the arrive event
                        AddEvent(nextArriveEvent);
                        //DEBUG!
                        if (departEvent.Vehicle.FlexibleRouting)
                        {
                            var scheduledArrivalTime = departEvent.Vehicle.TripIterator.Current.ScheduledTimeWindows[
                                departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex][0];

                            ConsoleLogger.Log("Event arrival time:"+nextArrivalTime+", Scheduled arrival time:"+scheduledArrivalTime);
                        }
                        //END DEBUG
                    }

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------


            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUESTS-----------------------------------------------------------
          
            if (evt.Category == 5 && evt is DynamicRequestCheckEvent dynamicRequestCheckEvent && evt.Time <= SimulationTimeWindow[1]) // if the event is a dynamic request check event and the current event time is lower than the end time of the simulation
            {

                if (dynamicRequestCheckEvent.GenerateNewDynamicRequest) // checks if the current event dynamic request event check is supposed to generate a new customer dynamic request event
                {
                    var rng = RandomNumberGenerator.Random;
                    var pickup = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                    while (pickup == Depot) //if the pickup is the depot has to generate another pickup stop
                    {
                        pickup = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                    }
                    var delivery = pickup;
                    var distance = DistanceCalculator.CalculateHaversineDistance(pickup.Latitude, pickup.Longitude,
                        delivery.Latitude, delivery.Longitude);
                    var requestTime = evt.Time + 1;
                    while (delivery == pickup || delivery == Depot) //if the delivery stop is equal to the pickup stop or depot stop, it needs to generate a different delivery stop
                    {
                       
                        delivery = TransportationNetwork.Stops[rng.Next(0, TransportationNetwork.Stops.Count)];
                        distance = DistanceCalculator.CalculateHaversineDistance(pickup.Latitude, pickup.Longitude,
                            delivery.Latitude, delivery.Longitude);
                    }
                    var pickupTime = rng.Next(requestTime + 5*60, requestTime + 60 * 60);//the minimum pickup time is 5 minutes above the requestTime and maximum pickup 30 minutes after the request time, 
                    var deliveryTime = rng.Next(pickupTime + 15 * 60, pickupTime + 30 * 60);//delivery time will be at minimum 15 minutes above the pickuptime and at max 30 minutes from the pickup time
                    Stop[] pickupDelivery = new[] { pickup, delivery };
                    long[] desiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
                    var nextCustomerRequestEvent =
                        EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickupDelivery,desiredTimeWindow); //Generates a pickup and delivery customer request (dynamic)
                    AddEvent(nextCustomerRequestEvent);
                }

                var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(evt.Time + 10,DynamicRequestProbabilityThreshold); //generates a new dynamic request check 10 seconds later than the current evt
                AddEvent(eventDynamicRequestCheck);
                
            }
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE ( after a dynamic request has been accepted)
            if (evt.Category == 4 && evt is CustomerRequestEvent customerRequestEvent && customerRequestEvent.SolutionObject != null)
            {            
                var solutionObject = customerRequestEvent.SolutionObject;
                var vehicleFlexibleRouting = VehicleFleet.FindAll(v => v.FlexibleRouting);
                ConsoleLogger.Log("Flexible routing vehicles count: "+vehicleFlexibleRouting.Count);
                foreach (var vehicle in vehicleFlexibleRouting)
                {
                    var solutionRoute = solutionObject.GetVehicleStops(vehicle);
                    var solutionTimeWindows = solutionObject.GetVehicleTimeWindows(vehicle);

                        if (vehicle.TripIterator.Current != null)
                        {
                            var currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                            var currentStopList = new List<Stop>(vehicle.TripIterator.Current.Stops); //current stoplist for vehicle (before adding the new request)
                            var currentTimeWindows = new List<long[]>(vehicle.TripIterator.Current.ScheduledTimeWindows);
                            var customers = solutionObject.GetVehicleCustomers(vehicle); //contains all customers (already inside and not yet in vehicle)
                            List<Stop> visitedStops = new List<Stop>();
                            List<long[]> visitedTimeWindows = new List<long[]>();
                            ConsoleLogger.Log("Vehicle "+vehicle.Id+":");
                            ConsoleLogger.Log("Current stop: "+currentStopList[currentStopIndex].ToString());
                            //construction of already visited stops list
                            if (currentStopIndex > 0)
                            {
                                ConsoleLogger.Log("Visited stops:");
                                for (int index = 0; index < currentStopIndex; index++)
                                {
                                    visitedStops.Add(vehicle.TripIterator.Current.VisitedStops[index]);
                                    visitedTimeWindows.Add(vehicle.TripIterator.Current.StopsTimeWindows[index]);
                                    ConsoleLogger.Log(currentStopList[index].ToString() + " - " +
                                                      vehicle.TripIterator.Current.VisitedStops[index].ToString());
                                    //ConsoleLogger.Log(currentStopList[index].ToString()+ " - TW:{" + currentTimeWindows[index][0] + "," + currentTimeWindows[index][1] + "}");
                                }
                            }

                            //end of visited stops list construction
                            //inserts the already visited stops at the beginning of the  solutionRoute list
                            for (int e = visitedStops.Count-1;e>=0;e--)
                            {
                              
                                    solutionRoute.Insert(0, visitedStops[e]);
                                    solutionTimeWindows.Insert(0, visitedTimeWindows[e]);
                            }
                            vehicle.TripIterator.Current.AssignStops(solutionRoute,solutionTimeWindows,currentStopIndex);
                            vehicle.TripIterator.Current.ExpectedCustomers = customers.FindAll(c=>!c.IsInVehicle);//the expected customers for the current vehicle are the ones that are not in that vehicle
                            vehicle.PrintRoute(vehicle.TripIterator.Current.Stops,vehicle.TripIterator.Current.ScheduledTimeWindows,customers);

                            var vehicleEvents = Events.FindAll(e => (e is VehicleStopEvent vse && vse.Vehicle == vehicle && vse.Time >= evt.Time)  || (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle && cve.Time >= evt.Time)).OrderBy(e => e.Time).ThenBy(e => e.Category).ToList(); //gets all next vehicle depart or arrive events
                            ConsoleLogger.Log("ALL NEXT VEHICLE "+vehicle.Id+" EVENTS (COUNT:"+vehicleEvents.Count+") (TIME >=" +evt.Time+ "):");
                            foreach (var vEvent in vehicleEvents)
                            {
                                if (vEvent is VehicleStopEvent vehicleStopArriveEvent && vEvent.Category == 0) //vehicle arrive stop event
                                {
                                    ConsoleLogger.Log(vehicleStopArriveEvent.GetTraceMessage());
                                }

                                if (vEvent is VehicleStopEvent vehicleStopDepartEvent && vEvent.Category == 1) //vehicle depart stop event
                                {
                                    ConsoleLogger.Log(vehicleStopDepartEvent.GetTraceMessage());
                                    if (vehicleStopDepartEvent.Stop == vehicle.TripIterator.Current.StopsIterator.CurrentStop)
                                    {
                                        ConsoleLogger.Log("New event depart: "+ (vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1] + 2));
                                        vEvent.Time = (int) vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1]+1; //recalculates new event depart time
                                    }
                                }

                                if (vEvent is CustomerVehicleEvent customerVehicleEvent && (vEvent.Category == 2 || vEvent.Category == 3)) //if customer enter vehicle or leave vehicle event
                                {
                                    ConsoleLogger.Log(customerVehicleEvent.GetTraceMessage());
                                    Events.Remove(vEvent);     
                                }
                            }

                        }
                }

            }
            //END OF INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE
            if (currentNumberOfEvents != Events.Count) //If the size of the events list has changed, the event list has to be sorted
                SortEvents();
        }


        public override void Handle(Event evt)
        {
         
            evt.Treat();
            TotalEventsHandled++;

            var msg = evt.GetTraceMessage();
            if (msg != "")
            {
                _eventLogger.Log(evt.GetTraceMessage());
            }

            switch (evt)
            {
                case CustomerVehicleEvent customerVehicleEvent:
                    _validationsLogger.Log(customerVehicleEvent.GetValidationsMessage(_validationsCounter));
                    _validationsCounter++;
                    break;
                case CustomerRequestEvent customerRequestEvent:
                        TotalDynamicRequests++;
                        var newCustomer = customerRequestEvent.Customer;
                    if (VehicleFleet.FindAll(v=>v.FlexibleRouting == true).Count>0 && newCustomer != null && TotalServedDynamicRequests <1)
                    {
                        var flexibleRoutingVehicles = VehicleFleet.FindAll(v => v.FlexibleRouting);
                        List<Stop> startDepots = new List<Stop>();
                        List<Stop> endDepots = new List<Stop>();
                        List<Vehicle> dataModelVehicles = new List<Vehicle>();
                        List<Customer> allExpectedCustomers = new List<Customer>();
                        foreach (var vehicle in flexibleRoutingVehicles)
                        {
                           
                                dataModelVehicles.Add(vehicle);
                                if (vehicle.TripIterator.Current != null)
                                {
                                    List<Customer> expectedCustomers = new List<Customer>(vehicle.TripIterator.Current.ExpectedCustomers);
                                    foreach (var customer in expectedCustomers)
                                    {
                                        if (!allExpectedCustomers.Contains(customer))
                                        {
                                            allExpectedCustomers.Add(customer);
                                        }
                                    }
                                    List<Customer> currentCustomers = vehicle.Customers;
                                    foreach (var currentCustomer in currentCustomers)
                                    {
                                        if (!allExpectedCustomers.Contains(currentCustomer))
                                        {
                                            allExpectedCustomers.Add(currentCustomer);
                                        }
                                    }
                                  

                                    foreach (var customer in allExpectedCustomers)
                                    {
                                        if (customer.IsInVehicle)
                                        {
                                            var v = VehicleFleet.Find(veh => veh.Customers.Contains(customer));
                                            ConsoleLogger.Log(" Customer "+customer.Id+" is already inside vehicle"+v.Id+": Already visited: " + customer.PickupDelivery[0] +
                                                              ", Need to visit:" + customer.PickupDelivery[1]);
                                        }
                                    }
                                    var currentStop = vehicle.TripIterator.Current.StopsIterator.CurrentStop;
                                    var dummyStop = new Stop(currentStop.Id, currentStop.Code,"dummy "+currentStop.Name, currentStop.Latitude,
                                        currentStop.Longitude);//need to use dummyStop otherwise the solver will fail, because the startDepot stop is also a pickup delivery stop
                                    dummyStop.IsDummy = true;
                                    startDepots.Add(dummyStop);
                                    endDepots.Add(Depot);
                                    expectedCustomers.Add(newCustomer); //adds the new dynamic customer
                                    if (!allExpectedCustomers.Contains(newCustomer))
                                    {
                                        allExpectedCustomers.Add(newCustomer);
                                    }
                                }

                        }
                        
                        //--------------------------------------------------------------------------------------------------------------------------
                        //Calculation of startDepotArrivalTime, if there is any moving vehicle, otherwise startDepotArrivalTime will be the current event Time
                        var movingVehicles = VehicleFleet.FindAll(v => !v.IsIdle && v.FlexibleRouting);
                        long[] startDepotsArrivalTimes = new long[dataModelVehicles.Count];
                        for (int i = 0; i < dataModelVehicles.Count ; i++)
                        {
                            startDepotsArrivalTimes[i] = evt.Time; //initializes startDepotArrivalTimes with the current event time
                        }
                        if (movingVehicles.Count > 0)//if there is a moving vehicle calculates the baseArrivalTime
                        {
                            ConsoleLogger.Log("Moving vehicles total:" + movingVehicles.Count);
                            foreach (var movingVehicle in movingVehicles)
                            {
                                var vehicleArrivalEvents = Events.FindAll(e =>
                                    e is VehicleStopEvent vse && e.Category == 0 && e.Time >= evt.Time && vse.Vehicle == movingVehicle);
                                foreach (var arrivalEvent in vehicleArrivalEvents)
                                {
                                    if (arrivalEvent is VehicleStopEvent vehicleStopEvent)
                                    {
                                        if (movingVehicle.TripIterator.Current != null && movingVehicle.TripIterator.Current.StopsIterator.CurrentStop == vehicleStopEvent.Stop)
                                        {
                                            var currentStartDepotArrivalTime = startDepotsArrivalTimes[dataModelVehicles.IndexOf(movingVehicle)];
                                            startDepotsArrivalTimes[dataModelVehicles.IndexOf(movingVehicle)] = Math.Max(vehicleStopEvent.Time, (int)currentStartDepotArrivalTime); //finds the biggest value between the current baseArrivalTime and the current vehicle's next stop arrival time, and updates its value on the array
                                        }
                                    }
                                }
                            }
                        }
                        //end of calculation of startDepotsArrivalTime
                        //--------------------------------------------------------------------------------------------------------------------------

                        var dataModel = new RoutingDataModel(startDepots, endDepots, dataModelVehicles, allExpectedCustomers,startDepotsArrivalTimes,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
                        var solver = new RoutingSolver(dataModel,false);
                        var solution = solver.TryGetFastSolution();
                        if (solution != null)
                        {
                            dataModel.PrintPickupDeliveries();
                            dataModel.PrintTimeWindows();
                            //dataModel.PrintTimeMatrix();
                            solver.PrintSolution(solution);
                            TotalServedDynamicRequests++;
                            ConsoleLogger.Log(newCustomer.ToString() + " was inserted into a vehicle service at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString() );

                            var solutionObject = solver.GetSolutionObject(solution);
                            customerRequestEvent.SolutionObject = solutionObject;
                        }
                        else
                        {
                            ConsoleLogger.Log(newCustomer.ToString() + " was not possible to be served at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());
                        }
                    }
                    break;
            }
        }
    }
}