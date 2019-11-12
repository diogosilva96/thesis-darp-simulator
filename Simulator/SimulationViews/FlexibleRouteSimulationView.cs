using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects;
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
                var dataModel = Simulation.GenerateRandomInitialDataModel(numberCustomers, numberVehicles, false);
                if (dataModel != null)
                {
                    RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                    var printableList = dataModel.GetSettingsPrintableList();
                    ConsoleLogger.Log(printableList);
                    dataModel.PrintPickupDeliveries();
                    var timeWindowSolution = routingSolver.TryGetSolution(null);
                    RoutingSolutionObject routingSolutionObject = null;
                    ;
                    if (timeWindowSolution != null)
                    {
                        routingSolver.PrintSolution(timeWindowSolution);
                        routingSolutionObject = routingSolver.GetSolutionObject(timeWindowSolution);
                    }
                    Simulation.AssignVehicleFlexibleTrips(routingSolutionObject, Simulation.SimulationParams.SimulationTimeWindow[0]);
                }
                Simulation.InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)

        }

        public FlexibleRouteSimulationView(Simulation simulation) : base(simulation)
        {
        }
    }
}
