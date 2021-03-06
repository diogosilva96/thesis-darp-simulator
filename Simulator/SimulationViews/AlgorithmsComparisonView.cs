﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Algorithms;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.SimulationViews
{
    class AlgorithmsComparisonView:AbstractView
    {


        public override void PrintView()
        {
            var metricsPath =
                Path.Combine(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath, @"algorithmMetrics.csv"));
            var algorithmsLogger = new Logger.Logger(new FileRecorder(Path.Combine(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath, @"algorithms.csv"))));
                var dataSetLogger = new Logger.Logger(new FileRecorder(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath,@"algorithmsDataset.csv"), "DataModelId,CustomersNumber,VehicleNumber,MaxRideTimeDurationInMinutes,MaxAllowedUpperBoundLimitInMinutes,Seed"));
            var vehicleNumber = 20;
            var count = 0;
            //var algorithmsMetrics = new AlgorithmMetrics();
            Simulation.Params.VehicleNumber = vehicleNumber;
            for (int customersNumber = 25; customersNumber <= 100; customersNumber = customersNumber + 25)
            {
                Simulation.Params.NumberInitialRequests = customersNumber;
                for (int i = 0; i < 10; i++) // tests 10 different data models
                {
                   
                    
                    bool allowDropNodes = false;
                    RandomNumberGenerator.GenerateNewRandomSeed();
                    var dataModel = DataModelFactory.Instance().CreateInitialSimulationDataModel(allowDropNodes, Simulation);
                    var printableList = dataModel.GetSettingsPrintableList();
                    ConsoleLogger.Log(printableList);
                    dataSetLogger.Log(dataModel.GetCSVSettingsMessage());
                    for (int searchTime = 5; searchTime <= 90; searchTime = searchTime + 5) //test different same datamodel with different search times
                    {
                        AlgorithmContainer algorithmContainer = new AlgorithmContainer();
                        foreach (var searchAlgorithm in algorithmContainer.SearchAlgorithms)
                        {
                            var algorithm = new SearchAlgorithm(searchAlgorithm, searchTime);
                            algorithm.Test(dataModel, allowDropNodes);
                            ConsoleLogger.Log(algorithm.GetResultPrintableList());

                            if (count == 0)
                            {
                                //logs base message type style
                                algorithmsLogger.Log(algorithm.GetCSVMessageStyle());
                            }                            
                            algorithmsLogger.Log(algorithm.GetCSVResultsMessage());
                            count++;
                        }
                    }
                }
            }

            //algorithmsMetrics.SaveMetrics(metricsPath);
        }

        public AlgorithmsComparisonView(Objects.Simulation.Simulation simulation) : base(simulation)
        {
        }
    }
}
