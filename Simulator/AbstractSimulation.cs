using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using GraphLibrary;
using GraphLibrary.GraphLibrary;
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

        protected DirectedGraph<Stop, double> StopsGraph;

        protected RoutesDataObject RoutesDataObject;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;

        protected string LoggerPath;

        protected int SimulationStartHour;

        protected int SimulationEndHour;

        

        protected AbstractSimulation()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            ConsoleLogger = new Logger.Logger(consoleRecorder);
            LoggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(LoggerPath))
            {
                Directory.CreateDirectory(LoggerPath);
            }

            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
           
            RoutesDataObject = new RoutesDataObject(true);
            var stopsNetworkGraph = new StopsNetworkGraphLoader(RoutesDataObject.Stops,RoutesDataObject.Routes);
            stopsNetworkGraph.LoadGraph();
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

        public void ConfigSimulation()
        {

                //for (int index = 0; index < numRoutes; index++)
                //{
                //}
                //var vehicle = new Vehicle(30, 53, StopsGraph);
                //VehicleFleet.Add(vehicle);
                //ConsoleLogger.Log(this.ToString()+ VehicleFleet.Count+" vehicles were successfully generated.");
        
            insertLabel:
            try
                {
                    ConsoleLogger.Log(this.ToString() + "Insert the start hour of the simulation (inclusive).");
                    SimulationStartHour = int.Parse(Console.ReadLine());
                    ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation (exclusive).");
                    SimulationEndHour = int.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    ConsoleLogger.Log(this.ToString() + "Error Wrong input, please insert integer numbers for the start and end hour.");
                    goto insertLabel;
                }                

                GenerateVehicleServices();
                GenerateVehicleServiceEvents();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public abstract void GenerateVehicleServices();
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
