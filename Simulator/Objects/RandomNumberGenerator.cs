﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Simulator.Objects
{
    public static class RandomNumberGenerator
    {
        public static int Seed
        {
            get => _seed;
            set {
                if (value != _seed)
                {
                    _seed = value;
                    Random = new Random(_seed);
                }
           
            }
        }

        static RandomNumberGenerator()
        {
            Seed = new Random().Next(int.MaxValue);
            GenerateNewRandomSeed();
        }
        private static int _seed;

        public static void GenerateNewRandomSeed()
        {
            Seed = Random.Next(int.MaxValue);
        }

        public static Random Random;

    }
}
