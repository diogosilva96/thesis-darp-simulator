using System.ComponentModel.Design;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Simulator.Objects.Data_Objects.DARP.Solvers;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public class DataModelFactory:IDataModelFactory
    {
        private readonly int _vehicleSpeed; //vehicle speed in km/h

        public DataModelFactory(int vehicleSpeed)
        {
            _vehicleSpeed = vehicleSpeed;
        }
        public DataModel CreateDataModel(Stop depot,int type)
        {
            DataModel dataModel = null;
            switch (type)
            {
                case 1:
                    dataModel = new PickupDeliveryDataModel(depot);
                    break;
                case 2:
                    dataModel = new TimeWindowDataModel(depot, _vehicleSpeed);
                    break;
                default:
                    dataModel = new PickupDeliveryDataModel(depot);
                    break;
            }
            return dataModel;
        }
    }
}
