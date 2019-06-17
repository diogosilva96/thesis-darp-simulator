using System;
using System.Data.SqlTypes;
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
        private bool CategorySuccess { get; set; } //if true vehicle was not full and customer entered or left vehicle

        public CustomerVehicleEvent(int category, int time, Customer customer, Vehicle vehicle) : base(category, time)
        {
            Category = category; //Cat 2 = enters vehicle, cat 3 = leaves vehicle
            Time = time;
            Customer = customer;
            Vehicle = vehicle;
            Service = vehicle.ServiceIterator.Current;
            CategorySuccess = false;
        }

        public override string GetTraceMessage()
        {
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            string message = "";
            if (Customer != null && Vehicle != null)
            {
                message = timestamp+ splitter+this.ToString() +splitter+"Vehicle:"+Vehicle.Id+splitter+ "Trip:" + Service.Trip.Id + splitter + "ServiceId:" + Service.Id + splitter+ "Customer pickup:" + Customer.PickupDelivery[0].Id + splitter + "Customer delivery:" + Customer.PickupDelivery[1].Id; ;  
            }

            return message;
        }

        public string GetValidationsMessage(int validationId)
        {
            //(CustomerId, Category,CategorySuccess (was the customer able to leave or enter vehicle (1 true, 0 false)), VehicleId, RouteId, TripId, Service.Id, StopId,Time)
            string message = "";
            int stopId;
            stopId = Category == 2 ? Customer.PickupDelivery[0].Id : Customer.PickupDelivery[1].Id;
            var catSuccess = CategorySuccess == true ? 1 : 0;

            message = validationId + "," + Customer.Id + "," + Category + ","+catSuccess+"," + Vehicle.Id + ","+Service.Trip.Route.Id+"," + Service.Trip.Id +","+ Service.Id+"," + stopId + "," +
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
                    CategorySuccess = Customer.Enter(Vehicle,Time);
                    AlreadyHandled = true;

                }

                if (Category == 3)
                {
                    //Customer left vehicle i at stop x with destination y
                    CategorySuccess = Customer.Leave(Vehicle,Time);
                    AlreadyHandled = true;

                }
            }
        }
    }
}
