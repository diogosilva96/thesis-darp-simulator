using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MathNet.Numerics.Statistics;
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


                var algorithmsLogger = new Logger.Logger(new FileRecorder(Path.Combine(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath, @"algorithms.csv")), "AlgorithmName, AllowDropNodes, SolutionIsFeasible, SearchTimeLimit, ComputationTime, ObjectiveValue, MaxUpperBoundInMinutes, TotalServedCustomers, TotalDistanceTraveledInMeters, TotalRouteTimesInMinutes, VehiclesNumberUsed, DataModelId"));
                var dataSetLogger = new Logger.Logger(new FileRecorder(Path.Combine(Simulation.Params.CurrentSimulationLoggerPath,@"algorithmsDatset.csv"), "DataModelId,CustomersNumber,VehicleNumber,MaxRideTimeDurationInMinutes,MaxAllowedUpperBoundLimitInMinutes,Seed"));
                var vehicleNumber = 20;
                for (int customersNumber = 50; customersNumber <= 200; customersNumber = customersNumber + 25)
                {
                    for (int searchTime = 20; searchTime <= 20; searchTime = searchTime + 20)
                    {
                            for (int i = 0; i <10; i++) // tests 10 different data models for the same setting
                            {
                                var allowDropNodes = false;
                                RandomNumberGenerator.GenerateNewRandomSeed();
                                //Print("Allow drop nodes penalties?");
                                //Print("1 - Yes");
                                //Print("2 - No");
                                //bool allowDropNodes = GetIntInput(1, 2) == 1;
                                //Print("Use random generated Data to test the different algorithms?");
                                //Print("1 - Yes");
                                //Print("2 - No");
                                //var randomDataModelOption = GetIntInput(1, 2);

                                //RoutingDataModel dataModel = null;
                                //Print("Please insert the number of available Vehicles:");
                                //var vehicleNumber = GetIntInput(1, int.MaxValue);
                                //if (randomDataModelOption == 1)
                                //{
                                //    Print("Please insert the number of customers to be generated:");
                                //    var numberCustomers = GetIntInput(1,int.MaxValue);
                        
                                //    dataModel = Simulation.GenerateRandomInitialDataModel(numberCustomers, vehicleNumber, allowDropNodes);
                                //}
                                //else
                                //{
                                //    var baseProjectPath = Directory
                                //        .GetParent(Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).FullName).FullName)
                                //        .FullName;
                                //    var dataSetPath = @Path.Combine(baseProjectPath, @"Datasets");
                                //    DirectoryInfo d = new DirectoryInfo(dataSetPath); //Assuming Test is your Folder
                                //    FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
                                //    Print("Please select one of the existing  Data files:");
                                //    var index = 0;
                                //    foreach (var file in Files)
                                //    {
                                //        index++;
                                //        Console.WriteLine(index + " - " + file.Name);
                                //    }

                                //    var fileOption = GetIntInput(1, index);
                                //    var selectedFile = Files[fileOption - 1];
                                //    string filePath = Path.Combine(selectedFile.DirectoryName, selectedFile.Name);
                                //    DataSet dataSet = new DataSet(filePath);
                                //    List<Vehicle> dataModelVehicles = new List<Vehicle>();
                                //    List<Stop> startDepots = new List<Stop>();
                                //    List<Stop> endDepots = new List<Stop>();
                                //    dataSet.PrintDataInfo();
                                //    List<long> startDepotArrivalTimes = new List<long>(vehicleNumber);
                                //    for (int i = 0; i < vehicleNumber; i++)
                                //    {
                                //        dataModelVehicles.Add(new Vehicle(Simulation.VehicleSpeed, dataSet.VehicleCapacities[1], true));
                                //        startDepots.Add(dataSet.Stops[0]);
                                //        // startDepots.Add(null); //dummy start depot
                                //        endDepots.Add(dataSet.Stops[0]);
                                //        startDepotArrivalTimes.Add(0);
                                //    }

                                    //dataSet.PrintDistances();
                                    //dataSet.PrintTimeWindows();
                                    //var indexManager = new DataModelIndexManager(startDepots, endDepots, dataModelVehicles, dataSet.Customers, startDepotArrivalTimes);
                                    //dataModel = new RoutingDataModel(indexManager, Simulation.MaxCustomerRideTime, Simulation.MaxAllowedUpperBoundTime);
                                    //var dataModel = Simulation.GenerateRandomInitialDataModel(customersNumber, vehicleNumber, allowDropNodes);
                                    //Print("Please insert the search time limit:");
                                    //var searchTime = GetIntInput(1, int.MaxValue);
                                var dataModel = DataModelFactory.Instance().CreateRandomInitialDataModel(vehicleNumber,customersNumber , allowDropNodes, Simulation.Params);
                                var printableList = dataModel.GetSettingsPrintableList();
                                ConsoleLogger.Log(printableList);
                                dataSetLogger.Log(dataModel.GetCSVSettingsMessage());             
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
