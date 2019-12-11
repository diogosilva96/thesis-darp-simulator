using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Events;
using Simulator.Objects.Data_Objects.Simulation_Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class DataModelFactory
    {
        private static DataModelFactory _instance;
        //Lock syncronization object for multithreading (might not be needed)
        private static object syncLock = new object();
        public static DataModelFactory Instance() //Singleton
        {
            // Support multithreaded apps through Double checked locking pattern which (once the instance exists) avoids locking each time the method is invoked

            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new DataModelFactory();
                    }
                }
            }
            return _instance;
        }


        public override string ToString()
        {
            return "["+this.GetType().Name+"] ";
        }

        public RoutingDataModel CreateRandomInitialDataModel(int numberVehicles,int numberCustomers,bool allowDropNodes, Simulation.Simulation simulation)
        {
            Console.WriteLine(this.ToString()+"Creating new random DataModel for "+numberVehicles+ " Vehicles and "+numberCustomers+ " Customers, AllowDropNodes: "+allowDropNodes);
            GenerateNewDataModelLabel:
            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            List<long> startDepotsArrivalTime = new List<long>(numberVehicles);
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < numberVehicles; i++)
            {
                var vehicle = new Vehicle(simulation.Params.VehicleSpeed, simulation.Params.VehicleCapacity,simulation.Context.Depot,simulation.Context.Depot);
                dataModelVehicles.Add(vehicle);
                startDepotsArrivalTime.Add(0);//startDepotArrival time  = 0 for all the vehicles
            }

            var customersToBeServed = new List<Customer>();
            List<Stop> excludedStops = new List<Stop>();
            foreach (var vehicle in dataModelVehicles)
            {
                if (!excludedStops.Contains(vehicle.StartStop))
                {
                    excludedStops.Add(vehicle.StartStop);
                }

                if (!excludedStops.Contains(vehicle.EndStop))
                {
                    excludedStops.Add(vehicle.EndStop);
                }
            }
           
            for (int i = 0; i < numberCustomers; i++) //generate 5 customers with random timeWindows and random pickup and delivery stops
            {
                var requestTime = 0;
                var pickupTimeWindow = new int[] { requestTime, simulation.Params.SimulationTimeWindow[1] };//the customer pickup time will be between the current request time and the end of simulation time
                var customer = CustomerFactory.Instance().CreateRandomCustomer(simulation.Context.Stops, excludedStops, requestTime, pickupTimeWindow);//Generates a random customer
                customersToBeServed.Add(customer);
            }
            var indexManager = new DataModelIndexManager( dataModelVehicles, customersToBeServed, startDepotsArrivalTime);

            var routingDataModel = new RoutingDataModel(indexManager, simulation.Params.MaximumCustomerRideTime, simulation.Params.MaximumAllowedDeliveryDelay);
            var solver = new RoutingSolver(routingDataModel, allowDropNodes);
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.Automatic;
            searchParameters.SolutionLimit = 1; //until it finds 1 solution
            var solution = solver.TryGetSolution(searchParameters);
            if (solution == null)
            {
                goto GenerateNewDataModelLabel;
            }

            return routingDataModel;
        }

        public RoutingDataModel CreateCurrentSimulationDataModel(Simulation.Simulation simulation,Customer newCustomer, int currentTime)
        {
            var flexibleRoutingVehicles = simulation.Context.VehicleFleet.FindAll(v => v.FlexibleRouting);
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
                            var v = simulation.Context.VehicleFleet.Find(veh => veh.Customers.Contains(customer));
                            Console.WriteLine(" Customer " + customer.Id + " is already inside vehicle" + v.Id + ": Already visited: " + customer.PickupDelivery[0] +
                                              ", Need to visit:" + customer.PickupDelivery[1]);
                        }
                    }
                    var currentStop = vehicle.TripIterator.Current.StopsIterator.CurrentStop;
                    expectedCustomers.Add(newCustomer); //adds the new dynamic customer
                    if (!allExpectedCustomers.Contains(newCustomer))
                    {
                        allExpectedCustomers.Add(newCustomer);
                    }
                }

            }

            //--------------------------------------------------------------------------------------------------------------------------
            //Calculation of startDepotArrivalTime, if there is any moving vehicle, otherwise startDepotArrivalTime will be the current event Time
            var movingVehicles = simulation.Context.VehicleFleet.FindAll(v => !v.IsIdle && v.FlexibleRouting);
            List<long> startDepotArrivalTimesList = new List<long>(dataModelVehicles.Count);
            for (int i = 0; i < dataModelVehicles.Count; i++)
            {
                startDepotArrivalTimesList.Add(currentTime);//initializes startDepotArrivalTimes with the current event time
            }
            if (movingVehicles.Count > 0)//if there is a moving vehicle calculates the baseArrivalTime
            {
                Console.WriteLine("Moving vehicles total:" + movingVehicles.Count);
                foreach (var movingVehicle in movingVehicles)
                {
                    var vehicleArrivalEvents = simulation.Events.FindAll(e =>
                        e is VehicleStopEvent vse && e.Category == 0 && e.Time >= currentTime && vse.Vehicle == movingVehicle);
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
            var indexManager = new DataModelIndexManager(dataModelVehicles, allExpectedCustomers, startDepotArrivalTimesList);
            var dataModel = new RoutingDataModel(indexManager, simulation.Params.MaximumCustomerRideTime, simulation.Params.MaximumAllowedDeliveryDelay);
            return dataModel;
        }

        public RoutingDataModel CreateRouteDataModel(Route route,Simulation.Simulation simulation)
        {
            var routeTrip = route.Trips[0];
            var demandDataObject = simulation.Context.DemandsDataObject;
            var stops = new List<Stop>(routeTrip.Stops);
            List<int> demands = new List<int>();
            foreach (var stop in stops)
            {
                var stopDemand = demandDataObject.GetDemand(stop.Id, route.Id, 20);
                demands.Add(stopDemand); //generates dictionay for each stop with the demand
            }

            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            int numberVehicles = 1;
            List<long> startDepotsArrivalTime = new List<long>(numberVehicles);
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < numberVehicles; i++)
            {
                var vehicle = new Vehicle(simulation.Params.VehicleSpeed, simulation.Params.VehicleCapacity, simulation.Context.Depot,
                    simulation.Context.Depot);
                dataModelVehicles.Add(vehicle);
                startDepotsArrivalTime.Add(0);//startDepotArrival time  = 0 for all the vehicles
            }

            var customersToBeServed = new List<Customer>();


            for (int i = 0; i < 4; i++)
            {
                var stopIndex = demands.FindIndex(d => d == demands.Max());
                var maxDemandStop = stops[stopIndex];
                stops.RemoveAt(stopIndex);
                demands.RemoveAt(stopIndex);
                var pickupTime = RandomNumberGenerator.Random.Next(0, 30*60);
                var deliveryTime = RandomNumberGenerator.Random.Next(pickupTime + 15 * 60, pickupTime + 45 * 60);
                var timeWindow = new long[] {pickupTime, deliveryTime};
                customersToBeServed.Add(new Customer(new Stop[] { maxDemandStop, stops[RandomNumberGenerator.Random.Next(stops.IndexOf(maxDemandStop) + 1, stops.Count)] },timeWindow, 0));
            }
            
            var indexManager = new DataModelIndexManager( dataModelVehicles, customersToBeServed, startDepotsArrivalTime);
            var routingDataModel = new RoutingDataModel(indexManager, simulation.Params.MaximumCustomerRideTime, simulation.Params.MaximumAllowedDeliveryDelay);
            routingDataModel.PrintTimeMatrix();
            routingDataModel.PrintPickupDeliveries();
            routingDataModel.PrintTimeWindows();

            return routingDataModel;
        }
        public RoutingDataModel CreateFixedDataModel(Simulation.Simulation simulation) { //test and debug
            List<Vehicle> dataModelVehicles = new List<Vehicle>();
            int numberVehicles = 1;
            List<long> startDepotsArrivalTime = new List<long>(numberVehicles);
            //Creates two available vehicles to be able to perform flexible routing for the pdtwdatamodel
            for (int i = 0; i < numberVehicles; i++)
            {
                var vehicle = new Vehicle(simulation.Params.VehicleSpeed, simulation.Params.VehicleCapacity, simulation.Context.Depot,simulation.Context.Depot);
                dataModelVehicles.Add(vehicle);
                startDepotsArrivalTime.Add(0);//startDepotArrival time  = 0 for all the vehicles
            }

            var customersToBeServed = new List<Customer>();

            customersToBeServed.Add(new Customer(new Stop[] { simulation.Context.Stops[1], simulation.Context.Stops[2] },new long[]{500,1200},0));
                customersToBeServed.Add(new Customer(new Stop[] { simulation.Context.Stops[1], simulation.Context.Stops[3] }, new long[] { 600,1600 }, 0));
                //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops[5], TransportationNetwork.Stops[4] }, new long[] { 800,3500 }, 0));
                //customersToBeServed.Add(new Customer(new Stop[] { TransportationNetwork.Stops[8], TransportationNetwork.Stops[9] }, new long[] {3000, 5000 }, 0));

            var indexManager = new DataModelIndexManager(dataModelVehicles, customersToBeServed, startDepotsArrivalTime);
            var routingDataModel = new RoutingDataModel(indexManager, simulation.Params.MaximumCustomerRideTime, simulation.Params.MaximumAllowedDeliveryDelay);
            routingDataModel.PrintTimeMatrix();
            routingDataModel.PrintPickupDeliveries();
            routingDataModel.PrintTimeWindows();
            return routingDataModel;
        }
    }
}
