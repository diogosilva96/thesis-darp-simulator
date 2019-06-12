using System;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator.Events
{
    class CustomerVehicleEvent:Event
    {
        //Event when a customer enters or leaves a vehicle
        public Customer Customer { get; internal set; }
        public Vehicle Vehicle { get; internal set; }

        public Service Service { get; internal set; }

        public CustomerVehicleEvent(int category, int time, Customer customer, Vehicle vehicle) : base(category, time)
        {
            Category = category; //Cat 2 = enters vehicle, cat 3 = leaves vehicle
            Time = time;
            Customer = customer;
            Vehicle = vehicle;
            Service = vehicle.ServiceIterator.Current;
        }

        public override string GetTraceMessage()
        {
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            string message = "";
            if (Customer != null && Vehicle != null)
            {
                message = timestamp+ splitter+this.ToString() +splitter+"Vehicle:"+Vehicle.Id+splitter+ "Trip:" + Service.Trip.Id + splitter + "Start_time:" + Service.StartTime + splitter+ "Customer pickup:" + Customer.PickupDelivery[0].Id + splitter + "Customer delivery:" + Customer.PickupDelivery[1].Id; ;  
            }

            return message;
        }

        public string GetValidationsMessage()
        {
            //(CustomerId, Category, VehicleId, RouteId, TripId, Service.Id, StopId,Time)
            string message = "";
            int stopId;
            if (Category == 2)
            {
                stopId = Customer.PickupDelivery[0].Id;
            }
            else
            {
                stopId = Customer.PickupDelivery[1].Id;
            }

            message = Customer.Id + "," + Category + "," + Vehicle.Id + ","+Service.Trip.Route.Id+"," + Service.Trip.Id +","+ Service.Id+"," + stopId + "," +
                      TimeSpan.FromSeconds(Time).ToString();
                       
            

            return message;
        }

        public override void Treat()
        {
            if (Vehicle != null && Customer != null && !AlreadyHandled && Vehicle.ServiceIterator.Current == Service)
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
