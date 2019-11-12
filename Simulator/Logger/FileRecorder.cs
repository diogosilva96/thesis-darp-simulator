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
            _streamWriter = new StreamWriter(path,false);
       
        }
        public void Record(string message)
        {
            
            _streamWriter.WriteLine(message);
            _streamWriter.Flush();
        }

        public void Record(List<string> messages)
        {
            foreach (var message in messages)
            {
                Record(message);
            }
        }

        public FileRecorder(string path, string header)
        {
            _streamWriter = new StreamWriter(path, false);
            _streamWriter.WriteLine(header);
        }
    }
}
