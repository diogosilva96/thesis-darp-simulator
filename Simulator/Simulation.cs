using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.OrTools.ConstraintSolver;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Algorithms;
using Simulator.Objects.Data_Objects.DARP;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private readonly Logger.Logger _eventLogger;

        private readonly Logger.Logger _validationsLogger;

        private int _validationsCounter;

        public DarpDataModel DarpDataModel;

        private readonly int _vehicleSpeed;

        private readonly int _vehicleCapacity;

        public int TotalDynamicRequests;

        public int TotalServedDynamicRequests;

        public int[] SimulationTimeWindow;

        public int MaxCustomerRideTime = 45* 60; //max customer ride time 

        public int MaxAllowedUpperBoundTime = 30 * 60;//max delay of timeWindows upperBound (relative to desired timeWindows)

        public Stop Depot; //the stop that every flexible service vehicle starts and ends at 

        public int TotalSimulationTime => SimulationTimeWindow[0] >= 0 && SimulationTimeWindow[1] >= 0 && SimulationTimeWindow[1] != 0 ? SimulationTimeWindow[1] - SimulationTimeWindow[0] : 0; //in seconds

        private readonly double _dynamicRequestProbabilityThreshold; //randomly generated value has to be
                                                                     //than this in order to generate a dynamic request


        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            _vehicleCapacity = 20;
            _vehicleSpeed = 30;
            _dynamicRequestProbabilityThreshold = 0.02;
            SimulationTimeWindow = new int[2];
            TotalDynamicRequests = 0;
            TotalServedDynamicRequests = 0;
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 24 * 24 * 60; // 24hours in seconds
            Depot = TransportationNetwork.Stops.Find(s => s.Id == 2183);
        }

        public override void Init()
        {
            TotalEventsHandled = 0;
            _validationsCounter = 1;
            Events.Clear(); //clears all events 
            VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
            
        }
        public override void InitEvents()
        {
            var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(SimulationTimeWindow[0], _dynamicRequestProbabilityThreshold);//initializes dynamic requests
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
        
        public override void DisplayOptionsMenu()
        {
            var numOptions = 4;
            ConsoleLogger.Log("Please Select one of the options:");
            ConsoleLogger.Log("1 - Standard Bus route simulation");
            ConsoleLogger.Log("2 - Flexible Bus route simulation");
            ConsoleLogger.Log("3 - Algorithms Test & Results");
            ConsoleLogger.Log("4 - Single Bus route flexible simulation");
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
                case 2:FlexibleBusRouteOption();
                    break;
                case 3:AlgorithmComparisonOption();
                    break;
                case 4:SingleBusRouteFlexibleOption();
                    break;
                default: StandardBusRouteOption();
                    break;
            }
        }

        public void InitDataModel(int vehicleNumber)
        {
            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            List<Stop> startDepots = new List<Stop>(); //array with the start depot for each vehicle, each index is a vehicle
            List<Stop> endDepots = new List<Stop>();//array with the end depot for each vehicle, each index is a vehicle
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < vehicleNumber; i++)
            {
                dataModelVehicles.Add(new Vehicle(_vehicleSpeed, 20, TransportationNetwork.ArcDictionary, true));
                startDepots.Add(Depot);
               // startDepots.Add(null); //dummy start depot
                endDepots.Add(Depot);
               // endDepots.Add(null);//dummy end depot
            }

            var customersToBeServed = new List<Customer>();
            // Pickup and deliveries definition using static generated stop requests
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new long[] { 2250, 3500 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359) }, new long[] { 1000, 2700 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018) }, new long[] { 2200, 4000 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523) }, new long[] { 2000, 2900 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884) }, new long[] { 2300, 2900 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555) }, new long[] { 1900, 2300 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2200) }, new long[] { 1500, 3000 }, 0));
            long[] startDepotsArrivalTime = new long[] { 0, 0 };
            DarpDataModel = new DarpDataModel(startDepots,endDepots, dataModelVehicles, customersToBeServed,startDepotsArrivalTime,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
            //Print datamodel data
            DarpDataModel.PrintTimeMatrix();
            DarpDataModel.PrintPickupDeliveries();
            DarpDataModel.PrintTimeWindows();
        }

        public void FlexibleBusRouteOption()
        {
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = (int)TimeSpan.FromHours(3).TotalSeconds;
            InitDataModel(2);
            if (DarpDataModel != null)
            {
                DarpSolver darpSolver = new DarpSolver(false);

                var timeWindowSolution = darpSolver.TryGetFastSolution(DarpDataModel);
                
                DarpDataModel.DistanceMatrix = new MatrixBuilder().GetDistanceMatrix(DarpDataModel.IndexManager.Stops);
                DarpSolutionObject darpSolutionObject = null;
;
                if (timeWindowSolution != null)
                {
                    darpSolver.PrintSolution(timeWindowSolution);
                    darpSolutionObject = darpSolver.GetSolutionObject(timeWindowSolution);
                   

                }

                AssignVehicleFlexibleTrips(darpSolutionObject, SimulationTimeWindow[0]);
            }

        }
        public void SingleBusRouteFlexibleOption()
        {
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = (int)TimeSpan.FromHours(3).TotalSeconds;
            Random rand = new Random();
            InitDataModel(2);



            DarpSolver darpSolver = new DarpSolver(false);
            Assignment timeWindowSolution = null;
            timeWindowSolution = darpSolver.TryGetFastSolution(DarpDataModel);
            if (timeWindowSolution != null)
            {
                darpSolver.PrintSolution(timeWindowSolution);
                DarpSolutionObject darpSolutionObject = darpSolver.GetSolutionObject(timeWindowSolution);

                ///////////////////////////////////////////////////////////////////////
               var darps1 = new DarpSolver(false);
                List<DarpSolutionObject> solutionObjects = new List<DarpSolutionObject>();
                for (int j = 0; j < darpSolutionObject.VehicleNumber; j++)
                {
                    List<Stop> startDepots = new List<Stop>();
                    List<Stop> endDepots = new List<Stop>();
                    List<Vehicle> vehicles = new List<Vehicle>();
                    var vehicle = darpSolutionObject.IndexToVehicle(j);
                    vehicles.Add(vehicle);
                    var stops = darpSolutionObject.GetVehicleStops(vehicle);
                    var vehicleTW = darpSolutionObject.GetVehicleTimeWindows(vehicle);
                    List<Customer> customers = darpSolutionObject.GetVehicleCustomers(vehicle);



                    List<Customer> customersToBeRemoved = new List<Customer>();
                    var numStops = 2;
                    var baseStop = stops[numStops + 1];
                    foreach (var customer in customers)
                    {
                        var index = 0;
                        foreach (var stop in stops)
                        {
                            if (index > numStops)
                            {
                                break;
                            }

                            if (stops.FindIndex(s => s == customer.PickupDelivery[0]) <=
                                stops.FindIndex(s => s == customer.PickupDelivery[1]) &&
                                customer.PickupDelivery[1] == stop)
                            {
                                customersToBeRemoved.Add(customer);

                            }

                            if (customer.PickupDelivery[0] == stop &&
                                stops.FindIndex(s => s == customer.PickupDelivery[1]) > numStops)
                            {
                                customer.PickupDelivery[0] = stops[numStops + 1];

                            }

                            index++;
                        }

                        if (customer.PickupDelivery[1] == stops[numStops + 1])
                        {
                            customersToBeRemoved.Add(customer);
                        }
                    }

                    foreach (var cust in customersToBeRemoved)
                    {
                        customers.Remove(cust);
                    }



                    for (int i = 0; i <= numStops; i++)
                    {
                        stops.RemoveAt(0); //removes the first numstops of the stops list
                        vehicleTW.RemoveAt(0);
                    }

                    var customersToBeUpdated = customers.FindAll(c => c.PickupDelivery[0] == baseStop);
                    foreach (var cust in customersToBeUpdated)
                    {
                        var newPickupTimeIndex =
                            darpSolutionObject.GetVehicleStops(vehicle).FindIndex(s => s == baseStop);
                        cust.DesiredTimeWindow[0] =
                            (int) vehicleTW[
                                    newPickupTimeIndex]
                                [0]; //updates the timewindow so that it uses the current time of the simulation
                    }

                    startDepots.Add(null);
                    endDepots.Add(Depot);
                    customers.Add(new Customer(
                        new Stop[]
                        {
                            TransportationNetwork.Stops.Find(stop1 => stop1.Id == 450),
                            TransportationNetwork.Stops.Find(stop1 => stop1.Id == 385)
                        }, new long[] {4500, 6000}, 0));
                    long[] startDepotsArrivalTime = new long[]{0,0};
                    var darpM = new DarpDataModel(startDepots, endDepots, vehicles, customers,startDepotsArrivalTime,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
                    darpM.PrintTimeMatrix();
                    darpM.PrintPickupDeliveries();
                    darpM.PrintTimeWindows();
                    var sol = darps1.TryGetFastSolution(darpM);
                    if (sol != null)
                    {
                        solutionObjects.Add(darps1.GetSolutionObject(sol));
                    }
                    else
                    {
                        ConsoleLogger.Log("No sol");
                    }
                }

                if (solutionObjects.Count > 0)
                {
                    foreach (var solution in solutionObjects)
                    {
                        ConsoleLogger.Log("Solution:");
                        ConsoleLogger.Log("Total Distance: "+solution.TotalDistanceInMeters);
                        ConsoleLogger.Log("Load: "+solution.TotalLoad);
                        ConsoleLogger.Log("Total Time: "+solution.TotalTimeInSeconds);
                        ConsoleLogger.Log("Route:");
                        foreach (var stop in solution.GetVehicleStops(solution.IndexToVehicle(0)))
                        {
                            if (stop != null)
                            {
                                ConsoleLogger.Log(stop.ToString());
                            }
                        }
                        ConsoleLogger.Log("");
                    }
                }
                AssignVehicleFlexibleTrips(darpSolutionObject,SimulationTimeWindow[0]);
            }
            else
            {
                ConsoleLogger.Log("Solution not found!");
            }
        }


        public void AlgorithmComparisonOption()
        {
            bool allowDropNodes = AllowDropNodesMenu();
            InitDataModel(2);
            AlgorithmStatistics algorithmStatistics = new AlgorithmStatistics(DarpDataModel);
            var algorithmStatList = algorithmStatistics.GetSearchAlgorithmsResultsList(10,allowDropNodes);
            var printList = algorithmStatistics.GetPrintableStatisticsList(algorithmStatList);
            foreach (var printableItem in printList)
            {
                ConsoleLogger.Log(printableItem);
            }
        }

        public bool AllowDropNodesMenu()
        {
            wrongKeyLabel:
            ConsoleLogger.Log("Allow drop nodes penalties?");
            ConsoleLogger.Log("1 - Yes");
            ConsoleLogger.Log("2 - No");
            int key = 0;
            try
            {
                key = int.Parse(Console.ReadLine());
                if (key <= 0 && key > 2)
                {
                    ConsoleLogger.Log("Wrong input, please retype using a valid input!");
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {
                goto wrongKeyLabel;
            }

            var allowDropNodes = key == 1;
            return allowDropNodes;
        }
        public void StandardBusRouteOption()
        {

            DisplayStartEndHourMenu();
            AssignAllConventionalTripsToVehicles();
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
        private void AssignVehicleFlexibleTrips(DarpSolutionObject darpSolutionObject,int time)
        {
            if (darpSolutionObject != null)
            {
                //Adds the flexible trip vehicles to the vehicleFleet
                for (int j = 0; j < darpSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                {
                    var solutionVehicle = darpSolutionObject.IndexToVehicle(j);
                    var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                    trip.StartTime =
                       time+(int)darpSolutionObject.GetVehicleTimeWindows(solutionVehicle)[0][0]; //start time, might need to change!
                    trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                    trip.Stops = darpSolutionObject.GetVehicleStops(solutionVehicle);
                    trip.ExpectedCustomers = darpSolutionObject.GetVehicleCustomers(solutionVehicle);
                    trip.ExpectedTimeWindows = darpSolutionObject.GetVehicleTimeWindows(solutionVehicle);
                    solutionVehicle.AddTrip(trip); //adds the new flexible trip to the vehicle
                    
                   
                    VehicleFleet.Add(solutionVehicle); //adds the vehicle to the vehicle fleet
                }
            }
            else
            {
                ConsoleLogger.Log("darpSolutionObject is null.");
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

                            var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, TransportationNetwork.ArcDictionary,
                                false);
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

        public override void PrintSimulationSettings()
        {
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("|     Simulation Settings     |");
            ConsoleLogger.Log("-------------------------------");
            ConsoleLogger.Log("Number of vehicles:" + VehicleFleet.Count);
            ConsoleLogger.Log("Vehicle average speed: " + _vehicleSpeed + " km/h.");
            ConsoleLogger.Log("Vehicle capacity: " + _vehicleCapacity + " seats.");
            List<Route> distinctRoutes = new List<Route>();
            foreach (var vehicle in VehicleFleet)
            {
                if (vehicle.TripIterator != null)
                {
                    vehicle.TripIterator.Reset();

                    while (vehicle.TripIterator.MoveNext()) //iterates over each vehicle service
                    {
                        if (vehicle.TripIterator.Current != null)
                        {
                            var route = vehicle.TripIterator.Current.Route;
                            if (!distinctRoutes.Contains(route)) //if the route isn't in distinct routes list adds it
                            {
                                distinctRoutes.Add(route);
                            }
                        }
                    }
                    vehicle.TripIterator.Reset();
                    vehicle.TripIterator.MoveNext();
                }
            }
            ConsoleLogger.Log("Number of distinct routes:"+distinctRoutes.Count);

        }



        public override void PrintSimulationStatistics()
        {
            IRecorder fileRecorder =
                new FileRecorder(Path.Combine(Environment.CurrentDirectory, @"Logger/stats_logs.txt"));
            var myFileLogger = new Logger.Logger(fileRecorder);
            var toPrintList = new List<string>();
            toPrintList.Add(ToString() + "Total number of events handled: " +
                            Events.FindAll(e => e.AlreadyHandled).Count + " out of " + Events.Count + ".");
            toPrintList.Add(ToString() + "Vehicle Fleet Size: " + VehicleFleet.Count + " vehicle(s).");
            toPrintList.Add(ToString()+ "Average dynamic requests per hour: "+TotalDynamicRequests / TimeSpan.FromSeconds(TotalSimulationTime).TotalHours);
            toPrintList.Add(ToString() + "Total simulation time: "+TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours.");
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");
           
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
            if (evt.Category == 0 && evt is VehicleStopEvent eventArrive)
            {
                var arrivalTime = evt.Time;
                var customerLeaveVehicleEvents = EventGenerator.GenerateCustomerLeaveVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, arrivalTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = customerLeaveVehicleEvents.Count > 0 ? customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time : arrivalTime;

                List<Event> customersEnterVehicleEvents = null;
                if (eventArrive.Vehicle.TripIterator.Current != null && eventArrive.Vehicle.TripIterator.Current.HasStarted)
                {
                    int expectedDemand = 0;
                    try
                    {
                        expectedDemand = !eventArrive.Vehicle.FlexibleRouting ? TransportationNetwork.DemandsDataObject.GetDemand(eventArrive.Stop.Id, eventArrive.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(eventArrive.Time).Hours) : 0;
                 
                    }
                    catch (Exception)
                    {
                        expectedDemand = 0;
                    }

                    customersEnterVehicleEvents = EventGenerator.GenerateCustomersEnterVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, lastInsertedLeaveTime, expectedDemand);
                    if (customersEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = customersEnterVehicleEvents[customersEnterVehicleEvents.Count - 1].Time;
                }
       
                AddEvent(customersEnterVehicleEvents);
                AddEvent(customerLeaveVehicleEvents);


                var maxInsertedTime = Math.Max(lastInsertedEnterTime, lastInsertedLeaveTime); ; //gets the highest value of the last insertion in order to maintain precedence constraints for the depart evt, meaning that the stop depart only happens after every customer has already entered and left the vehicle on that stop location

                //INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS!
        
                   
                        if (eventArrive.Vehicle.TripIterator.Current != null && eventArrive.Vehicle.FlexibleRouting)
                        {
                            var customersToEnterAtCurrentStop = eventArrive.Vehicle.TripIterator.Current.ExpectedCustomers.FindAll(c => c.PickupDelivery[0] == eventArrive.Stop); //gets all the customers that have the current stop as the pickup stop

                            if (customersToEnterAtCurrentStop.Count > 0) //check if there is customers to enter at current stop
                            {
                                foreach (var customer in customersToEnterAtCurrentStop) //iterates over every customer that has the actual stop as the pickup stop, in order to make them enter the vehicle
                                {
                                    var enterTime = maxInsertedTime > customer.DesiredTimeWindow[0] ? maxInsertedTime +1: customer.DesiredTimeWindow[0]; //case maxinserted time is greather than desired time window the maxinserted time +1 will be the new enterTime of the customer, othersie it is the customer's desiredtimewindow
                                    var customerEnterVehicleEvt =
                                        EventGenerator.GenerateCustomerEnterVehicleEvent(eventArrive.Vehicle, (int)enterTime, customer); //generates the enter event
                                    AddEvent(customerEnterVehicleEvt); //adds to the event list
                                    maxInsertedTime = (int)enterTime; //updates the maxInsertedTime
                                }
                            }
                        }
                

                // END OF INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS

                //VEHICLE DEPART STOP EVENT
               
                        if (eventArrive.Vehicle.TripIterator.Current?.ExpectedTimeWindows != null)
                        {
                            var currentStopIndex =
                                eventArrive.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                            var newDepartTime = eventArrive.Vehicle.TripIterator.Current.ExpectedTimeWindows[currentStopIndex][1]; //gets the expected depart time
                            maxInsertedTime = newDepartTime != 0 ? (int)Math.Max(maxInsertedTime, newDepartTime) : maxInsertedTime; //if new depart time != 0,new maxInsertedTime will be the max between maxInsertedtime and the newDepartTime, else the value stays the same.
                            //If maxInsertedTime is still max value between the previous maxInsertedTime and newDepartTime, this means that there has been a delay in the flexible trip (compared to the model generated by the solver)
                        }

                var departEvent = EventGenerator.GenerateVehicleDepartEvent(eventArrive.Vehicle, maxInsertedTime + 1);
                AddEvent(departEvent);


            }
            //END OF INSERTION OF CUSTOMER ENTER, LEAVE VEHICLE EVENTS AND OF VEHICLE DEPART EVENT--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION (APPEND) OF VEHICLE NEXT STOP ARRIVE EVENT
            if (evt.Category == 1 && evt is VehicleStopEvent eventDepart)
            {
                    var departTime = eventDepart.Time; //the time the vehicle departed on the previous depart event

                    if (eventDepart.Vehicle.TripIterator.Current != null)
                    {
                        var stopTuple = Tuple.Create(eventDepart.Vehicle.TripIterator.Current.StopsIterator.CurrentStop,
                            eventDepart.Vehicle.TripIterator.Current.StopsIterator.NextStop);
                        eventDepart.Vehicle.ArcDictionary.TryGetValue(stopTuple, out var distance);


                        var travelTime =
                            DistanceCalculator.DistanceToTravelTime(eventDepart.Vehicle.Speed, distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                        eventDepart.Vehicle.TripIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
                        var arriveEvent = EventGenerator.GenerateVehicleArriveEvent(eventDepart.Vehicle, nextArrivalTime); //generates the arrive event
                        AddEvent(arriveEvent);
                        //DEBUG!
                        if (eventDepart.Vehicle.FlexibleRouting)
                        {
                            var scheduledArrivalTime = eventDepart.Vehicle.TripIterator.Current.ExpectedTimeWindows[
                                eventDepart.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex][0];

                            ConsoleLogger.Log("Event arrival time:"+nextArrivalTime+", Scheduled arrival time:"+scheduledArrivalTime);
                        }
                        //END DEBUG
                    }

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------


            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUESTS-----------------------------------------------------------
          
            if (evt.Category == 5 && evt is DynamicRequestCheckEvent eventDRCE && evt.Time <= SimulationTimeWindow[1]) // if the event is a dynamic request check event and the current event time is lower than the end time of the simulation
            {

                if (eventDRCE.GenerateNewDynamicRequest) // checks if the current event dynamic request event check is supposed to generate a new customer dynamic request event
                {
                    var rnd = new Random();
                    var pickup = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];
                    while (pickup == Depot) //if the pickup is the depot has to generate another pickup stop
                    {
                        pickup = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];
                    }
                    var delivery = pickup;
                    var distance = DistanceCalculator.CalculateHaversineDistance(pickup.Latitude, pickup.Longitude,
                        delivery.Latitude, delivery.Longitude);
                    var requestTime = evt.Time + 1;
                    while (delivery == pickup || distance < 1000 || delivery == Depot) //if the delivery stop is equal to the pickup stop or depot stop or its direct ride distance is lower than 1000 meters, it needs to generate a different delivery stop
                    {
                       
                        delivery = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];
                        distance = DistanceCalculator.CalculateHaversineDistance(pickup.Latitude, pickup.Longitude,
                            delivery.Latitude, delivery.Longitude);
                    }
                    var pickupTime = rnd.Next(requestTime + 5*60, requestTime + 60 * 60);//the minimum pickup time is 5 minutes above the requestTime and maximum pickup 30 minutes after the request time, 
                    var deliveryTime = rnd.Next(pickupTime + 15 * 60, pickupTime + 30 * 60);//delivery time will be at minimum 15 minutes above the pickuptime and at max 30 minutes from the pickup time
                    Stop[] pickupDelivery = new[] { pickup, delivery };
                    long[] desiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
                    var eventReq =
                        EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickupDelivery,desiredTimeWindow); //Generates a pickup and delivery customer request (dynamic)
                    AddEvent(eventReq);
                }

                var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(evt.Time + 10,_dynamicRequestProbabilityThreshold); //generates a new dynamic request check 10 seconds later than the current evt
                AddEvent(eventDynamicRequestCheck);
                
            }
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE ( after a dynamic request has been accepted)
            if (evt.Category == 4 && evt is CustomerRequestEvent eventCRE && eventCRE.SolutionObject != null)
            {
                var nextEvents = Events.FindAll(ev => ev.Time >= eventCRE.Time);
                var solutionObject = eventCRE.SolutionObject;
                foreach (var nextEvent in nextEvents)
                {
                    Console.WriteLine(nextEvent.ToString());
                }

                foreach (var vehicle in VehicleFleet)
                {
                    var solutionRoute = solutionObject.GetVehicleStops(vehicle);
                    var solutionTimeWindows = solutionObject.GetVehicleTimeWindows(vehicle);

                        if (vehicle.TripIterator.Current != null)
                        {
                            var currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                            var currentStopList = new List<Stop>(vehicle.TripIterator.Current.Stops); //current stoplist for vehicle (before adding the new request)
                            var currentTimeWindows = new List<long[]>(vehicle.TripIterator.Current.ExpectedTimeWindows);
                            List<Stop> visitedStops = new List<Stop>();
                            List<long[]> visitedTimeWindows = new List<long[]>();
                            ConsoleLogger.Log("Vehicle "+vehicle.Id+":");
                            ConsoleLogger.Log("Visited stops:");
                            //construction of already visited stops list
                            for (int index = 0; index < currentStopIndex; index++ )
                            {
                                visitedStops.Add(currentStopList[index]);       
                                visitedTimeWindows.Add(currentTimeWindows[index]);
                                ConsoleLogger.Log(currentStopList[index].ToString()+ " - TW:{" + currentTimeWindows[index][0] + "," + currentTimeWindows[index][1] + "}");
                            }
                            //end of visited stops list construction
                            //inserts the already visited stops at the beginning of the  solutionRoute list
                            for (int e = visitedStops.Count-1;e>=0;e--)
                            {
                                solutionRoute.Insert(0,visitedStops[e]);
                                solutionTimeWindows.Insert(0,visitedTimeWindows[e]);
                            }
                            //end of visited stops insertion
                            if (solutionRoute != currentStopList)
                            {

                                ConsoleLogger.Log("Route is different!");
                                ConsoleLogger.Log("Old route:");
                                for (int z=0;z<currentStopList.Count;z++)
                                {
                                    
                                    ConsoleLogger.Log(currentStopList[z]+ " - TW:{" + currentTimeWindows[z][0] + "," + currentTimeWindows[z][1] + "}");
                                }
                                ConsoleLogger.Log("New route:");
                                for (int x=0;x<solutionRoute.Count;x++)
                                {
                                    if (solutionRoute[x] != null)
                                    {
                                        ConsoleLogger.Log(solutionRoute[x] +" - TW:{"+solutionTimeWindows[x][0]+","+solutionTimeWindows[x][1]+"}");
                                    }
                                }
                            }
                            else
                            {
                                ConsoleLogger.Log("Route is the same for vehicle "+vehicle.Id);
                            }

                            if (vehicle.TripIterator.Current != null)
                            {
                                vehicle.TripIterator.Current.AssignStops(solutionRoute,solutionTimeWindows,currentStopIndex);

                            }

                            var vehicleEvents = Events.FindAll(e => e is VehicleStopEvent vse && vse.Vehicle == vehicle && e.Time >= evt.Time); //gets all next vehicle depart or arrive events
                            foreach (var vEvent in vehicleEvents)
                            {
                                ConsoleLogger.Log(vEvent.GetTraceMessage());
                            }

                        }
                }
                ConsoleLogger.Log("append event finish");
                
            }
            //END OF INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE
            if (currentNumberOfEvents != Events.Count) //If the size of the events list has changed, the event list has to be sorted
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
                        TotalDynamicRequests++;
                        var newCustomer = customerRequestEvent.Customer;
                    if (VehicleFleet.FindAll(v=>v.FlexibleRouting == true).Count>0 && newCustomer != null)
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
                                        allExpectedCustomers.Add(customer);
                                    }
                                    List<Customer> currentCustomers = vehicle.Customers;
                                    
                                    var stops = vehicle.TripIterator.Current.Stops;
                                    var expectedVehicleTimeWindow = vehicle.TripIterator.Current.ExpectedTimeWindows;
                                    var currentStop = vehicle.TripIterator.Current.StopsIterator.CurrentStop;
                                    var currentStopIndex = vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                                    foreach (var customer in currentCustomers)
                                    {
                                        var index = 0;
                                        foreach (var stop in stops)
                                        {
                                            if (index > currentStopIndex)
                                            {
                                                break;
                                            }

                                            if (customer.PickupDelivery[0] == stop &&
                                                stops.FindIndex(s => s == customer.PickupDelivery[1]) >
                                                currentStopIndex)
                                            {
                                                customer.PickupDelivery[0] = stops[currentStopIndex];

                                            }

                                            index++;
                                        }
                                    }

                                    var customersToBeUpdated =
                                        currentCustomers.FindAll(c => c.PickupDelivery[0] == currentStop);
                                    foreach (var customer in customersToBeUpdated)
                                    {
                                        var newPickupTimeIndex = vehicle.TripIterator.Current.Stops.FindIndex(s => s == currentStop);
                                        customer.DesiredTimeWindow[0] =
                                            (int) expectedVehicleTimeWindow[
                                                    newPickupTimeIndex]
                                                [0]; //updates the time window so that it uses the current time of the simulation
                                    }

                                    foreach (var customer in currentCustomers)
                                    {
                                        if (!expectedCustomers.Contains(customer))
                                        {
                                            expectedCustomers.Add(customer);
                                            allExpectedCustomers.Add(customer);
                                        }
                                    }
                                                       
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
                        //Calculation of baseArrivalTime, if there is any moving vehicle, otherwise baseArrivalTime will be the current event Time
                        
                        var movingVehicles = VehicleFleet.FindAll(v => !v.IsIdle && v.FlexibleRouting);
                        long[] startDepotsArrivalTimes = new long[dataModelVehicles.Count];
                        for (int i = 0; i < dataModelVehicles.Count ; i++)
                        {
                            startDepotsArrivalTimes[i] = evt.Time+1; //initializes startDepotArrivalTimes with the current event time +1 second
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
                                        if (movingVehicle.TripIterator.Current != null && movingVehicle.TripIterator.Current.StopsIterator.CurrentStop ==vehicleStopEvent.Stop)
                                        {
                                            ConsoleLogger.Log(vehicleStopEvent.GetTraceMessage());
                                            var currentStartDepotArrivalTime = startDepotsArrivalTimes[dataModelVehicles.IndexOf(movingVehicle)];
                                            startDepotsArrivalTimes[dataModelVehicles.IndexOf(movingVehicle)] = Math.Max(vehicleStopEvent.Time+1, (int)currentStartDepotArrivalTime); //finds the biggest value between the current baseArrivalTime and the current vehicle's next stop arrival time, and updates its value on the array
                                        }
                                    }
                                }
                            }
                        }
                        //debug
                        for (int r = 0; r < dataModelVehicles.Count; r++)
                        {
                            ConsoleLogger.Log("Next arrival time for vehicle " + dataModelVehicles[r].Id+ " : " + startDepotsArrivalTimes[r]);
                        }
                        //end of debug
                        //end of base arrivalTimeCalculation
                        //--------------------------------------------------------------------------------------------------------------------------


                        var dataModel = new DarpDataModel(startDepots, endDepots, dataModelVehicles,
                            allExpectedCustomers,startDepotsArrivalTimes,MaxCustomerRideTime,MaxAllowedUpperBoundTime);
                        dataModel.PrintPickupDeliveries();
                        //dataModel.PrintTimeWindows();
                        //dataModel.PrintTimeMatrix();
                        
                        var solver = new DarpSolver(false);
                        var solution = solver.TryGetFastSolution(dataModel);
                        if (solution != null)
                        {
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