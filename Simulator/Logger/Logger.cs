using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Simulator.Logger
{
    public class Logger // based on strategy design pattern
    {
        private readonly IRecorder _recorder;

        public Logger(IRecorder recorder)
        {
            _recorder = recorder;
        }

        public void Log(string message)
        {
            _recorder.Record(message);
        }

        public void Log(List<string> messages)
        {
            _recorder.Record(messages);
        }
    }
}
