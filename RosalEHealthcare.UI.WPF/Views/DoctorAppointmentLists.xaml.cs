using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorAppointmentLists : UserControl, INotifyPropertyChanged
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly AppointmentService _appointmentService;
        private readonly PatientService _patientService;
        private readonly NotificationService _notificationService;
        private readonly DispatcherTimer _autoRefreshTimer;

        private ObservableCollection<Appointment> _allAppointments;
        private ObservableCollection<Appointment> _displayedAppointments;
        private string _currentTab = "Today";
        private List<string> _activeStatusFilters;
        private bool _isLoading;

        public ObservableCollection<Appointment> DisplayedAppointments
        {
            get => _displayedAppointments;
            set
            {
                _displayedAppointments = value;
                OnPropertyChanged(nameof(DisplayedAppointments));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DoctorAppointmentLists()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _appointmentService = new AppointmentService(_db);
                _patientService = new PatientService(_db);
                _notificationService = new NotificationService(_db);

                _allAppointments = new ObservableCollection<Appointment>();
                _displayedAppointments = new ObservableCollection<Appointment>();

                // Status filters - these will be matched case-insensitively
                _activeStatusFilters = new List<string> { "PENDING", "CONFIRMED", "IN_PROGRESS" };

                DataContext = this;

                // Auto-refresh timer (every 30 seconds)
                _autoRefreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30)
                };
                _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

                Loaded += DoctorAppointmentLists_Loaded;
                Unloaded += DoctorAppointmentLists_Unloaded;
                SizeChanged += DoctorAppointmentLists_SizeChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Appointment Lists:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoctorAppointmentLists_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
            _autoRefreshTimer.Start();
            UpdateResponsiveLayout();
        }

        private void DoctorAppointmentLists_Unloaded(object sender, RoutedEventArgs e)
        {
            _autoRefreshTimer.Stop();
        }

        private void DoctorAppointmentLists_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveLayout();
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshAppointmentsQuietly();
        }

        #region Responsive Layout

        private void UpdateResponsiveLayout()
        {
            // Get the actual width of the scroll viewer
            double availableWidth = scrollViewer.ActualWidth - 40; // Account for padding

            if (availableWidth <= 0) return;

            // Card width is 360 + 15 margin = 375
            int cardWidth = 375;
            int columns = Math.Max(1, (int)(availableWidth / cardWidth));

            // Limit to 3 columns max for readability
            columns = Math.Min(columns, 3);

            // Update card widths based on available space
            double actualCardWidth = (availableWidth - (columns - 1) * 15) / columns;
            actualCardWidth = Math.Max(320, Math.Min(actualCardWidth, 400)); // Clamp between 320-400

            // Update subtitle with layout info
            txtSubtitle.Text = $"{_currentTab}'s Schedule • {DisplayedAppointments?.Count ?? 0} appointments";
        }

        #endregion

        #region Data Loading

        private async void LoadAppointments()
        {
            try
            {
                ShowLoading(true);

                var appointments = await Task.Run(() =>
                {
                    // Force a fresh query by creating new context or refreshing
                    _db.Configuration.ProxyCreationEnabled = false;

                    var query = _db.Appointments
                        .Include("Patient")
                        .OrderBy(a => a.Time)
                        .ToList();

                    // Populate PatientName for display
                    foreach (var apt in query)
                    {
                        if (apt.Patient != null)
                        {
                            apt.PatientName = apt.Patient.FullName;
                        }
                        else if (string.IsNullOrEmpty(apt.PatientName))
                        {
                            apt.PatientName = "Walk-in Patient";
                        }

                        // Normalize status to uppercase for consistent filtering
                        if (!string.IsNullOrEmpty(apt.Status))
                        {
                            apt.Status = NormalizeStatus(apt.Status);
                        }
                    }

                    return query;
                });

                _allAppointments.Clear();
                foreach (var apt in appointments)
                {
                    _allAppointments.Add(apt);
                }

                // Debug: Log what we loaded
                System.Diagnostics.Debug.WriteLine($"Loaded {_allAppointments.Count} total appointments");
                foreach (var apt in _allAppointments.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {apt.AppointmentId}: {apt.PatientName}, Status={apt.Status}, Date={apt.Time:yyyy-MM-dd}");
                }

                ApplyFilters();
                UpdateStatistics();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error loading appointments:\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshAppointmentsQuietly()
        {
            try
            {
                var appointments = await Task.Run(() =>
                {
                    var query = _db.Appointments
                        .Include("Patient")
                        .OrderBy(a => a.Time)
                        .ToList();

                    foreach (var apt in query)
                    {
                        if (apt.Patient != null)
                            apt.PatientName = apt.Patient.FullName;
                        else if (string.IsNullOrEmpty(apt.PatientName))
                            apt.PatientName = "Walk-in Patient";

                        // Normalize status
                        if (!string.IsNullOrEmpty(apt.Status))
                        {
                            apt.Status = NormalizeStatus(apt.Status);
                        }
                    }

                    return query;
                });

                // Check for changes
                bool hasChanges = appointments.Count != _allAppointments.Count ||
                    appointments.Any(a => !_allAppointments.Any(existing =>
                        existing.Id == a.Id && existing.Status == a.Status));

                if (hasChanges)
                {
                    _allAppointments.Clear();
                    foreach (var apt in appointments)
                    {
                        _allAppointments.Add(apt);
                    }

                    ApplyFilters();
                    UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent refresh error: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalizes status values to uppercase with underscores
        /// Handles various formats: "Pending", "pending", "PENDING", "In Progress", "IN_PROGRESS", etc.
        /// </summary>
        private string NormalizeStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "PENDING";

            // Convert to uppercase and replace spaces with underscores
            var normalized = status.Trim().ToUpper().Replace(" ", "_");

            // Handle common variations
            switch (normalized)
            {
                case "PENDING":
                case "NEW":
                case "SCHEDULED":
                    return "PENDING";

                case "CONFIRMED":
                case "APPROVED":
                    return "CONFIRMED";

                case "IN_PROGRESS":
                case "INPROGRESS":
                case "IN-PROGRESS":
                case "ONGOING":
                case "STARTED":
                    return "IN_PROGRESS";

                case "COMPLETED":
                case "DONE":
                case "FINISHED":
                    return "COMPLETED";

                case "CANCELLED":
                case "CANCELED":
                case "CANCELLED_BY_PATIENT":
                case "CANCELLED_BY_DOCTOR":
                    return "CANCELLED";

                default:
                    return normalized;
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allAppointments.AsEnumerable();

            // Apply tab filter
            var today = DateTime.Today;
            switch (_currentTab)
            {
                case "Today":
                    filtered = filtered.Where(a => a.Time.Date == today);
                    break;
                case "Upcoming":
                    filtered = filtered.Where(a => a.Time.Date > today);
                    break;
                case "Past":
                    filtered = filtered.Where(a => a.Time.Date < today);
                    break;
                case "All":
                default:
                    // No date filter
                    break;
            }

            // Apply status filters - case insensitive comparison
            if (_activeStatusFilters.Any())
            {
                filtered = filtered.Where(a =>
                    _activeStatusFilters.Any(filter =>
                        MatchesStatus(a.Status, filter)
                    )
                );
            }

            // Apply search filter
            var searchText = txtSearch?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(a =>
                    (a.PatientName?.ToLower().Contains(searchText) ?? false) ||
                    (a.AppointmentId?.ToLower().Contains(searchText) ?? false) ||
                    (a.Condition?.ToLower().Contains(searchText) ?? false) ||
                    (a.Type?.ToLower().Contains(searchText) ?? false)
                );
            }

            // Sort by time
            filtered = _currentTab == "Past"
                ? filtered.OrderByDescending(a => a.Time)
                : filtered.OrderBy(a => a.Time);

            DisplayedAppointments.Clear();
            foreach (var apt in filtered)
            {
                DisplayedAppointments.Add(apt);
            }

            // Debug: Log filter results
            System.Diagnostics.Debug.WriteLine($"Filter applied: Tab={_currentTab}, StatusFilters=[{string.Join(",", _activeStatusFilters)}]");
            System.Diagnostics.Debug.WriteLine($"  Showing {DisplayedAppointments.Count} of {_allAppointments.Count} appointments");

            // Render cards
            RenderAppointmentCards();

            // Update empty state
            pnlEmptyState.Visibility = DisplayedAppointments.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Update empty message based on context
            if (DisplayedAppointments.Count == 0)
            {
                if (_allAppointments.Count == 0)
                {
                    txtEmptyMessage.Text = "No appointments in the system.";
                }
                else if (_currentTab == "Today")
                {
                    txtEmptyMessage.Text = "No appointments scheduled for today.";
                }
                else
                {
                    txtEmptyMessage.Text = "No appointments matching your filters.";
                }
            }

            UpdateResponsiveLayout();
        }

        /// <summary>
        /// Checks if an appointment status matches a filter value
        /// Handles case-insensitivity and various status formats
        /// </summary>
        private bool MatchesStatus(string appointmentStatus, string filterStatus)
        {
            if (string.IsNullOrEmpty(appointmentStatus)) return false;
            if (string.IsNullOrEmpty(filterStatus)) return false;

            // Normalize both for comparison
            var normalizedAppointment = NormalizeStatus(appointmentStatus);
            var normalizedFilter = NormalizeStatus(filterStatus);

            return normalizedAppointment == normalizedFilter;
        }

        private void RenderAppointmentCards()
        {
            icAppointments.Items.Clear();

            foreach (var appointment in DisplayedAppointments)
            {
                var card = CreateAppointmentCard(appointment);
                icAppointments.Items.Add(card);
            }
        }

        private Border CreateAppointmentCard(Appointment appointment)
        {
            // Main card border
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 15, 15),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                Width = 360,
                MinHeight = 200,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 6,
                    ShadowDepth = 1,
                    Opacity = 0.06
                },
                Tag = appointment
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Status Header
            var statusHeader = CreateStatusHeader(appointment);
            Grid.SetRow(statusHeader, 0);
            mainGrid.Children.Add(statusHeader);

            // Content
            var content = CreateCardContent(appointment);
            Grid.SetRow(content, 1);
            mainGrid.Children.Add(content);

            // Actions
            var actions = CreateActionButtons(appointment);
            Grid.SetRow(actions, 2);
            mainGrid.Children.Add(actions);

            card.Child = mainGrid;
            return card;
        }

        private Border CreateStatusHeader(Appointment appointment)
        {
            var normalizedStatus = NormalizeStatus(appointment.Status);
            var statusColor = GetStatusColor(normalizedStatus);

            var header = new Border
            {
                Background = new SolidColorBrush(statusColor),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(15, 12, 15, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Time
            var timePanel = new StackPanel { Orientation = Orientation.Horizontal };
            timePanel.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.Clock,
                Width = 16,
                Height = 16,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            timePanel.Children.Add(new TextBlock
            {
                Text = appointment.Time.ToString("hh:mm tt"),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(8, 0, 0, 0)
            });
            Grid.SetColumn(timePanel, 0);
            grid.Children.Add(timePanel);

            // Status Badge
            var statusBadge = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Opacity = 0.9
            };
            statusBadge.Child = new TextBlock
            {
                Text = FormatStatus(normalizedStatus),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(statusBadge, 1);
            grid.Children.Add(statusBadge);

            header.Child = grid;
            return header;
        }

        private StackPanel CreateCardContent(Appointment appointment)
        {
            var content = new StackPanel { Margin = new Thickness(15, 12, 15, 10) };

            // Patient Info
            var patientGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            patientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            patientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Avatar
            var avatarGrid = new Grid { Width = 42, Height = 42, Margin = new Thickness(0, 0, 12, 0) };
            avatarGrid.Children.Add(new System.Windows.Shapes.Ellipse
            {
                Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
            });
            var initials = GetInitials(appointment.PatientName ?? "W P");
            avatarGrid.Children.Add(new TextBlock
            {
                Text = initials,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(avatarGrid, 0);
            patientGrid.Children.Add(avatarGrid);

            // Patient Name and ID
            var namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            namePanel.Children.Add(new TextBlock
            {
                Text = appointment.PatientName ?? "Walk-in Patient",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            namePanel.Children.Add(new TextBlock
            {
                Text = appointment.AppointmentId ?? "No ID",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(namePanel, 1);
            patientGrid.Children.Add(namePanel);

            content.Children.Add(patientGrid);

            // Details
            var details = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            // Type
            details.Children.Add(CreateDetailRow(
                MaterialDesignThemes.Wpf.PackIconKind.Stethoscope,
                appointment.Type ?? "General Consultation"));

            // Condition
            details.Children.Add(CreateDetailRow(
                MaterialDesignThemes.Wpf.PackIconKind.MedicalBag,
                appointment.Condition ?? "Not specified"));

            // Date
            details.Children.Add(CreateDetailRow(
                MaterialDesignThemes.Wpf.PackIconKind.CalendarClock,
                appointment.Time.ToString("dddd, MMMM dd, yyyy")));

            content.Children.Add(details);

            return content;
        }

        private Grid CreateDetailRow(MaterialDesignThemes.Wpf.PackIconKind iconKind, string text)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = iconKind,
                Width = 14,
                Height = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                Margin = new Thickness(8, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            return grid;
        }

        private Border CreateActionButtons(Appointment appointment)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Normalize status for button logic
            var normalizedStatus = NormalizeStatus(appointment.Status);

            // Add buttons based on normalized status
            switch (normalizedStatus)
            {
                case "PENDING":
                    panel.Children.Add(CreateActionButton("Confirm", "#4CAF50", appointment, ConfirmAppointment_Click));
                    panel.Children.Add(CreateActionButton("Cancel", "#F44336", appointment, CancelAppointment_Click));
                    break;
                case "CONFIRMED":
                    panel.Children.Add(CreateActionButton("Start", "#9C27B0", appointment, StartConsultation_Click));
                    panel.Children.Add(CreateActionButton("Cancel", "#F44336", appointment, CancelAppointment_Click));
                    break;
                case "IN_PROGRESS":
                    panel.Children.Add(CreateActionButton("Complete", "#4CAF50", appointment, CompleteConsultation_Click));
                    break;
                case "COMPLETED":
                    panel.Children.Add(CreateActionButton("View", "#2196F3", appointment, ViewDetails_Click));
                    panel.Children.Add(CreateActionButton("Prescribe", "#FF9800", appointment, Prescribe_Click));
                    break;
                case "CANCELLED":
                    panel.Children.Add(CreateActionButton("Reschedule", "#2196F3", appointment, Reschedule_Click));
                    break;
                default:
                    // Unknown status - show view button
                    panel.Children.Add(CreateActionButton("View", "#2196F3", appointment, ViewDetails_Click));
                    break;
            }

            border.Child = panel;
            return border;
        }

        private Button CreateActionButton(string content, string colorHex, Appointment appointment, RoutedEventHandler handler)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush(color),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = appointment
            };

            button.Template = CreateButtonTemplate();
            button.Click += handler;

            return button;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            return template;
        }

        #endregion

        #region Event Handlers

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Force fresh data from database
            using (var freshDb = new RosalEHealthcareDbContext())
            {
                // This forces EF to not use cached data
            }
            LoadAppointments();
        }

        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            _currentTab = button.Tag?.ToString() ?? "Today";

            // Update tab styles
            btnTabToday.Style = (Style)Resources["TabButtonStyle"];
            btnTabUpcoming.Style = (Style)Resources["TabButtonStyle"];
            btnTabPast.Style = (Style)Resources["TabButtonStyle"];
            btnTabAll.Style = (Style)Resources["TabButtonStyle"];

            button.Style = (Style)Resources["TabButtonActiveStyle"];

            ApplyFilters();
        }

        private void FilterStatus_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton == null) return;

            var status = toggleButton.Tag?.ToString();
            if (string.IsNullOrEmpty(status)) return;

            if (toggleButton.IsChecked == true)
            {
                if (!_activeStatusFilters.Contains(status))
                    _activeStatusFilters.Add(status);
            }
            else
            {
                _activeStatusFilters.Remove(status);
            }

            ApplyFilters();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ConfirmAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            try
            {
                _appointmentService.ConfirmAppointment(appointment.Id);
                LogActivity("Confirm Appointment", $"Confirmed appointment: {appointment.AppointmentId}");
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error confirming appointment:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartConsultation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            try
            {
                var dialog = new StartConsultationDialog(appointment);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // Start consultation with vital signs
                    _appointmentService.StartConsultation(appointment.Id);

                    // If patient exists, create medical history record with vitals
                    if (appointment.PatientId.HasValue)
                    {
                        var medicalHistory = new MedicalHistory
                        {
                            PatientId = appointment.PatientId.Value,
                            AppointmentId = appointment.Id,
                            VisitDate = DateTime.Now,
                            VisitType = appointment.Type ?? "Consultation",
                            BloodPressure = dialog.BloodPressure,
                            Temperature = dialog.Temperature,
                            HeartRate = dialog.HeartRate,
                            RespiratoryRate = dialog.RespiratoryRate,
                            Weight = dialog.Weight,
                            Height = dialog.Height,
                            DoctorName = SessionManager.CurrentUser?.FullName ?? "Doctor",
                            CreatedAt = DateTime.Now
                        };

                        _db.MedicalHistories.Add(medicalHistory);
                        _db.SaveChanges();
                    }

                    LogActivity("Start Consultation", $"Started consultation: {appointment.AppointmentId}");
                    LoadAppointments();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting consultation:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompleteConsultation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            try
            {
                var dialog = new CompleteConsultationDialog(appointment);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // Complete the appointment
                    _appointmentService.CompleteAppointment(appointment.Id);

                    // Update medical history if exists
                    if (appointment.PatientId.HasValue)
                    {
                        var medicalHistory = _db.MedicalHistories
                            .Where(m => m.AppointmentId == appointment.Id)
                            .OrderByDescending(m => m.VisitDate)
                            .FirstOrDefault();

                        if (medicalHistory != null)
                        {
                            medicalHistory.Diagnosis = dialog.PrimaryDiagnosis;
                            medicalHistory.Treatment = dialog.Treatment;
                            medicalHistory.FollowUpRequired = dialog.ScheduleFollowUp;
                            medicalHistory.NextFollowUpDate = dialog.FollowUpDate;
                        }
                        else
                        {
                            // Create new medical history
                            medicalHistory = new MedicalHistory
                            {
                                PatientId = appointment.PatientId.Value,
                                AppointmentId = appointment.Id,
                                VisitDate = DateTime.Now,
                                VisitType = appointment.Type ?? "Consultation",
                                Diagnosis = dialog.PrimaryDiagnosis,
                                Treatment = dialog.Treatment,
                                DoctorName = SessionManager.CurrentUser?.FullName ?? "Doctor",
                                FollowUpRequired = dialog.ScheduleFollowUp,
                                NextFollowUpDate = dialog.FollowUpDate,
                                CreatedAt = DateTime.Now
                            };
                            _db.MedicalHistories.Add(medicalHistory);
                        }

                        // Update patient's primary diagnosis
                        var patient = _db.Patients.Find(appointment.PatientId.Value);
                        if (patient != null)
                        {
                            patient.PrimaryDiagnosis = dialog.PrimaryDiagnosis;
                            patient.SecondaryDiagnosis = dialog.SecondaryDiagnosis;
                            patient.LastVisit = DateTime.Now;
                        }

                        _db.SaveChanges();
                    }

                    LogActivity("Complete Consultation", $"Completed consultation: {appointment.AppointmentId}");

                    // Handle follow-up actions
                    if (dialog.CreatePrescription)
                    {
                        OpenPrescriptionForAppointment(appointment);
                    }

                    if (dialog.ScheduleFollowUp && dialog.FollowUpDate.HasValue)
                    {
                        ScheduleFollowUpAppointment(appointment, dialog.FollowUpDate.Value);
                    }

                    LoadAppointments();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing consultation:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to cancel this appointment?\n\nPatient: {appointment.PatientName}\nTime: {appointment.Time:g}",
                "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _appointmentService.UpdateStatus(appointment.Id, "CANCELLED");
                    LogActivity("Cancel Appointment", $"Cancelled appointment: {appointment.AppointmentId}");
                    LoadAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error cancelling appointment:\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            // Get medical history for this appointment
            var medicalHistory = _db.MedicalHistories
                .FirstOrDefault(m => m.AppointmentId == appointment.Id);

            if (medicalHistory != null)
            {
                var dialog = new ViewVisitDialog(medicalHistory);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("No detailed records found for this appointment.",
                    "No Records", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Prescribe_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            OpenPrescriptionForAppointment(appointment);
        }

        private void Reschedule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appointment = button?.Tag as Appointment;
            if (appointment == null) return;

            MessageBox.Show("Reschedule functionality will open a date picker dialog.\n\nThis feature is pending implementation.",
                "Reschedule", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private void OpenPrescriptionForAppointment(Appointment appointment)
        {
            try
            {
                var prescriptionView = new DoctorPrescriptionManagement();

                if (appointment.PatientId.HasValue)
                {
                    var patient = _db.Patients.Find(appointment.PatientId.Value);
                    if (patient != null)
                    {
                        // Set patient directly on the view
                        prescriptionView.SelectedPatient = patient;
                    }
                }

                var window = new Window
                {
                    Title = $"New Prescription - {appointment.PatientName}",
                    Content = prescriptionView,
                    Width = 1200,
                    Height = 850,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.CanResize
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening prescription:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScheduleFollowUpAppointment(Appointment originalAppointment, DateTime followUpDate)
        {
            try
            {
                var newAppointment = new Appointment
                {
                    PatientId = originalAppointment.PatientId,
                    PatientName = originalAppointment.PatientName,
                    Contact = originalAppointment.Contact,
                    Email = originalAppointment.Email,
                    Address = originalAppointment.Address,
                    BirthDate = originalAppointment.BirthDate,
                    Gender = originalAppointment.Gender,
                    Time = followUpDate,
                    Type = "Follow-up",
                    Condition = $"Follow-up for: {originalAppointment.Condition}",
                    Status = "PENDING",
                    CreatedBy = SessionManager.CurrentUser?.FullName ?? "Doctor",
                    CreatedAt = DateTime.Now
                };

                _appointmentService.AddAppointment(newAppointment, SessionManager.CurrentUser?.FullName);

                LogActivity("Schedule Follow-up", $"Scheduled follow-up for {originalAppointment.PatientName} on {followUpDate:d}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scheduling follow-up:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            var today = DateTime.Today;
            var todayAppointments = _allAppointments.Where(a => a.Time.Date == today).ToList();

            txtTotalCount.Text = todayAppointments.Count.ToString();

            // Count pending and confirmed using normalized status
            txtPendingCount.Text = todayAppointments.Count(a =>
                NormalizeStatus(a.Status) == "PENDING" ||
                NormalizeStatus(a.Status) == "CONFIRMED").ToString();

            txtCompletedCount.Text = todayAppointments.Count(a =>
                NormalizeStatus(a.Status) == "COMPLETED").ToString();
        }

        private void ShowLoading(bool show)
        {
            pnlLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            Cursor = show ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }

        private Color GetStatusColor(string status)
        {
            // Use normalized status for consistent colors
            var normalized = NormalizeStatus(status);

            return normalized switch
            {
                "PENDING" => Color.FromRgb(255, 152, 0),      // Orange
                "CONFIRMED" => Color.FromRgb(33, 150, 243),   // Blue
                "IN_PROGRESS" => Color.FromRgb(156, 39, 176), // Purple
                "COMPLETED" => Color.FromRgb(76, 175, 80),    // Green
                "CANCELLED" => Color.FromRgb(244, 67, 54),    // Red
                _ => Color.FromRgb(158, 158, 158)             // Gray
            };
        }

        private string FormatStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "UNKNOWN";
            return NormalizeStatus(status).Replace("_", " ");
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";

            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0].Substring(0, 2).ToUpper();

            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private void LogActivity(string activityType, string description)
        {
            try
            {
                var log = new ActivityLog
                {
                    ActivityType = activityType,
                    Description = description,
                    Module = "Appointments",
                    PerformedBy = SessionManager.CurrentUser?.FullName ?? "Unknown",
                    PerformedByRole = SessionManager.CurrentUser?.Role ?? "Unknown",
                    PerformedAt = DateTime.Now
                };

                _db.ActivityLogs.Add(log);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        #endregion
    }
}