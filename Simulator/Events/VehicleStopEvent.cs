using System;
using GraphLibrary.Objects;
using Simulator.Objects;


namespace Simulator.Events
{
    class VehicleStopEvent:Event
    {
        //Event when a vehicle either arrives at or departs from a stop
        public Stop Stop { get;internal set; }
        public Vehicle Vehicle { get; internal set; }

        public Service Service { get; internal set; }
        public VehicleStopEvent(int category, int time, Vehicle vehicle, Stop stop) : base(category, time)
        {
            Category = category;//category 0 = arrived stop, category 1 = left stop
            Time = time;
            Vehicle = vehicle;
            Stop = stop;
            Service = vehicle.ServiceIterator.Current;
        }


        public override void Treat()
        {
            if (Vehicle != null && Stop != null && !AlreadyHandled && Vehicle.ServiceIterator.Current == Service)
            {
                if (Category == 0)
                {
                    Vehicle.Arrive(Stop, Time);
                    AlreadyHandled = true;
                }

                if (Category == 1)
                {
                    Vehicle.Depart(Stop, Time);
                    AlreadyHandled = true;
                }
            }

        }
  

        public override string GetMessage()
        {
            string date_string = "["+DateTime.Now.ToString()+"] ";
            string message = "";
            if (Stop != null && Vehicle != null)
            {
                message = date_string + this.ToString() + Vehicle.ToString();
                if (Category == 0)
                {
                    message = message + " ARRIVED at " + Stop+ ".";
                }

                if (Category == 1)
                {
                    message = message+ " LEFT " + Stop + ".";
                }
            }

            return message;
        }
    }
}
