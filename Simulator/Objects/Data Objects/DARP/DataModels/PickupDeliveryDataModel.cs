using System;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public class PickupDeliveryDataModel:DataModel
    {


        public PickupDeliveryDataModel(Stop depot) : base(depot)
        {
        }



        protected override void UpdateMatrix()
        {
            Matrix = new MatrixBuilder().GetDistanceMatrix(Stops);
        }

    }
}
