using System;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.DARP.DataModels;

namespace Simulator.Objects.Data_Objects.DARP.Solvers
{
    public abstract class Solver
    {
        protected DataModel DataModel;
        protected RoutingIndexManager RoutingIndexManager;
        protected RoutingModel RoutingModel;
        protected Google.OrTools.ConstraintSolver.Solver ConstraintSolver;
        protected int TransitCallbackIndex;
        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void Init()
        {
            // Create RoutingModel Index RoutingIndexManager
            RoutingIndexManager = new RoutingIndexManager(
                DataModel.Matrix.GetLength(0),
                DataModel.VehicleNumber,
                DataModel.DepotIndex);

            //Create routing model
            RoutingModel = new RoutingModel(RoutingIndexManager);

            // Create and register a transit callback.
            TransitCallbackIndex = RoutingModel.RegisterTransitCallback(
                (long fromIndex, long toIndex) =>
                {
                    // Convert from routing variable Index to time matrix or distance matrix NodeIndex.
                    var fromNode = RoutingIndexManager.IndexToNode(fromIndex);
                    var toNode = RoutingIndexManager.IndexToNode(toIndex);
                    return DataModel.Matrix[fromNode, toNode];
                }
            );
            InitHookMethod(); //for the subclasses to define
        }

        public abstract void InitHookMethod();

        public RoutingSearchParameters GetSearchParameters()
        {
            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;

            return searchParameters;
        }

        public Assignment GetSolution(DataModel dataModel)
        {
            DataModel = dataModel;
            Init();
            var searchParameters = GetSearchParameters();
            //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_dataModel.InitialRoutes, true);
            //Get the solution of the problem
            Assignment solution = RoutingModel.SolveWithParameters(searchParameters);
            return solution;
        }

        public Assignment GetSolution(DataModel dataModel,int searchTimeLimit)
        {
            DataModel = dataModel;
            Init();

            var searchParameters = GetSearchParameters();
            SetSearchStrategy(searchParameters,searchTimeLimit); //sets a search strategy with a time limit
            Assignment solution = RoutingModel.SolveWithParameters(searchParameters); //solves the problem
            return solution;
        }
        public void SetSearchStrategy(RoutingSearchParameters searchParam,int searchTimeLimit)
        {
            searchParam.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            searchParam.TimeLimit = new Duration { Seconds = searchTimeLimit };
            searchParam.LogSearch = false; //logs the search if true

        }

        public abstract void Print(Assignment solution);
    }
}
