using System;
using GraphLibrary.Objects;
using Simulator.Objects;


namespace Simulator.Events
{
    class VehicleStopEvent:Event
    {
        //Event when a vehicle either arrives at or departs from a stop
        public Stop Stop { get;set; }
        public Vehicle Vehicle { get; set; }
        public VehicleStopEvent(int category, int time, Vehicle vehicle, Stop stop) : base(category, time)
        {
            Category = category;//category 0 = arrived stop, category 1 = left stop
            Time = time;
            Vehicle = vehicle;
            Stop = stop;
        }


        public override void Treat()
        {
            if (Category == 0)
            {
                Vehicle.Arrive(Stop, Time);
            }
            if (Category == 1)
            {
                Vehicle.Depart(Stop, Time);
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
                    message = message + " arrived at " + Stop+ " at " +
                              Time + ".";
                }

                if (Category == 1)
                {
                    message = message+ " left " + Stop + " at " + Time +
                              ".";
                }
            }

            return message;
        }
    }
}
