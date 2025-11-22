using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        // Medical fields
        public string PrimaryDiagnosis { get; set; }
        public string SecondaryDiagnosis { get; set; }
        public string Allergies { get; set; }
        public string BloodType { get; set; }
        public DateTime? LastVisit { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // ADDED: Properties needed for DoctorPrescriptionManagement
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }

        // ADDED: Computed property for Age
        public int Age
        {
            get
            {
                if (!BirthDate.HasValue) return 0;
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        // Add this property if not exists
        public bool IsArchived { get; set; } = false;

        // Add this computed property to Patient.cs
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "?";
                var words = FullName.Split(' ');
                if (words.Length >= 2)
                    return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();
                return FullName.Length >= 2 ? FullName.Substring(0, 2).ToUpper() : FullName.ToUpper();
            }
        }
    }
}
