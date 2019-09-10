using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace Simulator.Objects.Data_Objects.PDTW
{
    public abstract class AlgorithmTester
    {
        public string Name;
        public double ComputeTime;
        public string Type;
        public object AlgorithmValue;
        public int MaxUpperBound;
        public Assignment Solution;
        public PdtwDataModel DataModel;
        public bool SolutionIsFeasible;
        public int SearchTimeLimit;
        protected PdtwSolver Solver;

        public AlgorithmTester(PdtwDataModel dataModel)
        {
            DataModel = dataModel;
            SolutionIsFeasible = false;
            Solver = new PdtwSolver();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"]";
        }

        public void Test()
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
                        MaxUpperBound = Solver.MaxUpperBound;
                        ComputeTime = elapsedSeconds;
                        Solution = solution;
                        SolutionIsFeasible = true;
                        break;
                    }
                }
        }
        public abstract Assignment TryGetSolutionHookMethod(int maxUpperBound);
    }
}
