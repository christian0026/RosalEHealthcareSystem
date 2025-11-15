using RosalEHealthcare.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public class PdfExportService
    {
        private readonly string _exportFolder;

        public PdfExportService()
        {
            // Create exports folder in Documents
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _exportFolder = Path.Combine(documentsPath, "RosalHealthcare", "PatientRecords");

            if (!Directory.Exists(_exportFolder))
                Directory.CreateDirectory(_exportFolder);
        }

        public string ExportPatientRecord(Patient patient, List<MedicalHistory> medicalHistory, List<Prescription> prescriptions)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            var fileName = $"PatientRecord_{patient.PatientId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(_exportFolder, fileName);

            var html = GeneratePatientRecordHtml(patient, medicalHistory, prescriptions);
            File.WriteAllText(filePath, html, Encoding.UTF8);

            // Convert to PDF using browser print (user will need to print to PDF)
            return filePath;
        }

        private string GeneratePatientRecordHtml(Patient patient, List<MedicalHistory> medicalHistory, List<Prescription> prescriptions)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Patient Medical Record</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; color: #333; }
        .header { text-align: center; border-bottom: 3px solid #4CAF50; padding-bottom: 20px; margin-bottom: 30px; }
        .header h1 { color: #2E7D32; margin: 0; }
        .header p { color: #666; margin: 5px 0; }
        .section { margin-bottom: 30px; page-break-inside: avoid; }
        .section-title { background: #4CAF50; color: white; padding: 10px 15px; border-radius: 5px; font-size: 18px; font-weight: bold; margin-bottom: 15px; }
        .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }
        .info-item { margin-bottom: 10px; }
        .info-label { font-weight: bold; color: #666; font-size: 12px; }
        .info-value { font-size: 14px; margin-top: 3px; }
        .history-item { border-left: 3px solid #4CAF50; padding-left: 15px; margin-bottom: 20px; }
        .history-date { color: #4CAF50; font-weight: bold; margin-bottom: 5px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th { background: #E8F5E9; padding: 10px; text-align: left; font-size: 13px; }
        td { padding: 10px; border-bottom: 1px solid #E0E0E0; font-size: 13px; }
        .footer { margin-top: 50px; padding-top: 20px; border-top: 2px solid #E0E0E0; text-align: center; color: #999; font-size: 12px; }
        @media print {
            body { margin: 20px; }
            .no-print { display: none; }
        }
    </style>
</head>
<body>");

            // Header
            sb.AppendLine($@"
    <div class='header'>
        <h1>ROSAL HEALTHCARE</h1>
        <p>Patient Medical Record</p>
        <p>Generated on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
    </div>");

            // Patient Information
            sb.AppendLine(@"
    <div class='section'>
        <div class='section-title'>Patient Information</div>
        <div class='info-grid'>");

            sb.AppendLine($@"
            <div class='info-item'>
                <div class='info-label'>Patient ID</div>
                <div class='info-value'>{patient.PatientId}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Full Name</div>
                <div class='info-value'>{patient.FullName}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Date of Birth</div>
                <div class='info-value'>{patient.BirthDate?.ToString("MMMM dd, yyyy") ?? "N/A"}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Age / Gender</div>
                <div class='info-value'>{patient.Age} years / {patient.Gender}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Contact Number</div>
                <div class='info-value'>{patient.Contact}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Email</div>
                <div class='info-value'>{patient.Email}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Blood Type</div>
                <div class='info-value'>{patient.BloodType ?? "N/A"}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Address</div>
                <div class='info-value'>{patient.Address}</div>
            </div>");

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");

            // Medical Information
            sb.AppendLine(@"
    <div class='section'>
        <div class='section-title'>Medical Information</div>
        <div class='info-grid'>");

            sb.AppendLine($@"
            <div class='info-item'>
                <div class='info-label'>Primary Diagnosis</div>
                <div class='info-value'>{patient.PrimaryDiagnosis ?? "None"}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Secondary Diagnosis</div>
                <div class='info-value'>{patient.SecondaryDiagnosis ?? "None"}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Allergies</div>
                <div class='info-value'>{patient.Allergies ?? "None reported"}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Last Visit</div>
                <div class='info-value'>{patient.LastVisit?.ToString("MMMM dd, yyyy") ?? "N/A"}</div>
            </div>");

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");

            // Medical History
            if (medicalHistory != null && medicalHistory.Any())
            {
                sb.AppendLine(@"
    <div class='section'>
        <div class='section-title'>Medical History</div>");

                foreach (var history in medicalHistory.OrderByDescending(h => h.VisitDate).Take(10))
                {
                    sb.AppendLine($@"
        <div class='history-item'>
            <div class='history-date'>{history.VisitDate:MMMM dd, yyyy} - {history.VisitType}</div>
            <div><strong>Diagnosis:</strong> {history.Diagnosis}</div>
            <div><strong>Treatment:</strong> {history.Treatment}</div>
            <div><strong>Doctor:</strong> {history.DoctorName}</div>
            {(string.IsNullOrEmpty(history.BloodPressure) ? "" : $"<div><strong>BP:</strong> {history.BloodPressure} | <strong>Temp:</strong> {history.Temperature}°C | <strong>HR:</strong> {history.HeartRate} BPM</div>")}
        </div>");
                }

                sb.AppendLine("    </div>");
            }

            // Prescriptions
            if (prescriptions != null && prescriptions.Any())
            {
                sb.AppendLine(@"
    <div class='section'>
        <div class='section-title'>Recent Prescriptions</div>
        <table>");

                sb.AppendLine(@"
            <tr>
                <th>Date</th>
                <th>Prescription ID</th>
                <th>Diagnosis</th>
                <th>Medicines</th>
            </tr>");

                foreach (var prescription in prescriptions.OrderByDescending(p => p.CreatedAt).Take(5))
                {
                    var medicines = prescription.Medicines != null ?
                        string.Join(", ", prescription.Medicines.Select(m => m.MedicineName)) :
                        "N/A";

                    sb.AppendLine($@"
            <tr>
                <td>{prescription.CreatedAt:MMM dd, yyyy}</td>
                <td>{prescription.PrescriptionId}</td>
                <td>{prescription.PrimaryDiagnosis}</td>
                <td>{medicines}</td>
            </tr>");
                }

                sb.AppendLine(@"
        </table>
    </div>");
            }

            // Footer
            sb.AppendLine($@"
    <div class='footer'>
        <p><strong>ROSAL HEALTHCARE MANAGEMENT SYSTEM</strong></p>
        <p>This is a confidential medical document. Unauthorized disclosure is prohibited.</p>
        <p>Printed by: {SessionManager.GetUserFullName()} on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
    </div>

    <div class='no-print' style='text-align: center; margin-top: 30px;'>
        <button onclick='window.print()' style='background: #4CAF50; color: white; border: none; padding: 15px 30px; font-size: 16px; border-radius: 5px; cursor: pointer;'>
            Print to PDF
        </button>
    </div>

</body>
</html>");

            return sb.ToString();
        }
    }
}