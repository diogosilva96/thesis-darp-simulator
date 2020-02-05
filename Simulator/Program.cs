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
            var transportationNetworkDataLoader = new TransportationNetworkDataLoader(true);
            SimulationContext simulationContext = null;
            var option = 1;
            var count = 0;
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
                            Path.Combine(loggerBasePath, @"SimulationStats" + currentTime + ".csv");
                        var simulationStatsRecorder = new FileRecorder(currentSimulationStatsFileName);
                        var fileLogger = new Logger.Logger(simulationStatsRecorder);
                      
                            for (int numDynamicCustomersHour = 20; numDynamicCustomersHour <= 60; numDynamicCustomersHour=numDynamicCustomersHour+20)
                            {
                                for (int numIterations = 0; numIterations < 10; numIterations++)
                                {
                                    simulationContext = new SimulationContext(transportationNetworkDataLoader);
                                    SimulationParams simulationParams =
                                        new SimulationParams(30 * 60, 30 * 60, numDynamicCustomersHour, 0, 5,4);
                                    Simulation simulation = new Simulation(simulationParams, simulationContext);
                                    simulation.InitializeFlexibleSimulation(false);
                                    if (count == 0)
                                    {
                                        fileLogger.Log(simulation.Stats.GetSimulationStatsCSVFormatMessage());
                                    }

                                    var statsMessage = simulation.Stats.GetSimulationStatsCSVMessage();
                                    fileLogger.Log(statsMessage);
                                    count++;
                                }
                            }

                            break;
                    }
                    else
                    {
                        {
                           simulationContext = new SimulationContext(transportationNetworkDataLoader);
                            SimulationParams simulationParams = new SimulationParams(30 * 60, 30 * 60, 5, 15, 20,4);
                            AbstractSimulation simulation = new Simulation(simulationParams, simulationContext);
                            SimulationViews.ViewFactory.Instance().Create(0, (Simulation) simulation).PrintView();
                        }
                    }
                }

                Console.Read();

            }      
    }
}
