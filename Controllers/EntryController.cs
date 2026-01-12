using GalaxyBookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite; // Use ONLY this namespace
using System;
using System.Collections.Generic;

namespace GalaxyBookWeb.Controllers
{
    public class EntryController : Controller
    {
        // FIX 1: Removed "Version=3;" (Not supported in Microsoft.Data.Sqlite)
        private string connString = "Data Source=GalaxyBook.db;";

        public IActionResult Index()
        {
            return View();
        }

        // 1. API to Fill the Dropdown
        [HttpGet]
        public JsonResult GetEmployees(string type)
        {
            var list = new List<EmployeeSelect>();
            // FIX 2: Use SqliteConnection (lowercase 'lite')
            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                string query = "SELECT EnglishName, GujaratiName FROM EmployeeMaster WHERE EntryType = @Type AND Active = 'Yes' ORDER BY EnglishName";
                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Type", type);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EmployeeSelect
                            {
                                EnglishName = reader["EnglishName"].ToString(),
                                GujaratiName = reader["GujaratiName"].ToString()
                            });
                        }
                    }
                }
            }
            return Json(list);
        }

        // 2. Setup Database (Run this once via browser: /Entry/SetupDatabase)
        public IActionResult SetupDatabase()
        {
            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    // Create EmployeeMaster
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS EmployeeMaster (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EnglishName TEXT,
                            GujaratiName TEXT,
                            EntryType TEXT,
                            Active TEXT DEFAULT 'Yes'
                        );";
                    cmd.ExecuteNonQuery();

                    // Create RateMaster
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS RateMaster (
                            RateType TEXT PRIMARY KEY,
                            Val_A DECIMAL, Val_B DECIMAL, Val_C DECIMAL, 
                            Val_D DECIMAL, Val_E DECIMAL, Val_F DECIMAL, Val_G DECIMAL
                        );";
                    cmd.ExecuteNonQuery();

                    // Create EmployeeEntries
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS EmployeeEntries (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EmpName_English TEXT,
                            EmpName_Gujarati TEXT,
                            Uppad INTEGER,
                            EntryDate TEXT,
                            EntryType TEXT,
                            Col_A TEXT, Col_B TEXT, Col_C TEXT, Col_D TEXT, 
                            Col_E TEXT, Col_F TEXT, Col_G TEXT, Col_Ct TEXT
                        );";
                    cmd.ExecuteNonQuery();

                    // Seed Data
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO EmployeeMaster (EnglishName, GujaratiName, EntryType, Active) 
                        VALUES ('John Doe', 'જોન ડો', 'પેલ', 'Yes');
                        INSERT OR IGNORE INTO EmployeeMaster (EnglishName, GujaratiName, EntryType, Active) 
                        VALUES ('Jane Smith', 'જેન સ્મિથ', 'મથાળા', 'Yes');
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
            return Content("Database Tables Created Successfully!");
        }

        // 3. API to Load Existing Data
        [HttpPost]
        public JsonResult LoadData([FromBody] DailyEntryViewModel model)
        {
            var response = new DailyEntryViewModel();
            response.Rows = new List<GridRow>();

            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                string query = @"SELECT Id, Col_A, Col_B, Col_C, Col_D, Col_E, Col_F, Col_G, Col_Ct, Uppad 
                                 FROM EmployeeEntries 
                                 WHERE EmpName_English = @Name AND EntryDate = @Date AND EntryType = @Type";

                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Name", model.EnglishName);
                    cmd.Parameters.AddWithValue("@Date", model.Date);
                    cmd.Parameters.AddWithValue("@Type", model.EntryType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        // FIX 3: Replaced DataTable logic with standard Reader loop
                        while (reader.Read())
                        {
                            // Capture Uppad from the first row (it repeats for all rows in this design)
                            response.Uppad = Convert.ToInt32(reader["Uppad"]);

                            response.Rows.Add(new GridRow
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                A = reader["Col_A"].ToString(),
                                B = reader["Col_B"].ToString(),
                                C = reader["Col_C"].ToString(),
                                D = reader["Col_D"].ToString(),
                                E = reader["Col_E"].ToString(),
                                F = reader["Col_F"].ToString(),
                                G = reader["Col_G"].ToString(),
                                Ct = reader["Col_Ct"].ToString()
                            });
                        }
                    }
                }
            }
            return Json(response);
        }

        // 4. API to Save Data
        [HttpPost]
        public JsonResult SaveData([FromBody] DailyEntryViewModel model)
        {
            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in model.Rows)
                        {
                            // If ID exists, UPDATE
                            if (row.Id != null && row.Id > 0)
                            {
                                string uQ = @"UPDATE EmployeeEntries SET 
                                              EmpName_English=@Eng, EmpName_Gujarati=@Guj, Uppad=@Uppad, 
                                              Col_A=@A, Col_B=@B, Col_C=@C, Col_D=@D, Col_E=@E, Col_F=@F, Col_G=@G, Col_Ct=@Ct
                                              WHERE Id=@Id";
                                using (var cmd = new SqliteCommand(uQ, con, trans))
                                {
                                    cmd.Parameters.AddWithValue("@Id", row.Id);
                                    cmd.Parameters.AddWithValue("@Eng", model.EnglishName);
                                    cmd.Parameters.AddWithValue("@Guj", model.GujaratiName);
                                    cmd.Parameters.AddWithValue("@Uppad", model.Uppad);
                                    cmd.Parameters.AddWithValue("@A", row.A ?? "0");
                                    cmd.Parameters.AddWithValue("@B", row.B ?? "0");
                                    cmd.Parameters.AddWithValue("@C", row.C ?? "0");
                                    cmd.Parameters.AddWithValue("@D", row.D ?? "0");
                                    cmd.Parameters.AddWithValue("@E", row.E ?? "0");
                                    cmd.Parameters.AddWithValue("@F", row.F ?? "0");
                                    cmd.Parameters.AddWithValue("@G", row.G ?? "0");
                                    cmd.Parameters.AddWithValue("@Ct", row.Ct ?? "0");
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            // Else INSERT
                            else
                            {
                                string iQ = @"INSERT INTO EmployeeEntries 
                                              (EmpName_English, EmpName_Gujarati, Uppad, EntryDate, EntryType, Col_A, Col_B, Col_C, Col_D, Col_E, Col_F, Col_G, Col_Ct) 
                                              VALUES (@Eng, @Guj, @Uppad, @Date, @Type, @A, @B, @C, @D, @E, @F, @G, @Ct)";
                                using (var cmd = new SqliteCommand(iQ, con, trans))
                                {
                                    cmd.Parameters.AddWithValue("@Eng", model.EnglishName);
                                    cmd.Parameters.AddWithValue("@Guj", model.GujaratiName);
                                    cmd.Parameters.AddWithValue("@Uppad", model.Uppad);
                                    cmd.Parameters.AddWithValue("@Date", model.Date);
                                    cmd.Parameters.AddWithValue("@Type", model.EntryType);
                                    cmd.Parameters.AddWithValue("@A", row.A ?? "0");
                                    cmd.Parameters.AddWithValue("@B", row.B ?? "0");
                                    cmd.Parameters.AddWithValue("@C", row.C ?? "0");
                                    cmd.Parameters.AddWithValue("@D", row.D ?? "0");
                                    cmd.Parameters.AddWithValue("@E", row.E ?? "0");
                                    cmd.Parameters.AddWithValue("@F", row.F ?? "0");
                                    cmd.Parameters.AddWithValue("@G", row.G ?? "0");
                                    cmd.Parameters.AddWithValue("@Ct", row.Ct ?? "0");
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        trans.Commit();
                        return Json(new { success = true, message = "Saved Successfully!" });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }
    }
}