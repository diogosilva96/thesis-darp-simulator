using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Simulator.Objects
{
    public class FileDataReader
    {

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
            Debug.WriteLine(this.ToString()+counter+ "lines were imported with a string array size of "+ importedData[0].Length+".");
            return importedData;
        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] "; ;
        }
    }
}
