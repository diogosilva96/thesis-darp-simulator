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
  

        public CustomerRequestEvent(int category, int time, Customer customer) : base(category, time)
        {
            Time = time;
            Category = category;
            Customer = customer;
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

    }
}
