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

                ConsoleLogger.Log("Please insert the number of customers to be served: ");
                var numberCustomers = GetIntInput(1, int.MaxValue);
                ConsoleLogger.Log("Please insert the number of available vehicles: ");
                var numberVehicles = GetIntInput(1, numberCustomers);
                ConsoleLogger.Log("Allow drop nodes?");
                ConsoleLogger.Log("1 - Yes");
                ConsoleLogger.Log("2 - No");
                var allowDropNodes = GetIntInput(1, 2) == 1;
                //var dataModel = DataModelFactory.Instance().CreateRandomInitialDataModel(numberVehicles,numberCustomers,allowDropNodes,Simulation.Params);
                //var dataModel = DataModelFactory.Instance().CreateFixedDataModel(Simulation.Params);
                var dataModel = DataModelFactory.Instance()
                    .CreateRouteDataModel(Simulation.Context.Routes.Find(r => r.Id == 9), Simulation);
                if (dataModel != null)
                {
                    RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                    var printableList = dataModel.GetSettingsPrintableList();
                    ConsoleLogger.Log(printableList);
                    dataModel.PrintPickupDeliveries();
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
                    }
                    Simulation.AssignVehicleFlexibleTrips(routingSolutionObject, Simulation.Params.SimulationTimeWindow[0]);
                }
        }

        public FlexibleRouteSimulationView(Simulation simulation) : base(simulation)
        {
        }
    }
}
