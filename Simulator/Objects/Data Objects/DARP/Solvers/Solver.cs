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
        protected int TransitCallbackIndex;
        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }


        public void Init()
        {
            // Create Routing Index Manager
            Manager = new RoutingIndexManager(
                DataModel.Matrix.GetLength(0),
                DataModel.VehicleNumber,
                DataModel.DepotIndex);

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

            InitHookMethod();
            
        }

        public abstract void InitHookMethod();

        public Assignment Solve(DataModel dataModel)
        {
            DataModel = dataModel;
            Init();
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
            //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            //searchParameters.TimeLimit = new Duration { Seconds = 10 };
            //searchParameters.LogSearch = true; //logs the search


            //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_dataModel.InitialRoutes, true);

            //Get the solution of the problem
            Assignment solution = Routing.SolveWithParameters(searchParameters);
            return solution;
        }

        public abstract void Print(Assignment solution);
    }
}
