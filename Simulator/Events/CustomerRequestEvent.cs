using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.DARP;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Events
{
    public class CustomerRequestEvent:Event
    {
        public Customer Customer { get; internal set; }

        public DarpSolutionObject SolutionObject;

        public CustomerRequestEvent(int category, int time, Customer customer) : base(category, time)
        {
            Time = time;
            Category = category;
            Customer = customer;
            SolutionObject = null;
        }


        public override string GetTraceMessage()
        {
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            string message = "";
            if (Customer != null)
            {
                message = timestamp + splitter+ this.ToString() + splitter +"Customer pickup:"+Customer.PickupDelivery[0].Id +splitter+"Customer delivery:"+Customer.PickupDelivery[1].Id;
            }

            return message;
        }

        public override void Treat()
        {
            if (Customer != null && !AlreadyHandled)
            {
                Console.WriteLine("New request:"+Customer+" - "+Customer.PickupDelivery[0]+" -> "+Customer.PickupDelivery[1]+", TimeWindows: {"+Customer.DesiredTimeWindow[0]+","+Customer.DesiredTimeWindow[1]+"} at "+TimeSpan.FromSeconds(Time).ToString());
                AlreadyHandled = true;
                //AddRequest to the scheduler
            }
        }
    }
}
