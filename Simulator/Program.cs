using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {

            SimulationContext simulationContext = new SimulationContext();
            var option = 2;
                while (true)
                {
                    if (option == 1)
                    {
                        var loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
                        if (!Directory.Exists(loggerPath))
                        {
                            Directory.CreateDirectory(loggerPath);
                        }

                        var loggerBasePath = Path.Combine(loggerPath, DateTime.Now.ToString("MMMM dd"));
                        if (!Directory.Exists(loggerBasePath))
                        {
                            Directory.CreateDirectory(loggerBasePath);
                        }

                        var currentTime = DateTime.Now.ToString("HH:mm:ss");
                        var auxTime = currentTime.Split(":");
                        currentTime = auxTime[0] + auxTime[1] + auxTime[2];
                        var currentSimulationStatsFileName =
                            Path.Combine(loggerBasePath, @"SimulationStats" + currentTime + ".txt");
                        var simulationStatsRecorder = new FileRecorder(currentSimulationStatsFileName);
                        var fileLogger = new Logger.Logger(simulationStatsRecorder);
                        for (int numIterations = 0; numIterations < 10; numIterations++)
                        {
                            for (int numDynamicCustomersHour = 5;
                                numDynamicCustomersHour < 15;
                                numDynamicCustomersHour++)
                            {
                                SimulationParams simulationParams =
                                    new SimulationParams(30 * 60, 30 * 60, numDynamicCustomersHour, 15, 5);
                                Simulation simulation = new Simulation(simulationParams, simulationContext);
                                simulation.InitializeFlexibleSimulation(false);
                                var statsMessage = simulation.Stats.GetCSVStatsMessage();
                                fileLogger.Log(statsMessage);
                            }
                        }
                    }
                    else
                    {
                        {
                            SimulationParams simulationParams = new SimulationParams(30 * 60, 30 * 60, 5, 15, 20);
                            AbstractSimulation simulation = new Simulation(simulationParams, simulationContext);
                            SimulationViews.ViewFactory.Instance().Create(0, (Simulation) simulation).PrintView();
                        }
                    }
                }

                Console.Read();

            }      
    }
}
