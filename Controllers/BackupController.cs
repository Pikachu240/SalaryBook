using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite; // For clearing pools
using System;
using System.IO;
using System.IO.Compression; // Built-in ZIP support

namespace GalaxyBookWeb.Controllers
{
    public class BackupController : Controller
    {
        private const string DbFileName = "GalaxyBook.db";
        private const string BackupFileName = "GalaxyBook.bak"; // Internal name inside zip

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. BACKUP (Download Zip)
        // ==========================================
        [HttpGet]
        public IActionResult DownloadBackup()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbFileName);

            if (!System.IO.File.Exists(dbPath))
                return Content("Database not found! Run the app setup first.");

            try
            {
                // 1. Clear locks to ensure we can read the file safely
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 2. Create a Zip in Memory
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry(BackupFileName);
                        using (var entryStream = entry.Open())
                        using (var fileStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }

                    // 3. Return the Zip File
                    memoryStream.Position = 0;
                    string fileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmm}.zip";
                    return File(memoryStream.ToArray(), "application/zip", fileName);
                }
            }
            catch (Exception ex)
            {
                return Content("Error creating backup: " + ex.Message);
            }
        }

        // ==========================================
        // 2. RESTORE (Upload Zip)
        // ==========================================
        [HttpPost]
        public IActionResult RestoreBackup(IFormFile backupFile)
        {
            if (backupFile == null || backupFile.Length == 0)
                return RedirectToAction("Index");

            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbFileName);
            string safetyPath = dbPath + ".old";

            try
            {
                // 1. Force Release Locks (CRITICAL)
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 2. Create Safety Backup of current DB
                if (System.IO.File.Exists(dbPath))
                {
                    if (System.IO.File.Exists(safetyPath)) System.IO.File.Delete(safetyPath);
                    System.IO.File.Move(dbPath, safetyPath);
                }

                // 3. Extract Uploaded Zip
                using (var stream = backupFile.OpenReadStream())
                using (var archive = new ZipArchive(stream))
                {
                    // Look for our specific file inside the zip
                    var entry = archive.GetEntry(BackupFileName);
                    if (entry != null)
                    {
                        entry.ExtractToFile(dbPath, overwrite: true);
                    }
                    else
                    {
                        // Fallback: If they uploaded a zip with a different internal structure
                        // just try to grab the first file ending in .db or .bak
                        var dbEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".db") || e.Name.EndsWith(".bak"));
                        if (dbEntry != null)
                        {
                            dbEntry.ExtractToFile(dbPath, overwrite: true);
                        }
                        else
                        {
                            throw new Exception("Invalid Backup File. Could not find database inside zip.");
                        }
                    }
                }

                ViewData["Message"] = "Database Restored Successfully!";
                ViewData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                // Attempt to Rollback
                if (System.IO.File.Exists(safetyPath))
                {
                    if (System.IO.File.Exists(dbPath)) System.IO.File.Delete(dbPath);
                    System.IO.File.Move(safetyPath, dbPath);
                }

                ViewData["Message"] = "Restore Failed: " + ex.Message;
                ViewData["MessageType"] = "danger";
            }

            return View("Index");
        }
    }
}