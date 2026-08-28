public class ParkingSession
{
    public int Id { get; set; }
    public string CarNumber { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }

    public string Duration
    {
        get
        {
            if (ExitTime == null) return "Hali chiqmagan";
            var span = ExitTime.Value - EntryTime;
            return $"{(int)span.TotalHours} soat {span.Minutes} daqiqa";
        }
    }
}