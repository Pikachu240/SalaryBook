using GalaxyBookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SQLite;
using System;
using Microsoft.Data.Sqlite; // <--- CHANGED THIS

namespace GalaxyBookWeb.Controllers
{
    public class RateController : Controller
    {
        // This connection string assumes the DB is in the root folder
        private string connString = "Data Source=GalaxyBook.db;Version=3;";

        // GET: /Rate/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Rate/GetRates?type=...
        [HttpGet]
        public JsonResult GetRates(string type)
        {
            var model = new RateViewModel { RateType = type };

            using (var con = new SQLiteConnection(connString))
            {
                con.Open();
                string query = "SELECT * FROM RateMaster WHERE RateType = @Type";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Type", type);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Val_A = Convert.ToDecimal(reader["Val_A"]);
                            model.Val_B = Convert.ToDecimal(reader["Val_B"]);
                            model.Val_C = Convert.ToDecimal(reader["Val_C"]);
                            model.Val_D = Convert.ToDecimal(reader["Val_D"]);
                            model.Val_E = Convert.ToDecimal(reader["Val_E"]);
                            model.Val_F = Convert.ToDecimal(reader["Val_F"]);
                            model.Val_G = Convert.ToDecimal(reader["Val_G"]);
                        }
                        else
                        {
                            // Default to 0 if not found
                            model.Val_A = 0; model.Val_B = 0; model.Val_C = 0;
                            model.Val_D = 0; model.Val_E = 0; model.Val_F = 0; model.Val_G = 0;
                        }
                    }
                }
            }
            return Json(model);
        }

        // POST: /Rate/Save
        [HttpPost]
        public JsonResult Save([FromBody] RateViewModel model)
        {
            using (var con = new SQLiteConnection(connString))
            {
                con.Open();
                string query = @"INSERT OR REPLACE INTO RateMaster 
                                 (RateType, Val_A, Val_B, Val_C, Val_D, Val_E, Val_F, Val_G) 
                                 VALUES (@Type, @A, @B, @C, @D, @E, @F, @G)";

                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Type", model.RateType);
                    cmd.Parameters.AddWithValue("@A", model.Val_A);
                    cmd.Parameters.AddWithValue("@B", model.Val_B);
                    cmd.Parameters.AddWithValue("@C", model.Val_C);
                    cmd.Parameters.AddWithValue("@D", model.Val_D);
                    cmd.Parameters.AddWithValue("@E", model.Val_E);
                    cmd.Parameters.AddWithValue("@F", model.Val_F);
                    cmd.Parameters.AddWithValue("@G", model.Val_G);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Rates Saved Successfully!" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }
    }
}