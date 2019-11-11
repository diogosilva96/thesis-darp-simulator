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

namespace Simulator.SimulationViews
{
    class AlgorithmsComparisonView:AbstractView
    {
        public AlgorithmsComparisonView(AbstractSimulation simulation) : base(simulation)
        {
        }

        public override void PrintView(int option)
        {
            if (option == 3)
            {
                IRecorder algorithmsRecorder = new FileRecorder(Path.Combine(Simulation.CurrentSimulationLoggerPath, @"algorithms.txt"));
                var algorithmsLogger = new Logger.Logger(algorithmsRecorder);
                for (int customersNumber = 25; customersNumber <= 100; customersNumber = customersNumber + 25)
                {
                    for (int vehicleNumber = 5; vehicleNumber <= 20; vehicleNumber = vehicleNumber + 5)
                    {
                        for (int searchTime = 15; searchTime <= 60; searchTime = searchTime + 15)
                        {
                            for (int i = 0; i < 10; i++) // tests 10 different data models for the same setting
                            {
                                var allowDropNodes = true;
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
                                var dataModel = Simulation.GenerateRandomInitialDataModel(customersNumber,
                                        vehicleNumber, allowDropNodes);
                                var printableList = dataModel.GetSettingsPrintableList();
                                Print(printableList);
                                algorithmsLogger.Log(dataModel.GetCSVSettingsMessage());
                                AlgorithmContainer algorithmContainer = new AlgorithmContainer(dataModel);
                                var testedAlgorithms =
                                    algorithmContainer.GetTestedSearchAlgorithms(searchTime, allowDropNodes);

                                foreach (var algorithm in testedAlgorithms)
                                {
                                    var resultsPrintableList = algorithm.GetResultPrintableList();
                                    Print(resultsPrintableList);
                                    algorithmsLogger.Log(algorithm.GetCSVResultsMessage());
                                }
                            }
                        }
                    }
                }
            }
            NextView.PrintView(option);
        }
    }
}
