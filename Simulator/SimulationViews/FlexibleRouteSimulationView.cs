using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects;
using Simulator.Objects.Data_Objects.Routing;

namespace Simulator.SimulationViews
{
    class FlexibleRouteSimulationView:AbstractView
    {
        public FlexibleRouteSimulationView(AbstractSimulation simulation) : base(simulation)
        {
        }

        public override void PrintView(int option)
        {
            if (option == 2)
            {
                Print("Please insert the number of customers to be served: ");
                var numberCustomers = GetIntInput(1, int.MaxValue);
                Print("Please insert the number of available vehicles: ");
                var numberVehicles = GetIntInput(1, numberCustomers);
                var dataModel = Simulation.GenerateRandomInitialDataModel(numberCustomers, numberVehicles, false);
                if (dataModel != null)
                {
                    RoutingSolver routingSolver = new RoutingSolver(dataModel, false);
                    var printableList = dataModel.GetSettingsPrintableList();
                    Print(printableList);
                    dataModel.PrintPickupDeliveries();
                    var timeWindowSolution = routingSolver.TryGetSolution(null);
                    RoutingSolutionObject routingSolutionObject = null;
                    ;
                    if (timeWindowSolution != null)
                    {
                        routingSolver.PrintSolution(timeWindowSolution);
                        routingSolutionObject = routingSolver.GetSolutionObject(timeWindowSolution);
                    }
                    Simulation.AssignVehicleFlexibleTrips(routingSolutionObject, Simulation.SimulationTimeWindow[0]);
                }
                Simulation.InitSimulationLoggers(); //simulation loggers init
                Simulation.InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)
            }
            else
            {
                NextView.PrintView(option);
            }
        }
    }
}
