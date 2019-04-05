using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GraphLibrary.Objects
{
    public class
        TripStopsDataObject //Class that contains the data from the vertices (Stops) and trips (which enables to gather vertices for the directed graph)
    {

        public List<Trip> Trips { get; internal set; }
        public List<Stop> Stops { get; internal set; }

        public TripStopsDataObject()
        {
            Trips = new List<Trip>();
            Stops = new List<Stop>();
            Init();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }

        private List<string[]>
            _stopsData
        {
            get;
            set;
        } // list with string arr (stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon)

        public List<string[]> TripsStopData { get; internal set; } //list with string arr (trip_id,stop_id)

        public void Init()
        {

            List<string[]> _stopTimesData = null;
            
            LoadTrips();
            _stopsData = GenerateStopsData();
            LoadStops();
            _stopTimesData = GenerateStopTimesDataList();

            if (_stopTimesData != null)
            {
                var tripsStopTuplePath =
                    Path.Combine(Environment.CurrentDirectory, @"files\trip_stops.txt"); //file generated from stop_times.txt and stop.txt
                if (!File.Exists(tripsStopTuplePath)
                ) //if the file doesn't exists, generate the dictionary required to sort the stops in ascending order then export to txt, then reads from the txt the next time the program is executed (to save computational time)
                {
                    var _tripIdList = GenerateTripIdList();
                    var _tripsStopTupleDictionary = GenerateTripStopTuplesDictionary(_tripIdList, _stopTimesData);
                    ExportTripStopsToTxt(_tripsStopTupleDictionary,tripsStopTuplePath);
                }
                FileDataReader fdr = new FileDataReader();
                TripsStopData = fdr.ImportData(tripsStopTuplePath, ',');
                LoadTripStops();

            }
            else
            {
                Console.WriteLine(this+
                                  " Error! Failed to generate the data structure because the required files do not exist!");
            }
        }
        public Stop FindStop(int sId)
        {
            Stop stop = null;
            bool stopFound = false;
            foreach (var _stop in Stops)
            {
                if (_stop.Id == sId)
                {
                    stop = _stop;
                    stopFound = true;
                }

                if (stopFound) break;
            }

            return stop;
        }

        public void LoadStops()
        {
            Console.WriteLine(this + "Loading Stops...");
            var watch = Stopwatch.StartNew();
            foreach (var stopData in _stopsData)
            {
                var auxLat = stopData[4].Split(".");
                var auxLon = stopData[5].Split(".");
                var stop = new Stop(int.Parse(stopData[0]), stopData[1], stopData[2],
                    stopData[3], double.Parse(auxLat[0] + "," + auxLat[1]),
                    double.Parse(auxLon[0] + "," + auxLon[1]));
                if (!Stops.Contains(stop))
                {
                    Stops.Add(stop);
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + Stops.Count+" stops were successfully loaded in " + seconds +
                              " seconds.");
        }
        public void LoadTripStops()
        {
            
            if (TripsStopData != null)
            {
                var count = 0;
                Console.WriteLine(this + "Inserting Stops into trips...");
                var watch = Stopwatch.StartNew();
                int tripId = 0;
                foreach (var trip in Trips)
                {
                    foreach (var tripStopData in TripsStopData)
                    {
                        if (trip.Id == int.Parse(tripStopData[0]))
                        {
                            var stopId = int.Parse(tripStopData[1]);
                            Stop stop = FindStop(stopId);
                            
                            trip.Stops.Add(stop);
                        }

                        count++;
                    }

                    if (count > 200)
                    {
                      
                        break;
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this + "Stops were successfully inserted into trips in " + seconds +
                                  " seconds.");
            }
        }
        public List<string[]> GenerateStopsData()
        {

            var stops_Path = Path.Combine(Environment.CurrentDirectory, @"files\stops.txt"); //files from google transit (GTFS file)
            if (!File.Exists(stops_Path))
            {
                Console.WriteLine(this+ "Error! File stops.txt does not exist!");
                return null;
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                _stopsData = fdr.ImportData(stops_Path, ',');
            }

            return _stopsData;
        }

    public void LoadTrips()
        {
           
            string tripsPath = Path.Combine(Environment.CurrentDirectory, @"files\trips.txt");
            if (!File.Exists(tripsPath))
            {
                Console.WriteLine(this + "Error! File trips.txt does not exist!");
            }
            else
            {
                Console.WriteLine(this + "Loading Trips...");
                var watch = Stopwatch.StartNew();
                var fdr = new FileDataReader();
                var _tripsData = fdr.ImportData(tripsPath, ',');
                foreach (var tripData in _tripsData)
                {
                    Trip trip = new Trip(int.Parse(tripData[2]), tripData[3]);
                    if (!Trips.Contains(trip))
                    {
                        Trips.Add(trip);
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() +Trips.Count+" trips were successfully loaded in " + seconds +
                                  " seconds.");
            }
        }
        private void ExportTripStopsToTxt(Dictionary<int, List<Tuple<int, int>>> _tripsStopTupleDictionary,string path)
        {
            using (var file = new StreamWriter(path, true)) //writes the data to a file
            {
                file.WriteLine("trip_id,stop_id");
                foreach (var Trip_StopTuple in _tripsStopTupleDictionary)
                {
                    var tuples = Trip_StopTuple.Value;
                    foreach (var tuple in tuples)
                    {
                        var text = Trip_StopTuple.Key.ToString() + ',' + tuple.Item2;
                        file.WriteLine(
                            text); // writes the trip_id,stop_id with the stop order already sorted in ascent order 
                    }
                }
            }
        }

        public List<string[]> GenerateStopTimesDataList()
        {
            var stop_times_Path =
                Path.Combine(Environment.CurrentDirectory, @"files\stop_times.txt"); // files from google transit (GTFS file)
            if (!File.Exists(stop_times_Path))
            {
                Console.WriteLine(this + " Error! File stop_times.txt does not exist!");
                return null;
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                var _stopTimesData = fdr.ImportData(stop_times_Path, ',');
                var stopTimesData = new List<string[]>();
                foreach (var singleData in _stopTimesData)
                {
                    stopTimesData.Add(singleData);
                }

                return stopTimesData;
            }
        }

        private List<int> GenerateTripIdList()
        {
            var stop_times_Path =
                Path.Combine(Environment.CurrentDirectory, @"files\stop_times.txt"); // files from google transit (GTFS file)
            if (!File.Exists(stop_times_Path))
            {
                Console.WriteLine(this + " Error! File stop_times.txt does not exist!");
                return null;
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                var _stopTimesData = fdr.ImportData(stop_times_Path, ',');
                var tripsIdList = new List<int>();
                foreach (var singleData in _stopTimesData)
                    if (!tripsIdList.Contains(int.Parse(singleData[0])))
                        tripsIdList.Add(
                            int.Parse(singleData[0])); //adds the trip_id if it doesn't exist yet in trips_id_list

                return tripsIdList;
            }

           
        }


        private Dictionary<int, List<Tuple<int, int>>> GenerateTripStopTuplesDictionary(List<int> _tripsList,List<string[]> _stopTimesData)
        {
            var tripStopTuplesDictionary = new Dictionary<int, List<Tuple<int, int>>>();
            var stopTupleList = new List<Tuple<int, int>>();
            Console.WriteLine(this + "Generating the required data structure...");
            var watch = Stopwatch.StartNew();

            foreach (var id in _tripsList)
            {
                foreach (var dataInfo in _stopTimesData)
                    if (id == int.Parse(dataInfo[0]))
                    {
                        var stopSeq = int.Parse(dataInfo[4]);
                        var stopId = int.Parse(dataInfo[3]);
                        stopTupleList.Add(Tuple.Create(stopSeq,
                            stopId));
                    }

                stopTupleList
                    .Sort(); //sorts the list in order to get the connecting vertices, sorts by stop_seq (ascending order)
                tripStopTuplesDictionary.Add(id, stopTupleList);
                stopTupleList = new List<Tuple<int, int>>();
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this + "The data structure has been successfully generated in " + seconds +
                              " seconds.");
            return tripStopTuplesDictionary;
        }
    }
}