using System;
using System.Data;
using Microsoft.Data.Sqlite; // NuGet: Microsoft.Data.Sqlite
using System.IO;

namespace GalaxyBookWeb
{
    public static class SQLiteHelper
    {
        // 1. Connection String (Note: Removed 'Version=3;' as it causes errors in the new lib)
        private static string dbFile = "GalaxyBook.db";
        private static string connectionString = $"Data Source={dbFile};";

        // 2. Initialize Database (Create Tables)
        public static void InitializeDatabase()
        {
            // We don't strictly need CreateFile; the library creates it automatically on Open()

            using (var con = new SqliteConnection(connectionString))
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    // 1. EmployeeMaster
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS EmployeeMaster (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, -- Changed EmployeeID to Id for consistency
                            EnglishName TEXT,
                            GujaratiName TEXT,
                            EntryType TEXT,
                            Active TEXT
                        );";
                    cmd.ExecuteNonQuery();

                    // 2. RateMaster
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS RateMaster (
                            RateType TEXT PRIMARY KEY, 
                            Val_A DECIMAL, Val_B DECIMAL, Val_C DECIMAL, Val_D DECIMAL,
                            Val_E DECIMAL, Val_F DECIMAL, Val_G DECIMAL
                        );";
                    cmd.ExecuteNonQuery();

                    // 3. ShortcutSettings
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ShortcutSettings (
                            ActionName TEXT PRIMARY KEY,
                            KeyCode TEXT
                        );";
                    cmd.ExecuteNonQuery();

                    // 4. EmployeeEntries
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS EmployeeEntries (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EmpName_English TEXT,
                            EmpName_Gujarati TEXT,
                            Uppad INTEGER,
                            EntryDate TEXT, -- Stored as YYYY-MM-DD
                            EntryType TEXT,
                            Col_A TEXT, Col_B TEXT, Col_C TEXT, Col_D TEXT,
                            Col_E TEXT, Col_F TEXT, Col_G TEXT, Col_Ct TEXT
                        );";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Execute Insert/Update/Delete
        public static int ExecuteNonQuery(string query, SqliteParameter[] parameters = null)
        {
            using (var con = new SqliteConnection(connectionString))
            {
                con.Open();
                using (var cmd = new SqliteCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Execute Select (Returns DataTable)
        // NOTE: SqliteDataAdapter does not exist in Core, so we load manually.
        public static DataTable GetDataTable(string query, SqliteParameter[] parameters = null)
        {
            using (var con = new SqliteConnection(connectionString))
            {
                con.Open();
                using (var cmd = new SqliteCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);

                    using (var reader = cmd.ExecuteReader())
                    {
                        var dt = new DataTable();
                        dt.Load(reader); // This automatically fills the DataTable from the reader
                        return dt;
                    }
                }
            }
        }

        // 5. Execute Scalar (Get Single Value)
        public static object ExecuteScalar(string query, SqliteParameter[] parameters = null)
        {
            using (var con = new SqliteConnection(connectionString))
            {
                con.Open();
                using (var cmd = new SqliteCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}