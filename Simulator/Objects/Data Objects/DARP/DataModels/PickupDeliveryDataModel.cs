using System;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public class PickupDeliveryDataModel:DataModel
    {
        public int[][] PickupsDeliveries => GetPickupDeliveryIndexMatrix();


        public PickupDeliveryDataModel(Stop depot) : base(depot)
        {
        }

        private int[][] GetPickupDeliveryIndexMatrix()//returns the pickupdelivery stop matrix using indexes (based on the pickupdeliverystop list) instead of stop id's
        {
            int[][] pickupsDeliveries = new int[base.Customers.Count][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            int insertCounter = 0;
            foreach (var customer in base.Customers)
            {
                var pickup = customer.PickupDelivery[0];
                var delivery = customer.PickupDelivery[1];
                var pickupDeliveryInd = new int[] { base.Stops.IndexOf(pickup), base.Stops.IndexOf(delivery) };
                pickupsDeliveries[insertCounter] = pickupDeliveryInd;
                insertCounter++;
            }

            return pickupsDeliveries;
        }


        protected override void UpdateMatrix()
        {
            Matrix = new MatrixBuilder().GetDistanceMatrix(Stops);
        }

    }
}
