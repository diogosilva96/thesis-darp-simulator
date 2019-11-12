using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Logger
{
    public interface IRecorder
    {
        void Record(string message);
        void Record(List<string> messages);
    }
}
