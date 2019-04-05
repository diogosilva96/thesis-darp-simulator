using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Simulator.Logger
{
    class FileRecorder:IRecorder
    {
        private readonly StreamWriter _streamWriter;
        public FileRecorder(string path)
        {
            _streamWriter = new StreamWriter(path,true);
        }
        public void Record(string message)
        { 
            _streamWriter.WriteLine(message);
        }
    }
}
