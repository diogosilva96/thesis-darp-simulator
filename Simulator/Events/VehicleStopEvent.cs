using System;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;


namespace Simulator.Events
{
    class VehicleStopEvent:Event
    {
        //Event when a vehicle either arrives at or departs from a stop
        public Stop Stop { get;internal set; }
        public Vehicle Vehicle { get; internal set; }

        public Trip Trip { get; internal set; }
        public VehicleStopEvent(int category, int time, Vehicle vehicle, Stop stop) : base(category, time)
        {
            Category = category;//category 0 = arrived stop, category 1 = left stop
            Time = time;
            Vehicle = vehicle;
            Stop = stop;
            Trip = vehicle.TripIterator.Current;
        }
        public override string GetTraceMessage()
        {
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            string message = "";
            if (Stop != null && Vehicle != null)
            {
                message = timestamp + splitter+ this.ToString() +splitter+"Vehicle:"+ Vehicle.Id+splitter+ "Trip:" + Trip.Id + splitter + "ServiceStartTime:" + Trip.StartTime+splitter+Stop;
            }

            return message;
        }
    }
}
