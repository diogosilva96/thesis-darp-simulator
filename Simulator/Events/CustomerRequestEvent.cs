using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Events
{
    public class CustomerRequestEvent:Event
    {
        public Customer Customer { get; internal set; }

        public RoutingSolutionObject SolutionObject;

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
                message = timestamp + splitter+ this.ToString() + splitter +"Customer Id: "+Customer.Id+splitter+"Pickup: "+Customer.PickupDelivery[0].Id +splitter+"Delivery: "+Customer.PickupDelivery[1].Id+splitter+"Pickup time: "+Customer.DesiredTimeWindow[0]+splitter+"Delivery time: "+Customer.DesiredTimeWindow[1];
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
