using System;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public class PickupDeliveryTimeWindowModel:DataModel //Vehicle routing problem with pickup and delivery time windows data model
    {
        public long[,] TimeWindows => GetTimeWindowsArray();

        private readonly int _vehicleSpeed;

        private readonly int _dayInMinutes = 1440; //24hours = 1440minutes

        public PickupDeliveryTimeWindowModel(Stop depot,int vehicleSpeed) : base(depot)
        {
            _vehicleSpeed = vehicleSpeed;
        }

        protected override void UpdateMatrix()
        {
            Matrix = new MatrixBuilder().GetTimeMatrix(Stops,_vehicleSpeed);
        }

        private long[,] GetInitialTimeWindowsArray(long[,] timeWindows)
        { 
            //Loop to initialize each cell of the timewindow array at the maximum minutes value (1440minutes - 24 hours)
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                timeWindows[i, 0] = 0; //lower bound of the timewindow is initialized with 0
                timeWindows[i, 1] = _dayInMinutes; //Upper bound of the timewindow with a max long value
                
            }

            return timeWindows;
        }

        public long[,] GetTimeWindowsArray()
        {
            long[,] timeWindows = new long[Stops.Count, 2];
            timeWindows = GetInitialTimeWindowsArray(timeWindows);

            foreach (var customer in Customers)
            {
                //LOWER BOUND (MINIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var  customerMinTimeWindow = TimeSpan.FromSeconds(customer.DesiredTimeWindow[0]).TotalMinutes; //customer min time window in minutes
                var arrayMinTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0]; //gets current min timewindow for the pickupstop in minutes
                //If there are multiple min time window values for a given stop, the minimum time window will be the maximum timewindow between all those values
                //because the vehicle must arrive that stop at most, at the greatest min time window value, in order to satisfy all requests
                timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0] =
                        Math.Max((long) arrayMinTimeWindow, (long) customerMinTimeWindow); //the mintimewindow (lower bound) is the maximum value between the current timewindow in the array and the current customer timewindow
  

                //UPPER BOUND (MAXIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMaxTimeWindow = TimeSpan.FromSeconds(customer.DesiredTimeWindow[1]).TotalMinutes; //customer max time window in minutes
                var arrayMaxTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1]; //gets curent max timewindow for the delivery stop in minutes
                //If there are multiple max timewindows for a given stop, the maximum time window will be the minimum between all those values
                //because the vehicle must arrive that stop at most, at the lowest max time window value, in order to satisfy all the requests
                timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1] = Math.Min((long)arrayMaxTimeWindow, (long)customerMaxTimeWindow);//the maxtimewindow (upper bound) is the minimum value between the current  timewindow in the array and the current customer timewindow
                    

                
            }

           // timeWindows = ClearTimeWindowsArray(timeWindows);
           
            return timeWindows;
        }

        private long[,] ClearTimeWindowsArray(long[,] timeWindows)
        {
            //loop to remove the previously inserted maxvalues
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                var minTime = timeWindows[i, 0];
                var maxTime = timeWindows[i, 1];
                if (minTime == _dayInMinutes || maxTime == _dayInMinutes)
                {
                    if (minTime == _dayInMinutes)
                    {
                        timeWindows[i, 0] = 0; //removes the maxvalue
                    }
                    if (maxTime == _dayInMinutes)
                    {
                        minTime = timeWindows[i, 0];
                        timeWindows[i, 1] = minTime+5;//min time plus 5 mins
                    }
                }

            }
            return timeWindows;
        }
        public void PrintTimeWindows() //For debug purposes
        {
            Console.WriteLine(this.ToString()+"Time Windows:");
            for (int i = 0; i < TimeWindows.GetLength(0); i++)
            {
                Console.Write(IndexToStop(i)+"{");
                for (int j = 0; j < TimeWindows.GetLength(1); j++)
                {
                    if (j == 0)
                    {
                        Console.Write(TimeWindows[i, j]+",");
                    }
                    else
                    {

                        Console.WriteLine(TimeWindows[i, j] + "}");
                    }
                }
            }
            
        }
    }
}
