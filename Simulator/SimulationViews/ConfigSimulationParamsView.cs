﻿using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Objects;

namespace Simulator.SimulationViews
{
    class ConfigSimulationParamsView:AbstractView
    {

        public override void PrintView()
        {
                ConsoleLogger.Log("Do you want to insert a custom random number generator Seed:");
                ConsoleLogger.Log("1 - Yes");
                ConsoleLogger.Log("2 - No");
                var customSeedOption = GetIntInput(1, 2);
                if (customSeedOption == 1)
                {
                    ConsoleLogger.Log("Please insert the Custom Seed (Current = " + RandomNumberGenerator.Seed + "):");
                    GetIntInput(0, int.MaxValue);
                }
                ConsoleLogger.Log("Please insert the Maximum Allowed UpperBound Time (Current = " + TimeSpan.FromSeconds(Simulation.Params.MaximumAllowedUpperBoundTime).TotalMinutes + " minutes): ");
                Simulation.Params.MaximumAllowedUpperBoundTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
                ConsoleLogger.Log("Please insert the Maximum Customer Ride Time Duration (Current = " + TimeSpan.FromSeconds(Simulation.Params.MaximumCustomerRideTime).TotalMinutes + " minutes):");
                Simulation.Params.MaximumCustomerRideTime = (int)TimeSpan.FromMinutes(GetIntInput(0, int.MaxValue)).TotalSeconds;
                int startTimeHour = 0;
                int endTimeHour = 0;
                ConsoleLogger.Log("Insert the start hour of the simulation:");
                startTimeHour = GetIntInput(0, 24);
                ConsoleLogger.Log("Insert the end hour of the simulation:");
                endTimeHour = GetIntInput(startTimeHour, 24);
                Simulation.Params.SimulationTimeWindow[0] = (int)TimeSpan.FromHours(startTimeHour).TotalSeconds;//hours in seconds
                Simulation.Params.SimulationTimeWindow[1] = (int)TimeSpan.FromHours(endTimeHour).TotalSeconds;//hours in seconds
                ConsoleLogger.Log("Please insert the Vehicle Speed (Current = " + Simulation.Params.VehicleSpeed + " km/h):");
                Simulation.Params.VehicleSpeed = GetIntInput(1, 100);
                ConsoleLogger.Log("Please insert the vehicle capacity (Current = " + Simulation.Params.VehicleCapacity + "):");
                Simulation.Params.VehicleCapacity = GetIntInput(1, 80);
        }

        public ConfigSimulationParamsView(Objects.Simulation.Simulation simulation) : base(simulation)
        {
        }
    }
    
}