using System;
using System.IO;
using System.Media;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class NotificationSoundPlayer
    {
        private static bool _soundEnabled = true;
        private static bool _initialized = false;
        private static SoundPlayer _notificationSound;
        private static SoundPlayer _alertSound;
        private static SoundPlayer _successSound;

        public static bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        /// <summary>
        /// Initialize the sound player. Call this once at application startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                LoadSounds();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sound initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets whether sound is enabled
        /// </summary>
        /// <param name="enabled">True to enable sounds, false to disable</param>
        public static void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
        }

        private static void LoadSounds()
        {
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
            if (!_initialized) Initialize();

            try
            {
                if (_notificationSound != null)
                    _notificationSound.Play();
                else
                    SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play notification error: {ex.Message}");
            }
        }

        public static void PlayAlert()
        {
            if (!_soundEnabled) return;
            if (!_initialized) Initialize();

            try
            {
                if (_alertSound != null)
                    _alertSound.Play();
                else
                    SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play alert error: {ex.Message}");
            }
        }

        public static void PlaySuccess()
        {
            if (!_soundEnabled) return;
            if (!_initialized) Initialize();

            try
            {
                if (_successSound != null)
                    _successSound.Play();
                else
                    SystemSounds.Asterisk.Play();
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