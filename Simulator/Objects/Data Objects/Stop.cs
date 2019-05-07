namespace Simulator.Objects.Data_Objects
{
    public class Stop
    {
        public int Id { get; }
        public string Code { get;}
        public string Name { get;}
        public string Description { get;}
        public double Latitude { get;}
        public double Longitude { get;}

        public Stop(int id, string code,string name, string description, double lat, double lon)
        {
            Id = id;
            Code = code;
            Name = name;
            Description = description;
            Latitude = lat;
            Longitude = lon;
        }

        public override string ToString()
        {
            return "Stop "+Id+" ";
        }


    }
}
