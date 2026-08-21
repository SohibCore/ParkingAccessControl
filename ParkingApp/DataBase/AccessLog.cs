namespace ParkingApp.DataBase
{
    public class AccessLog
    {
        public int Id { get; set; }
        public string CarNumber { get; set; } = null!;
        public string Timestamp { get; set; } = null!;
        public int Granted { get; set; }
        public string Apartment { get; set; } = null!;
        public string EventType { get; set; } = null!;
    }
}
