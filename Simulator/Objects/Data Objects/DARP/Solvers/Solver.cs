using System;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP.DataModels;

namespace Simulator.Objects.Data_Objects.DARP.Solvers
{
    public abstract class Solver
    {
        protected DataModel DataModel;
        protected RoutingIndexManager Manager;
        protected RoutingModel Routing;
        protected Google.OrTools.ConstraintSolver.Solver ConstraintSolver;
        protected RoutingDimension RoutingDimension;
        protected Assignment Solution;
        protected int TransitCallbackIndex;
        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void InitManager()
        {
            if (DataModel != null)
            {
                // Create Routing Index Manager
                Manager = new RoutingIndexManager(
                    DataModel.Matrix.GetLength(0),
                    DataModel.VehicleNumber,
                    DataModel.DepotIndex);
            }
            else
            {
                throw new NullReferenceException("Data model is null");
            }

        }

        public void InitRouting()
        {
            if (Manager != null)
            {

                //Create routing model
                Routing = new RoutingModel(Manager);

                // Create and register a transit callback.
                TransitCallbackIndex = Routing.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                        var fromNode = Manager.IndexToNode(fromIndex);
                        var toNode = Manager.IndexToNode(toIndex);
                        return DataModel.Matrix[fromNode, toNode];
                    }
                );
            }
            else
            {
                throw new NullReferenceException("Manager is null");
            }
        }

        public void Init(DataModel dataModel)
        {
            DataModel = dataModel;
            InitManager();
            InitRouting();
            InitHookMethod();
            
        }

        public abstract void InitHookMethod();
        public abstract void Solve();

        public abstract void PrintSolution();
    }
}
