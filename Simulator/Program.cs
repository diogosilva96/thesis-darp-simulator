using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Events;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var transportationNetworkDataLoader = new TransportationNetworkDataLoader(true);
            var simulationParams = new SimulationParams(30 * 60, 30 * 60, 5, 10, 20, 2);
                while (true)
                {              
                   
                    var simulationContext = new SimulationContext(transportationNetworkDataLoader);
                    AbstractSimulation simulation = new Simulation(simulationParams, simulationContext);
                    SimulationViews.ViewFactory.Instance().Create(0, (Simulation) simulation).PrintView();
                    var lastSimParams = simulationParams;
                    var simTimeInHours = (lastSimParams.TotalSimulationTime / 3600);
                    simulationParams = new SimulationParams(lastSimParams.MaximumRelativeCustomerRideTime, lastSimParams.MaximumAllowedDeliveryDelay, lastSimParams.NumberDynamicRequestsPerHour, lastSimParams.NumberInitialRequests, lastSimParams.VehicleNumber, simTimeInHours);

                }

                Console.Read();

            }      
    }
}
