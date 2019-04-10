using System;
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

        public int ServiceTime => RealTimeWindow[1]-RealTimeWindow[0];

        private Logger.Logger _consoleLogger;

        public Stop[] PickupDelivery;

        public int[] RealTimeWindow;

        //public int[] DesiredTimeWindow;

        private bool _isInVehicle;

        
        public Customer(Stop pickUpStop,Stop dropOffStop)
        {

            PickupDelivery = new Stop[] {pickUpStop,dropOffStop};
            Init();
        }


        private void Init()
        {
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
                    _consoleLogger.Log(v.State +this.ToString()+"ENTERED at " + PickupDelivery[0] +
                                       " at " + t.ToString()+ ".");
                    RealTimeWindow[0] = time;
                    _isInVehicle = true;
                }
                else
                {
                    _consoleLogger.Log(v.State+this.ToString() + "was not serviced at "+PickupDelivery[0]+" at "+t.ToString()+", because vehicle is full!");
                    _isInVehicle = false;
                }

                return customerAdded; //returns true if vehicle is not full and false if it is full
            }

            return false;
        }

        public bool Leave(Vehicle v, int time)
        {
            if (_isInVehicle)
            {
                var customerLeft = v.RemoveCustomer(this);
                if (customerLeft)
                {
                    TimeSpan t = TimeSpan.FromSeconds(time);
                    _consoleLogger.Log(v.State+this.ToString() + "LEFT at " + PickupDelivery[1] +
                                       "at " + t.ToString() + ".");
                    RealTimeWindow[1] = time;
                    _isInVehicle = false;
                }

                return customerLeft;
            }

            return false;
        }
    }
}
