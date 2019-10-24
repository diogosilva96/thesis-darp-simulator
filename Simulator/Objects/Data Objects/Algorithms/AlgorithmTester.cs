using System;
using System.Diagnostics;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    public abstract class AlgorithmTester
    {
        public string Name;
        public double ComputationTimeInSeconds; //in seconds
        public string Type;
        public object AlgorithmValue;
        public int MaxUpperBoundInMinutes; //in minutes
        public Assignment Solution;
        public DarpDataModel DataModel;
        public bool SolutionIsFeasible;
        public int SearchTimeLimitInSeconds; //in seconds
        public DarpSolver Solver;

        protected AlgorithmTester(DarpDataModel dataModel,bool allowDropNodes)
        {
            DataModel = dataModel;
            SolutionIsFeasible = false;
            Solver = new DarpSolver(allowDropNodes);
        }

        public override string ToString()
        {
            return "["+GetType().Name+"]";
        }

        public void Test() //tests the algorithm using different maxUpperBound values until it finds the earliest feasible maxupperbound value, then saves its metrics
        {
            Console.WriteLine(this.ToString() + " testing algorithm: " + Name);

            var watch = Stopwatch.StartNew();
            var solution = TryGetSolutionHookMethod();
            watch.Stop();
            var elapsedSeconds = watch.ElapsedMilliseconds * 0.001;
            SolutionIsFeasible = solution != null;
            if (SolutionIsFeasible) //solution != null (means earliest feasible solution was found)
            {
                //Saves the important metrics for the earliest feasible solution
                MaxUpperBoundInMinutes = Solver.MaxUpperBound;
                ComputationTimeInSeconds = elapsedSeconds;
                Solution = solution;
            }
        }

        public abstract Assignment TryGetSolutionHookMethod();
    }
}
