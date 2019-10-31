using System;
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
            Seed = 1;
        }
        private static int _seed;


        public static Random Random;

    }
}
