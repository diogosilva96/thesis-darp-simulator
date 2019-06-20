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

        public void GenerateVehicleFleet(int n)
        {
            for (int index = 0; index < n; index++)
            {
                var vehicle = new Vehicle(30, 22, StopsGraph);
                VehicleFleet.Add(vehicle);
            }
            ConsoleLogger.Log(this.ToString()+ VehicleFleet.Count+" vehicles were successfully generated.");
        
            insertLabel:
            int startHour = 0;
            int endHour = 0;
                try
                {
                    ConsoleLogger.Log(this.ToString() + "Insert the start hour of the simulation.");
                    startHour = int.Parse(Console.ReadLine());
                    ConsoleLogger.Log(this.ToString() + "Insert the end hour of the simulation.");
                    endHour = int.Parse(Console.ReadLine());
                }
                catch (Exception e)
                {
                    ConsoleLogger.Log(this.ToString() + "Error Wrong input, please insert integer numbers.");
                    goto insertLabel;
                }                


            //int i = 1;
            //foreach (var route in RoutesDataObject.Routes)
            //{
            //    ConsoleLogger.Log(i + " - " + route.Name);
            //    i++;
            //}
            //ConsoleLogger.Log(this.ToString() + "Please select the route that you want to simulate.");
            //int routeIndex = int.Parse(Console.ReadLine());
            AssignVehicleServices(startHour,endHour);
            GenerateVehicleServiceEvents();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        public abstract void AssignVehicleServices(int startHour,int endHour);
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
