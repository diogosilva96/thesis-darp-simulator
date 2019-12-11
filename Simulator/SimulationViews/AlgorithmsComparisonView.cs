using System;
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
            var algorithmsLogger = new Logger.Logger(new FileRecorder(Path.Combine(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath, @"algorithms.csv"))));
                var dataSetLogger = new Logger.Logger(new FileRecorder(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath,@"algorithmsDataset.csv"), "DataModelId,CustomersNumber,VehicleNumber,MaxRideTimeDurationInMinutes,MaxAllowedUpperBoundLimitInMinutes,Seed"));
            var vehicleNumber = 20;
            var count = 0;
            for (int customersNumber = 50; customersNumber <= 100; customersNumber = customersNumber + 50)
            {

                bool allowDropNodes = false;
                RandomNumberGenerator.Seed = 2;
                var dataModel = DataModelFactory.Instance().CreateRandomInitialDataModel(vehicleNumber, customersNumber, allowDropNodes, Simulation);
                var printableList = dataModel.GetSettingsPrintableList();
                ConsoleLogger.Log(printableList);
                dataSetLogger.Log(dataModel.GetCSVSettingsMessage());
                for (int searchTime = 20; searchTime <= 60; searchTime = searchTime + 20) //test different same datamodel with different search times
                {
                    AlgorithmContainer algorithmContainer = new AlgorithmContainer();
                    var algorithm = algorithmContainer.SearchAlgorithms[2];

                    var algorithmsTester = new SearchAlgorithmTester(algorithm, searchTime);
                    algorithmsTester.Test(dataModel, allowDropNodes);
                    ConsoleLogger.Log(algorithmsTester.GetResultPrintableList());
                    if (count == 0)
                    {

                        //logs base message type style
                        algorithmsLogger.Log(algorithmsTester.GetCSVMessageStyle());
                    }
                    algorithmsLogger.Log(algorithmsTester.GetCSVResultsMessage());
                    count++;
                }
            }

            for (int customersNumber = 50; customersNumber <= 150; customersNumber = customersNumber + 50)
                {
                    for (int i = 0; i < 10; i++) // tests 10 different data models
                    {

                        bool allowDropNodes = false;
                        RandomNumberGenerator.GenerateNewRandomSeed();
                        var dataModel = DataModelFactory.Instance().CreateRandomInitialDataModel(vehicleNumber, customersNumber, allowDropNodes, Simulation);
                        var printableList = dataModel.GetSettingsPrintableList();
                        ConsoleLogger.Log(printableList);
                        dataSetLogger.Log(dataModel.GetCSVSettingsMessage());
                    for (int searchTime = 20; searchTime <= 60; searchTime = searchTime + 20) //test different same datamodel with different search times
                        {
                            AlgorithmContainer algorithmContainer = new AlgorithmContainer();
                            foreach (var searchAlgorithm in algorithmContainer.SearchAlgorithms)
                            {
                                var algorithmsTester = new SearchAlgorithmTester(searchAlgorithm,searchTime);
                                algorithmsTester.Test(dataModel,allowDropNodes);
                                ConsoleLogger.Log(algorithmsTester.GetResultPrintableList());
                                algorithmsLogger.Log(algorithmsTester.GetCSVResultsMessage());
                            }
                        }
                    }
                }
        }

        public AlgorithmsComparisonView(Objects.Simulation.Simulation simulation) : base(simulation)
        {
        }
    }
}
