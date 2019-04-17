using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GraphLibrary.Objects
{
    public class
        TripStopsDataObject //Class that contains the data from the vertices (Stops) and trips (which enables to gather vertices for the directed graph)
    {
        public List<Trip> Trips { get; internal set; }
        public List<Stop> Stops { get; internal set; }

        public List<Route> Routes { get; internal set; }

        private bool _urbanOnly;

        private List<Stop> routeStops { get; set; }

        public TripStopsDataObject(bool urbanOnly)
        {
            _urbanOnly = urbanOnly;
            Trips = new List<Trip>();
            Stops = new List<Stop>();
            Routes = new List<Route>();

            Load();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }


        public List<string[]> TripsStopData { get; internal set; } //list with string arr (trip_id,stop_id)

        public void Load()
        {
            Console.WriteLine(this+"Loading all the necessary data...");
            Console.WriteLine(this + "Urban routes only:" + _urbanOnly);
            List<string[]> _stopTimesData = null;
            var stopsPath = Path.Combine(Environment.CurrentDirectory, @"files\stops.txt"); //files from google transit (GTFS file)
            var routesPath = Path.Combine(Environment.CurrentDirectory, @"files\routes.txt"); //files from google transit (GTFS file)
            var stopTimesPath =
                Path.Combine(Environment.CurrentDirectory, @"files\stop_times.txt"); // files from google transit (GTFS file)
            string tripsPath = Path.Combine(Environment.CurrentDirectory, @"files\trips.txt");
            var tripStopsPath =
                Path.Combine(Environment.CurrentDirectory, @"files\trip_stops.txt"); //file generated from stop_times.txt and stop.txt
            var routesData = GenerateListData(routesPath);
            LoadRoutes(routesData);
            var tripsData = GenerateListData(tripsPath);
            LoadTrips(tripsData);
            var stopsData = GenerateListData(stopsPath);
            LoadStops(stopsData);;
            var stopTimesDataList = GenerateListData(stopTimesPath);

                if (!File.Exists(tripStopsPath)
                ) //if the file doesn't exists, generate the dictionary required to sort the stops in ascending order then export to txt, then reads from the txt the next time the program is executed (to save computational time)
                {
                  
                    var tripsStopTupleDictionary = GenerateTripStopTuplesDictionary(stopTimesDataList);
                    ExportTripStopsToTxt(tripsStopTupleDictionary,tripStopsPath);
                }
                FileDataReader fdr = new FileDataReader();
                TripsStopData = fdr.ImportData(tripStopsPath, ',');
                LoadStopsIntoTrips();  
                LoadTripStartTime(TripsStopData);
                if (_urbanOnly)
                {
                    LoadUrbanStops();
                }
                //foreach (var route in Routes)
                //{
                //    if (route.UrbanRoute)
                //    {
                //        Console.WriteLine(route.ToString()+ " total trips:"+route.Trips.Count);
                //        foreach (var trip in route.Trips)
                //        {
                //            var trs = route.Trips.FindAll(tr => tr.Stops.SequenceEqual(trip.Stops) && tr != trip);

                //            Console.WriteLine(trip+" Equal trips:" + trs.Count);
                     

                //        }
                //        Console.WriteLine("Route ended");
                //    }
                //}
        }


        public void LoadUrbanStops()
        {
            var urbanStops = new List<Stop>();
            if (Trips.Count > 0 && Trips != null)
            {
                foreach (var trip in Trips)
                {
                    foreach (var s in trip.Stops)
                    {
                        if (!urbanStops.Contains(s))
                        {
                            urbanStops.Add(s);
                        }
                    }
                }

                Stops = urbanStops;
            }

            Console.WriteLine(this.ToString()+urbanStops.Count+" urban stops were successfully loaded.");
        }

        public void LoadTripStartTime(List<string[]> tripStopTimesData)
        {

            Console.WriteLine(this + "Loading Trips Start times...");
            var watch = Stopwatch.StartNew();
            List<int> tripIdList = new List<int>();
            foreach (var tripStopTime in tripStopTimesData)
            {
                var tripId = int.Parse(tripStopTime[0]);

                if (!tripIdList.Contains(tripId))
                {
                    tripIdList.Add(tripId);
                    var trip = Trips.Find(t => t.Id == tripId);
                    if (trip != null)
                    {
                        var tripStartTime = tripStopTime[2]; // start time in hour/minute/second
                        var startTimeInSeconds = TimeSpan.Parse(tripStartTime).TotalSeconds;
                        
                        trip.StartTime = Convert.ToInt32(startTimeInSeconds);
                    }
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + "Trip start times were successfully loaded in " + seconds +
                              " seconds.");
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
                    if (_urbanOnly)
                    {
                        if (route.UrbanRoute)
                        {
                            Routes.Add(route);
                        }
                    }
                    else
                    {
                        Routes.Add(route);
                    }
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
        public void LoadStopsIntoTrips()
        {
            
            if (TripsStopData != null)
            {
                Console.WriteLine(this + "Inserting Stops into Trips...");
                var watch = Stopwatch.StartNew();
                int prevTripId = 0;
                Trip tr = null;
                foreach (var tripStopData in TripsStopData)
                {
                        var tripId = int.Parse(tripStopData[0]);
                        if (prevTripId != tripId)
                        {
                             tr = Trips.Find(t => t.Id == tripId);
                        }
                        prevTripId = tripId;
                    if (tr != null)
                        {

                            if (Trips.Contains(tr))
                            {
                                var stopId = int.Parse(tripStopData[1]);
                                Stop stop = Stops.Find(s => s.Id == stopId);
                                tr.Stops.Add(stop);
                            }
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
                    int routeId = int.Parse(tripData[0]);
                    Trip trip = new Trip(int.Parse(tripData[2]), tripData[3]);
                 

                    var route = Routes.Find(r => r.Id == routeId);
                    if (route != null)
                    {
                        if (_urbanOnly)
                        {
                            if (route.UrbanRoute)
                            {
                                if (!route.Trips.Contains(trip))
                                {
                                    Trips.Add(trip);
                                    route.Trips.Add(trip);
                                }
                            }
                        }
                        else
                        {
                            if (!route.Trips.Contains(trip))
                            {
                                Trips.Add(trip);
                                route.Trips.Add(trip);
                            }
                        }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() +Trips.Count+" trips were successfully loaded in " + seconds +
                                  " seconds.");

        }

  
        private void ExportTripStopsToTxt(Dictionary<int, List<Tuple<int, string[]>>> tripsStopTupleDictionary,string path)
        {
            using (var file = new StreamWriter(path, true)) //writes the data to a file
            {
                file.WriteLine("trip_id,stop_id,arrival_time");
                foreach (var tripStopTuple in tripsStopTupleDictionary)
                {
                    var tuples = tripStopTuple.Value;
                    foreach (var tuple in tuples)
                    {
                        var text = tripStopTuple.Key.ToString() + ',' + tuple.Item2[0] + ',' + tuple.Item2[1];
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


        private Dictionary<int, List<Tuple<int, string[]>>> GenerateTripStopTuplesDictionary(List<string[]> stopTimesData)
        {
            var tripsIdList = GenerateTripIdList();
            var tripStopTuplesDictionary = new Dictionary<int, List<Tuple<int,string[]>>>();
            Console.WriteLine(this + "Generating the required data structure...");

            var watch = Stopwatch.StartNew();
            List<Tuple<int,string[]>> stopTupleList = new List<Tuple<int,string[]>>();

            foreach (var tripId in tripsIdList)
            {
                foreach (var dataInfo in stopTimesData)
                    if (tripId == int.Parse(dataInfo[0]))
                    {
                       
                        var stopSeq = int.Parse(dataInfo[4]);
                        var stopId = dataInfo[3];
                        var stopArrivalTime = dataInfo[1];
                        string[] stopIdArrivalTimeStrings = new string[2];
                        stopIdArrivalTimeStrings[0] = stopId;
                        stopIdArrivalTimeStrings[1] = stopArrivalTime;
                        var tuple = Tuple.Create(stopSeq, stopIdArrivalTimeStrings);
                        stopTupleList.Add(tuple);

                    }
                stopTupleList.Sort();
                tripStopTuplesDictionary.Add(tripId, stopTupleList);
                stopTupleList = new List<Tuple<int, string[]>>();
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