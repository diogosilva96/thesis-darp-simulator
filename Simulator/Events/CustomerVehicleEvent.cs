using System;
using Simulator.Objects;

namespace Simulator.Events
{
    class CustomerVehicleEvent:Event
    {
        //Event when a customer enters or leaves a vehicle
        public Customer Customer { get; internal set; }
        public Vehicle Vehicle { get; internal set; }

        public CustomerVehicleEvent(int category, int time, Customer customer, Vehicle vehicle) : base(category, time)
        {
            Category = category; //Cat 2 = enters vehicle, cat 3 = leaves vehicle
            Time = time;
            Customer = customer;
            Vehicle = vehicle;
        }

        public override string GetMessage()
        {
            string dateString = "[" + DateTime.Now.ToString() + "] ";
            string message = "";
            if (Customer != null && Vehicle != null)
            {
                message = dateString+this.ToString() + Customer.ToString();

                    if (Category == 2)
                    {
                        if (Vehicle.IsFull)
                        {
                            message = dateString+this.ToString() + Vehicle.ToString() +"is FULL,"+Customer.ToString() + " was not served.";
                        }
                        else
                        {
                            message = message + " ENTERED "+Vehicle.ToString() + " at " +
                                              Customer.PickupDelivery[0] + " with destination to "+Customer.PickupDelivery[1]+".";

                        }
                    }

                    if (Category == 3)
                    {
                        message =  message + " LEFT vehicle " + Vehicle.ToString() +
                                          " at " +
                                          Customer.PickupDelivery[1]+ " with origin stop as "+Customer.PickupDelivery[0]+".";
                    }          
            }

            return message;
        }

        public override void Treat()
        {
            if (Vehicle != null && Customer != null && !AlreadyHandled)
            {
                if (Category == 2)
                {
                    //Customer entered vehicle i at stop x with destination y
                    Customer.Enter(Vehicle,Time);
                    AlreadyHandled = true;

                }

                if (Category == 3)
                {
                    //Customer left vehicle i at stop x with destination y
                    Customer.Leave(Vehicle,Time);
                    AlreadyHandled = true;

                }
            }
        }
    }
}
