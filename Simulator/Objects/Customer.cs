﻿using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Transactions;
using GraphLibrary.Objects;
using MathNet.Numerics.Distributions;
using Simulator.Logger;


namespace Simulator.Objects
{
    public class Customer:Person
    {

        public int RideTime => RealTimeWindow[1]-RealTimeWindow[0];

        private readonly Logger.Logger _consoleLogger;

        public Stop[] PickupDelivery;

        public int[] RealTimeWindow;

        public int[] DesiredTimeWindow;

        private bool _isInVehicle;

        public int WaitingTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public Customer(Stop pickUpStop,Stop deliveryStop)
        {

            PickupDelivery = new Stop[] {pickUpStop,deliveryStop};
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
            RealTimeWindow = new int[2];
            _isInVehicle = false;
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
                    _consoleLogger.Log(v.SeatsState +this.ToString()+"ENTERED at " + PickupDelivery[0] +
                                       " at " + t.ToString()+ ".");
                    RealTimeWindow[0] = time;
                    _isInVehicle = true;
                }
                else
                {
                    _consoleLogger.Log(v.SeatsState+this.ToString() + "was not serviced at "+PickupDelivery[0]+" at "+t.ToString()+", because vehicle is full!");
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
                    _consoleLogger.Log(vehicle.SeatsState+this.ToString() + "LEFT at " + PickupDelivery[1] +
                                       "at " + t.ToString() + ".");
                    RealTimeWindow[1] = time;
                    _isInVehicle = false;
                    if (vehicle.ServiceIterator.Current.StopsIterator.IsDone && vehicle.Customers.Count == 0)//this means that the service is complete
                    {
                        vehicle.ServiceIterator.Current.Finish(time); //Finishes the service
                        _consoleLogger.Log(vehicle.ToString()+vehicle.ServiceIterator.Current + " FINISHED at " +
                                           TimeSpan.FromSeconds(time).ToString() + ", Duration:" + Math.Round(TimeSpan.FromSeconds(vehicle.ServiceIterator.Current.RouteDuration).TotalMinutes) + " minutes.");
                        vehicle.ServiceIterator.MoveNext();
                    }

                }

                return customerLeft;
            }

            return false;
        }
    }
}
