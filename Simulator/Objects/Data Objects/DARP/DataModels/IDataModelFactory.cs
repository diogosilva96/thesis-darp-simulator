using Simulator.Objects.Data_Objects.DARP.Solvers;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public interface IDataModelFactory
    {
        DataModel CreateDataModel(Stop depot,int type);
    }
}
