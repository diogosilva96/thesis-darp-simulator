using System;

namespace Simulator.Events
{
    public abstract class Event
    {
        public int Time { get; internal set; }
        public int Category { get; internal set; }

        public bool AlreadyHandled { get; internal set; }

        public Event(int category, int time)
        {
            Category = category;
            Time = time;
            AlreadyHandled = false;
        }


        public abstract string GetMessage();

        public abstract void Treat();

        public override string ToString()
        {
            return "[Event category:" + Category + " time: " + Time+"] ";
        }
    }
}
