using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class FileDataExporter
    {
        public void ExportStops(List<Stop> stops,string path)
        {
            using (var file = new StreamWriter(path, false))
            {
                file.WriteLine("stopId,Code,Name,Latitude,Longitude,Urban");
                foreach (var stop in stops)
                {
                    int urban;
                    if (stop.IsUrban)
                    {
                        urban = 1;
                    }
                    else
                    {
                        urban = 0;
                    }
                    var auxLat = Convert.ToString(stop.Latitude).Split(",");
                    var auxLon = Convert.ToString(stop.Longitude).Split(",");
                    file.WriteLine(stop.Id + "," + stop.Code + "," + stop.Name + "," + auxLat[0] + "." + auxLat[1] + "," + auxLon[0] + "." + auxLon[1] + "," + urban);
                }
            }
        }

        public void ExportTripStops(Dictionary<int, List<Tuple<int, string[]>>> tripsStopTupleDictionary, string path)
        {
            using (var file = new StreamWriter(path, false)) //writes the data to a file
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

        public void ExportTrips(List<Route> routes, string path)
        {
            using (var file = new StreamWriter(path, false))
            {
                file.WriteLine("tripId,Headsign,routeId");
                foreach (var route in routes)
                {
                    foreach (var trip in route.Trips)
                    {
                        file.WriteLine(trip.Id+","+trip.Headsign+","+route.Id);
                    }
                }
            }        
        }

        public void ExportTripStopSequence(List<Route> routes, string path)
        {
            using (var file = new StreamWriter(path, false))
            {
                file.WriteLine("id,tripId,stopId,stopNum");
                int id = 1;
                foreach (var route in routes)
                {
                    foreach (var trip in route.Trips)
                    {
                        foreach (var stop in trip.Stops)
                        {

                            file.WriteLine(id + "," + trip.Id + "," + stop.Id + "," + trip.Stops.IndexOf(stop));
                            id++;
                        }
                    }
                }
            }
        }

        public void ExportTripStartTimes(List<Route> routes, string path)
        {
            using (var file = new StreamWriter(path, false))
            {
                file.WriteLine("id,tripId,startTime");
                int id = 1;
                foreach (var route in routes)
                {
                    foreach (var trip in route.Trips)
                    {
                        foreach (var startTime in trip.StartTimes)
                        {

                            file.WriteLine(id + "," + trip.Id + "," + TimeSpan.FromSeconds(startTime).ToString());
                            id++;
                        }
                    }
                }
            }
        }
    }
}
