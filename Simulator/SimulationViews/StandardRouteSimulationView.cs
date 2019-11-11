using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;

namespace Simulator.SimulationViews
{
    class StandardRouteSimulationView:AbstractView
    {
        public StandardRouteSimulationView(AbstractSimulation simulation) : base(simulation)
        {
        }

        public override void PrintView(int option)
        {
            if (option == 1)
            {
                int startTimeHour = 0;
                int endTimeHour = 0;
                Print("Insert the start hour of the simulation.");
                startTimeHour = GetIntInput(0, 24);
                Print("Insert the end hour of the simulation.");
                endTimeHour = GetIntInput(startTimeHour, 24);
                Simulation.AssignAllConventionalTripsToVehicles();
                Simulation.InitSimulationLoggers();
                Simulation.InitVehicleEvents(); //initializes vehicle events and dynamic requests events (if there is any event to be initialized)
            } 
            NextView.PrintView(option);
        }
    }
}
