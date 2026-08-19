using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;

namespace ParkingApp
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=parking.db";
        public DatabaseService()
        {
            CreateTable();
        }
        private void CreateTable()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS Residents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    ApartmentNumber TEXT NOT NULL,
                    PlateNumber TEXT NOT NULL UNIQUE
                );";

            command.ExecuteNonQuery();
        }
        public void Add(string FullName, string Apartment, string CarNumber)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Residents (FullName, ApartmentNumber, PlateNumber)
                                 VALUES (@name, @apt, @plate);";

            command.Parameters.AddWithValue("@name", FullName);
            command.Parameters.AddWithValue("@apt", Apartment);
            command.Parameters.AddWithValue("@plate", Normalize(CarNumber));
            command.ExecuteNonQuery();
        }
        public (string FullName, string Apartment)? GetByCarNumber(string CarNumber)
        {
            var normalized = Normalize(CarNumber);

            using var connetion = new SqliteConnection(_connectionString);
            connetion.Open();

            using var command = connetion.CreateCommand();
            command.CommandText = @"SELECT FullName, ApartmentNumber FROM Residents WHERE CarNumber = @plate;";
            command.Parameters.AddWithValue("@plate", normalized);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetString(reader.GetOrdinal("FullName")), reader.GetString(reader.GetOrdinal("ApartmentNumber")));
            }
            return null;
        }
        public static string Normalize(string car)
        {
            return new string(car.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
