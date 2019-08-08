using System;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
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
        protected RoutingSearchParameters SearchParameters;
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

            InitHookMethod(); //for the subclasses to define
            InitSearchParameters();
        }

        public abstract void InitHookMethod();

        public void InitSearchParameters()
        {
            // Setting first solution heuristic.
            SearchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            SearchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;
        }

        public Assignment GetSolution(DataModel dataModel)
        {
            DataModel = dataModel;
            Init();
            //Assignment initialSolution = _routing.ReadAssignmentFromRoutes(_dataModel.InitialRoutes, true);

            //Get the solution of the problem
            Assignment solution = Routing.SolveWithParameters(SearchParameters);
            return solution;
        }

        public Assignment GetSolution(DataModel dataModel,int searchTimeLimit)
        {
            DataModel = dataModel;
            Init();
            SetSearchStrategy(searchTimeLimit); //sets a search strategy with a time limit
            Assignment solution = Routing.SolveWithParameters(SearchParameters); //solves the problem
            return solution;
        }
        public void SetSearchStrategy(int searchTimeLimit)
        {
            SearchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            SearchParameters.TimeLimit = new Duration { Seconds = searchTimeLimit };
            SearchParameters.LogSearch = false; //logs the search if true
        }

        public abstract void Print(Assignment solution);
    }
}
