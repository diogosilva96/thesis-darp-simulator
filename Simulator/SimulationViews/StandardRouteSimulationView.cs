using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects.Simulation;

namespace Simulator.SimulationViews
{
    class StandardRouteSimulationView:AbstractView
    {

        public override void PrintView()
        {

                int startTimeHour = 0;
                int endTimeHour = 0;
                ConsoleLogger.Log("Insert the start hour of the simulation.");
                startTimeHour = GetIntInput(0, 24);
                ConsoleLogger.Log("Insert the end hour of the simulation.");
                endTimeHour = GetIntInput(startTimeHour, 24);
                Simulation.InitializeVehiclesConventionalRoutes();
        }

        public StandardRouteSimulationView(Simulation simulation) : base(simulation)
        {
        }
    }
}
