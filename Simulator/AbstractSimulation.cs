using System;
using System.Collections.Generic;
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
    public abstract class AbstractSimulation
    {
        public List<Event> Events;

        public List<Vehicle> VehicleFleet;

        protected Logger.Logger _consoleLogger;

        protected Logger.Logger _fileLogger;

        protected TripStopsDataObject _tsDataObject;

        protected DirectedGraph<Stop, double> _stopsGraph;

        protected EventGenerator _eventGenerator;

        protected int _totalEventsHandled;

        public AbstractSimulation()
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
            string loggerPath = @Path.Combine(Environment.CurrentDirectory, @"Logger");
            if (!Directory.Exists(loggerPath))
            {
                Directory.CreateDirectory(loggerPath);
            }

            IRecorder fileRecorder = new FileRecorder(Path.Combine(loggerPath, @"sim.txt"));
            _fileLogger = new Logger.Logger(fileRecorder);
            Events = new List<Event>();
            VehicleFleet = new List<Vehicle>();
            StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph( true);
            _tsDataObject = stopsNetworkGraph.TripStopDataObject;
            stopsNetworkGraph.LoadGraph();
            _stopsGraph = stopsNetworkGraph.StopsGraph;
            _eventGenerator = new EventGenerator();
            _totalEventsHandled = 0;
        }
        public void Simulate()
        {
            if (Events != null)
            {
                    
                    for (int i = 0; i < Events.Count ;i++)
                    {
                            Handle(Events[i]);
                            Append(Events[i]);                            
                            //SortEvents();


                    }
                    PrintSolution();
            }
            
        }

        public void GenerateVehicleFleet(int n)
        {
            for (int index = 0; index < n; index++)
            {
                var vehicle = new Vehicle(17, 22, _stopsGraph);
                VehicleFleet.Add(vehicle);
            }
            AssignVehicleServices();
            GenerateVehicleServiceEvents();
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
