using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using GraphLibrary;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using Simulator.Events;
using Simulator.GraphLibrary;
using Simulator.Logger;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator
{
    public abstract class AbstractSimulation
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        protected Logger.Logger ConsoleLogger;

        protected Logger.Logger FileLogger;

        protected DirectedGraph<Stop, double> StopsGraph;

        protected RoutesDataObject RoutesDataObject;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;

        protected AbstractSimulation()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            ConsoleLogger = new Logger.Logger(consoleRecorder);
            string loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }

            IRecorder fileRecorder = new FileRecorder(Path.Combine(loggerPath, @"events_logs.txt"));
            FileLogger = new Logger.Logger(fileRecorder);
            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
            var stopsNetworkGraph = new StopsNetworkGraphLoader( true);
            stopsNetworkGraph.LoadGraph();
            RoutesDataObject = stopsNetworkGraph.RouteInformationDataObject;
            StopsGraph = stopsNetworkGraph.StopsGraph;
            EventGenerator = new EventGenerator();
            TotalEventsHandled = 0;
        }
        public void Simulate()
        {
         
            if (Events.Count > 0)
            {                
                ConsoleLogger.Log(this.ToString()+"Press any key to start the simulation...");
                Console.ReadLine();
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < Events.Count ;i++)
                {
                            var currentEvent = Events[i];
                            Handle(currentEvent);
                            Append(currentEvent);                  
                            SortEvents();
                }
                watch.Stop();
                ConsoleLogger.Log("-----------------------------------------------------");
                ConsoleLogger.Log(this.ToString()+"Simulation finished after "+TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds+" seconds.");
                PrintSolution();
            }
            
        }

        public void GenerateVehicleFleet(int n)
        {
            for (int index = 0; index < n; index++)
            {
                var vehicle = new Vehicle(30, 22, StopsGraph);
                VehicleFleet.Add(vehicle);
            }
            ConsoleLogger.Log(this.ToString()+ VehicleFleet.Count+" vehicles were successfully created.");
            AssignVehicleServices();
            GenerateVehicleServiceEvents();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public abstract void AssignVehicleServices();
        public abstract void GenerateVehicleServiceEvents();

        public abstract void Handle(Event evt);

        public abstract void Append(Event evt);

        public void SortEvents()
        {
            Events = Events.OrderBy(evt => evt.Time).ThenBy(evt => evt.Category).ToList();
        }
        public bool AddEvent(Event evt)
        {
            if (evt != null)
            {
                if (!Events.Contains(evt))
                {
                    Events.Add(evt);
                    return true;
                }
            }
            return false;
        }

        public bool AddEvent(IEnumerable<Event> eventSet)
        {
            if (eventSet == null)
            {
                return false;
            }

            foreach (var evt in eventSet)
            {
               Events.Add(evt);
            }
            return true;
        }
        public abstract void PrintSolution();
    }
}
