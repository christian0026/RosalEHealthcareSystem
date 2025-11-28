using RosalEHealthcare.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RosalEHealthcare.Data.Services
{
    public class ExcelExportService
    {
        private readonly string _exportFolder;

        public ExcelExportService()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _exportFolder = Path.Combine(documentsPath, "RosalHealthcare", "Exports");

            if (!Directory.Exists(_exportFolder))
                Directory.CreateDirectory(_exportFolder);
        }

        /// <summary>
        /// Export patients to CSV (Excel-compatible)
        /// </summary>
        public string ExportPatientsToExcel(List<Patient> patients)
        {
            var fileName = $"Patients_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportFolder, fileName);

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("Patient ID,Full Name,Age,Gender,Contact,Email,Address,Blood Type,Primary Diagnosis,Allergies,Last Visit,Status");

            // Data rows
            foreach (var patient in patients)
            {
                sb.AppendLine($"\"{patient.PatientId}\"," +
                             $"\"{patient.FullName}\"," +
                             $"{patient.Age}," +
                             $"\"{patient.Gender}\"," +
                             $"\"{patient.Contact}\"," +
                             $"\"{patient.Email ?? ""}\"," +
                             $"\"{patient.Address?.Replace("\"", "\"\"")}\"," +
                             $"\"{patient.BloodType ?? ""}\"," +
                             $"\"{patient.PrimaryDiagnosis ?? ""}\"," +
                             $"\"{patient.Allergies ?? ""}\"," +
                             $"\"{patient.LastVisit?.ToString("yyyy-MM-dd") ?? ""}\"," +
                             $"\"{patient.Status}\"");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        /// <summary>
        /// Open file in default application
        /// </summary>
        public void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(filePath);
            }
        }
    }
}