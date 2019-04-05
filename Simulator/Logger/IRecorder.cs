using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Logger
{
    public interface IRecorder
    {
        void Record(string message);
    }
}
