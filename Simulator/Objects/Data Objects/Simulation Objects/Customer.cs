using System;

namespace Simulator.Objects.Data_Objects
{
    public class Customer:Person
    {

        public int RideTime => RealTimeWindow[1]-RealTimeWindow[0];

        public Stop[] PickupDelivery;

        public int[] RealTimeWindow;

        public int[] DesiredTimeWindow;

        private bool _isInVehicle;

        public bool AlreadyServed;

        public int RequestTime;//request timestamp in seconds

        public int WaitingTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public Customer(Stop[] pickupDelivery, int requestTime)
        {
            PickupDelivery = pickupDelivery;
           

            RequestTime = requestTime;
            Init();
         
        }

        public Customer(Stop[] pickupDelivery, int[] desiredTimeWindow, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            DesiredTimeWindow = desiredTimeWindow;
            Init();
        }

        public void Init()
        {
            _isInVehicle = false;
            AlreadyServed = false;
            RealTimeWindow = new int[2];
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
                    Console.WriteLine(v.SeatsState +this.ToString()+"ENTERED at " + PickupDelivery[0] +
                                       " at " + t.ToString()+ ".");
                    RealTimeWindow[0] = time;
                    _isInVehicle = true;
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

        public bool Leave(Vehicle vehicle, int time)
        {
            if (_isInVehicle)
            {
                var customerLeft = vehicle.RemoveCustomer(this);
                if (customerLeft)
                {
                    TimeSpan t = TimeSpan.FromSeconds(time);
                    Console.WriteLine(vehicle.SeatsState+this.ToString() + "LEFT at " + PickupDelivery[1] +
                                       "at " + t.ToString() + ".");
                    RealTimeWindow[1] = time;
                    _isInVehicle = false;
                    AlreadyServed = true;
                    if (vehicle.TripIterator.Current.StopsIterator.IsDone && vehicle.Customers.Count ==0)//this means that the service is complete
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
