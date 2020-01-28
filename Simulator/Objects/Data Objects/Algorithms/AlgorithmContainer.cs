using System;
using System.Collections.Generic;
using System.IO;
using Google.OrTools.ConstraintSolver;


namespace Simulator.Objects.Data_Objects.Algorithms
{
    public class AlgorithmContainer
    {
        public List<FirstSolutionStrategy.Types.Value> FirstSolutionAlgorithms;
        public List<LocalSearchMetaheuristic.Types.Value> SearchAlgorithms;

        public override string ToString()
        {
            return "["+this.GetType().Name+"]";
        }

        public AlgorithmContainer()
        {
           FirstSolutionAlgorithms = GetFirstSolutionStrategyList();
            SearchAlgorithms = GetSearchStrategyList();

        }

        private List<FirstSolutionStrategy.Types.Value> GetFirstSolutionStrategyList()
        {
            List<FirstSolutionStrategy.Types.Value> firstSolutionAlgorithms =
                new List<FirstSolutionStrategy.Types.Value>();
           firstSolutionAlgorithms = new List<FirstSolutionStrategy.Types.Value>();
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Automatic);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathMostConstrainedArc);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.AllUnperformed);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.BestInsertion);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Christofides);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.EvaluatorStrategy);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.FirstUnboundMinValue);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.GlobalCheapestArc);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.LocalCheapestArc);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.LocalCheapestInsertion);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Savings);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.SequentialCheapestInsertion);
           firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Sweep);
           return firstSolutionAlgorithms;


        }

        private List<LocalSearchMetaheuristic.Types.Value> GetSearchStrategyList()
        {
            List<LocalSearchMetaheuristic.Types.Value> searchAlgorithms = new List<LocalSearchMetaheuristic.Types.Value>();
            searchAlgorithms = new List<LocalSearchMetaheuristic.Types.Value>();
            searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GenericTabuSearch);
            searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GreedyDescent);
            searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch);
            searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing);
            searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.TabuSearch);
            //searchAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.Automatic); //not being used
            return searchAlgorithms;
        }

    }
}
