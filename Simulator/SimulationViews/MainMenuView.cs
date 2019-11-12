using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Simulation;

namespace Simulator.SimulationViews
{
    public class MainMenuView : AbstractView
    {
        public override void PrintView()
        {
            ConsoleLogger.Log("Please Select one of the options:");
            ConsoleLogger.Log("1 - Standard Bus route simulation");
            ConsoleLogger.Log("2 - Flexible Bus route simulation");
            ConsoleLogger.Log("3 - Algorithms Test & Results");
            ConsoleLogger.Log("4 - Config Simulation");
            ConsoleLogger.Log("5 - Exit");
            var option = GetIntInput(1, 5);
            ViewFactory.Instance().Create(option, Simulation).PrintView();

        }

        public MainMenuView(Simulation simulation) : base(simulation)
        {
        }
    }
}
