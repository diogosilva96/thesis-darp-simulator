using System;
using System.Data.SqlTypes;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Events
{
    class CustomerVehicleEvent:Event
    {
        //Event when a customer enters or leaves a vehicle
        public Customer Customer { get; internal set; }
        public Vehicle Vehicle { get; internal set; }
        public Trip Trip { get; internal set; }
        public bool OperationSuccess { get; set; } //if true vehicle was not full and customer entered or left vehicle

        public CustomerVehicleEvent(int category, int time, Customer customer, Vehicle vehicle) : base(category, time)
        {
            Category = category; //Cat 2 = enters vehicle, cat 3 = leaves vehicle
            Time = time;
            Customer = customer;
            Vehicle = vehicle;
            Trip= vehicle.TripIterator.Current;
            OperationSuccess = false;
        }

        public override string GetTraceMessage()
        {
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            string message = "";
            if (Customer != null && Vehicle != null)
            {
                message = timestamp+ splitter+this.ToString() +splitter+"Vehicle:"+Vehicle.Id+splitter+ "Trip:" + Trip.Id + splitter + "Trip StartTime:" + Trip.StartTime + splitter+ "Customer Id: " + Customer.Id + splitter + "Pickup: " + Customer.PickupDelivery[0].Id + splitter + "Delivery: " + Customer.PickupDelivery[1].Id + splitter + "Pickup time: " + Customer.DesiredTimeWindow[0] + splitter + "Delivery time: " + Customer.DesiredTimeWindow[1]; ;  
            }

            return message;
        }

        public string GetValidationsMessage(int validationId)
        {
            //(CustomerId, Category,OperationSuccess (was the customer able to leave or enter vehicle (1 true, 0 false)), VehicleId, RouteId, TripId, ServiceStartTime, StopId,Time)
            string message = "";
            int stopId;
            stopId = Category == 2 ? Customer.PickupDelivery[0].Id : Customer.PickupDelivery[1].Id;
            var catSuccess = OperationSuccess == true ? 1 : 0;

            message = validationId + "," + Customer.Id + "," + Category + ","+catSuccess+"," + Vehicle.Id + ","+Trip.Route.Id+"," + Trip.Id +","+ TimeSpan.FromSeconds(Trip.StartTime).ToString()+"," + stopId + "," +
                      TimeSpan.FromSeconds(Time).ToString();
                       
            

            return message;
        }

    }
}
