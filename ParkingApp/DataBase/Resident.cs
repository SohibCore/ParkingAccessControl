namespace ParkingApp.DataBase
{
    public class Resident
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Apartment { get; set; } = null!;
        public string CarNumber { get; set; } = null!;
    }
}
