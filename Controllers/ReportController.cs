using GalaxyBookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;

namespace GalaxyBookWeb.Controllers
{
    public class ReportController : Controller
    {
        private string connString = "Data Source=GalaxyBook.db;";

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetReport(string fromDate, string toDate, string type)
        {
            var response = new ReportResponse();

            using (var con = new SqliteConnection(connString))
            {
                con.Open();

                // 1. Get Rates
                string rateQ = "SELECT * FROM RateMaster WHERE RateType = @Type";
                using (var cmd = new SqliteCommand(rateQ, con))
                {
                    cmd.Parameters.AddWithValue("@Type", type);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            response.RateA = Convert.ToDecimal(reader["Val_A"]);
                            response.RateB = Convert.ToDecimal(reader["Val_B"]);
                            response.RateC = Convert.ToDecimal(reader["Val_C"]);
                            response.RateD = Convert.ToDecimal(reader["Val_D"]);
                            response.RateE = Convert.ToDecimal(reader["Val_E"]);
                            response.RateF = Convert.ToDecimal(reader["Val_F"]);
                            response.RateG = Convert.ToDecimal(reader["Val_G"]);
                        }
                    }
                }

                // 2. Fetch Aggregated Data
                // Note: The SQL handles nulls with IFNULL to prevent crashes
                string query = @"
                    SELECT 
                        M.GujaratiName, 
                        SUM(CAST(IFNULL(E.Col_A,0) AS INTEGER)) as A,
                        SUM(CAST(IFNULL(E.Col_B,0) AS INTEGER)) as B,
                        SUM(CAST(IFNULL(E.Col_C,0) AS INTEGER)) as C,
                        SUM(CAST(IFNULL(E.Col_D,0) AS INTEGER)) as D,
                        SUM(CAST(IFNULL(E.Col_E,0) AS INTEGER)) as E,
                        SUM(CAST(IFNULL(E.Col_F,0) AS INTEGER)) as F,
                        SUM(CAST(IFNULL(E.Col_G,0) AS INTEGER)) as G,
                        SUM(CAST(IFNULL(E.Col_Ct,0) AS DECIMAL)) as Ct,
                        
                        -- Subquery for Uppad (Sums the daily max uppad)
                        (SELECT SUM(DailyMax) FROM (
                            SELECT MAX(Uppad) as DailyMax FROM EmployeeEntries 
                            WHERE EmpName_English = M.EnglishName 
                            AND EntryDate BETWEEN @From AND @To AND EntryType = @Type
                            GROUP BY EntryDate
                        )) as TotalUppad

                    FROM EmployeeMaster M
                    LEFT JOIN EmployeeEntries E ON M.EnglishName = E.EmpName_English 
                        AND E.EntryDate BETWEEN @From AND @To 
                        AND E.EntryType = @Type
                    WHERE M.Active = 'Yes' AND M.EntryType = @Type
                    GROUP BY M.EnglishName, M.GujaratiName
                    ORDER BY M.EnglishName";

                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@From", fromDate);
                    cmd.Parameters.AddWithValue("@To", toDate);
                    cmd.Parameters.AddWithValue("@Type", type);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new ReportViewModel();
                            row.Name = reader["GujaratiName"].ToString();
                            row.A = Convert.ToInt32(reader["A"]);
                            row.B = Convert.ToInt32(reader["B"]);
                            row.C = Convert.ToInt32(reader["C"]);
                            row.D = Convert.ToInt32(reader["D"]);
                            row.E = Convert.ToInt32(reader["E"]);
                            row.F = Convert.ToInt32(reader["F"]);
                            row.G = Convert.ToInt32(reader["G"]);
                            row.Ct = Convert.ToDecimal(reader["Ct"]);

                            // Uppad might be DBNull
                            row.Uppad = reader["TotalUppad"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUppad"]) : 0;

                            // Calculate Totals
                            row.TotalKam = (row.A * response.RateA) +
                                           (row.B * response.RateB) +
                                           (row.C * response.RateC) +
                                           (row.D * response.RateD) +
                                           (row.E * response.RateE) +
                                           (row.F * response.RateF) +
                                           (row.Ct * response.RateG);

                            row.Jama = row.TotalKam - row.Uppad;
                            row.TotalCount = row.A + row.B + row.C + row.D + row.E + row.F + row.G;

                            // Only add if there is data
                            if (row.TotalCount > 0 || row.Uppad > 0 || row.TotalKam > 0)
                            {
                                response.Rows.Add(row);
                            }
                        }
                    }
                }
            }

            return Json(response);
        }
    }
}