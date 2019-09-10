﻿using System;
using System.Diagnostics;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.PDTW;

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
        public PdtwDataModel DataModel;
        public bool SolutionIsFeasible;
        public int SearchTimeLimitInSeconds; //in seconds
        public PdtwSolver Solver;

        protected AlgorithmTester(PdtwDataModel dataModel)
        {
            DataModel = dataModel;
            SolutionIsFeasible = false;
            Solver = new PdtwSolver();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"]";
        }

        public void Test() //tests the algorithm using different maxUpperBound values until it finds the earliest feasible maxupperbound value, then saves its metrics
        {
            Console.WriteLine(this.ToString() + " testing " + Type + ": " + Name);
            //for loop that tries to find the earliest feasible solution (trying to minimize the maximum upper bound) within a maximum delay delivery time (upper bound), using the current customer requests
                for (int maxUpperBound = 0; maxUpperBound < 30; maxUpperBound++)
                {
                    var watch = Stopwatch.StartNew();
                    var solution = TryGetSolutionHookMethod(maxUpperBound);
                    watch.Stop();
                    var elapsedSeconds = watch.ElapsedMilliseconds * 0.001;

                    if (solution != null) //solution != null (means earliest feasible solution was found)
                    {
                        //Saves the important metrics for the earliest feasible solution
                        MaxUpperBoundInMinutes = Solver.MaxUpperBound;
                        ComputationTimeInSeconds = elapsedSeconds;
                        Solution = solution;
                        SolutionIsFeasible = true;
                        break;
                    }
                }
        }

        public abstract Assignment TryGetSolutionHookMethod(int maxUpperBound);
    }
}
