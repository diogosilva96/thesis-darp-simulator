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

        private DarpSolutionObject _darpSolutionObject; //CHANGE THIS!

        public DarpDataModel DarpDataModel;

        private readonly int _vehicleSpeed;

        private readonly int _vehicleCapacity;

        public List<Customer> DynamicCustomers; //Customers that request service during the simulation

        public int TotalDynamicRequests;

        public int[] SimulationTimeWindow;

        public int TotalSimulationTime => SimulationTimeWindow[0] >= 0 && SimulationTimeWindow[1] >= 0 && SimulationTimeWindow[1] != 0 ? SimulationTimeWindow[1] - SimulationTimeWindow[0] : 0; //in seconds

        private readonly double _dynamicRequestProbabilityThreshold; //randomly generated value has to be lower than this in order to generate a dynamic request


        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            _vehicleCapacity = 20;
            _vehicleSpeed = 30;
            _dynamicRequestProbabilityThreshold = 0.02;
            DynamicCustomers = new List<Customer>();
            SimulationTimeWindow = new int[2];
            TotalDynamicRequests = 0;
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 24 * 24 * 60; // 24hours in seconds
        }

        public override void Init()
        {
            TotalEventsHandled = 0;
            _validationsCounter = 1;
            Events.Clear(); //clears all events 
            VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
            DynamicCustomers.Clear();
            
        }
        public override void InitVehicleEvents()
        {
            var minStartTime = 24 * 60 * 60; //gets the min start time to initialize the dynamic request event check, initializes it at 24 hours (in seconds), which is the max value possible
            foreach (var vehicle in VehicleFleet)
                if (vehicle.ServiceTrips.Count > 0) //if the vehicle has services to be done
                {
                    vehicle.TripIterator.Reset();
                    vehicle.TripIterator.MoveNext();//initializes the serviceIterator
                    if (vehicle.TripIterator.Current != null)
                    {
                        var arriveEvt = EventGenerator.GenerateVehicleArriveEvent(vehicle, vehicle.TripIterator.Current.StartTime); //Generates the first event for every vehicle (arrival at the first stop of the route)
                        Events.Add(arriveEvt);
                        minStartTime = Math.Min(minStartTime, vehicle.TripIterator.Current.StartTime);//gets the simulation min startTime
                    }
                }
            
            if (Events.Count > 0) //initializes dynamic request check events
            {
                var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(minStartTime, _dynamicRequestProbabilityThreshold);
                AddEvent(eventDynamicRequestCheck);
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
                startDepots.Add(TransportationNetwork.Stops.Find(s => s.Id == 2183));
               // startDepots.Add(null); //dummy start depot
                endDepots.Add(TransportationNetwork.Stops.Find(s => s.Id == 2183));
               // endDepots.Add(null);//dummy end depot
            }

            var customersToBeServed = new List<Customer>();
            // Pickup and deliveries definition using static generated stop requests
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new int[] { 3250, 4500 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359) }, new int[] { 2000, 3700 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018) }, new int[] { 3200, 5000 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523) }, new int[] { 3000, 3900 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884) }, new int[] { 3300, 3900 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555) }, new int[] { 2900, 3300 }, 0));
            customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2200) }, new int[] { 2900, 4000 }, 0));
           
            DarpDataModel = new DarpDataModel(startDepots,endDepots, dataModelVehicles, customersToBeServed);
            //Print datamodel data
            DarpDataModel.PrintTimeMatrix();
            DarpDataModel.PrintPickupDeliveries();
            DarpDataModel.PrintTimeWindows();
        }

        public void FlexibleBusRouteOption()
        {
            DisplayStartEndHourMenu();
            InitDataModel(2);
            DarpSolver darpSolver = new DarpSolver(false, 4);
            Assignment timeWindowSolution = null;
            timeWindowSolution = darpSolver.TryGetFastSolution(DarpDataModel);
            AssignVehicleFlexibleTrips(_darpSolutionObject);
            if (timeWindowSolution != null)
            {
                darpSolver.PrintSolution(timeWindowSolution);
                _darpSolutionObject = darpSolver.GetSolutionObject(timeWindowSolution);
                AssignVehicleFlexibleTrips(_darpSolutionObject);
                darpSolver.PrintSolution(timeWindowSolution); //initial solution
            }

        }
        public void SingleBusRouteFlexibleOption()
        {
        
            var route = TransportationNetwork.Routes[1];
            Random rand = new Random();
            var serviceTrip = route.Trips[0]; //ADD assignvehicletrip function here!
            var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, TransportationNetwork.ArcDictionary,false);
            v.AddTrip(serviceTrip); //Adds the service to the vehicle
            VehicleFleet.Add(v);
            InitDataModel(2);


            //DarpDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new int[] { 2700, 3700 }, 0));

            //DarpDataModel.AddInitialRoute(serviceStops);


            //DarpDataModel.PrintTimeMatrix();
            //DarpDataModel.PrintPickupDeliveries();
            //DarpDataModel.PrintTimeWindows();
            DarpSolver darpSolver = new DarpSolver(false,4);
            Assignment timeWindowSolution = null;
            timeWindowSolution = darpSolver.TryGetFastSolution(DarpDataModel);


            //DarpSolver pdtwSolver2 = new DarpSolver(30);
            ////comparing first solution and the one with search
            //var twSolutionWithSearchLimit = pdtwSolver2.TryGetSolutionWithSearchStrategy(DarpDataModel, 30);
            //pdtwSolver2.PrintSolution(twSolutionWithSearchLimit);
            //if (timeWindowSolution != null && twSolutionWithSearchLimit != null)
            //{
            //    var tw1 = darpSolver.GetSolutionObject(timeWindowSolution);
            //    var tw2 = pdtwSolver2.GetSolutionObject(twSolutionWithSearchLimit);

            //    for (int i = 0; i < DarpDataModel.VehicleNumber; i++)
            //    {
            //        var tw1Vstops = tw1.GetVehicleStops(tw1.IndexToVehicle(i));
            //        var tw2Vstops = tw2.GetVehicleStops(tw2.IndexToVehicle(i));
            //        var tw1Vtw = tw1.GetVehicleTimeWindows(tw1.IndexToVehicle(i));
            //        var tw2Vtw = tw2.GetVehicleTimeWindows(tw2.IndexToVehicle(i));
            //        var tw1Vcust = tw1.GetVehicleCustomers(tw1.IndexToVehicle(i));
            //        var tw2Vcust = tw2.GetVehicleCustomers(tw2.IndexToVehicle(i));
            //        ConsoleLogger.Log("Tw1 and Tw2 vehicle stops are equal:" + tw1Vstops.SequenceEqual(tw2Vstops));
            //        ConsoleLogger.Log("Tw1 and Tw2 vehicle time windows are equal:" + tw1Vtw.SequenceEqual(tw2Vtw));
            //        ConsoleLogger.Log("Tw1 and Tw2 vehicle customers are equal:" + tw1Vcust.SequenceEqual(tw2Vcust));
            //    }
            //}

            if (timeWindowSolution != null)
            {
                darpSolver.PrintSolution(timeWindowSolution);
                _darpSolutionObject = darpSolver.GetSolutionObject(timeWindowSolution);

                ///////////////////////////////////////////////////////////////////////
                List<Stop> startDepots = new List<Stop>();
                List<Stop> endDepots = new List<Stop>();
                List<Vehicle> vehicles = new List<Vehicle>();
                var vehicle = _darpSolutionObject.IndexToVehicle(0);
                vehicles.Add(vehicle);
                var stops = _darpSolutionObject.GetVehicleStops(vehicle);
                var vehicleTW = _darpSolutionObject.GetVehicleTimeWindows(vehicle);
                List<Customer> customers = _darpSolutionObject.GetVehicleCustomers(vehicle);


               
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
                        if (stops.FindIndex(s => s == customer.PickupDelivery[0]) <= stops.FindIndex(s => s == customer.PickupDelivery[1]) && customer.PickupDelivery[1] == stop)
                        {
                            customersToBeRemoved.Add(customer);

                        }

                        if (customer.PickupDelivery[0] == stop &&
                            stops.FindIndex(s => s == customer.PickupDelivery[1]) > numStops)
                        {
                                              customer.PickupDelivery[0] = stops[numStops+1];
                           
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
                    var newPickupTimeIndex = _darpSolutionObject.GetVehicleStops(vehicle).FindIndex(s => s == baseStop);
                    cust.DesiredTimeWindow[0] = (int)vehicleTW[newPickupTimeIndex][0]; //updates the timewindow so that it uses the current time of the simulation
                }

                startDepots.Add(null);
                endDepots.Add(TransportationNetwork.Stops.Find(s => s.Id == 2183));
                customers.Add(new Customer(new Stop[]{TransportationNetwork.Stops.Find(stop1 => stop1.Id == 450),TransportationNetwork.Stops.Find(stop1 => stop1.Id == 385)},new int[]{4500,6000},0 ));
                var darpM = new DarpDataModel(startDepots,endDepots,vehicles,customers);
                darpM.PrintTimeMatrix();
                darpM.PrintPickupDeliveries();
                darpM.PrintTimeWindows();
                var darps1 = new DarpSolver(false,5);
                var sol = darps1.TryGetFastSolution(darpM);
                if (sol != null)
                {
                    ConsoleLogger.Log("Dynamic Req:");
                    darps1.PrintSolution(sol);
                }
                else
                {
                    ConsoleLogger.Log("No sol");
                }
                //////////////////////////////////////////////////////////////////////
                ////InitialRoutes
                //var initialStartEnds = new List<Stop>();
                //initialStartEnds.Add(DarpDataModel.IndexToStop(DarpDataModel.Starts[0]));
                //var vehicles = new List<Vehicle>();
                //vehicles.Add(DarpDataModel.Vehicles[0]);
                //var initialRoute = _darpSolutionObject.GetVehicleStops(vehicles[0]);
                //var routeCustomers = _darpSolutionObject.GetVehicleCustomers(vehicles[0]);
                //var initialRouteDataModel = new DarpDataModel(initialStartEnds,initialStartEnds,_vehicleSpeed,vehicles);
                //Dictionary<Vehicle, List<Stop>> initialRoutes = new Dictionary<Vehicle, List<Stop>>();
                //initialRoutes.Add(vehicles[0],initialRoute);
                //initialRouteDataModel.Customers = _darpSolutionObject.GetVehicleCustomers(vehicles[0]);
                //initialRouteDataModel.AddInitialRoute(vehicles[0],initialRoute);
                //var initialDarpSolver = new DarpSolver(false);
                //var initSolution = initialDarpSolver.TryGetFastSolution(initialRouteDataModel);
                //if (initSolution == null)
                //{
                //    ConsoleLogger.Log("initSolution is null");
                //}
                //else
                //{
                //    initialDarpSolver.PrintSolution(initSolution);
                //}
                ////End of initial routes
                AssignVehicleFlexibleTrips(_darpSolutionObject);
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
        private void AssignVehicleFlexibleTrips(DarpSolutionObject darpSolutionObject)
        {
            if (darpSolutionObject != null)
            {
                //Adds the flexible trip vehicles to the vehicleFleet
                for (int j = 0; j < darpSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                {
                    var solutionVehicle = _darpSolutionObject.IndexToVehicle(j);
                    var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                    trip.StartTime =
                        SimulationTimeWindow[0]+(int)_darpSolutionObject.GetVehicleTimeWindows(solutionVehicle)[0][0]; //start time, might need to change!
                    trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                    trip.Stops = _darpSolutionObject.GetVehicleStops(solutionVehicle);
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
            toPrintList.Add(ToString()+ "Average dynamic requests per hour:"+TotalDynamicRequests / TimeSpan.FromSeconds(TotalSimulationTime).TotalHours);
            toPrintList.Add(ToString() + "Total simulation time:"+TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours.");
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
            var eventsCount = Events.Count;
        

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
                        if (_darpSolutionObject != null)
                        {
                            expectedDemand = !_darpSolutionObject.ContainsVehicle(eventArrive.Vehicle) ? TransportationNetwork.DemandsDataObject.GetDemand(eventArrive.Stop.Id, eventArrive.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(eventArrive.Time).Hours) : 0;
                        }
                        else
                        {
                            expectedDemand = TransportationNetwork.DemandsDataObject.GetDemand(eventArrive.Stop.Id, eventArrive.Vehicle.TripIterator.Current.Route.Id, TimeSpan.FromSeconds(eventArrive.Time).Hours);
                        }
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
                if (_darpSolutionObject != null)
                {
                    if (_darpSolutionObject.ContainsVehicle(eventArrive.Vehicle))//Checks if the current event vehicle is contained on the solution object;
                    {
                        var customersToEnterAtCurrentStop = _darpSolutionObject.GetVehicleCustomers(eventArrive.Vehicle).FindAll(c =>
                                c.PickupDelivery[0] ==
                                eventArrive
                                    .Stop); //gets all the customers that have the current stop as the pickup stop
                        if (customersToEnterAtCurrentStop.Count > 0) //check if there is customers to enter at current stop
                        {
                            foreach (var customer in customersToEnterAtCurrentStop) //iterates over every customer that has the actual stop as the pickup stop, in order to make them enter the vehicle
                            {
                                var enterTime = maxInsertedTime > customer.DesiredTimeWindow[0] ? maxInsertedTime +1: customer.DesiredTimeWindow[0]; //case maxinserted time is greather than desired time window the maxinserted time +1 will be the new enterTime of the customer, othersie it is the customer's desiredtimewindow
                                var customerEnterVehicleEvt =
                                    EventGenerator.GenerateCustomerEnterVehicleEvent(eventArrive.Vehicle,
                                        enterTime, customer); //generates the enter event
                                AddEvent(customerEnterVehicleEvt); //adds to the event list
                                maxInsertedTime = enterTime; //updates the maxInsertedTime
                            }
                        }
                    }
                }

                // END OF INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS

                //VEHICLE DEPART STOP EVENT
                if (_darpSolutionObject != null)
                {
                    if (_darpSolutionObject.ContainsVehicle(eventArrive.Vehicle))
                    {
                        var newDepartTime = (int)_darpSolutionObject.GetVehicleStopTimeWindow(eventArrive.Vehicle, eventArrive.Stop)[1];//gets the solution depart time
                        maxInsertedTime = newDepartTime != 0 ? Math.Max(maxInsertedTime, newDepartTime) : maxInsertedTime; //if new depart time != 0,new maxInsertedTime will be the max between maxInsertedtime and the newDepartTime, else the value stays the same.
                        //If maxInsertedTime is still max value between the previous maxInsertedTime and newDepartTime, this means that there has been a delay in the flexible trip (compared to the model generated by the solver)
                    }
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
                            DistanceCalculator.DistanceToTravelTime(eventDepart.Vehicle.Speed,
                                distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                        eventDepart.Vehicle.TripIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
                        var arriveEvent = EventGenerator.GenerateVehicleArriveEvent(eventDepart.Vehicle, nextArrivalTime); //generates the arrive event
                        AddEvent(arriveEvent);
                        //DEBUG!
                        if (_darpSolutionObject != null && _darpSolutionObject.ContainsVehicle(eventDepart.Vehicle))
                        {
                            ConsoleLogger.Log("Event arrival time:"+nextArrivalTime+", Scheduled arrival time:"+_darpSolutionObject.GetVehicleStopTimeWindow(eventDepart.Vehicle,eventDepart.Vehicle.TripIterator.Current.StopsIterator.CurrentStop)[0]);
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
                    var delivery = pickup;
                    var requestTime = evt.Time + 1;
                    while (pickup == delivery) delivery = TransportationNetwork.Stops[rnd.Next(0, TransportationNetwork.Stops.Count)];
                    var pickupTime = rnd.Next(requestTime + 60, requestTime + 30 * 60);//the minimum pickup time is one minute above the requestTime and maximum pickup 30 minutes after the request time, 
                    var deliveryTime = rnd.Next(pickupTime + 10 * 60, pickupTime + 30 * 60);//delivery time will be at minimum 10 minutes from the pickuptime and at max 30 minutes from the pickup time
                    Stop[] pickupDelivery = new[] { pickup, delivery };
                    int[] desiredTimeWindow = new[] { pickupTime, deliveryTime };
                    var eventReq =
                        EventGenerator.GenerateCustomerRequestEvent(evt.Time + 1, pickupDelivery,desiredTimeWindow); //Generates a pickup and delivery customer request (dynamic)
                    AddEvent(eventReq);
                }

                var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(evt.Time + 10,_dynamicRequestProbabilityThreshold); //generates a new dynamic request check 10 seconds later than the current evt
                AddEvent(eventDynamicRequestCheck);
              


            }
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
                    if (DynamicCustomers != null && !DynamicCustomers.Contains(customerRequestEvent.Customer))
                    {
                        DynamicCustomers.Add(customerRequestEvent.Customer);
                        TotalDynamicRequests++;
                    }
                
                    break;
            }
        }
    }
}