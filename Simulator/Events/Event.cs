using System;

namespace Simulator.Events
{
    public abstract class Event : IComparable<Event>
    {
        public int Time { get; internal set; }
        public int Category { get; internal set; }

        public Event(int category, int time)
        {
            Category = category;
            Time = time;
        }

        public Event(int category)
        {
            Category = category;
            Time = 0;
        }

        public abstract string GetMessage();
        public int CompareTo(Event evt)
        {
            return Time.CompareTo(evt.Time);
        }

        public abstract void Treat();

        public override string ToString()
        {
            return "[Event category:" + Category + " time: " + Time+"] ";
        }
    }
}
