using System;
using System.IO;
using System.Media;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class NotificationSoundPlayer
    {
        private static bool _soundEnabled = true;
        private static SoundPlayer _notificationSound;
        private static SoundPlayer _alertSound;
        private static SoundPlayer _successSound;

        public static bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        static NotificationSoundPlayer()
        {
            try
            {
                // Try to load custom sounds, fall back to system sounds
                LoadSounds();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sound initialization error: {ex.Message}");
            }
        }

        private static void LoadSounds()
        {
            // Check for custom sound files in application directory
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var soundsPath = Path.Combine(appPath, "Sounds");

            if (Directory.Exists(soundsPath))
            {
                var notificationPath = Path.Combine(soundsPath, "notification.wav");
                var alertPath = Path.Combine(soundsPath, "alert.wav");
                var successPath = Path.Combine(soundsPath, "success.wav");

                if (File.Exists(notificationPath))
                    _notificationSound = new SoundPlayer(notificationPath);

                if (File.Exists(alertPath))
                    _alertSound = new SoundPlayer(alertPath);

                if (File.Exists(successPath))
                    _successSound = new SoundPlayer(successPath);
            }
        }

        public static void PlayNotification()
        {
            if (!_soundEnabled) return;

            try
            {
                if (_notificationSound != null)
                {
                    _notificationSound.Play();
                }
                else
                {
                    // Fallback to system sound
                    SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play notification error: {ex.Message}");
            }
        }

        public static void PlayAlert()
        {
            if (!_soundEnabled) return;

            try
            {
                if (_alertSound != null)
                {
                    _alertSound.Play();
                }
                else
                {
                    // Fallback to system sound
                    SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play alert error: {ex.Message}");
            }
        }

        public static void PlaySuccess()
        {
            if (!_soundEnabled) return;

            try
            {
                if (_successSound != null)
                {
                    _successSound.Play();
                }
                else
                {
                    // Fallback to system sound
                    SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play success error: {ex.Message}");
            }
        }

        public static void PlayError()
        {
            if (!_soundEnabled) return;

            try
            {
                SystemSounds.Hand.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play error sound error: {ex.Message}");
            }
        }

        public static void PlayWarning()
        {
            if (!_soundEnabled) return;

            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play warning error: {ex.Message}");
            }
        }

        public static void Mute()
        {
            _soundEnabled = false;
        }

        public static void Unmute()
        {
            _soundEnabled = true;
        }

        public static void Toggle()
        {
            _soundEnabled = !_soundEnabled;
        }
    }
}