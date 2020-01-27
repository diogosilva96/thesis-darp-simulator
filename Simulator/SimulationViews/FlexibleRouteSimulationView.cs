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
            Simulation.Params.NumberInitialRequests = GetIntInput(1, int.MaxValue);
            ConsoleLogger.Log("Please insert the number of available vehicles: ");
            Simulation.Params.VehicleNumber  = GetIntInput(1, Simulation.Params.NumberInitialRequests);
            ConsoleLogger.Log("Allow drop nodes?");
            ConsoleLogger.Log("1 - Yes");
            ConsoleLogger.Log("2 - No");
            var allowDropNodes = GetIntInput(1, 2) == 1;
            Simulation.Params.Seed = 2;
            Simulation.InitializeFlexibleSimulation(allowDropNodes);
        }

        public FlexibleRouteSimulationView(Simulation simulation) : base(simulation)
        {
        }
    }
}
