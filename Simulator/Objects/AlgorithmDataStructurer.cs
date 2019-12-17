using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Logger;

namespace Simulator.Objects
{
    public class AlgorithmDataStructurer
    {

        public void StructureFile(string path)
        {
            FileDataReader fdr = new FileDataReader();
            var unstructuredData = fdr.ImportData(path, ',',false);
            var algorithmCounter = 0;
            var datasetId = 1;
            var algorithmsList = new List<string>();
            var dataSetList = new List<string[]>();
            var dataList = new List<string[]>();
            string[] currentDataset = null;
            foreach (var data in unstructuredData)
            {
                
                if (algorithmCounter == 0)
                {
                   
                    string[] dataSet = new string[data.Length+1];
                    dataSet[0] = datasetId++.ToString();
                    for (int i = 0; i < data.Length; i++)
                    {
                        dataSet[i + 1] = data[i];
                    }
                    Console.WriteLine("Dataset "+dataSet[0]+"- Customer Number: " + dataSet[1] + "; Vehicle number: " + dataSet[2] + " ; MaxRideTime: " + dataSet[3] + " ; MaxAllowedUpperBound: " + dataSet[4]);
                    dataSetList.Add(dataSet);
                    currentDataset = dataSet;
                }
                else
                {
                    var algorithmStats = data;
                    Console.WriteLine(algorithmStats[0]);
                    if (!algorithmsList.Contains(algorithmStats[0]))
                    {
                        algorithmsList.Add(algorithmStats[0]);
                    }

                        string[] algData = new string[algorithmStats.Length+1];
                        algData[0] = algorithmsList.FindIndex(a => a == algorithmStats[0]).ToString();
                        for (int i = 1; i < algorithmStats.Length; i++)
                        {
                            algData[i] = algorithmStats[i];
                        }

                        algData[algorithmStats.Length] = dataSetList.FindIndex(d => d == currentDataset).ToString();
                        dataList.Add(algData);
                        Console.WriteLine("Alg: " + algData[0] + " ; AllowdropNodes:" + algData[1] + " ; Feasible: " +
                                          algData[2] + " ; SearchTime: " + algData[3] + " ; ComputationTime:" +
                                          algData[4] + " ; ObjValue: " + algData[5] + " ; MaximumAllowedDeliveryDelay: " +
                                          algData[6] + " ; TotalServedRequests: " + algData[7] + " ; TotalDistance: " +
                                          algData[8] + " ; TotalTime: " + algData[9] + " ; VehiclesUsed: " +
                                          algData[10]+ " ; Dataset: "+algData[11]);
                }
                
                algorithmCounter++;
                if (algorithmCounter == 8)
                {
                    algorithmCounter = 0;
                }

             

            }
            IRecorder algorithmStructRecorder = new FileRecorder(Path.Combine(@Path.Combine(Environment.CurrentDirectory, @"Logger", @"AlgorithmsStruct.csv")));
            IRecorder datasetRecorder = new FileRecorder(Path.Combine(@Path.Combine(Environment.CurrentDirectory, @"Logger", @"Dataset.csv")));
            for (int j = 0; j < 2; j++)
            {

                IRecorder recorder;
                List<string[]> dataToBeRecorded = null;
                if (j == 0)
                {
                    dataToBeRecorded = dataList;
                    recorder = algorithmStructRecorder;
                    string firstLine = "AlgId, AllowdropNodes,Feasible,SearchTime,ComputationTime,ObjValue,MaximumAllowedDeliveryDelay,TotalServedRequests,TotalDistance,TotalTime, VehiclesUsed, DatasetId";
                    recorder.Record(firstLine);
                }
                else
                {
                    dataToBeRecorded = dataSetList;
                    recorder = datasetRecorder;
                    string firstLine = "DatasetId,CustomerNumber, Vehicle number,MaxRideTime,MaxAllowedUpperBound";
                    recorder.Record(firstLine);
                }

                foreach (var data in dataToBeRecorded)
                {
                    var toBeRecorded = "";
                    var splitter = ',';
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i != data.Length - 1)
                        {
                            toBeRecorded += data[i] + splitter;
                        }
                        else
                        {
                            toBeRecorded += data[i];
                        }

                    }

                    recorder.Record(toBeRecorded);
                }
            }


        }
    }
}
