using System.ComponentModel.Design;
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
            if (depot != null)
            {
                if (type == 1)
                {
                    dataModel = new PickupDeliveryDataModel(depot);
                }
                else if (type == 2)
                {
                    dataModel = new TimeWindowDataModel(depot,_vehicleSpeed);
                }
            }
            return dataModel;
        }
    }
}
