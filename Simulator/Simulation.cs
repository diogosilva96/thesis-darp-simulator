using System;
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

        public int VehicleSpeed;

        public int VehicleCapacity;

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

        private SimulationIO SimulationIO;
        public string CurrentSimulationLoggerPath;


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
            VehicleCapacity = 20;
            VehicleSpeed = 30;
            DynamicRequestProbabilityThreshold = dynamicRequestProbability;
            SimulationTimeWindow = new int[2];
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 4*60*60; // 8hours in seconds
            SimulationIO = new SimulationIO(this);
            MaxCustomerRideTime = maxCustomerRideTimeSeconds;
            MaxAllowedUpperBoundTime = maxAllowedUpperBoundTimeSeconds;
            Depot = TransportationNetwork.Stops.Find(s => s.Id == 2183);
        }

        public void Init()
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var auxTime = currentTime.Split(":");
            currentTime = auxTime[0] + auxTime[1] +auxTime[2];
            CurrentSimulationLoggerPath = Path.Combine(_loggerBasePath, currentTime);
            if (!Directory.Exists(CurrentSimulationLoggerPath))
            {
                Directory.CreateDirectory(CurrentSimulationLoggerPath);
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
            IRecorder fileRecorder = new FileRecorder(Path.Combine(CurrentSimulationLoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(CurrentSimulationLoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
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
                var option = SimulationIO.GetMainMenuOption();
                switch (option)
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
                        ConfigSimulationOption();
                        break;
                    case 5:
                        Environment.Exit(0);
                        break;
                    default:
                        StandardBusRouteOption();
                        break;
                }
                if (Events.Count > 0) //it means there is the need to simulate
                {
                    SimulationIO.PrintSimulationSettings();
                    Simulate();
                    SimulationIO.PrintSimulationStats();
                }
            }
        }


        public void ConfigSimulationOption()
        {
           SimulationIO.ConfigSimulationMenu();
        }
        public RoutingDataModel GenerateRandomInitialDataModel()
        {
            var numberCustomers = SimulationIO.GetNumberCustomersMenuOption();
            var vehicleNumber = SimulationIO.GetNumberVehiclesMenuOption();
            GenerateNewDataModelLabel:
            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            List<Stop> startDepots = new List<Stop>(); //array with the start depot for each vehicle, each index is a vehicle
            List<Stop> endDepots = new List<Stop>();//array with the end depot for each vehicle, each index is a vehicle
            List<long> startDepotsArrivalTime = new List<long>(vehicleNumber);
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < vehicleNumber; i++)
            {
                dataModelVehicles.Add(new Vehicle(VehicleSpeed, 20, true));
                startDepots.Add(Depot);
                endDepots.Add(Depot);
                startDepotsArrivalTime.Add(0);//startDepotArrival time  = 0 for all the vehicles
            }

            var customersToBeServed = new List<Customer>();
            var customerGenerator = new CustomerGenerator();
            List<Stop> excludedStops = new List<Stop>();
            excludedStops.Add(Depot);
            for (int i = 0; i < numberCustomers; i++) //generate 5 customers with random timeWindows and random pickup and delivery stops
            {
                var requestTime = 0;
                var pickupTimeWindow = new int[] {requestTime, SimulationTimeWindow[1]};//the customer pickup time will be between the current request time and the end of simulation time
                var customer = customerGenerator.GenerateRandomCustomer(TransportationNetwork.Stops, excludedStops, requestTime,
                    pickupTimeWindow);
                customersToBeServed.Add(customer);
            }
            var indexManager = new DataModelIndexManager(startDepots,endDepots,dataModelVehicles,customersToBeServed,startDepotsArrivalTime);
            var routingDataModel = new RoutingDataModel(indexManager,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
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
            var dataModel=GenerateRandomInitialDataModel();
            if (dataModel != null)
            {
                RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                dataModel.PrintDataModelSettings();
                dataModel.PrintPickupDeliveries();
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
            IRecorder algorithmsRecorder = new FileRecorder(Path.Combine(CurrentSimulationLoggerPath, @"algorithms.txt"));
            var algorithmsLogger = new Logger.Logger(algorithmsRecorder);
            for (int i = 0; i < 2; i++)// tests 10 different data models
            {
                var allowDropNodes = SimulationIO.GetAllowDropNodesMenuOption();
                var dataModel = SimulationIO.GetAlgorithmComparisonMenuDataModelOption();
                var searchTime = SimulationIO.GetSearchTimeLimitMenuOption();
                dataModel.PrintDataModelSettings();
                AlgorithmStatistics algorithmStatistics = new AlgorithmStatistics(dataModel);
                var algorithmStatList = algorithmStatistics.GetSearchAlgorithmsResultsList(searchTime, allowDropNodes);
                var printableList = algorithmStatistics.GetPrintableStatisticsList(algorithmStatList);
                var loggerList = algorithmStatistics.GetFileLoggerList(algorithmStatList);

                foreach (var printableItem in printableList)
                {
                    SimulationIO.Print(printableItem);
                }

                foreach (var listItem in loggerList)
                {
                    algorithmsLogger.Log(listItem);
                }
            }
        }

        public void StandardBusRouteOption()
        {
            SimulationIO.ConfigStartEndHourMenu();
            AssignAllConventionalTripsToVehicles();
            InitSimulationLoggers();
            InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)
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
                throw new ArgumentNullException("Routing solution object is null");
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
                            var v = new Vehicle(VehicleSpeed, VehicleCapacity,false);
                            v.AddTrip(trip); //Adds the service
                            VehicleFleet.Add(v);
                            tripCount++;
                        }
                    }
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
                                    SimulationIO.Print("Vehicle expected depart time" + currentVehicleTrip.ScheduledTimeWindows[currentVehicleTrip.StopsIterator.CurrentIndex][1] + " customer arrival time:" + customer.DesiredTimeWindow[0]);
                                    if (sameStops.Count > 1)
                                    {
                                        SimulationIO.Print("SameStops");
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
                        if (departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop != null)
                        {
                            var nextStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.IsDummy
                                ? TransportationNetwork.Stops.Find(s =>
                                    s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.Id)
                                : departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop;
                            var stopTuple = Tuple.Create(currentStop, nextStop);
                            TransportationNetwork.ArcDictionary.TryGetValue(stopTuple, out var distance);

                            if (distance == 0)
                            {
                                distance = DistanceCalculator.CalculateHaversineDistance(currentStop.Latitude,
                                    currentStop.Longitude, nextStop.Latitude, nextStop.Longitude);
                            }

                            var travelTime =
                                DistanceCalculator.DistanceToTravelTime(departEvent.Vehicle.Speed,
                                    distance); //Gets the time it takes to travel from the currentStop to the nextStop
                            var nextArrivalTime =
                                Convert.ToInt32(departTime +
                                                travelTime); //computes the arrival time for the next arrive event
                            departEvent.Vehicle.TripIterator.Current.StopsIterator
                                .Next(); //Moves the iterator to the next stop
                            var nextArriveEvent =
                                EventGenerator.GenerateVehicleArriveEvent(departEvent.Vehicle,
                                    nextArrivalTime); //generates the arrive event
                            AddEvent(nextArriveEvent);
                            //DEBUG!
                            if (departEvent.Vehicle.FlexibleRouting)
                            {
                                var scheduledArrivalTime =
                                    departEvent.Vehicle.TripIterator.Current.ScheduledTimeWindows[
                                        departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex][0];

                                SimulationIO.Print("Event arrival time:" + nextArrivalTime +
                                                  ", Scheduled arrival time:" + scheduledArrivalTime);
                            }
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
                    List<Stop> excludedStops = new List<Stop>();
                    excludedStops.Add(Depot);
                    var requestTime = evt.Time + 1;                 
                    var pickupTimeWindow = new int[] {requestTime + 5 * 60, requestTime + 60 * 60};
                    var customer = new CustomerGenerator().GenerateRandomCustomer(TransportationNetwork.Stops,excludedStops,requestTime,pickupTimeWindow);
                    var nextCustomerRequestEvent =
                        EventGenerator.GenerateCustomerRequestEvent(requestTime, customer); //Generates a pickup and delivery customer request (dynamic)
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
                SimulationIO.Print("Flexible routing vehicles count: "+vehicleFlexibleRouting.Count);
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
                            SimulationIO.Print("Vehicle " + vehicle.Id + ":");
                            SimulationIO.Print("Current stop: " +currentStopList[currentStopIndex].ToString());
                            //construction of already visited stops list
                            if (currentStopIndex > 0)
                            {
                                SimulationIO.Print("Visited stops:");
                                for (int index = 0; index < currentStopIndex; index++)
                                {
                                    visitedStops.Add(vehicle.TripIterator.Current.VisitedStops[index]);
                                    visitedTimeWindows.Add(vehicle.TripIterator.Current.StopsTimeWindows[index]);
                                    SimulationIO.Print(currentStopList[index].ToString() + " - " +
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
                        SimulationIO.Print("ALL NEXT VEHICLE " +vehicle.Id+" EVENTS (COUNT:"+vehicleEvents.Count+") (TIME >=" +evt.Time+ "):");
                            foreach (var vEvent in vehicleEvents)
                            {
                                if (vEvent is VehicleStopEvent vehicleStopArriveEvent && vEvent.Category == 0) //vehicle arrive stop event
                                {
                                    SimulationIO.Print(vehicleStopArriveEvent.GetTraceMessage());
                                }

                                if (vEvent is VehicleStopEvent vehicleStopDepartEvent && vEvent.Category == 1) //vehicle depart stop event
                                {
                                    SimulationIO.Print(vehicleStopDepartEvent.GetTraceMessage());
                                    if (vehicleStopDepartEvent.Stop == vehicle.TripIterator.Current.StopsIterator.CurrentStop)
                                    {
                                    SimulationIO.Print("New event depart: " + (vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1] + 2));
                                        vEvent.Time = (int) vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1]+1; //recalculates new event depart time
                                    }
                                }

                                if (vEvent is CustomerVehicleEvent customerVehicleEvent && (vEvent.Category == 2 || vEvent.Category == 3)) //if customer enter vehicle or leave vehicle event
                                {
                                    SimulationIO.Print(customerVehicleEvent.GetTraceMessage());
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
                    if (VehicleFleet.FindAll(v=>v.FlexibleRouting == true).Count>0 && newCustomer != null && VehicleFleet.FindAll(v=>v.TripIterator.Current != null && !v.TripIterator.Current.IsDone).Count>0)
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
                                        SimulationIO.Print(" Customer " +customer.Id+" is already inside vehicle"+v.Id+": Already visited: " + customer.PickupDelivery[0] +
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
                        List<long> startDepotArrivalTimesList = new List<long>(dataModelVehicles.Count);
                        for (int i = 0; i < dataModelVehicles.Count ; i++)
                        {
                            startDepotArrivalTimesList.Add(evt.Time);//initializes startDepotArrivalTimes with the current event time
                        }
                        if (movingVehicles.Count > 0)//if there is a moving vehicle calculates the baseArrivalTime
                        {
                            SimulationIO.Print("Moving vehicles total:" + movingVehicles.Count);
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
                                            var currentStartDepotArrivalTime = startDepotArrivalTimesList[dataModelVehicles.IndexOf(movingVehicle)];
                                            startDepotArrivalTimesList[dataModelVehicles.IndexOf(movingVehicle)] = Math.Max(vehicleStopEvent.Time, (int)currentStartDepotArrivalTime); //finds the biggest value between the current baseArrivalTime and the current vehicle's next stop arrival time, and updates its value on the array
                                        }
                                    }
                                }
                            }
                        }
                        //end of calculation of startDepotsArrivalTime
                        //--------------------------------------------------------------------------------------------------------------------------
                        var indexManager = new DataModelIndexManager(startDepots,endDepots,dataModelVehicles,allExpectedCustomers,startDepotArrivalTimesList);
                        var dataModel = new RoutingDataModel(indexManager,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
                        var solver = new RoutingSolver(dataModel,false);
                        var solution = solver.TryGetFastSolution();
                        if (solution != null)
                        {
                            dataModel.PrintPickupDeliveries();
                            dataModel.PrintTimeWindows();
                            //dataModel.PrintTimeMatrix();
                            solver.PrintSolution(solution);
                            TotalServedDynamicRequests++;
                            SimulationIO.Print(newCustomer.ToString() + " was inserted into a vehicle service at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString() );

                            var solutionObject = solver.GetSolutionObject(solution);
                            customerRequestEvent.SolutionObject = solutionObject;
                        }
                        else
                        {
                            SimulationIO.Print(newCustomer.ToString() + " was not possible to be served at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());
                        }
                    }
                    break;
            }
        }
    }
}