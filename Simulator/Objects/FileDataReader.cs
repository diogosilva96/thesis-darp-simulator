using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GraphLibrary.Objects
{
    public class FileDataReader
    {
        private readonly string _classDescriptor;
        public FileDataReader()
        {
            _classDescriptor = "[" + GetType().Name + "]";
        }
        public List<string[]> ImportData(string filePath,char dataSplitter)//Imports data from file and ignores the first line
        {
            List<string[]> importedData = new List<string[]>();
            string line;
            int counter = 0;
            StreamReader file = new StreamReader(filePath);
            bool firstLine = true;
            while ((line = file.ReadLine()) != null)
            {
                if (!firstLine) 
                {
                    var dataArray = line.Split(dataSplitter);
                    importedData.Add(dataArray);
                    counter++;
                }
                else
                {
                    firstLine = false;
                }
            }
            file.Close();
            Debug.WriteLine(_classDescriptor+counter+ " lines were imported with array size of "+ importedData[0].Length+".");
            return importedData;
        }

    }
}
