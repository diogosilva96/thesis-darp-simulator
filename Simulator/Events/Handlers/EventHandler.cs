using Simulator.Logger;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
   abstract class EventHandler:IEventHandler
   {
       protected Simulation Simulation;

       protected Logger.Logger _consoleLogger;
        public IEventHandler Successor { get; set; }

        public abstract void Handle(Event evt);


        protected void Log(Event evt)
        {
            Simulation.Stats.TotalEventsHandled++;
            var msg = evt.GetTraceMessage();
            if (msg != "")
            {
                Simulation.EventLogger.Log(evt.GetTraceMessage());
            }
        }
        public EventHandler(Simulation simulation)
        {
            Simulation = simulation;
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
        }


    }
}
