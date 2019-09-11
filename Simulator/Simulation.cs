﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.OrTools.ConstraintSolver;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Algorithms;
using Simulator.Objects.Data_Objects.PDTW;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator
{
    public class Simulation : AbstractSimulation
    {
        private readonly Logger.Logger _eventLogger;

        private readonly Logger.Logger _validationsLogger;

        private int _validationsCounter;

        private PdtwSolutionObject _pdtwSolutionObject; //CHANGE THIS!

        public PdtwDataModel PdtwDataModel;


        private int[] _simulationStartEndTime;

        private readonly int _vehicleSpeed;

        private readonly int _vehicleCapacity;


        public Simulation()
        {
            IRecorder fileRecorder = new FileRecorder(Path.Combine(LoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(LoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            _vehicleCapacity = 20;
            _vehicleSpeed = 30;
        }

        public override void Init()
        {
            TotalEventsHandled = 0;
            _validationsCounter = 1;
            _simulationStartEndTime = new int[2];
            Events.Clear(); //clears all events 
            VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
            
        }
        public override void InitVehicleEvents()
        {
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

        public override void OptionsMenu()
        {
            var numOptions = 3;
            ConsoleLogger.Log("Please Select one of the options:");
            ConsoleLogger.Log("1 - Standard Bus route simulation (static routing)");
            ConsoleLogger.Log("2 - Single Bus route flexible simulation");
            ConsoleLogger.Log("3 - Algorithm comparison");
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
                case 3:AlgorithmComparisonOption();
                    break;
                default: StandardBusRouteOption();
                    break;
            }
        }

        public void InitDataModel()
        {
            PdtwDataModel = new PdtwDataModel(TransportationNetwork.Stops.Find(s => s.Id == 2183), _vehicleSpeed);//data model
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < 2; i++)
            {

                var vehicle = new Vehicle(_vehicleSpeed, _vehicleCapacity, TransportationNetwork.ArcDictionary, true);
                PdtwDataModel.AddVehicle(vehicle);
            }
            // Pickup and deliveries definition using static generated stop requests
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 438), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new int[] { 3250, 4500 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1359) }, new int[] { 2000, 3700 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2270), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2018) }, new int[] { 3200, 5000 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2319), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1523) }, new int[] { 3000, 3900 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1884) }, new int[] { 3300, 3900 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 399), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 555) }, new int[] { 2900, 3300 }, 0));
            PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 430), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2200) }, new int[] { 2900, 4000 }, 0));
            //Print datamodel data
            PdtwDataModel.PrintTimeMatrix();
            PdtwDataModel.PrintPickupDeliveries();
            PdtwDataModel.PrintTimeWindows();
        }
        public void SingleBusRouteFlexibleOption()
        {
        
            var route = TransportationNetwork.Routes[1];
            Random rand = new Random();
            var serviceTrip = route.Trips[0]; //ADD assignvehicletrip function here!
            var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, TransportationNetwork.ArcDictionary,false);
            v.AddTrip(serviceTrip); //Adds the service to the vehicle
            VehicleFleet.Add(v);
            InitDataModel();


            //PdtwDataModel.AddCustomer(new Customer(new Stop[] { TransportationNetwork.Stops.Find(stop1 => stop1.Id == 1106), TransportationNetwork.Stops.Find(stop1 => stop1.Id == 2430) }, new int[] { 2700, 3700 }, 0));

            //PdtwDataModel.AddInitialRoute(serviceStops);


            //PdtwDataModel.PrintTimeMatrix();
            //PdtwDataModel.PrintPickupDeliveries();
            //PdtwDataModel.PrintTimeWindows();
            PdtwSolver pdtwSolver = new PdtwSolver(false);
            Assignment timeWindowSolution = null;
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
            for (int maxUpperBound = 0; maxUpperBound < 30; maxUpperBound++)
            {
                timeWindowSolution = pdtwSolver.TryGetFastSolution(PdtwDataModel,maxUpperBound);
                if (timeWindowSolution != null)
                {
                    break;
                }
            }

            //PdtwSolver pdtwSolver2 = new PdtwSolver(30);
            ////comparing first solution and the one with search
            //var twSolutionWithSearchLimit = pdtwSolver2.TryGetSolutionWithSearchStrategy(PdtwDataModel, 30);
            //pdtwSolver2.PrintSolution(twSolutionWithSearchLimit);
            //if (timeWindowSolution != null && twSolutionWithSearchLimit != null)
            //{
            //    var tw1 = pdtwSolver.GetSolutionObject(timeWindowSolution);
            //    var tw2 = pdtwSolver2.GetSolutionObject(twSolutionWithSearchLimit);

            //    for (int i = 0; i < PdtwDataModel.VehicleNumber; i++)
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
                pdtwSolver.PrintSolution(timeWindowSolution);
                _pdtwSolutionObject = pdtwSolver.GetSolutionObject(timeWindowSolution);
                AssignVehicleFlexibleTrips(_pdtwSolutionObject);
            }
        }

        public void AlgorithmComparisonOption()
        {
            InitDataModel();
            AlgorithmStatistics algorithmStatistics = new AlgorithmStatistics(PdtwDataModel);
            var algorithmStatList = algorithmStatistics.GetSearchAlgorithmsResultsList(10,true);
            var printList = algorithmStatistics.GetPrintableStatisticsList(algorithmStatList);
            foreach (var printableItem in printList)
            {
                ConsoleLogger.Log(printableItem);
            }
        }
        private void AssignVehicleFlexibleTrips(PdtwSolutionObject pdtwSolutionObject)
        {
            if (pdtwSolutionObject != null)
            {
                //Adds the flexible trip vehicles to the vehicleFleet
                for (int j = 0; j < pdtwSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                {
                    var solutionVehicle = _pdtwSolutionObject.IndexToVehicle(j);
                    var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                    trip.StartTime =
                        (int)_pdtwSolutionObject.GetVehicleTimeWindows(solutionVehicle)[0][0]; //start time
                    trip.Route = TransportationNetwork.Routes.Find(r => r.Id == 1000); //flexible route Id
                    trip.Stops = _pdtwSolutionObject.GetVehicleStops(solutionVehicle);
                    solutionVehicle.AddTrip(trip); //adds the new flexible trip to the vehicle
                    VehicleFleet.Add(solutionVehicle); //adds the vehicle to the vehicle fleet
                }
            }
            else
            {
                ConsoleLogger.Log("pdtwSolutionObject is null.");
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
                    _simulationStartEndTime[0] = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
                    ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation (exclusive).");
                    _simulationStartEndTime[1] = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
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
                var allRouteTrips = route.Trips.FindAll(t => TimeSpan.FromSeconds(t.StartTime).Hours >= _simulationStartEndTime[0] && TimeSpan.FromSeconds(t.StartTime).Hours < _simulationStartEndTime[1]);
                if (allRouteTrips.Count > 0)
                {
                    var tripCount = 0;
                    foreach (var trip in allRouteTrips) //Generates a new vehicle for each service, meaning that the number of services will be equal to the number of vehicles
                    {
                        if (trip.IsDone == true)
                        {
                            trip.Reset();
                        }
                        var v = new Vehicle(_vehicleSpeed, _vehicleCapacity, TransportationNetwork.ArcDictionary, false);
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
            if (_simulationStartEndTime[0] == 0 && _simulationStartEndTime[1] == 0) return;
            ConsoleLogger.Log("Start hour:" + _simulationStartEndTime[0]);
            ConsoleLogger.Log(  "End Hour:" + _simulationStartEndTime[1]);

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
            var rnd = new Random();

            //INSERTION (APPEND) OF CUSTOMER ENTER VEHICLE AND LEAVE VEHICLE EVENTS AND GENERATION OF THE DEPART EVENT FROM THE CURRENT STOP---------------------------------------
            if (evt.Category == 0 && evt is VehicleStopEvent eventArrive)
            {
                var arrivalTime = evt.Time;
                var customerLeaveVehicleEvents = EventGenerator.GenerateCustomerLeaveVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, arrivalTime); //Generates customer leave vehicle event
                var lastInsertedLeaveTime = 0;
                var lastInsertedEnterTime = 0;
                lastInsertedLeaveTime = customerLeaveVehicleEvents.Count > 0 ? customerLeaveVehicleEvents[customerLeaveVehicleEvents.Count - 1].Time : arrivalTime;

                List<Event> customersEnterVehicleEvents = null;
                if (eventArrive.Vehicle.TripIterator.Current != null && eventArrive.Vehicle.TripIterator.Current.HasStarted && !_pdtwSolutionObject.ContainsVehicle(eventArrive.Vehicle))
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

                    customersEnterVehicleEvents = EventGenerator.GenerateCustomersEnterVehicleEvents(eventArrive.Vehicle, eventArrive.Stop, lastInsertedLeaveTime, rnd.Next(1, 7), expectedDemand);
                    if (customersEnterVehicleEvents.Count > 0)
                        lastInsertedEnterTime = customersEnterVehicleEvents[customersEnterVehicleEvents.Count - 1].Time;
                }
       
                AddEvent(customersEnterVehicleEvents);
                AddEvent(customerLeaveVehicleEvents);


                var maxInsertedTime = Math.Max(lastInsertedEnterTime, lastInsertedLeaveTime); ; //gets the highest value of the last insertion in order to maintain precedence constraints for the depart evt, meaning that the stop depart only happens after every customer has already entered and left the vehicle on that stop location

                //INSERTION OF CUSTOMER ENTER VEHICLE FOR THE FLEXIBLE REQUESTS!
                if (_pdtwSolutionObject != null)
                {
                    if (_pdtwSolutionObject.ContainsVehicle(eventArrive.Vehicle))//Checks if the current event vehicle is contained on the solution object;
                    {
                        var customersToEnterAtCurrentStop = _pdtwSolutionObject.GetVehicleCustomers(eventArrive.Vehicle).FindAll(c =>
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
                if (_pdtwSolutionObject != null)
                {
                    if (_pdtwSolutionObject.ContainsVehicle(eventArrive.Vehicle))
                    {
                        var newDepartTime = (int)_pdtwSolutionObject.GetVehicleStopTimeWindow(eventArrive.Vehicle, eventArrive.Stop)[1];//gets the solution depart time
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
                            new Calculator().DistanceToTravelTime(eventDepart.Vehicle.Speed,
                                distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                        eventDepart.Vehicle.TripIterator.Current.StopsIterator.Next(); //Moves the iterator to the next stop
                        var arriveEvent = EventGenerator.GenerateVehicleArriveEvent(eventDepart.Vehicle, nextArrivalTime); //generates the arrive event
                        AddEvent(arriveEvent);
                        //DEBUG!
                        if (_pdtwSolutionObject.ContainsVehicle(eventDepart.Vehicle))
                        {
                            ConsoleLogger.Log("Event arrival time:"+nextArrivalTime+", Scheduled arrival time:"+_pdtwSolutionObject.GetVehicleStopTimeWindow(eventDepart.Vehicle,eventDepart.Vehicle.TripIterator.Current.StopsIterator.CurrentStop)[0]);
                        }
                        //END DEBUG
                    }

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUESTS-----------------------------------------------------------
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
                    PdtwDataModel.AddCustomer(customerRequestEvent.Customer);
                    break;
            }
        }
    }
}