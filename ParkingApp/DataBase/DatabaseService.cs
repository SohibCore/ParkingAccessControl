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
            logCommand.CommandText = @"CREATE TABLE IF NOT EXISTS ParkingSessions (
                       Id INTEGER PRIMARY KEY AUTOINCREMENT,
                       CarNumber TEXT NOT NULL,
                       Apartment TEXT,
                       EntryTime TEXT NOT NULL,
                       ExitTime TEXT,
                       Granted INTEGER NOT NULL );";

            logCommand.ExecuteNonQuery();
        }

        //Logs
        public void DeleteLog(int Id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM ParkingSessions WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", Id);
            command.ExecuteNonQuery();
        }
        public List<ParkingSession> GetSessions()
        {
            var sessions = new List<ParkingSession>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, CarNumber, Apartment, EntryTime, ExitTime FROM ParkingSessions ORDER BY EntryTime DESC;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                DateTime? exitTime = null;
                int exitOrdinal = reader.GetOrdinal("ExitTime");
                if (!reader.IsDBNull(exitOrdinal))
                {
                    exitTime = DateTime.Parse(reader.GetString(exitOrdinal));
                }

                sessions.Add(new ParkingSession
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CarNumber = reader.GetString(reader.GetOrdinal("CarNumber")),
                    Apartment = reader.IsDBNull(reader.GetOrdinal("Apartment")) ? null : reader.GetString(reader.GetOrdinal("Apartment")),
                    EntryTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("EntryTime"))),
                    ExitTime = exitTime
                });
            }

            return sessions;
        }
        public int StartSession(string carNumber, string? apartment, bool granted)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO ParkingSessions (CarNumber, Apartment, EntryTime, Granted)
                             VALUES (@car, @apt, @time, @granted);
                             SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@car", carNumber);
            command.Parameters.AddWithValue("@apt", (object?)apartment ?? DBNull.Value);
            command.Parameters.AddWithValue("@time", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@granted", granted ? 1 : 0);

            return Convert.ToInt32(command.ExecuteScalar());
        }
        public void EndSession(string carNumber)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                    UPDATE ParkingSessions 
                    SET ExitTime = @time
                    WHERE Id = (
                    SELECT Id FROM ParkingSessions
                    WHERE CarNumber = @car AND ExitTime IS NULL
                    ORDER BY EntryTime DESC
                    LIMIT 1 );";
            command.Parameters.AddWithValue("@car", carNumber);
            command.Parameters.AddWithValue("@time", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
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
        public void Update(int Id, string? FullName, string? Apartment, string? CarNumber)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            var setClauses = new List<string>();
            if (FullName != null)
            {
                setClauses.Add("FullName = @name");
                command.Parameters.AddWithValue("@name", FullName);
            }
            if (Apartment != null)
            {
                setClauses.Add("ApartmentNumber = @home");
                command.Parameters.AddWithValue("@home", Apartment);
            }
            if (CarNumber != null)
            {
                setClauses.Add("CarNumber = @number");
                command.Parameters.AddWithValue("@number", CarNumber);
            }

            command.CommandText = $"UPDATE Residents SET {string.Join(", ", setClauses)} WHERE Id = @Id";
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
