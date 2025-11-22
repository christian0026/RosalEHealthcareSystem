using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class PrescriptionTemplateService
    {
        private readonly RosalEHealthcareDbContext _db;

        public PrescriptionTemplateService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        public IEnumerable<PrescriptionTemplate> GetAllTemplates()
        {
            return _db.PrescriptionTemplates
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UsageCount)
                .ToList();
        }

        public IEnumerable<PrescriptionTemplate> GetTemplatesByDoctor(int doctorId)
        {
            return _db.PrescriptionTemplates
                .Where(t => t.IsActive && (t.DoctorId == doctorId || t.DoctorId == null))
                .OrderByDescending(t => t.UsageCount)
                .ToList();
        }

        public PrescriptionTemplate GetById(int id)
        {
            return _db.PrescriptionTemplates.Find(id);
        }

        public PrescriptionTemplate AddTemplate(PrescriptionTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            template.CreatedAt = DateTime.Now;
            template.UsageCount = 0;

            _db.PrescriptionTemplates.Add(template);
            _db.SaveChanges();
            return template;
        }

        public void UpdateTemplate(PrescriptionTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            var entry = _db.Entry(template);
            if (entry.State == EntityState.Detached)
                _db.PrescriptionTemplates.Attach(template);
            entry.State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void IncrementUsageCount(int templateId)
        {
            var template = GetById(templateId);
            if (template != null)
            {
                template.UsageCount++;
                template.LastUsedAt = DateTime.Now;
                UpdateTemplate(template);
            }
        }

        public void DeleteTemplate(int id)
        {
            var template = GetById(id);
            if (template != null)
            {
                template.IsActive = false;
                UpdateTemplate(template);
            }
        }
    }
}