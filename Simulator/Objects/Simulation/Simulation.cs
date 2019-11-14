using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Simulation
{
    public class Simulation : AbstractSimulation
    {
        private Logger.Logger _eventLogger;

        private Logger.Logger _validationsLogger;

        private readonly Logger.Logger _consoleLogger;

        public SimulationParams Params;

        public SimulationStats Stats;

        public Simulation(SimulationParams @params)
        {
            Params = @params;
            var recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
        }

        public void Init()
        {
            Params.InitParams(); //inits the params that need to be updates (seed and loggerPaths)
            Events.Clear(); //clears all events 
            VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
        }


        public void InitVehicleEvents()
        {
            var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(Params.SimulationTimeWindow[0], Params.DynamicRequestThreshold);//initializes dynamic requests
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
                SimulationViews.ViewFactory.Instance().Create(0,this).PrintView();
                Simulate();

            }
        }

        public override void OnSimulationStart()
        {
            
            IRecorder fileRecorder = new FileRecorder(Path.Combine(Params.CurrentSimulationLoggerPath, @"event_logs.txt"));
            _eventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(Params.CurrentSimulationLoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,CategorySuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            _validationsLogger = new Logger.Logger(validationsRecorder);
            InitVehicleEvents();//initializes vehicle events and dynamic requests events (if there is any event to be initialized)
            Params.VehicleNumber = VehicleFleet.Count;
            Params.PrintParams();
            var paramsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"params.txt");
            Params.SaveParams(paramsPath);
            Stats = new SimulationStats(this);//initializes Stats
        }

        public void AssignVehicleFlexibleTrips(RoutingSolutionObject routingSolutionObject,int time)
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

        public void AssignAllConventionalTripsToVehicles() //assigns all the conventional trips to n vehicles where n = the number of trips, conventional trip is an already defined trip with fixed routes
        {
            foreach (var route in TransportationNetwork.Routes)
            {
                var allRouteTrips = route.Trips.FindAll(t => t.StartTime >= Params.SimulationTimeWindow[0] && t.StartTime < Params.SimulationTimeWindow[1]);
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
                            var v = new Vehicle(Params.VehicleSpeed, Params.VehicleCapacity,false);
                            v.AddTrip(trip); //Adds the service
                            VehicleFleet.Add(v);
                            tripCount++;
                        }
                    }
                }
            }
        }

        public override void OnSimulationEnd()
        {
            var statsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"stats_logs.txt");
            Stats.PrintStats();
            Stats.SaveStats(statsPath);
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
                                    _consoleLogger.Log("Vehicle expected depart time" + currentVehicleTrip.ScheduledTimeWindows[currentVehicleTrip.StopsIterator.CurrentIndex][1] + " customer arrival time:" + customer.DesiredTimeWindow[0]);
                                    if (sameStops.Count > 1)
                                    {
                                        _consoleLogger.Log("SameStops");
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

                                _consoleLogger.Log("Event arrival time:" + nextArrivalTime +
                                                  ", Scheduled arrival time:" + scheduledArrivalTime);
                            }
                        }

                        //END DEBUG
                    }

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------


            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF PICKUP AND DELIVERY CUSTOMER REQUESTS-----------------------------------------------------------
          
            if (evt.Category == 5 && evt is DynamicRequestCheckEvent dynamicRequestCheckEvent && evt.Time <= Params.SimulationTimeWindow[1]) // if the event is a dynamic request check event and the current event time is lower than the end time of the simulation
            {

                if (dynamicRequestCheckEvent.GenerateNewDynamicRequest) // checks if the current event dynamic request event check is supposed to generate a new customer dynamic request event
                {
                    List<Stop> excludedStops = new List<Stop>();
                    excludedStops.Add(TransportationNetwork.Depot);
                    var requestTime = evt.Time + 1;                 
                    var pickupTimeWindow = new int[] {requestTime + 5 * 60, requestTime + 60 * 60};
                    var customer = new Customer(TransportationNetwork.Stops,excludedStops,requestTime,pickupTimeWindow);//Generates a random customer
                    var nextCustomerRequestEvent =
                        EventGenerator.GenerateCustomerRequestEvent(requestTime, customer); //Generates a pickup and delivery customer request (dynamic)
                    AddEvent(nextCustomerRequestEvent);
                }

                var eventDynamicRequestCheck = EventGenerator.GenerateDynamicRequestCheckEvent(evt.Time + 10,Params.DynamicRequestThreshold); //generates a new dynamic request check 10 seconds later than the current evt
                AddEvent(eventDynamicRequestCheck);
                
            }
            //END OF INSERTION OF PICKUP DELIVERY CUSTOMER REQUEST-----------------------------------------------------------
            //--------------------------------------------------------------------------------------------------------
            //INSERTION OF EVENTS FOR THE NEWLY GENERATED ROUTE ( after a dynamic request has been accepted)
            if (evt.Category == 4 && evt is CustomerRequestEvent customerRequestEvent && customerRequestEvent.SolutionObject != null)
            {            
                var solutionObject = customerRequestEvent.SolutionObject;
                var vehicleFlexibleRouting = VehicleFleet.FindAll(v => v.FlexibleRouting);
                _consoleLogger.Log("Flexible routing vehicles count: "+vehicleFlexibleRouting.Count);
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
                            _consoleLogger.Log("Vehicle " + vehicle.Id + ":");
                            _consoleLogger.Log("Current stop: " +currentStopList[currentStopIndex].ToString());
                            //construction of already visited stops list
                            if (currentStopIndex > 0)
                            {
                                _consoleLogger.Log("Visited stops:");
                                for (int index = 0; index < currentStopIndex; index++)
                                {
                                    visitedStops.Add(vehicle.TripIterator.Current.VisitedStops[index]);
                                    visitedTimeWindows.Add(vehicle.TripIterator.Current.StopsTimeWindows[index]);
                                    _consoleLogger.Log(currentStopList[index].ToString() + " - " +
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

                            var vehicleEvents = Events.FindAll(e => (e is VehicleStopEvent vse && vse.Vehicle == vehicle && vse.Time >= evt.Time)  || (e is CustomerVehicleEvent cve && cve.Vehicle == vehicle && cve.Time >= evt.Time)).OrderBy(e => e.Time).ThenBy(e => e.Category).ToList(); //gets all next vehicle depart or arrive events
                            _consoleLogger.Log("ALL NEXT VEHICLE " +vehicle.Id+" EVENTS (COUNT:"+vehicleEvents.Count+") (TIME >=" +evt.Time+ "):");
                            foreach (var vEvent in vehicleEvents)
                            {
                                if (vEvent is VehicleStopEvent vehicleStopArriveEvent && vEvent.Category == 0) //vehicle arrive stop event
                                {
                                    _consoleLogger.Log(vehicleStopArriveEvent.GetTraceMessage());
                                }

                                if (vEvent is VehicleStopEvent vehicleStopDepartEvent && vEvent.Category == 1) //vehicle depart stop event
                                {
                                    _consoleLogger.Log(vehicleStopDepartEvent.GetTraceMessage());
                                    if (vehicleStopDepartEvent.Stop == vehicle.TripIterator.Current.StopsIterator.CurrentStop)
                                    {
                                        _consoleLogger.Log("New event depart: " + (vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1] + 2));
                                        vEvent.Time = (int) vehicle.TripIterator.Current.ScheduledTimeWindows[vehicle.TripIterator.Current.StopsIterator.CurrentIndex][1]+1; //recalculates new event depart time
                                    }
                                }

                                if (vEvent is CustomerVehicleEvent customerVehicleEvent && (vEvent.Category == 2 || vEvent.Category == 3)) //if customer enter vehicle or leave vehicle event
                                {
                                    _consoleLogger.Log(customerVehicleEvent.GetTraceMessage());
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
            Stats.TotalEventsHandled++;

            var msg = evt.GetTraceMessage();
            if (msg != "")
            {
                _eventLogger.Log(evt.GetTraceMessage());
            }

            switch (evt)
            {
                case CustomerVehicleEvent customerVehicleEvent:
                    _validationsLogger.Log(customerVehicleEvent.GetValidationsMessage(Stats.ValidationsCounter++));
                    break;
                case CustomerRequestEvent customerRequestEvent:
                        Stats.TotalDynamicRequests++;
                        var newCustomer = customerRequestEvent.Customer;
                    if (VehicleFleet.FindAll(v=>v.FlexibleRouting).Count>0 && newCustomer != null && VehicleFleet.FindAll(v=>v.TripIterator.Current != null && !v.TripIterator.Current.IsDone).Count>0)
                    {
                        var dataModel = DataModelFactory.Instance().CreateCurrentSimulationDataModel(this,newCustomer,evt.Time);
                        var solver = new RoutingSolver(dataModel,false);
                        var solution = solver.TryGetSolution(null);
                        if (solution != null)
                        {
                            dataModel.PrintPickupDeliveries();
                            dataModel.PrintTimeWindows();
                            //dataModel.PrintTimeMatrix();
                            solver.PrintSolution(solution);
                            Stats.TotalServedDynamicRequests++;
                            _consoleLogger.Log(newCustomer.ToString() + " was inserted into a vehicle service at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString() );

                            var solutionObject = solver.GetSolutionObject(solution);
                            customerRequestEvent.SolutionObject = solutionObject;
                        }
                        else
                        {
                            _consoleLogger.Log(newCustomer.ToString() + " was not possible to be served at "+TimeSpan.FromSeconds(customerRequestEvent.Time).ToString());
                        }
                    }
                    break;
            }
        }
    }
}