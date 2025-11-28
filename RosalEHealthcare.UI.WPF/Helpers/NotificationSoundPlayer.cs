using System;
using System.IO;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    /// <summary>
    /// Handles playing notification sounds
    /// </summary>
    public static class NotificationSoundPlayer
    {
        private static MediaPlayer _mediaPlayer;
        private static bool _isInitialized = false;
        private static bool _soundEnabled = true;

        // Sound file paths - UPDATE THESE FILE NAMES TO MATCH YOUR ACTUAL FILES
        private static readonly string SoundFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Resources",
            "Notification Sounds"
        );

        // Map priority to sound files - CHANGE THESE FILE NAMES TO YOUR ACTUAL FILES
        private static readonly string UrgentSound = "new-notification-024-370048.mp3";  // For urgent notifications
        private static readonly string HighSound = "new-notification-021-370045.mp3";    // For high priority
        private static readonly string NormalSound = "new-notification-010-352755.mp3";  // For normal notifications
        private static readonly string LowSound = "new-notification-022-370046.mp3";     // For low priority

        /// <summary>
        /// Initialize the sound player
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.MediaFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Media playback failed: {e.ErrorException?.Message}");
                };
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize sound player: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable notification sounds
        /// </summary>
        public static void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
        }

        /// <summary>
        /// Check if sounds are enabled
        /// </summary>
        public static bool IsSoundEnabled => _soundEnabled;

        /// <summary>
        /// Play notification sound based on priority
        /// </summary>
        /// <param name="priority">urgent, high, normal, or low</param>
        public static void PlaySound(string priority = "normal")
        {
            if (!_soundEnabled || !_isInitialized) return;

            try
            {
                string soundFile;

                switch (priority.ToLower())
                {
                    case "urgent":
                        soundFile = UrgentSound;
                        break;
                    case "high":
                        soundFile = HighSound;
                        break;
                    case "low":
                        soundFile = LowSound;
                        break;
                    case "normal":
                    default:
                        soundFile = NormalSound;
                        break;
                }

                string fullPath = Path.Combine(SoundFolder, soundFile);

                if (File.Exists(fullPath))
                {
                    _mediaPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _mediaPlayer.Volume = 0.7; // 70% volume
                    _mediaPlayer.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found: {fullPath}");
                    // Fallback to system sound
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
                // Fallback to system sound
                try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
            }
        }

        /// <summary>
        /// Play sound for a specific notification type
        /// </summary>
        public static void PlaySoundForType(string notificationType)
        {
            string priority;

            switch (notificationType)
            {
                // Urgent sounds
                case "SecurityAlert":
                case "OutOfStock":
                case "BackupFailed":
                case "RestoreFailed":
                case "AccountLocked":
                    priority = "urgent";
                    break;

                // High priority sounds
                case "AppointmentCancelled":
                case "AppointmentCompleted":
                case "LowStock":
                case "ExpiringMedicine":
                case "AppointmentReminder":
                    priority = "high";
                    break;

                // Low priority sounds
                case "PatientUpdated":
                case "UserModified":
                case "SettingsChanged":
                case "BackupSuccess":
                    priority = "low";
                    break;

                // Normal sounds for everything else
                default:
                    priority = "normal";
                    break;
            }

            PlaySound(priority);
        }

        /// <summary>
        /// Stop any currently playing sound
        /// </summary>
        public static void Stop()
        {
            try
            {
                _mediaPlayer?.Stop();
            }
            catch { }
        }

        /// <summary>
        /// Set volume (0.0 to 1.0)
        /// </summary>
        public static void SetVolume(double volume)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Max(0, Math.Min(1, volume));
            }
        }
    }
}