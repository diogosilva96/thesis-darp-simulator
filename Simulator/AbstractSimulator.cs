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
using Simulator.Logger;
using Simulator.Objects;

namespace Simulator
{
    public abstract class AbstractSimulator
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        protected Logger.Logger ConsoleLogger;

        protected Logger.Logger FileLogger;

        protected TripStopsDataObject TsDataObject;

        private readonly DirectedGraph<Stop, double> _stopsGraph;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;

        protected AbstractSimulator()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            ConsoleLogger = new Logger.Logger(consoleRecorder);
            string loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }

            IRecorder fileRecorder = new FileRecorder(Path.Combine(loggerPath, @"sim_events.txt"));
            FileLogger = new Logger.Logger(fileRecorder);
            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
            StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph( true);
            TsDataObject = stopsNetworkGraph.TripStopDataObject;
            stopsNetworkGraph.LoadGraph();
            _stopsGraph = stopsNetworkGraph.StopsGraph;
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
                            Handle(Events[i]);
                            Append(Events[i]);                            
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
                var vehicle = new Vehicle(30, 22, _stopsGraph);
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
                Events.Add(evt);
                return true;
            }
            else
            {
                return false;
            }
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
