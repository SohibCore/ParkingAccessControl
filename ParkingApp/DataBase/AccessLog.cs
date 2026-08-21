namespace ParkingApp.DataBase
{
    public class AccessLog
    {
        public int Id { get; set; }
        public string CarNumber { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public bool Granted { get; set; }
        public string? Apartment { get; set; }
        public string EventType { get; set; } = null!;
    }
}
