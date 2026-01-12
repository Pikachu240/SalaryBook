using GalaxyBookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Data;

namespace GalaxyBookWeb.Controllers
{
    public class SettingsController : Controller
    {
        private string connString = "Data Source=GalaxyBook.db;";

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetSettings()
        {
            var model = new SettingsViewModel();

            // Set defaults in case DB is empty
            model.SaveKey = "Alt+S";
            model.GenerateKey = "Alt+G";
            model.UpdateKey = "Alt+U";
            model.PrintKey = "Ctrl+P";
            model.NewKey = "Alt+N";
            model.CloseKey = "Escape";

            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                string query = "SELECT ActionName, KeyCode FROM ShortcutSettings";
                using (var cmd = new SqliteCommand(query, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string act = reader["ActionName"].ToString();
                            string key = reader["KeyCode"].ToString();

                            if (act == "Save") model.SaveKey = key;
                            if (act == "Generate") model.GenerateKey = key;
                            if (act == "Update") model.UpdateKey = key;
                            if (act == "Print") model.PrintKey = key;
                            if (act == "New") model.NewKey = key;
                            if (act == "Close") model.CloseKey = key;
                        }
                    }
                }
            }
            return Json(model);
        }

        [HttpPost]
        public JsonResult SaveSettings([FromBody] SettingsViewModel model)
        {
            using (var con = new SqliteConnection(connString))
            {
                con.Open();
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        SaveOne(con, "Save", model.SaveKey, trans);
                        SaveOne(con, "Generate", model.GenerateKey, trans);
                        SaveOne(con, "Update", model.UpdateKey, trans);
                        SaveOne(con, "Print", model.PrintKey, trans);
                        SaveOne(con, "New", model.NewKey, trans);
                        SaveOne(con, "Close", model.CloseKey, trans);

                        trans.Commit();
                        return Json(new { success = true, message = "Settings Saved Successfully!" });
                    }
                    catch (System.Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { success = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }

        private void SaveOne(SqliteConnection con, string act, string key, SqliteTransaction trans)
        {
            string query = "INSERT OR REPLACE INTO ShortcutSettings (ActionName, KeyCode) VALUES (@Act, @Key)";
            using (var cmd = new SqliteCommand(query, con, trans))
            {
                cmd.Parameters.AddWithValue("@Act", act);
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.ExecuteNonQuery();
            }
        }
    }
}