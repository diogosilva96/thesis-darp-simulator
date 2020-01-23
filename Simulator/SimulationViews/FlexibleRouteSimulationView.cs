using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Simulation;

namespace Simulator.SimulationViews
{
    class FlexibleRouteSimulationView:AbstractView
    {

        public override void PrintView()
        {

                //ConsoleLogger.Log("Please insert the number of customers to be served: ");
                //var numberCustomers = GetIntInput(1, int.MaxValue);
                //Simulation.Params.NumberInitialRequests = numberCustomers;
                //ConsoleLogger.Log("Please insert the number of available vehicles: ");
                //Simulation.Params.VehicleNumber = GetIntInput(1, numberCustomers);
                //ConsoleLogger.Log("Allow drop nodes?");
                //ConsoleLogger.Log("1 - Yes");
                //ConsoleLogger.Log("2 - No");
                //var allowDropNodes = GetIntInput(1, 2) == 1;
                //var dataModel = DataModelFactory.Instance().CreateFixedDataModel(Simulation);
            var allowDropNodes = false;
            Simulation.Params.VehicleNumber = 3;
            Simulation.Params.NumberInitialRequests = 5;
            Simulation.Params.Seed = 2;
            //var dataModel = DataModelFactory.Instance().CreateFixedDataModel(Simulation);
            var dataModel = DataModelFactory.Instance().CreateInitialSimulationDataModel(allowDropNodes, Simulation);
            if (dataModel != null)
                {
                    RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                    var printableList = dataModel.GetSettingsPrintableList();
                    ConsoleLogger.Log(printableList);
                    dataModel.PrintDataStructures();
                RoutingSearchParameters searchParameters =
                    operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy =
                    FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing;
                searchParameters.TimeLimit = new Duration { Seconds = 10 };
                var timeWindowSolution = routingSolver.TryGetSolution(searchParameters);
                    RoutingSolutionObject routingSolutionObject = null;
                    ;
                    if (timeWindowSolution != null)
                    {
                        
                        routingSolver.PrintSolution(timeWindowSolution);
                        
                    routingSolutionObject = routingSolver.GetSolutionObject(timeWindowSolution);
                    for (int j = 0; j < routingSolutionObject.VehicleNumber; j++) //Initializes the flexible trips
                        {
                            var solutionVehicle = routingSolutionObject.IndexToVehicle(j);
                            var solutionVehicleStops = routingSolutionObject.GetVehicleStops(solutionVehicle);
                            var solutionTimeWindows = routingSolutionObject.GetVehicleTimeWindows(solutionVehicle);
                            var solutionVehicleCustomers = routingSolutionObject.GetVehicleCustomers(solutionVehicle);
                            Simulation.InitializeVehicleFlexibleRoute(solutionVehicle, solutionVehicleStops, solutionVehicleCustomers, solutionTimeWindows);
                        }

                    }



                }
        }

        public FlexibleRouteSimulationView(Simulation simulation) : base(simulation)
        {
        }
    }
}
