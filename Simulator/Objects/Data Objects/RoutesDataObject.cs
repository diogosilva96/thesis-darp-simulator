using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Simulator.Objects.Data_Objects
{
    public class
        RoutesDataObject //Class that contains the data for the Stops, trips and routes
    {
        private List<Trip> _trips { get; set; }
        public List<Stop> Stops { get; internal set; }
        public List<Route> Routes { get; internal set; }

        private bool _urbanOnly;

        public RoutesDataObject(bool urbanOnly)
        {
            _urbanOnly = urbanOnly;
            _trips = new List<Trip>();
            Stops = new List<Stop>();
            Routes = new List<Route>();

            Load();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }



        private Dictionary<int, int> _tripStartTimeDictionary;

        public void Load()
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine(this+"Loading all the necessary data...");
            Console.WriteLine(this + "Urban routes only:" + _urbanOnly);
            var stopsPath = Path.Combine(Environment.CurrentDirectory, @"files\stops.txt"); //files from google transit (GTFS file)
            var routesPath = Path.Combine(Environment.CurrentDirectory, @"files\routes.txt"); //files from google transit (GTFS file)
            var demandsPath = Path.Combine(Environment.CurrentDirectory, @"files\demands.csv");
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
            LoadStops(stopsData);
            var demandsData = GenerateListData(demandsPath);
            var stopTimesDataList = GenerateListData(stopTimesPath);
            FileDataExporter dataExporter = new FileDataExporter();

            if (!File.Exists(tripStopsPath)
                ) //if the file doesn't exists, generate the dictionary required to sort the stops in ascending order then export to txt, then reads from the txt the next time the program is executed (to save computational time)
                {
                  
                    var tripsStopTupleDictionary = GenerateTripStopTuplesDictionary(stopTimesDataList);
                    dataExporter.ExportTripStops(tripsStopTupleDictionary,tripStopsPath);
                }
                FileDataReader fdr = new FileDataReader();
                var tripsStopData = fdr.ImportData(tripStopsPath, ',');
                LoadStopsIntoTrips(tripsStopData);  
                LoadTripStartTimeDictionary(tripsStopData);
                AssignUrbanStops();
                RemoveRedundantTrips();
                //dataExporter.ExportStops(Stops, Path.Combine(Environment.CurrentDirectory, @"stops.txt"));
                //dataExporter.ExportTrips(Routes, Path.Combine(Environment.CurrentDirectory, @"trips.txt"));
                //dataExporter.ExportTripStopSequence(Routes, Path.Combine(Environment.CurrentDirectory, @"trip_stops.txt"));
                //dataExporter.ExportTripStartTimes(Routes, Path.Combine(Environment.CurrentDirectory, @"trip_start_times.txt"));
            if (_urbanOnly)
            {
                Routes = Routes.FindAll(r => r.UrbanRoute);
                List<Trip> Trips = new List<Trip>();
                foreach (var route in Routes)
                {
                    foreach (var trip in route.Trips)
                    {
                        Trips.Add(trip);
                    }
                }
                Stops = Stops.FindAll(s => s.IsUrban);
            }

            LoadStopDemands(demandsData);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(this+"All the necessary data was successfully generated in "+elapsedMs*0.001+" seconds.");
            string str;
            str = _urbanOnly ? "Urban " : "";
            Console.WriteLine(this+"Total of "+str+"Routes:"+Routes.Count);
            Console.WriteLine(this+"Total of "+str+"Route Trips:"+Routes.Sum(r=>r.Trips.Count));
            Console.WriteLine(this+"Total of "+str+"Stops:"+Stops.Count);
        
        }

        public void LoadStopDemands(List<string[]> demandsData)
        {
            if (demandsData != null)
            {
                Console.WriteLine(this + "Loading Stop Demands...");
                var watch = Stopwatch.StartNew();
                foreach (var demandData in demandsData)
                {
                    var route = Routes.Find(r=>r.Id == int.Parse(demandData[0]));
                    var stop = Stops.Find(s=>s.Id==int.Parse(demandData[1]));
                    var hourOfDay = int.Parse(demandData[2]);
                    var avgDemand = (int)Math.Round(double.Parse(demandData[3]));
                    if (route != null && stop != null)
                    {
                        Tuple<Route, int> routeHourTuple = Tuple.Create(route,hourOfDay);
                        if (!stop.DemandDictionary.ContainsKey(routeHourTuple))
                        {
                            stop.DemandDictionary.Add(routeHourTuple, avgDemand);
                        }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() + "Trip start times dictionary was successfully loaded in " + seconds +
                                  " seconds.");
            }
        }
        public void RemoveRedundantTrips() //simplifies trips removing all unnecessary trips from routes and adding the starttimes for each trip
        {

            List<Trip> routeTrips = new List<Trip>();
            int id = 1;
            int totalTrips = Routes.Sum(r => r.Trips.Count);
            Console.WriteLine(this+"Removing redundant trips...");
            var watch = Stopwatch.StartNew();
            foreach (var route in Routes)
            {
                    Trip newTrip = null;
                    List<Trip> addedTripsList = new List<Trip>(); // auxiliary list to later add to the route.trips
                    foreach (var trip in route.Trips)
                    {
                        var trips = route.Trips.FindAll(tr => tr.Stops.SequenceEqual(trip.Stops));
                        var findTrip = routeTrips.Find(t => t.Stops.SequenceEqual(trip.Stops));
                        if (findTrip == null)
                        {
                            newTrip = new Trip(id, trip.Headsign) {Stops = trip.Stops};

                            
                            foreach (var tr in trips)
                            {
                                if (_tripStartTimeDictionary.ContainsKey(tr.Id))
                                {
                                    newTrip.AddStartTime(_tripStartTimeDictionary[tr.Id]);
                                }
                            }
                            routeTrips.Add(newTrip);
                            addedTripsList.Add(newTrip);
                            id++;
                        }
                    }

                    foreach (var trip in addedTripsList)
                    {
                        route.Trips.Add(trip);
                    }
                    route.LoadRouteServices();
            }

            foreach (var route in Routes)
            {
                List<Trip> auxRemoveList = new List<Trip>(); //auxiliary list to later remove from the route.trips
                foreach (var trip in route.Trips)
                {
                    if (!routeTrips.Contains(trip))
                    {
                        auxRemoveList.Add(trip); 
                    }
                }
                foreach (var trip in auxRemoveList)
                {
                    route.Trips.Remove(trip);// removes the unnecessary trips
                }

            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(this+"A total of "+(totalTrips - (id-1))+" redundant trips were successfully removed in "+elapsedMs*0.001+" seconds.");
         
            _trips = routeTrips; 
        }

        public void AssignUrbanStops()
        {
            var urbanStops = new List<Stop>();
            if (_trips.Count > 0 && _trips != null)
            {
                var urbanRoutes =Routes.FindAll(r => r.UrbanRoute);
                foreach (var route in urbanRoutes)
                {
                    foreach (var trip in route.Trips)
                    {
                        foreach (var s in trip.Stops)
                        {
                            if (!urbanStops.Contains(s))
                            {
                                s.IsUrban = true;
                                urbanStops.Add(s);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(this.ToString()+urbanStops.Count+" urban stops were successfully assigned.");
        }

        public void LoadTripStartTimeDictionary(List<string[]> tripStopTimesData)
        {

            Console.WriteLine(this + "Loading Trip Start times dictionary...");
            var watch = Stopwatch.StartNew();
            Dictionary<int,int> tripIdStartTimeDict = new Dictionary<int,int>();
            foreach (var tripStopTime in tripStopTimesData)
            {
                var tripId = int.Parse(tripStopTime[0]);

                if (!tripIdStartTimeDict.ContainsKey(tripId))
                {
                        var tripStartTime = tripStopTime[2]; // start time in hour/minute/second
                        var startTimeInSeconds = TimeSpan.Parse(tripStartTime).TotalSeconds;
                        tripIdStartTimeDict.Add(tripId, Convert.ToInt32(startTimeInSeconds));
                    
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + "Trip start times dictionary was successfully loaded in " + seconds +
                              " seconds.");
            _tripStartTimeDictionary = tripIdStartTimeDict;
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
                var stop = new Stop(int.Parse(stopData[0]), stopData[1], stopData[2], double.Parse(auxLat[0] + "," + auxLat[1]),
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
        public void LoadStopsIntoTrips(List<string[]> tripsStopData)
        {
            
            if (tripsStopData != null)
            {
                Console.WriteLine(this + "Inserting Stop sequence into each Trip...");
                var watch = Stopwatch.StartNew();
                int prevTripId = 0;
                Trip tr = null;
                foreach (var tripStopData in tripsStopData)
                {
                        var tripId = int.Parse(tripStopData[0]);
                        if (prevTripId != tripId)
                        {


                            tr = _trips.Find(t => t.Id == tripId); 
                        }
                        prevTripId = tripId;
                    if (tr != null)
                        {

                            if (_trips.Contains(tr))
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
                    Route route = Routes.Find(r=>r.Id == routeId);

                if (route != null)
                    {                      
                           if (!route.Trips.Contains(trip))
                            {
                                _trips.Add(trip);
                                route.Trips.Add(trip);
                            }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() +_trips.Count+" trips were successfully loaded in " + seconds +
                                  " seconds.");

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