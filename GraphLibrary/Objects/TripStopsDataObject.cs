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

        public List<Route> Routes { get; internal set; }

        public TripStopsDataObject()
        {
            Trips = new List<Trip>();
            Stops = new List<Stop>();
            Routes = new List<Route>();
            Init();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }


        public List<string[]> TripsStopData { get; internal set; } //list with string arr (trip_id,stop_id)

        public void Init()
        {

            List<string[]> _stopTimesData = null;
            var stopsPath = Path.Combine(Environment.CurrentDirectory, @"files\stops.txt"); //files from google transit (GTFS file)
            var routesPath = Path.Combine(Environment.CurrentDirectory, @"files\routes.txt"); //files from google transit (GTFS file)
            var stopTimesPath =
                Path.Combine(Environment.CurrentDirectory, @"files\stop_times.txt"); // files from google transit (GTFS file)
            string tripsPath = Path.Combine(Environment.CurrentDirectory, @"files\trips.txt");
            var routesData = GenerateListData(routesPath);
            LoadRoutes(routesData);
            var tripsData = GenerateListData(tripsPath);
            LoadTrips(tripsData);
            var stopsData = GenerateListData(stopsPath);
            LoadStops(stopsData);
            var stopTimesDataList = GenerateListData(stopTimesPath);
            _stopTimesData = new List<string[]>();
            foreach (var singleData in stopTimesDataList)
            {
                _stopTimesData.Add(singleData);
            }

            if (_stopTimesData != null)
            {
                var tripsStopTuplePath =
                    Path.Combine(Environment.CurrentDirectory, @"files\trip_stops.txt"); //file generated from stop_times.txt and stop.txt
                if (!File.Exists(tripsStopTuplePath)
                ) //if the file doesn't exists, generate the dictionary required to sort the stops in ascending order then export to txt, then reads from the txt the next time the program is executed (to save computational time)
                {
                    var tripIdList = GenerateTripIdList();
                    var tripsStopTupleDictionary = GenerateTripStopTuplesDictionary(tripIdList, _stopTimesData);
                    ExportTripStopsToTxt(tripsStopTupleDictionary,tripsStopTuplePath);
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
            Stop Stop = null;
            foreach (var stop in Stops)
            {
                if (stop.Id == sId)
                {
                    Stop = stop;
                    break;
                }

            }

            return Stop;
        }

        public void LoadRoutes(List<string[]> routesData)
        {
            Console.WriteLine(this + "Loading Routes...");
            var watch = Stopwatch.StartNew();
            foreach (var routeData in routesData)
            {
                Route route = new Route(int.Parse(routeData[0]),routeData[2],routeData[3],routeData[4], int.Parse(routeData[1]));
                if (!Routes.Contains(route))
                {
                    Routes.Add(route);
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + Routes.Count + " routes were successfully loaded in " + seconds +
                              " seconds.");

        }

        public void LoadStops(List<string[]> stopsData)
        {
            Console.WriteLine(this + "Loading Stops...");
            var watch = Stopwatch.StartNew();
            foreach (var stopData in stopsData)
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
                    foreach (var tripStopData in TripsStopData)
                    {
                        var tr = Trips.Find(t => t.Id == int.Parse(tripStopData[0]));
                        if (tr != null)
                        {
                                var stopId = int.Parse(tripStopData[1]);
                                Stop stop = FindStop(stopId);

                                tr.Stops.Add(stop);
                            
                        }


                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this + "Stops were successfully inserted into trips in " + seconds +
                                  " seconds.");
            }
        }

        public List<string[]> GenerateListData(string path)
        {
            List<string[]> listData = null;  
            if (!File.Exists(path))
            {
                Console.WriteLine(this+" Error! File at "+path+ " does not exist!");
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                listData = fdr.ImportData(path, ',');
            }

            return listData;
        }

    public void LoadTrips(List<string[]>tripsData)
        {
           
      
            Console.WriteLine(this + "Loading Trips...");
                var watch = Stopwatch.StartNew();

                foreach (var tripData in tripsData)
                {
                    int RouteId = int.Parse(tripData[0]);
                    Trip trip = new Trip(int.Parse(tripData[2]), tripData[3]);
                    if (!Trips.Contains(trip))
                    {
                        Trips.Add(trip);
                    }

                    var route = Routes.Find(r => r.Id == RouteId);
                    if (route != null)
                    {
                        if (!route.Trips.Contains(trip)) // if the route is an urban route and isn't in trips adds it
                        {
                            route.Trips.Add(trip); // adds the trip to the route
                        }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() +Trips.Count+" trips were successfully loaded in " + seconds +
                                  " seconds.");

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


        private List<int> GenerateTripIdList()
        {
            var stopTimesPath =
                Path.Combine(Environment.CurrentDirectory, @"files\stop_times.txt"); // files from google transit (GTFS file)
            if (!File.Exists(stopTimesPath))
            {
                Console.WriteLine(this + " Error! File stop_times.txt does not exist!");
                return null;
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                var stopTimesData = fdr.ImportData(stopTimesPath, ',');
                var tripsIdList = new List<int>();
                foreach (var singleData in stopTimesData)
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