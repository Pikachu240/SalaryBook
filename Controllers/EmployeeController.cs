using GalaxyBookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GalaxyBookWeb.Controllers
{
    public class EmployeeController : Controller
    {
        // Connection string for the database file
        private string connString = "Data Source=GalaxyBook.db;";

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. SETUP DATABASE (CRITICAL FIX)
        // Run this URL once: /Employee/SetupTables
        // ==========================================
        public IActionResult SetupTables()
        {
            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    // Create EmployeeMaster Table
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS EmployeeMaster (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EnglishName TEXT,
                            GujaratiName TEXT,
                            EntryType TEXT,
                            Active TEXT DEFAULT 'Yes'
                        );";
                    cmd.ExecuteNonQuery();

                    // Optional: Create RateMaster Table (for other forms)
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS RateMaster (
                            RateType TEXT PRIMARY KEY,
                            Val_A DECIMAL, Val_B DECIMAL, Val_C DECIMAL, 
                            Val_D DECIMAL, Val_E DECIMAL, Val_F DECIMAL, Val_G DECIMAL
                        );";
                    cmd.ExecuteNonQuery();

                    // Optional: Create EmployeeEntries Table (for Daily Entry form)
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

                    // Seed Dummy Data
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO EmployeeMaster (Id, EnglishName, GujaratiName, EntryType, Active) 
                        VALUES (1, 'Test Employee', 'ટેસ્ટ', 'પેલ', 'Yes');
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
            return Content("Database Tables Created Successfully! You can now use the application.");
        }

        // ==========================================
        // 2. GET LIST (API)
        // ==========================================
        [HttpGet]
        public JsonResult GetList()
        {
            var list = new List<EmployeeViewModel>();

            // We use a try-catch here to gracefully handle if the table doesn't exist yet
            try
            {
                using (var con = new SqliteConnection(connString))
                {
                    con.Open();
                    string query = "SELECT Id, EnglishName, GujaratiName, EntryType, Active FROM EmployeeMaster ORDER BY EnglishName";
                    using (var cmd = new SqliteCommand(query, con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new EmployeeViewModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    EnglishName = reader["EnglishName"].ToString(),
                                    GujaratiName = reader["GujaratiName"].ToString(),
                                    EntryType = reader["EntryType"].ToString(),
                                    Active = reader["Active"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (SqliteException ex)
            {
                // If table is missing, return empty list instead of crashing
                if (ex.Message.Contains("no such table"))
                {
                    return Json(new List<EmployeeViewModel>());
                }
                throw; // Rethrow other errors
            }

            return Json(list);
        }

        // ==========================================
        // 3. SAVE / UPDATE (API)
        // ==========================================
        [HttpPost]
        public JsonResult Save([FromBody] EmployeeViewModel model)
        {
            if (string.IsNullOrEmpty(model.EnglishName))
                return Json(new { success = false, message = "Name is required" });

            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                string query;

                // Check if Update (Id exists) or Insert (Id is null/0)
                if (model.Id != null && model.Id > 0)
                {
                    query = "UPDATE EmployeeMaster SET EnglishName=@Eng, GujaratiName=@Guj, EntryType=@Type, Active=@Act WHERE Id=@Id";
                }
                else
                {
                    query = "INSERT INTO EmployeeMaster (EnglishName, GujaratiName, EntryType, Active) VALUES (@Eng, @Guj, @Type, @Act)";
                }

                using (var cmd = new SqliteCommand(query, con))
                {
                    if (model.Id != null && model.Id > 0) cmd.Parameters.AddWithValue("@Id", model.Id);

                    cmd.Parameters.AddWithValue("@Eng", model.EnglishName);
                    cmd.Parameters.AddWithValue("@Guj", model.GujaratiName ?? "");
                    cmd.Parameters.AddWithValue("@Type", model.EntryType);
                    cmd.Parameters.AddWithValue("@Act", model.Active);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Saved Successfully!" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

        // ==========================================
        // 4. AUTO-TRANSLATE (API)
        // ==========================================
        [HttpGet]
        public JsonResult Translate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Json("");

            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=gu&dt=t&q={Uri.EscapeDataString(text)}";
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    string result = wc.DownloadString(url);
                    int startIndex = result.IndexOf("\"") + 1;
                    int endIndex = result.IndexOf("\"", startIndex);
                    string translated = result.Substring(startIndex, endIndex - startIndex);
                    return Json(translated);
                }
            }
            catch
            {
                return Json(text); // Fallback to original text
            }
        }
    }
}