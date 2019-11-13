using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Simulator.Objects
{
    public class FileDataReader
    {

        public List<string[]> ImportData(string filePath,char dataSplitter,bool ignoreFirstLine)//Imports data from file and ignores the first line
        {
            List<string[]> importedData = new List<string[]>();
            string line;
            int counter = 0;
            StreamReader file = new StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                if (!ignoreFirstLine) 
                {
                    var dataArray = line.Split(dataSplitter);
                    importedData.Add(dataArray);
                    counter++;
                }
                else
                {
                    ignoreFirstLine = false;
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
