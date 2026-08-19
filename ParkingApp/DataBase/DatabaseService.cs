using Microsoft.Data.Sqlite;

namespace ParkingApp.DataBase
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
                    CarNumber TEXT NOT NULL UNIQUE
                );";

            command.ExecuteNonQuery();
        }
        public void Add(Resident resident)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Residents (FullName, ApartmentNumber, CarNumber)
                                 VALUES (@name, @apt, @plate);";

            command.Parameters.AddWithValue("@name", resident.FullName);
            command.Parameters.AddWithValue("@apt", resident.Apartment);
            command.Parameters.AddWithValue("@plate", Normalize(resident.CarNumber));
            command.ExecuteNonQuery();
        }
        public Resident? GetByCarNumber(string CarNumber)
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
                return new Resident
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Apartment = reader.GetString(reader.GetOrdinal("ApartmentNumber")),
                    CarNumber = reader.GetString(reader.GetOrdinal("CarNumber"))
                };
            }
            return null;
        }
        public static string Normalize(string car)
        {
            return new string(car.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }
        public List<Resident> GetAll()
        {
            var result = new List<Resident>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT Id, FullName, ApartmentNumber, CarNumber FROM Residents;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var Residents = new Resident
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Apartment = reader.GetString(reader.GetOrdinal("ApartmentNumber")),
                    CarNumber = reader.GetString(reader.GetOrdinal("CarNumber"))
                };
                result.Add(Residents);
            }
            return result;
        }
        public void Delete(int Id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Residents WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", Id);
            command.ExecuteNonQuery();
        }
    }
}
