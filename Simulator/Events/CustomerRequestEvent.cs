using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;

namespace Simulator.Events
{
    public class CustomerRequestEvent:Event
    {
        public Customer Customer;

        public CustomerRequestEvent(int category, int time, Customer customer) : base(category, time)
        {
            Time = time;
            Category = category;
            Customer = customer;
        }


        public override string GetMessage()
        {
            string dateString = "[" + DateTime.Now.ToString() + "] ";
            string message = "";
            if (Customer != null)
            {
                message = dateString + this.ToString() + Customer.ToString() +" requested serviced at "+ Time +" pickup:"+Customer.PickupDelivery[0]+ " dropoff:" +Customer.PickupDelivery[1];
            }

            return message;
        }

        public override void Treat()
        {
            if (Customer != null)
            {
                Console.WriteLine("New request:"+Customer+" - "+Customer.PickupDelivery[0]+" -> "+Customer.PickupDelivery[1]);
                //AddRequest to the scheduler
            }
        }
    }
}
