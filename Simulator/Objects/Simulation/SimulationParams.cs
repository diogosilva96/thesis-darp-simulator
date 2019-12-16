using System;
using System.IO;
using Simulator.Logger;

namespace Simulator.Objects.Simulation
{
    public class SimulationParams
    {

        public int Seed
        {
            get => RandomNumberGenerator.Seed;
            set => RandomNumberGenerator.Seed = value;
        }


        public int MaximumAllowedDeliveryDelay;

        public int MaximumCustomerRideTime;

        public int[] SimulationTimeWindow;

        public int TotalSimulationTime => SimulationTimeWindow[0] >= 0 && SimulationTimeWindow[1] >= 0 && SimulationTimeWindow[1] != 0 ? SimulationTimeWindow[1] - SimulationTimeWindow[0] : 0; //in seconds

        public int VehicleSpeed;

        public int VehicleCapacity;

        public int VehicleNumber;

        public string CurrentSimulationLoggerPath;

        public string LoggerBasePath;

        public int NumberDynamicRequestsPerHour;

        public int NumberInitialRequests;


        public SimulationParams(int maxCustomerRideTimeSeconds,int maxAllowedDeliveryDelaySeconds,int numberDynamicRequestsPerHour, int numberInitialRequests,int numberVehicles)
        {
            VehicleCapacity = 20;
            VehicleSpeed = 40;
            SimulationTimeWindow = new int[2];
            SimulationTimeWindow[0] = 0;
            SimulationTimeWindow[1] = 4 * 60 * 60; // 4hours in seconds
            VehicleNumber = numberVehicles;
            NumberInitialRequests = numberInitialRequests;
            NumberDynamicRequestsPerHour = numberDynamicRequestsPerHour;
            MaximumCustomerRideTime = maxCustomerRideTimeSeconds;
            MaximumAllowedDeliveryDelay = maxAllowedDeliveryDelaySeconds;
            InitParams();
        }

        public void InitParams() //inits a new seed and updates the LoggerPaths
        {
            RandomNumberGenerator.GenerateNewRandomSeed();
            UpdateLoggerPaths();
          
        }

        private void UpdateLoggerPaths()
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
            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var auxTime = currentTime.Split(":");
            currentTime = auxTime[0] + auxTime[1] + auxTime[2];
            CurrentSimulationLoggerPath = Path.Combine(LoggerBasePath, currentTime);
            if (!Directory.Exists(CurrentSimulationLoggerPath))
            {
                Directory.CreateDirectory(CurrentSimulationLoggerPath);
            }
        }
        public void PrintParams()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            Logger.Logger _consoleLogger = new Logger.Logger(consoleRecorder);
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("|     Simulation Settings     |");
            _consoleLogger.Log("-------------------------------");
            _consoleLogger.Log("Random Number Generator Seed: " + Seed);
            _consoleLogger.Log("Maximum Allowed UpperBound Time: " +
                               TimeSpan.FromSeconds(MaximumAllowedDeliveryDelay).TotalMinutes + " minutes");
            _consoleLogger.Log("Maximum Customer ride time: " +
                               TimeSpan.FromSeconds(MaximumCustomerRideTime).TotalMinutes + " minutes");
            _consoleLogger.Log("Simulation Start Time: " +
                               TimeSpan.FromSeconds(SimulationTimeWindow[0]).ToString());
            _consoleLogger.Log("Simulation End Time: " +
                               TimeSpan.FromSeconds(SimulationTimeWindow[1]).ToString());
            _consoleLogger.Log("Simulation Duration: " +
                               TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours");
            _consoleLogger.Log("Vehicle average speed: " + VehicleSpeed + " km/h.");
            _consoleLogger.Log("Vehicle capacity: " + VehicleCapacity + " seats.");
            _consoleLogger.Log("Vehicle number: "+VehicleNumber);
            _consoleLogger.Log("Dynamic Requests per hour: "+NumberDynamicRequestsPerHour);
            _consoleLogger.Log("Press any key to Start the Simulation...");
            Console.Read();
        }

        public void SaveParams(string path)
        {
            //Path.Combine(CurrentSimulationLoggerPath, @"settings.txt"
            //logs into a file the settings
            IRecorder settingsFileRecorder = new FileRecorder(path);
            var settingsLogger = new Logger.Logger(settingsFileRecorder);
            settingsLogger.Log(nameof(RandomNumberGenerator.Seed) + ": " + RandomNumberGenerator.Seed);
            settingsLogger.Log(nameof(MaximumAllowedDeliveryDelay) + ": " + MaximumAllowedDeliveryDelay);
            settingsLogger.Log(nameof(MaximumCustomerRideTime) + ": " + MaximumCustomerRideTime);
            settingsLogger.Log(nameof(SimulationTimeWindow) + "[0]: " + SimulationTimeWindow[0]);
            settingsLogger.Log(nameof(SimulationTimeWindow) + "[1]: " + SimulationTimeWindow[1]);
            settingsLogger.Log(nameof(VehicleNumber)+ " : "+VehicleNumber);
            settingsLogger.Log(nameof(VehicleSpeed) + ": " + VehicleSpeed);
            settingsLogger.Log(nameof(VehicleCapacity) + ": " + VehicleCapacity);
            settingsLogger.Log(nameof(NumberDynamicRequestsPerHour)+": "+NumberDynamicRequestsPerHour);
        }
    }
}
