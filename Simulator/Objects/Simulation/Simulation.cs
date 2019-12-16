using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            InitEventHandlers();

        }

        public void InitEventHandlers()
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
        public void Init()
        {
            Params.InitParams(); //inits the params that need to be updates (seed and loggerPaths)
            Params.Seed = 1;//debug
            Events.Clear(); //clears all events 
            Context.VehicleFleet.Clear(); //clears all vehicles from vehicle fleet
            
        }

        
        public void AppendVehicleArriveEvent(Vehicle vehicle)
        {
            
                if (vehicle.ServiceTrips.Count > 0) //if the vehicle has services to be done
                {
                    if (vehicle.TripIterator.Current == null)
                    {
                        vehicle.TripIterator.Reset();
                        vehicle.TripIterator.MoveNext(); //initializes the serviceIterator
                    }

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
            EventLogger = new Logger.Logger(fileRecorder);
            IRecorder validationsRecorder = new FileRecorder(Path.Combine(Params.CurrentSimulationLoggerPath, @"validations.txt"), "ValidationId,CustomerId,Category,OperationSuccess,VehicleId,RouteId,TripId,ServiceStartTime,StopId,Time");
            ValidationsLogger = new Logger.Logger(validationsRecorder);
            if (Params.NumberDynamicRequestsPerHour > 0)
            {
                AppendDynamicRequestEvents();
            }
            Params.VehicleNumber = Context.VehicleFleet.Count;
            Params.PrintParams();
            var paramsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"params.txt");
            Params.SaveParams(paramsPath);
            Stats = new SimulationStats(this);//initializes Stats
        }

        public void AppendDynamicRequestEvents()
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
                    var customer = CustomerFactory.Instance().CreateRandomCustomer(Context.Stops,
                        excludedStops, requestTime, pickupTimeWindow); //Generates a random customer
                    var customerRequestEvent =
                        EventGenerator.Instance().GenerateCustomerRequestEvent(requestTime, customer); //Generates a pickup and delivery customer request (dynamic)
                    this.AddEvent(customerRequestEvent);
                }
            }
            SortEvents();
        }
        public void AssignVehicleFlexibleTrips(RoutingSolutionObject routingSolutionObject,int time)
        {
            if (routingSolutionObject != null)
            {
                //Adds the flexible trip vehicles to the vehicleFleet
                for (int j = 0; j < routingSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                {
                    var solutionVehicle = routingSolutionObject.IndexToVehicle(j);
                    var solutionVehicleStops = routingSolutionObject.GetVehicleStops(solutionVehicle);
                    if (solutionVehicleStops.Count >= 2 && solutionVehicleStops[0] != solutionVehicleStops[1])
                    {
                        var trip = new Trip(20000 + solutionVehicle.Id, "Flexible trip " + solutionVehicle.Id);
                        trip.StartTime =
                            time + (int) routingSolutionObject
                                .GetVehicleTimeWindows(solutionVehicle)[0][0]; //start time, might need to change!
                        trip.Route = Context.Routes.Find(r => r.Id == 1000); //flexible route Id
                        trip.Stops = solutionVehicleStops;
                        trip.ExpectedCustomers = routingSolutionObject.GetVehicleCustomers(solutionVehicle);
                        trip.ScheduledTimeWindows = routingSolutionObject.GetVehicleTimeWindows(solutionVehicle);
                        solutionVehicle.AddTrip(trip); //adds the new flexible trip to the vehicle
                        solutionVehicle.StartStop = Context.Depot;
                        solutionVehicle.EndStop = Context.Depot;
                        AppendVehicleArriveEvent(solutionVehicle);
                    }
                    Context.VehicleFleet.Add(solutionVehicle); //adds the vehicle to the vehicle fleet
                }
            }
            else
            {
                throw new ArgumentNullException("Routing solution object is null");
            }
        }

        public void AssignAllConventionalTripsToVehicles() //assigns all the conventional trips to n vehicles where n = the number of trips, conventional trip is an already defined trip with fixed routes
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
        }

        public override void OnSimulationEnd()
        {
            var statsPath = Path.Combine(Params.CurrentSimulationLoggerPath, @"stats_logs.txt");
            Stats.PrintStats();
            Stats.SaveStats(statsPath);
        }
    }
}