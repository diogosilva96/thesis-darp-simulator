using System;
using System.Collections.Generic;

namespace Simulator.Objects.Data_Objects.Simulation_Objects
{
    public class Customer:Person
    {

        public long RideTime => RealTimeWindow[1]-RealTimeWindow[0];

        public Stop[] PickupDelivery;

        public long[] RealTimeWindow;//in seconds

        public long[] DesiredTimeWindow; //in seconds

        public bool IsInVehicle;

        public bool AlreadyServed;
        
        public int RequestTime;//request time in seconds

        public long WaitTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public long DelayTime =>
            RealTimeWindow[1] - DesiredTimeWindow[1] > 0 ? RealTimeWindow[1] - DesiredTimeWindow[1] : 0;

        public Customer(Stop[] pickupDelivery, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            RequestTime = requestTime;
            Init();
         
        }

        public Customer(List<Stop> stopsList, List<Stop> excludedStops, int requestTime, int[] pickupTimeWindow)
        {

            var rng = RandomNumberGenerator.Random;
            var pickup = stopsList[rng.Next(0, stopsList.Count)];
            while (excludedStops.Contains(pickup)) //if the pickup is the depot has to generate another pickup stop
            {
                pickup = stopsList[rng.Next(0, stopsList.Count)];
            }

            var delivery = pickup;

            while (delivery == pickup || excludedStops.Contains(delivery)) //if the delivery stop is equal to the pickup stop or depot stop, it needs to generate a different delivery stop
            {

                delivery = stopsList[rng.Next(0, stopsList.Count)];
            }


            var pickupTime = rng.Next(pickupTimeWindow[0], pickupTimeWindow[1]); //the minimum pickup time is 0 minutes above the requestTime and maximum pickup is the end time of the simulation 
            var deliveryTime = rng.Next(pickupTime + 15 * 60, pickupTime + 45 * 60); //delivery time will be at minimum 15 minutes above the pickuptime and at max 45 minutes from the pickup time
            if (pickupTime > deliveryTime)
            {
                throw new ArgumentException("Pickup time greater than deliveryTime");
            }

            PickupDelivery= new[] { pickup, delivery };
            DesiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
            Init();
        }

        public Customer(Stop[] pickupDelivery, long[] desiredTimeWindow, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            DesiredTimeWindow = desiredTimeWindow;
            Init();
        }

        public void Init()
        {
            IsInVehicle = false;
            AlreadyServed = false;
            RealTimeWindow = new long[2];
        }
        public override string ToString()
        {
            return "Customer "+Id+" ";
        }

        public bool Enter(Vehicle v, int time)
        {  
            if (!IsInVehicle)
            {
                var customerAdded = v.AddCustomer(this);
                TimeSpan t = TimeSpan.FromSeconds(time);
                if (customerAdded)
                {
                    if (v.TripIterator.Current.ExpectedCustomers.Contains(this))
                    {
                        v.TripIterator.Current.ExpectedCustomers.Remove(this);
                    }
                    RealTimeWindow[0] = time; //assigns the real enter time of the timewindow
                    IsInVehicle = true;
                    var waitTimeStr = "";
                    if (DesiredTimeWindow != null && RealTimeWindow != null)
                    {
                        waitTimeStr = "(Wait time: " + WaitTime + " seconds)";
                    }
                    Console.WriteLine(v.SeatsState + this.ToString() + waitTimeStr+ " ENTERED at " + PickupDelivery[0] +
                                      " at " + t.ToString() + ".");
                    
                }
                else
                {
                    Console.WriteLine(v.SeatsState+this.ToString() + "was not serviced at "+PickupDelivery[0]+" at "+t.ToString()+", because vehicle is FULL!");
                    IsInVehicle = false;
                }

                return customerAdded; //returns true if vehicle is not full and false if it is full
            }

            return false;
        }

        public void PrintPickupDelivery()
        {
            string stringToBePrinted = this.ToString() + " - PickupDelivery: [" + PickupDelivery[0] + " -> " + PickupDelivery[1] + "]";
            if (DesiredTimeWindow != null)
            {
                stringToBePrinted = stringToBePrinted + " - TimeWindows (in seconds): {"+DesiredTimeWindow[0]+","+DesiredTimeWindow[1]+"}";
            }
            Console.WriteLine(stringToBePrinted);
        }

        public bool Leave(Vehicle vehicle, int time)
        {
            if (IsInVehicle)
            {
                var customerLeft = vehicle.RemoveCustomer(this);
                if (customerLeft)
                {                   
                    TimeSpan t = TimeSpan.FromSeconds(time);
                    RealTimeWindow[1] = time; //assigns the real leave time of the time window
                    IsInVehicle = false;
                    AlreadyServed = true;
                    var delayTimeStr = "";
                    if (DesiredTimeWindow != null && RealTimeWindow != null)
                    {
                        delayTimeStr = " ; Delay time: " + DelayTime + " seconds";
                    }

                    Console.WriteLine(vehicle.SeatsState + this.ToString() + "(Ride time:" + this.RideTime + " seconds"+delayTimeStr+") LEFT at " + PickupDelivery[1] +
                                      " at " + t.ToString()+".");
                    if (vehicle.TripIterator.Current != null && (vehicle.TripIterator.Current.StopsIterator.IsDone && vehicle.Customers.Count ==0))//this means that the trip is complete
                    {
                        vehicle.TripIterator.Current.Finish(time); //Finishes the service
                        Console.WriteLine(vehicle.ToString()+vehicle.TripIterator.Current + " FINISHED at " +
                                           TimeSpan.FromSeconds(time).ToString() + ", Duration:" + Math.Round(TimeSpan.FromSeconds(vehicle.TripIterator.Current.RouteDuration).TotalMinutes) + " minutes.");
                        vehicle.TripIterator.MoveNext();
                        if (vehicle.TripIterator.Current == null)
                        {
                            vehicle.TripIterator.Reset();
                            vehicle.TripIterator.MoveNext();
                        }
                    }

                }

                return customerLeft;
            }

            return false;
        }
    }
}
