using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.GraphLibrary;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using GraphLibrary;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.Objects.Data_Objects;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {

            AbstractSimulation sim = new Simulation();
            sim.Simulate();
            Console.Read();

        }
    }
}
