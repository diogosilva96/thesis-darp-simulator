using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Events;
using Simulator.Events.Handlers;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Simulation
{
    public class Simulation : AbstractSimulation
    {
        public Logger.Logger EventLogger;

        public Logger.Logger ValidationsLogger;


        public SimulationParams Params;

        public SimulationStats Stats;

        public SimulationContext Context;

        public Simulation(SimulationParams @params, SimulationContext context)
        {
            Params = @params;
            Context = context;
            InitializeEventHandlers();

        }

        public void InitializeEventHandlers()
        {
            //var dynamicRequestCheckHandler = new RequestGenerationCheckHandler(this);

            var arrivalHandler = new VehicleArrivalHandler(this);
            FirstEventHandler = arrivalHandler;
            //dynamicRequestCheckHandler.Successor = arrivalHandler;
            var departureHandler = new VehicleDepartureHandler(this);
            arrivalHandler.Successor = departureHandler;
            var customerLeaveHandler = new CustomerLeaveHandler(this);
            departureHandler.Successor = customerLeaveHandler;
            var customerEnterHandler = new CustomerEnterHandler(this);
            customerLeaveHandler.Successor = customerEnterHandler;
            var customerRequestHandler = new DynamicRequestHandler(this);
            customerEnterHandler.Successor = customerRequestHandler;

        }




        public void InitializeFlexibleSimulation(bool allowDropNodes)
        {
            var dataModel = DataModelFactory.Instance().CreateInitialSimulationDataModel(allowDropNodes, this);
            if (dataModel != null)
            {
                RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                var printableList = dataModel.GetSettingsPrintableList();
                foreach (var tobePrinted in printableList)
                {
                    Console.WriteLine(tobePrinted);
                }
                //dataModel.PrintDataStructures();
                Assignment timeWindowSolution = null;
                    RoutingSearchParameters searchParameters =
                        operations_research_constraint_solver.DefaultRoutingSearchParameters();
                    searchParameters.FirstSolutionStrategy =
                        FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
                    searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.TabuSearch;
                    searchParameters.TimeLimit = new Duration {Seconds = 20}; 
                    timeWindowSolution = routingSolver.TryGetSolution(searchParameters);
                

                RoutingSolutionObject routingSolutionObject = null;
                if (timeWindowSolution != null)
                {

                    routingSolver.PrintSolution(timeWindowSolution);

                    routingSolutionObject = routingSolver.GetSolutionObject(timeWindowSolution);
                    for (int j = 0; j < routingSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                    {
                        var solutionVehicle = routingSolutionObject.IndexToVehicle(j);
                        var solutionVehicleStops = routingSolutionObject.GetVehicleStops(solutionVehicle);
                        var solutionTimeWindows = routingSolutionObject.GetVehicleTimeWindows(solutionVehicle);
                        var solutionVehicleCustomers = routingSolutionObject.GetVehicleCustomers(solutionVehicle);
                        InitializeVehicleFlexibleRoute(solutionVehicle, solutionVehicleStops, solutionVehicleCustomers, solutionTimeWindows);
                    }

                }
            }
            Simulate();
        }

        public override void OnSimulationStart()
        {
            
            IRecorder fileRecorder = new FileRecorder(Path.Combine(Params.CurrentSimulationLoggerPath, @"event_logs.txt"));
            EventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(Params.CurrentSimulationLoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,OperationSuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            ValidationsLogger = new Logger.Logger(validationsRecorder);
            if (Params.NumberDynamicRequestsPerHour > 0)
            {
                AddAllDynamicRequestEvents();
            }
            Params.VehicleNumber = Context.VehicleFleet.Count;
            Params.PrintParams();
            var paramsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"params.txt");
            Params.SaveParams(paramsPath);
            Stats = new SimulationStats(this);//initializes Stats
            SortEvents();
        }

        public void AddAllDynamicRequestEvents()
        {
            List<Stop> excludedStops = new List<Stop>();
            excludedStops.Add(Context.Depot);
            for (int hour = (int)TimeSpan.FromSeconds(Params.SimulationTimeWindow[0]).TotalHours; hour < (int)TimeSpan.FromSeconds(Params.SimulationTimeWindow[1]).TotalHours; hour++)
            {
                var hourInSeconds = TimeSpan.FromHours(hour).TotalSeconds;
                for (int i = 0; i < Params.NumberDynamicRequestsPerHour; i++)
                {
                    var maxHourTime = (int)hourInSeconds + (60 * 60)-1;
                    var requestTime = RandomNumberGenerator.Random.Next((int)hourInSeconds, (int)maxHourTime);
                    var pickupTimeWindow = new int[] {requestTime, maxHourTime};
                    var customer = CustomerFactory.Instance().CreateRandomCustomer(Context.Stops, excludedStops, requestTime, pickupTimeWindow,true,Params.VehicleSpeed); //Generates a random dynamic customer
                    var customerRequestEvent = EventGenerator.Instance().GenerateCustomerRequestEvent(requestTime, customer); //Generates a pickup and delivery customer request (dynamic)
                    AddEvent(customerRequestEvent);
                }
            }
        }
        public void InitializeVehicleFlexibleRoute(Vehicle solutionVehicle,List<Stop> solutionVehicleStops,List<Customer> solutionVehicleCustomers,List<long[]>solutionTimeWindows)
        {
            if (!Context.VehicleFleet.Contains(solutionVehicle))
            {
                solutionVehicle.StartStop = Context.Depot;
                solutionVehicle.EndStop = Context.Depot;
                Context.VehicleFleet.Add(solutionVehicle); //adds the vehicle to the vehicle fleet
            }

            //Adds the flexible trip to the solutionVehicle

            if (solutionVehicleStops.Count >= 2 && solutionVehicleStops[0] != solutionVehicleStops[1]) //if solutionRoute is a valid one
            {
                        if (solutionVehicle.TripIterator?.Current == null) //initializes vehicle trip and route, if the trip has not yet been initalized
                        {
                            var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                            trip.StartTime = (int) solutionTimeWindows[0][0]+1; //start time, might need to change!
                            trip.Route = Context.Routes.Find(r => r.Id == 1000); //flexible route Id
                            trip.Stops = solutionVehicleStops;
                            trip.ExpectedCustomers = solutionVehicleCustomers;
                            trip.ScheduledTimeWindows = solutionTimeWindows;
                            solutionVehicle.AddTrip(trip); //adds the new flexible trip to the vehicle
                            if (solutionVehicle.ServiceTrips.Count > 0) //if the vehicle has services to be done
                            {
                                if (solutionVehicle.TripIterator.Current == null)
                                {
                                    solutionVehicle.TripIterator.Reset();
                                    solutionVehicle.TripIterator.MoveNext(); //initializes the serviceIterator
                                }

                                if (solutionVehicle.TripIterator.Current != null)
                                {
                                    var arriveEvt =
                                        EventGenerator.GenerateVehicleArriveEvent(solutionVehicle,
                                            trip.StartTime); //Generates the first arrive event for every vehicle that has a route assigned in the solution
                                    Events.Add(arriveEvt);
                                }
                            }
                            Console.WriteLine("Vehicle " + solutionVehicle.Id + " route was successfully assigned!");
                        }
            }

            
                
        }

        public void InitializeVehiclesConventionalRoutes() //assigns all the conventional trips to n vehicles where n = the number of trips, conventional trip is an already defined trip with fixed routes
        {
            foreach (var route in Context.Routes)
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
                            var v = new Vehicle(Params.VehicleSpeed, Params.VehicleCapacity);
                            v.AddTrip(trip); //Adds the service
                            Context.VehicleFleet.Add(v);
                            tripCount++;
                        }
                    }
                }
            }
            Simulate();
        }

        public override void OnSimulationEnd()
        {
            var statsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"stats_logs.txt");
            foreach (var vehicle in Context.VehicleFleet.FindAll(v => v.FlexibleRouting))
            {

                vehicle.PrintRoute();//prints each vehicle route
            }
            Stats.PrintStats();
            var dynCustomers = Context.DynamicCustomers.OrderBy(c => c.DesiredTimeWindow[0]);
            //debug
            //foreach (var dynamicCustomer in dynCustomers)
            //{
            //    var tuple = Tuple.Create(dynamicCustomer.PickupDelivery[0], dynamicCustomer.PickupDelivery[1]);
            //    Context.ArcDistanceDictionary.TryGetValue(tuple, out var distance);
            //    Console.WriteLine("Served: " + dynamicCustomer.AlreadyServed+" minimum distance:"+distance);
            //    dynamicCustomer.PrintPickupDelivery();
            //}
            //end of debug
            Stats.SaveStats(statsPath);
        }
    }
}