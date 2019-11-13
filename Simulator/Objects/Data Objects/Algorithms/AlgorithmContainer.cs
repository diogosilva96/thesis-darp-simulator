using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Routing;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    public class AlgorithmContainer
    {
        private List<FirstSolutionStrategy.Types.Value> _firstSolutionAlgorithms;
        private List<LocalSearchMetaheuristic.Types.Value> _searchStrategyAlgorithms;
        public RoutingDataModel DataModel;
        public int TestId;

        public override string ToString()
        {
            return "["+this.GetType().Name+"]";
        }

        public AlgorithmContainer(RoutingDataModel dataModel)
        {
           GetFirstSolutionStrategyList();
           GetSearchStrategyList();
           DataModel = dataModel;
        }

        private void GetFirstSolutionStrategyList()
        {
           _firstSolutionAlgorithms = new List<FirstSolutionStrategy.Types.Value>();

           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Automatic);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathMostConstrainedArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.AllUnperformed);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.BestInsertion);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Christofides);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.EvaluatorStrategy);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.FirstUnboundMinValue);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.GlobalCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.LocalCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.LocalCheapestInsertion);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Savings);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.SequentialCheapestInsertion);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Sweep);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Unset);


        }

        private void GetSearchStrategyList()
        {
            _searchStrategyAlgorithms = new List<LocalSearchMetaheuristic.Types.Value>();
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GenericTabuSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GreedyDescent);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.TabuSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.Automatic);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.Unset);
        }


        public List<AlgorithmTester> GetTestedFirstSolutionAlgorithms(bool allowDropNodes)
        {
            List<AlgorithmTester> testedAlgorithms = new List<AlgorithmTester>();
            foreach (var firstSolutionStrategy in _firstSolutionAlgorithms)
            {
                AlgorithmTester algorithmTester = new FirstSolutionAlgorithmTester(DataModel,allowDropNodes,firstSolutionStrategy);
                algorithmTester.Test();
                testedAlgorithms.Add(algorithmTester);
            }
            return testedAlgorithms;
        }
        public List<AlgorithmTester> GetTestedSearchAlgorithms(int searchTimeLimitInSeconds,bool allowDropNodes)
        {
            List<AlgorithmTester> testedAlgorithmsList = new List<AlgorithmTester>();
            //TEST ALL the criterions (performance, time to compute, solution cost, etc)
            foreach (var searchStrategy in _searchStrategyAlgorithms)
            {
                AlgorithmTester algorithmTester= new SearchAlgorithmTester(DataModel,allowDropNodes,searchStrategy,searchTimeLimitInSeconds);
                algorithmTester.Test();
                testedAlgorithmsList.Add(algorithmTester); //adds it to the list
                
            }
            return testedAlgorithmsList;
        }

    }
}
