using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects;

namespace Simulator.SimulationViews
{
    class ConfigSimulationParamsView:AbstractView
    {
        public ConfigSimulationParamsView(AbstractSimulation simulation) : base(simulation)
        {
        }

        public override void PrintView(int option)
        {
            if (option == 4)
            {
                Print("Do you want to insert a custom random number generator Seed:");
                Print("1 - Yes");
                Print("2 - No");
                var customSeedOption = GetIntInput(1, 2);
                if (customSeedOption == 1)
                {
                    Print("Please insert the Custom Seed (Current = " + RandomNumberGenerator.Seed + "):");
                    GetIntInput(0, int.MaxValue);
                }
                Print("Please insert the Maximum Allowed UpperBound Time (Current = " + TimeSpan.FromSeconds(Simulation.MaxAllowedUpperBoundTime).TotalMinutes + " minutes): ");
                Simulation.MaxAllowedUpperBoundTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
                Print("Please insert the Maximum Customer Ride Time Duration (Current = " + TimeSpan.FromSeconds(Simulation.MaxCustomerRideTime).TotalMinutes + " minutes):");
                Simulation.MaxCustomerRideTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
                int startTimeHour = 0;
                int endTimeHour = 0;
                Print("Insert the start hour of the simulation.");
                startTimeHour = GetIntInput(0, 24);
                Print("Insert the end hour of the simulation.");
                endTimeHour = GetIntInput(startTimeHour, 24);
                Simulation.SimulationTimeWindow[0] = (int)TimeSpan.FromHours(startTimeHour).TotalSeconds;//hours in seconds
                Simulation.SimulationTimeWindow[1] = (int)TimeSpan.FromHours(endTimeHour).TotalSeconds;//hours in seconds
                Print("Please insert the Vehicle Speed (Current = " + Simulation.VehicleSpeed + "):");
                Simulation.VehicleSpeed = GetIntInput(1, 100);
                Print("Please insert the vehicle capacity (Current = " + Simulation.VehicleCapacity + "):");
                Simulation.VehicleCapacity = GetIntInput(1, 80);
            }
            else
            {
                NextView.PrintView(option);
            }
        }
    }
    
}
