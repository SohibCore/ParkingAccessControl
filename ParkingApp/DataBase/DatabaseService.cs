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

            using var logCommand = connection.CreateCommand();
            logCommand.CommandText = @"CREATE TABLE IF NOT EXISTS AccessLog (
                       Id INTEGER PRIMARY KEY AUTOINCREMENT,
                       CarNumber TEXT NOT NULL,
                       Timestamp TEXT NOT NULL,
                       Granted INTEGER NOT NULL,
                       Apartment TEXT,
                       EventType TEXT
                   );";

            logCommand.ExecuteNonQuery();
        }

        //Logs
        public void LogAccess(AccessLog access)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @" INSERT INTO AccessLog (CarNumber, Timestamp, Granted, Apartment, EventType)
                                     VALUES (@car, @time, @granted, @name, @type);";

            command.Parameters.AddWithValue("@car", access.CarNumber);
            command.Parameters.AddWithValue("@time", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@granted", access.Granted ? 1 : 0);
            command.Parameters.AddWithValue("@name", (object?)access.Apartment ?? DBNull.Value);
            command.Parameters.AddWithValue("@type", access.EventType ?? (object)DBNull.Value
            );

            command.ExecuteNonQuery();
        }
        public List<AccessLog> GetAllLogs()
        {
            var logs = new List<AccessLog>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, CarNumber, Timestamp, Granted, Apartment, EventType FROM AccessLog ORDER BY Timestamp DESC;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new AccessLog
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CarNumber = reader.GetString(reader.GetOrdinal("CarNumber")),
                    Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp"))),
                    Granted = reader.GetInt32(reader.GetOrdinal("Granted")) == 1,
                    Apartment = reader.IsDBNull(reader.GetOrdinal("Apartment")) ? null : reader.GetString(reader.GetOrdinal("Apartment")),
                    EventType = reader.GetString(reader.GetOrdinal("EventType"))
                });
            }

            return logs;
        }

        // Resident
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
            command.CommandText = @"SELECT FullName, ApartmentNumber, CarNumber FROM Residents WHERE CarNumber = @plate;";
            command.Parameters.AddWithValue("@plate", normalized);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Resident
                {
                    //Id = reader.GetInt32(reader.GetOrdinal("Id")),
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

        //Statistika
        public int GetTodayEntryCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT(*) FROM AccessLog 
                             WHERE EventType = 'IN' AND date(Timestamp) = date('now');";

            return Convert.ToInt32(command.ExecuteScalar());
        }
        public int GetTodayExitCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT(*) FROM AccessLog 
                             WHERE EventType = 'OUT' AND date(Timestamp) = date('now');";

            return Convert.ToInt32(command.ExecuteScalar());
        }
        public int GetTotalResidentsCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT(*) FROM Residents;";

            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
