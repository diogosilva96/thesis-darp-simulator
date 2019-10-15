using System;

namespace Simulator.Objects.Data_Objects.Simulation_Objects
{
    public class Customer:Person
    {

        public long RideTime => RealTimeWindow[1]-RealTimeWindow[0];

        public Stop[] PickupDelivery;

        public long[] RealTimeWindow;//in seconds

        public long[] DesiredTimeWindow; //in seconds

        private bool _isInVehicle;

        public bool AlreadyServed;
        
        public int RequestTime;//request time in seconds

        public long WaitTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public Customer(Stop[] pickupDelivery, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            RequestTime = requestTime;
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
            _isInVehicle = false;
            AlreadyServed = false;
            RealTimeWindow = new long[2];
        }
        public override string ToString()
        {
            return "Customer "+Id+" ";
        }

        public bool Enter(Vehicle v, int time)
        {  
            if (!_isInVehicle)
            {
                var customerAdded = v.AddCustomer(this);
                TimeSpan t = TimeSpan.FromSeconds(time);
                if (customerAdded)
                {
                    RealTimeWindow[0] = time; //assigns the real enter time of the timewindow
                    _isInVehicle = true;
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
                    _isInVehicle = false;
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
            if (_isInVehicle)
            {
                var customerLeft = vehicle.RemoveCustomer(this);
                if (customerLeft)
                {
                    TimeSpan t = TimeSpan.FromSeconds(time);
                    RealTimeWindow[1] = time; //assigns the real leave time of the time window
                    _isInVehicle = false;
                    AlreadyServed = true;
                    Console.WriteLine(vehicle.SeatsState + this.ToString() + "(Ride time:" + this.RideTime + " seconds) LEFT at " + PickupDelivery[1] +
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
