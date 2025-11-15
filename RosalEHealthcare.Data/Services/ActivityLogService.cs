using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class ActivityLogService
    {
        private readonly RosalEHealthcareDbContext _db;

        public ActivityLogService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        // Log a new activity
        public void LogActivity(string activityType, string description, string module, string performedBy, string performedByRole = null, string relatedEntityId = null)
        {
            try
            {
                var activity = new ActivityLog
                {
                    ActivityType = activityType,
                    Description = description,
                    Module = module,
                    PerformedBy = performedBy,
                    PerformedByRole = performedByRole,
                    RelatedEntityId = relatedEntityId,
                    PerformedAt = DateTime.Now
                };

                _db.ActivityLogs.Add(activity);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error silently - don't break the app if activity logging fails
                System.Diagnostics.Debug.WriteLine($"Activity logging failed: {ex.Message}");
            }
        }

        // Get recent activities
        public IEnumerable<ActivityLog> GetRecentActivities(int count = 20)
        {
            return _db.ActivityLogs
                .OrderByDescending(a => a.PerformedAt)
                .Take(count)
                .ToList();
        }

        // Get activities with pagination
        public IEnumerable<ActivityLog> GetActivitiesPaged(int pageNumber, int pageSize)
        {
            return _db.ActivityLogs
                .OrderByDescending(a => a.PerformedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Get total count
        public int GetTotalCount()
        {
            return _db.ActivityLogs.Count();
        }

        // Get activities by module
        public IEnumerable<ActivityLog> GetActivitiesByModule(string module, int count = 20)
        {
            return _db.ActivityLogs
                .Where(a => a.Module == module)
                .OrderByDescending(a => a.PerformedAt)
                .Take(count)
                .ToList();
        }

        // Get activities by user
        public IEnumerable<ActivityLog> GetActivitiesByUser(string performedBy, int count = 20)
        {
            return _db.ActivityLogs
                .Where(a => a.PerformedBy == performedBy)
                .OrderByDescending(a => a.PerformedAt)
                .Take(count)
                .ToList();
        }

        // Get activities by date range
        public IEnumerable<ActivityLog> GetActivitiesByDateRange(DateTime startDate, DateTime endDate)
        {
            return _db.ActivityLogs
                .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
                .OrderByDescending(a => a.PerformedAt)
                .ToList();
        }

        // Get today's activities
        public IEnumerable<ActivityLog> GetTodayActivities()
        {
            var today = DateTime.Today;
            return _db.ActivityLogs
                .Where(a => a.PerformedAt >= today)
                .OrderByDescending(a => a.PerformedAt)
                .ToList();
        }
    }
}