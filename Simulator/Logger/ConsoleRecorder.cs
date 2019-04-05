using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Logger
{
    class ConsoleRecorder:IRecorder
    {
        public void Record(string message)
        {
            Console.WriteLine(message);
        }
    }
}
