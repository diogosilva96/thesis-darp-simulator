using System;
using System.Collections.Generic;
using System.IO;
using Simulator.Logger;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class SimulationParams
    {

        public int Seed
        {
            get => RandomNumberGenerator.Seed;
            set => RandomNumberGenerator.Seed = value;
        }

        public string CurrentSimulationLoggerPath;

        public int MaximumAllowedUpperBoundTime;

        public int MaximumCustomerRideTime;

        public List<Vehicle> VehicleFleet;

        public double DynamicRequestThreshold;

        public int[] SimulationTimeWindow;

        public int TotalSimulationTime => SimulationTimeWindow[0] >= 0 && SimulationTimeWindow[1] >= 0 && SimulationTimeWindow[1] != 0 ? SimulationTimeWindow[1] - SimulationTimeWindow[0] : 0; //in seconds

        public int VehicleSpeed;

        public int VehicleCapacity;

        public string LoggerBasePath;

        public int TotalDynamicRequests;

        public int TotalServedDynamicRequests;

        public SimulationParams(int maxCustomerRideTimeSeconds,int maxAllowedUpperBoundTimeSeconds,double dynamicRequestThreshold)
        {
            var loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }
            LoggerBasePath = Path.Combine(loggerPath, DateTime.Now.ToString("MMMM dd"));
            if (!Directory.Exists(LoggerBasePath))
            {
                Directory.CreateDirectory(LoggerBasePath);
            }
            VehicleCapacity = 20;
            VehicleSpeed = 40;
            DynamicRequestThreshold = dynamicRequestThreshold;
            SimulationTimeWindow = new int[2];
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 4 * 60 * 60; // 4hours in seconds
            MaximumCustomerRideTime = maxCustomerRideTimeSeconds;
            MaximumAllowedUpperBoundTime = maxAllowedUpperBoundTimeSeconds;
        }
        public void PrintSimulationParams()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            Logger.Logger _consoleLogger = new Logger.Logger(consoleRecorder);
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("|     Simulation Settings     |");
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("Random Number Generator Seed: " + Seed);
            _consoleLogger.Log("Maximum Allowed UpperBound Time: " +
                               TimeSpan.FromSeconds(MaximumAllowedUpperBoundTime).TotalMinutes + " minutes");
            _consoleLogger.Log("Maximum Customer ride time: " +
                               TimeSpan.FromSeconds(MaximumCustomerRideTime).TotalMinutes + " minutes");
            _consoleLogger.Log("Dynamic request check probability threshold: " + DynamicRequestThreshold);
            _consoleLogger.Log("Simulation Start Time: " +
                               TimeSpan.FromSeconds(SimulationTimeWindow[0]).ToString());
            _consoleLogger.Log("Simulation End Time: " +
                               TimeSpan.FromSeconds(SimulationTimeWindow[1]).ToString());
            _consoleLogger.Log("Simulation Duration: " +
                               TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours");
            _consoleLogger.Log("Number of vehicles:" + VehicleFleet.Count);
            _consoleLogger.Log("Vehicle average speed: " + VehicleSpeed + " km/h.");
            _consoleLogger.Log("Vehicle capacity: " + VehicleCapacity + " seats.");

            //logs into a file the settings
            IRecorder settingsFileRecorder = new FileRecorder(Path.Combine(CurrentSimulationLoggerPath, @"settings.txt"));
            var settingsLogger = new Logger.Logger(settingsFileRecorder);
            settingsLogger.Log(nameof(RandomNumberGenerator.Seed) + ": " + RandomNumberGenerator.Seed);
            settingsLogger.Log(nameof(MaximumAllowedUpperBoundTime) + ": " +MaximumAllowedUpperBoundTime);
            settingsLogger.Log(nameof(MaximumCustomerRideTime) + ": " + MaximumCustomerRideTime);
            settingsLogger.Log(nameof(DynamicRequestThreshold) + ": " + DynamicRequestThreshold);
            settingsLogger.Log(nameof(SimulationTimeWindow) + "[0]: " + SimulationTimeWindow[0]);
            settingsLogger.Log(nameof(SimulationTimeWindow) + "[1]: " + SimulationTimeWindow[1]);
            settingsLogger.Log(nameof(VehicleFleet) + nameof(VehicleFleet.Count) + ": " + VehicleFleet.Count);
            settingsLogger.Log(nameof(VehicleSpeed) + ": " +VehicleSpeed);
            settingsLogger.Log(nameof(VehicleCapacity) + ": " + VehicleCapacity);
            _consoleLogger.Log("Press any key to Start the Simulation...");
            Console.Read();
        }
    }
}
