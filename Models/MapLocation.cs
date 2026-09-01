namespace MaxFood.Models
{
    public class MapLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Phone { get; set; }
        public string? WorkHours { get; set; }
        public string? IconType { get; set; } = "red"; // red, blue, green
    }
}