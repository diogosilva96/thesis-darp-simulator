using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Simulator.Objects
{
    public class MinimumSeconds:IDisposable
    {
        private DateTime _EndTime;

        public MinimumSeconds(double seconds)
        {
            _EndTime = DateTime.Now.AddSeconds(seconds);
        }

        public void Dispose()
        {
            while (DateTime.Now < _EndTime)
            {
                Thread.Sleep(100);
            }
        }
    }
}
