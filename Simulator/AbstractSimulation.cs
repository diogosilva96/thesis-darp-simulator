using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Simulator.Events;
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

        protected TransportationNetwork TransportationNetwork;

        protected EventGenerator EventGenerator;

        protected int TotalEventsHandled;

        protected string LoggerPath;

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
            
            TransportationNetwork = new TransportationNetwork();
            EventGenerator = EventGenerator.GetEventGenerator();
            TotalEventsHandled = 0;
        }

        public abstract void Init();
        public abstract void InitVehicleEvents();

        public abstract void PrintSimulationSettings();

        public void MainLoop()
        {
            while (true)
            {
                Init(); //initializes simulation variables
                OptionsMenu();
                InitVehicleEvents(); //initializes vehicle events (if there is any event to be initialized)
                if (Events.Count > 0) //it means there is the need to simulate
                {
                    PrintSimulationSettings();
                    Simulate();
                    PrintSimulationStatistics();
                }
            }
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
                
            }
            
        }

        public abstract void OptionsMenu();

        public abstract void Append(Event evt);
        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public abstract void Handle(Event evt);

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
        public abstract void PrintSimulationStatistics();
    }
}
