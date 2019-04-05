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
        public Stop PickUpStop { get; set; }
        public Stop DropOffStop { get; set; }

        public int ServiceTime => DropOffTime - PickUpTime;

        private Logger.Logger _consoleLogger;

        public int PickUpTime { get; internal set; }

        public int DropOffTime { get; internal set; }

        private bool _isInVehicle;

        
        public Customer(Stop pickUpStop,Stop dropOffStop)
        {
            PickUpStop = pickUpStop;
            DropOffStop = dropOffStop;
            Init();
        }

        public Customer()
        {
            PickUpStop = null;
            DropOffStop = null;
            Init();
           
        }

        private void Init()
        {
            IRecorder recorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(recorder);
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
                    _consoleLogger.Log(v.State +this.ToString()+"ENTERED at " + PickUpStop +
                                       " at " + t.ToString()+ ".");
                    PickUpTime = time;
                    _isInVehicle = true;
                }
                else
                {
                    _consoleLogger.Log(v.State+this.ToString() + "was not serviced at "+PickUpStop+" at "+t.ToString()+", because vehicle is full!");
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
                    _consoleLogger.Log(v.State+this.ToString() + "LEFT at " + DropOffStop +
                                       "at " + t.ToString() + ".");
                    DropOffTime = time;
                    _isInVehicle = false;
                }

                return customerLeft;
            }

            return false;
        }
    }
}
