﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Events;
using Simulator.Objects.Data_Objects;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            AbstractSimulation sim = new Simulation(45*60,30*60,0.02);
            sim.MainLoop();
            Console.Read();

        }
    }
}
