using System;
using Simulator.Objects;

namespace Simulator.Events
{
    class CustomerVehicleEvent:Event
    {
        //Event when a customer enters or leaves a vehicle
        public Customer Customer { get; set; }
        public Vehicle Vehicle { get; set; }
        public CustomerVehicleEvent(int category, int time, Customer customer, Vehicle vehicle) : base(category, time)
        {
            Category = category; //Cat 2 = enters vehicle, cat 3 = leaves vehicle
            Time = time;
            Customer = customer;
            Vehicle = vehicle;
        }

        public CustomerVehicleEvent(int category) : base(category)
        {
            Category = category;
            Customer = null;
            Vehicle = null;
        }
        public override string GetMessage()
        {
            string date_string = "[" + DateTime.Now.ToString() + "] ";
            string message = "";
            if (Customer != null && Vehicle != null)
            {
                message = date_string+this.ToString() + Customer.ToString();

                    if (Category == 2)
                    {
                        if (Vehicle.IsFull)
                        {
                            message = date_string+this.ToString() + Vehicle.ToString() +"is full,"+Customer.ToString() + " was not served at time"+Time+".";
                        }
                        else
                        {
                            message = message + " entered "+Vehicle.ToString() + " at " +
                                              Customer.PickUpStop + " with destination to "+Customer.DropOffStop+" at " + Time + ".";

                        }
                    }

                    if (Category == 3)
                    {
                        message =  message + " left vehicle " + Vehicle.ToString() +
                                          " at " +
                                          Customer.DropOffStop + " with origin stop as "+Customer.PickUpStop+" at " + Time + ".";
                    }          
            }

            return message;
        }

        public override void Treat()
        {
            if (Vehicle != null && Customer != null)
            {
                if (Category == 2)
                {
                    //Customer entered vehicle i at stop x with destination y
                    Customer.Enter(Vehicle,Time);
          
                }

                if (Category == 3)
                {
                    //Customer left vehicle i at stop x with destination y
                    Customer.Leave(Vehicle,Time);

                }
            }
        }
    }
}
