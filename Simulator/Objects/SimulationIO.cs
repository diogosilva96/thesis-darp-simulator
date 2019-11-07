using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    class SimulationIO
    {
        private Logger.Logger _consoleLogger;
        private Simulation _simulation;
        public SimulationIO(Simulation simulation)
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
            _simulation = simulation;
        }

        public int GetMainMenuOption()
        {
            _consoleLogger.Log("Please Select one of the options:");
            _consoleLogger.Log("1 - Standard Bus route simulation");
            _consoleLogger.Log("2 - Flexible Bus route simulation");
            _consoleLogger.Log("3 - Algorithms Test & Results");
            _consoleLogger.Log("4 - Config Simulation");
            _consoleLogger.Log("5 - Exit");
            return GetIntInput(1, 5);
        }

        public void ConfigSimulationMenu()
        {
            _consoleLogger.Log("Do you want to insert a custom random number generator Seed:");
            _consoleLogger.Log("1 - Yes");
           _consoleLogger.Log("2 - No");
            var customSeedOption = GetIntInput(1, 2);
            if (customSeedOption == 1)
            {
                _consoleLogger.Log("Please insert the Custom Seed (Current = " + RandomNumberGenerator.Seed + "):");
                GetIntInput(0, int.MaxValue);
            }
            _consoleLogger.Log("Please insert the Maximum Allowed UpperBound Time (Current = " + TimeSpan.FromSeconds(_simulation.MaxAllowedUpperBoundTime).TotalMinutes + " minutes): ");
            _simulation.MaxAllowedUpperBoundTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
            _consoleLogger.Log("Please insert the Maximum Customer Ride Time Duration (Current = " + TimeSpan.FromSeconds(_simulation.MaxCustomerRideTime).TotalMinutes + " minutes):");
            _simulation.MaxCustomerRideTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
            ConfigStartEndHourMenu();
            _consoleLogger.Log("Please insert the Vehicle Speed (Current = " + _simulation.VehicleSpeed + "):");
            _simulation.VehicleSpeed = GetIntInput(1, 100);
            _consoleLogger.Log("Please insert the vehicle capacity (Current = " + _simulation.VehicleCapacity + "):");
            _simulation.VehicleCapacity = GetIntInput(1, 80);

        }

        public void ConfigStartEndHourMenu()
        {
            int startTimeHour = 0;
            int endTimeHour = 0;
            _consoleLogger.Log("Insert the start hour of the simulation.");
            startTimeHour = GetIntInput(0, 24);
            _consoleLogger.Log("Insert the end hour of the simulation.");
            endTimeHour = GetIntInput(startTimeHour, 24);
            _simulation.SimulationTimeWindow[0] = (int)TimeSpan.FromHours(startTimeHour).TotalSeconds;//hours in seconds
            _simulation.SimulationTimeWindow[1] = (int)TimeSpan.FromHours(endTimeHour).TotalSeconds;//hours in seconds
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
                    _consoleLogger.Log("Wrong input, please retype using a valid integer number value needs to be in the range [" + minVal + "," + maxVal + "]");
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {
                _consoleLogger.Log("Wrong input, please retype using a valid integer number!");
                goto wrongKeyLabel;
            }

            return key;
        }

        public void PrintSimulationSettings()
        {
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("|     Simulation Settings     |");
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("Random Number Generator Seed: " + RandomNumberGenerator.Seed);
            _consoleLogger.Log("Maximum Allowed UpperBound Time: " + TimeSpan.FromSeconds(_simulation.MaxAllowedUpperBoundTime).TotalMinutes + " minutes");
            _consoleLogger.Log("Maximum Customer ride time: " + TimeSpan.FromSeconds(_simulation.MaxCustomerRideTime).TotalMinutes + " minutes");
            _consoleLogger.Log("Dynamic request check probability threshold: " + _simulation.DynamicRequestProbabilityThreshold);
            _consoleLogger.Log("Simulation Start Time: " + TimeSpan.FromSeconds(_simulation.SimulationTimeWindow[0]).ToString());
            _consoleLogger.Log("Simulation End Time: " + TimeSpan.FromSeconds(_simulation.SimulationTimeWindow[1]).ToString());
            _consoleLogger.Log("Simulation Duration: " + TimeSpan.FromSeconds(_simulation.TotalSimulationTime).TotalHours + " hours");
            _consoleLogger.Log("Number of vehicles:" + _simulation.VehicleFleet.Count);
            _consoleLogger.Log("Vehicle average speed: " + _simulation.VehicleSpeed + " km/h.");
            _consoleLogger.Log("Vehicle capacity: " + _simulation.VehicleCapacity + " seats.");

            //logs into a file the settings
            IRecorder settingsFileRecorder = new FileRecorder(Path.Combine(_simulation.CurrentSimulationLoggerPath, @"settings.txt"));
            var settingsLogger = new Logger.Logger(settingsFileRecorder);
            settingsLogger.Log(nameof(RandomNumberGenerator.Seed) + ": " + RandomNumberGenerator.Seed);
            settingsLogger.Log(nameof(_simulation.MaxAllowedUpperBoundTime) + ": " + _simulation.MaxAllowedUpperBoundTime);
            settingsLogger.Log(nameof(_simulation.MaxCustomerRideTime) + ": " + _simulation.MaxCustomerRideTime);
            settingsLogger.Log(nameof(_simulation.DynamicRequestProbabilityThreshold) + ": " + _simulation.DynamicRequestProbabilityThreshold);
            settingsLogger.Log(nameof(_simulation.SimulationTimeWindow) + "[0]: " + _simulation.SimulationTimeWindow[0]);
            settingsLogger.Log(nameof(_simulation.SimulationTimeWindow) + "[1]: " + _simulation.SimulationTimeWindow[1]);
            settingsLogger.Log(nameof(_simulation.VehicleFleet) + nameof(_simulation.VehicleFleet.Count) + ": " + _simulation.VehicleFleet.Count);
            settingsLogger.Log(nameof(_simulation.VehicleSpeed) + ": " + _simulation.VehicleSpeed);
            settingsLogger.Log(nameof(_simulation.VehicleCapacity) + ": " + _simulation.VehicleCapacity);
            _consoleLogger.Log("Press any key to Start the Simulation...");
            Console.Read();
        }

        public RoutingDataModel GetAlgorithmComparisonMenuDataModelOption()
        {
            _consoleLogger.Log("Use random generated Data to test the different algorithms?");
            _consoleLogger.Log("1 - Yes");
            _consoleLogger.Log("2 - No");
            var option = GetIntInput(1, 2);
            RoutingDataModel dataModel = null;
            if (option == 1)
            {
               dataModel = _simulation.GenerateRandomInitialDataModel();
            }
            else 
            {
                var baseProjectPath = Directory
                    .GetParent(Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).FullName).FullName)
                    .FullName;
                var dataSetPath = @Path.Combine(baseProjectPath, @"Datasets");
                DirectoryInfo d = new DirectoryInfo(dataSetPath); //Assuming Test is your Folder
                FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
                _consoleLogger.Log("Please select one of the existing  Data files:");
                var index = 0;
                foreach (var file in Files)
                {
                    index++;
                    Console.WriteLine(index + " - " + file.Name);
                }

                var fileOption = GetIntInput(1, index);
                var selectedFile = Files[fileOption - 1];
                string filePath = Path.Combine(selectedFile.DirectoryName, selectedFile.Name);
                DataSet dataSet = new DataSet(filePath);
                List<Vehicle> dataModelVehicles = new List<Vehicle>();
                List<Stop> startDepots = new List<Stop>();
                List<Stop> endDepots = new List<Stop>();
                dataSet.PrintDataInfo();
                var numVehicles = GetNumberVehiclesMenuOption();
                List<long> startDepotArrivalTimes = new List<long>(numVehicles);
                for (int i = 0; i < numVehicles; i++)
                {
                    dataModelVehicles.Add(new Vehicle(_simulation.VehicleSpeed, dataSet.VehicleCapacities[1], true));
                    startDepots.Add(dataSet.Stops[0]);
                    // startDepots.Add(null); //dummy start depot
                    endDepots.Add(dataSet.Stops[0]);
                    startDepotArrivalTimes[i] = 0;
                }

                dataSet.PrintDistances();
                dataSet.PrintTimeWindows();
                var indexManager = new DataModelIndexManager(startDepots,endDepots,dataModelVehicles,dataSet.Customers,startDepotArrivalTimes);
                dataModel = new RoutingDataModel(indexManager, _simulation.MaxCustomerRideTime, _simulation.MaxAllowedUpperBoundTime);
            }
            return dataModel;
        }

        public bool GetAllowDropNodesMenuOption()
        {
            _consoleLogger.Log("Allow drop nodes penalties?");
            _consoleLogger.Log("1 - Yes");
            _consoleLogger.Log("2 - No");
            return GetIntInput(1, 2) == 1;
        }

        public int GetNumberVehiclesMenuOption()
        {
            _consoleLogger.Log("Please insert the number of vehicles to be considered:");
            return GetIntInput(1, int.MaxValue);
        }

        public int GetNumberCustomersMenuOption()
        {
            _consoleLogger.Log("Please insert the number of customers to be randomly generated:");
            return GetIntInput(1, int.MaxValue);
        }

        public void Print(string message)
        {
            _consoleLogger.Log(message);
        }

        public void PrintSimulationStats()
        {
            IRecorder fileRecorder =
                new FileRecorder(Path.Combine(_simulation.CurrentSimulationLoggerPath, @"stats_logs.txt"));
            var myFileLogger = new Logger.Logger(fileRecorder);
            var toPrintList = new List<string>();
            var alreadyHandledEvents = _simulation.Events.FindAll(e => e.AlreadyHandled);
            toPrintList.Add("Total number of events handled: " +
                            alreadyHandledEvents.Count + " out of " + _simulation.Events.Count + ".");
            if (alreadyHandledEvents.Count <= _simulation.Events.Count)
            {
                var notHandledEvents = _simulation.Events.FindAll(e => !e.AlreadyHandled);
                foreach (var notHandledEvent in notHandledEvents)
                {
                    _consoleLogger.Log(notHandledEvent.ToString());
                }
            }
            toPrintList.Add(  "Vehicle Fleet Size: " + _simulation.VehicleFleet.Count + " vehicle(s).");
            toPrintList.Add( "Average Dynamic requests per hour: " + _simulation.TotalDynamicRequests / TimeSpan.FromSeconds(_simulation.TotalSimulationTime).TotalHours);
            toPrintList.Add("Total simulation time: " + TimeSpan.FromSeconds(_simulation.TotalSimulationTime).TotalHours + " hours.");
            toPrintList.Add("Simulation Computation Time: "+_simulation.ComputationTime+ " seconds.");
            toPrintList.Add("Total Dynamic Requests Served: " + _simulation.TotalServedDynamicRequests +" out of "+_simulation.TotalDynamicRequests);
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");
            foreach (var vehicle in _simulation.VehicleFleet.FindAll(v => v.FlexibleRouting))
            {
                vehicle.PrintRoute(vehicle.TripIterator.Current.Stops, vehicle.TripIterator.Current.ScheduledTimeWindows, vehicle.TripIterator.Current.ServicedCustomers); //scheduled route
                vehicle.PrintRoute(vehicle.TripIterator.Current.VisitedStops, vehicle.TripIterator.Current.StopsTimeWindows, vehicle.TripIterator.Current.ServicedCustomers); //simulation route
            }
            foreach (var route in TransportationNetwork.Routes)
            {

                var allRouteVehicles = _simulation.VehicleFleet.FindAll(v => v.TripIterator != null && v.TripIterator.Current.Route == route);

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
                    var overallStatsPrintableList = routeServicesStatistics.GetOverallStatsPrintableList();
                    var perServiceStatsPrintableList = routeServicesStatistics.GetPerServiceStatsPrintableList();

                    foreach (var perServiceStats in perServiceStatsPrintableList)
                    {
                        toPrintList.Add(perServiceStats);
                    }

                    foreach (var overallStats in overallStatsPrintableList)
                    {
                        toPrintList.Add(overallStats);
                    }
                }
            }          
            toPrintList.Add(" ");
            if (toPrintList.Count > 0)
            {
                foreach (var print in toPrintList)
                {
                    myFileLogger.Log(print);
                    _consoleLogger.Log(print);
                }
            }
        }

    }
}
