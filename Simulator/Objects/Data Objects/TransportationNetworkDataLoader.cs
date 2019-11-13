using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects
{
    public class TransportationNetworkDataLoader //Class that contains the data needed for the simulation such as Stops, trips, routes and demands
    {
        private List<Trip> Trips { get; set; }
        public List<Stop> Stops { get; internal set; }
        public List<Route> Routes { get; internal set; }
        public DemandsDataObject DemandsDataObject { get; internal set; }

        private string _baseDirectoryPath;

        private readonly bool _urbanOnly;

        public TransportationNetworkDataLoader(bool urbanOnly)
        {
            _urbanOnly = urbanOnly;
            Trips = new List<Trip>();
            Stops = new List<Stop>();
            Routes = new List<Route>();
            DemandsDataObject = new DemandsDataObject();
            Load();
        }

        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }


        private void Load()
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine(this+"Loading all the necessary data...");
            Console.WriteLine(this + "Urban routes only:" + _urbanOnly);
            _baseDirectoryPath= Directory
                .GetParent(Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).FullName).FullName)
                .FullName;
            var stopsPath = Path.Combine(_baseDirectoryPath, @"Data Files\stops.txt"); //files from google transit (GTFS file)
            var routesPath = Path.Combine(_baseDirectoryPath, @"Data Files\routes.txt"); //files from google transit (GTFS file)
            var demandsPath = Path.Combine(_baseDirectoryPath, @"Data Files\demands.csv");
            var stopTimesPath =
                Path.Combine(_baseDirectoryPath, @"Data Files\stop_times.txt"); // files from google transit (GTFS file)
            string tripsPath = Path.Combine(_baseDirectoryPath, @"Data Files\trips.txt");
            var tripStopsPath =
                Path.Combine(_baseDirectoryPath, @"Data Files\trip_stops.txt"); //file generated from stop_times.txt and stop.txt
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
                var tripsStopData = fdr.ImportData(tripStopsPath, ',',true);
                LoadStopsIntoTrips(tripsStopData);  
                LoadTripStartTimes(tripsStopData);
                AssignUrbanStops();
                //dataExporter.ExportStops(Stops, Path.Combine(Environment.CurrentDirectory, @"stops.txt"));
                //dataExporter.ExportTrips(Routes, Path.Combine(Environment.CurrentDirectory, @"trips.txt"));
                //dataExporter.ExportTripStopSequence(Routes, Path.Combine(Environment.CurrentDirectory, @"trip_stops.txt"));
                //dataExporter.ExportTripStartTimes(Routes, Path.Combine(Environment.CurrentDirectory, @"trip_start_times.txt"));
            if (_urbanOnly)
            {
                Routes = Routes.FindAll(r => r.UrbanRoute); // only urban routes
                Trips = Trips.FindAll(t => t.Route.UrbanRoute == true); // only urban trips
                Stops = Stops.FindAll(s => s.IsUrban); //only urban stops
            }
            
            LoadStopDemands(demandsData);
            RemoveDuplicateTrips();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(this+"All the necessary data was successfully generated in "+elapsedMs*0.001+" seconds.");
            string str;
            str = _urbanOnly ? "Urban " : "";
            Console.WriteLine(this+"Total of "+str+"Routes:"+Routes.Count);
            Console.WriteLine(this+"Total of "+str+"Route Trips:"+Routes.Sum(r=>r.Trips.Count));
            Console.WriteLine(this+"Total of "+str+"Stops:"+Stops.Count);
        
        }

        private void RemoveDuplicateTrips()//Clears the duplicate trips (with the same start time and same stopsequence)
        {
            Console.WriteLine(this + "Removing duplicate trips...");
            var watch = Stopwatch.StartNew();
            List<Trip> tripsToRemove = new List<Trip>();
            //Searching for duplicates code
            foreach (var route in Routes)
            {
                foreach (var trip in route.Trips)
                {
                   var foundTrips = route.Trips.FindAll(t => t.StartTime == trip.StartTime && t.Stops.SequenceEqual(trip.Stops) && t.Id != trip.Id); //searches for trips with the same startTime and a trip which is not the current one (different id) in the foreach
                   if (foundTrips.Count>0)
                   {
                       foreach (var foundTrip in foundTrips)
                       {
                           if (!tripsToRemove.Contains(foundTrip) && !tripsToRemove.Contains(trip)){ //if the searching trip and the found trip aren't yet in the tripsToRemove List it is added
                               // the above check, enables to remove all the duplicate trips, while leaving one single trip of the duplicated trips
                               tripsToRemove.Add(foundTrip);
                           }
                       }
                   }
                }
            }
            //End of searching for duplicates
            
            foreach (var route in Routes)
            {
                foreach (var tripToRemove in tripsToRemove)
                {
                    if (route.Trips.Contains(tripToRemove))// if the trip is in route.Trips
                    {
                        route.Trips.Remove(tripToRemove); //Removes the trip
                    }
                }
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + "Duplicate trips (Total:"+tripsToRemove.Count+") were successfully removed in " + seconds +
                              " seconds.");
        }
        private void LoadStopDemands(List<string[]> demandsData)
        {
            if (demandsData != null)
            {
                Console.WriteLine(this + "Loading Stop Demands...");
                var watch = Stopwatch.StartNew();
                foreach (var demandData in demandsData)
                {
                    var route = Routes.Find(r => r.Id == int.Parse(demandData[0]));
                    var stop = Stops.Find(s => s.Id == int.Parse(demandData[1]));
                    var hour = int.Parse(demandData[2]);
                    var demand = (int) Math.Round(Convert.ToDouble(double.Parse(demandData[3])));
                    if (stop != null && route != null)
                    {
                        DemandsDataObject.AddDemand(stop.Id, route.Id, hour, demand);
                    }
                }

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() + "Stop demands were successfully loaded in " + seconds +
                                  " seconds.");
            }
        }

        private void AssignUrbanStops()
        {
            var urbanStops = new List<Stop>(); //auxiliary list to know which stop has already been assigned as urban
            if (Trips.Count > 0 && Trips != null)
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

        private void LoadTripStartTimes(List<string[]> tripStopTimesData)
        {

            Console.WriteLine(this + "Loading Trip Start times...");
            List<int> auxTripIdList = new List<int>(); // list to know which trips start time has already been added
            var watch = Stopwatch.StartNew();
            foreach (var tripStopTime in tripStopTimesData)
            {
                var tripId = int.Parse(tripStopTime[0]);

                //the checks below are used to improve the search performance
                if (!auxTripIdList.Contains(tripId)) //if it doesn't contain in the list
                {
                        var trip = Trips.Find(t => t.Id == tripId);//finds the trip
                        if (trip != null) //if a trip was found adds the start time to that trip
                        {
                            var tripStartTime = tripStopTime[2]; // start time in hour/minute/second
                            var startTimeInSeconds = TimeSpan.Parse(tripStartTime).TotalSeconds;

                            trip.StartTime = Convert.ToInt32(startTimeInSeconds);
                            auxTripIdList.Add(tripId); //adds to the auxiliary list
                        }
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + "Trip start times were successfully loaded in " + seconds +
                              " seconds.");
        }
        private void LoadRoutes(List<string[]> routesData)
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
            var r = new Route(1000,"FLEX","Flexible Route","Flexible routing",3); //Flexible route 
            Routes.Add(r);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(this.ToString() + Routes.Count + " routes were successfully loaded in " + seconds +
                              " seconds.");

        }

        private void LoadStops(List<string[]> stopsData)
        {
            Console.WriteLine(this + "Loading Stops...");
            var watch = Stopwatch.StartNew();
            foreach (var stopData in stopsData)
            {
                var stop = new Stop(int.Parse(stopData[0]), stopData[1], stopData[2], double.Parse(stopData[4]),
                    double.Parse(stopData[5]));
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
        private void LoadStopsIntoTrips(List<string[]> tripsStopData)
        {
            
            if (tripsStopData != null)
            {
                Console.WriteLine(this + "Inserting Stop sequence into each Trip...");
                var watch = Stopwatch.StartNew();
                int prevTripId = 0;
                Trip tr = null;
                List<Stop> tripStops = new List<Stop>();
                foreach (var tripStopData in tripsStopData)
                {
                        var tripId = int.Parse(tripStopData[0]);
                        if (prevTripId != tripId)
                        {


                            tr = Trips.Find(t => t.Id == tripId);
                            tr.Stops = tripStops;
                            tripStops = new List<Stop>();
                        }
                        prevTripId = tripId;
                    if (tr != null)
                        {

                            if (Trips.Contains(tr))
                            {
                                var stopId = int.Parse(tripStopData[1]);
                                Stop stop = Stops.Find(s => s.Id == stopId);
                                tripStops.Add(stop);
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

        private List<string[]> GenerateListData(string path)
        {
            List<string[]> listData = null;  
            if (!File.Exists(path))
            {
                Console.WriteLine(this+" Error! File at "+path+ " does not exist!");
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                listData = fdr.ImportData(path, ',',true);
            }

            return listData;
        }

        private void LoadTrips(List<string[]>tripsData)
        {
            Console.WriteLine(this + "Loading Trips...");
                var watch = Stopwatch.StartNew();
                foreach (var tripData in tripsData)
                {
                    int routeId = int.Parse(tripData[0]);
                    Route route = Routes.Find(r=>r.Id == routeId);
                    Trip trip = new Trip(int.Parse(tripData[2]), tripData[3]);
                    trip.Route = route;

                if (route != null)
                    {                      
                           if (!route.Trips.Contains(trip))
                            {
                                Trips.Add(trip);
                                route.Trips.Add(trip);
                            }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                var seconds = elapsedMs * 0.001;
                Console.WriteLine(this.ToString() +Trips.Count+" trips were successfully loaded in " + seconds +
                                  " seconds.");

        }


        private List<int> GenerateTripIdList()
        {
            var stopTimesPath =
                Path.Combine(_baseDirectoryPath, @"Data Files\stop_times.txt"); // files from google transit (GTFS file)
            if (!File.Exists(stopTimesPath))
            {
                Console.WriteLine(this + " Error! File stop_times.txt does not exist!");
                return null;
            }
            else
            {
                FileDataReader fdr = new FileDataReader();
                var stopTimesData = fdr.ImportData(stopTimesPath, ',',true);
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