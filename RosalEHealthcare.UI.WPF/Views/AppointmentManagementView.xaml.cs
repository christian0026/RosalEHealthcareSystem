using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AppointmentManagementView : UserControl
    {
        private RosalEHealthcareDbContext _db;
        private AppointmentService _appointmentService;
        private PatientService _patientService;
        private User _currentUser;

        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalRecords = 0;
        private int _totalPages = 0;

        private int? _editingAppointmentId = null;

        public AppointmentManagementView()
        {
            InitializeComponent();
            InitializeServices();
            LoadInitialData();
        }

        public AppointmentManagementView(User user) : this()
        {
            _currentUser = user;
        }

        private void InitializeServices()
        {
            _db = new RosalEHealthcareDbContext();
            _appointmentService = new AppointmentService(_db);
            _patientService = new PatientService(_db);
            _currentUser = SessionManager.CurrentUser;
        }

        private void LoadInitialData()
        {
            try
            {
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}\n\n{ex.InnerException?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Load Appointments with Pagination

        private void LoadAppointments()
        {
            try
            {
                string keyword = txtSearch.Text?.Trim();
                DateTime? date = dpDate.SelectedDate;
                string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string timeSlot = (cmbTimeSlot.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // Get total filtered count
                _totalRecords = _appointmentService.GetFilteredCount(keyword, date, status, timeSlot);
                _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);

                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                // Get paged data
                var appointments = _appointmentService.GetPaged(_currentPage, _pageSize, keyword, date, status, timeSlot);
                dgAppointments.ItemsSource = appointments;

                // Update pagination
                UpdatePaginationInfo();
                BuildPaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            int start = (_currentPage - 1) * _pageSize + 1;
            int end = Math.Min(_currentPage * _pageSize, _totalRecords);

            if (_totalRecords == 0)
            {
                txtPaginationInfo.Text = "No appointments found";
            }
            else
            {
                txtPaginationInfo.Text = $"Showing {start}-{end} of {_totalRecords} appointments";
            }
        }

        private void BuildPaginationButtons()
        {
            paginationPanel.Children.Clear();

            // Previous button
            var btnPrevious = new Button
            {
                Content = "❮❮ Previous",
                Style = (Style)FindResource("PaginationButton"),
                Width = 100,
                IsEnabled = _currentPage > 1
            };
            btnPrevious.Click += (s, e) => { _currentPage--; LoadAppointments(); };
            paginationPanel.Children.Add(btnPrevious);

            // Page numbers
            int startPage = Math.Max(1, _currentPage - 2);
            int endPage = Math.Min(_totalPages, _currentPage + 2);

            // First page
            if (startPage > 1)
            {
                AddPageButton(1);
                if (startPage > 2)
                {
                    var ellipsis = new TextBlock
                    {
                        Text = "...",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 0),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold
                    };
                    paginationPanel.Children.Add(ellipsis);
                }
            }

            // Page range
            for (int i = startPage; i <= endPage; i++)
            {
                AddPageButton(i);
            }

            // Last page
            if (endPage < _totalPages)
            {
                if (endPage < _totalPages - 1)
                {
                    var ellipsis = new TextBlock
                    {
                        Text = "...",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 0),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold
                    };
                    paginationPanel.Children.Add(ellipsis);
                }
                AddPageButton(_totalPages);
            }

            // Next button
            var btnNext = new Button
            {
                Content = "Next ❯❯",
                Style = (Style)FindResource("PaginationButton"),
                Width = 100,
                IsEnabled = _currentPage < _totalPages
            };
            btnNext.Click += (s, e) => { _currentPage++; LoadAppointments(); };
            paginationPanel.Children.Add(btnNext);
        }

        private void AddPageButton(int pageNumber)
        {
            var btn = new Button
            {
                Content = pageNumber.ToString(),
                Style = pageNumber == _currentPage
                    ? (Style)FindResource("ActivePaginationButton")
                    : (Style)FindResource("PaginationButton"),
                Tag = pageNumber
            };
            btn.Click += PageButton_Click;
            paginationPanel.Children.Add(btn);
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int pageNumber)
            {
                _currentPage = pageNumber;
                LoadAppointments();
            }
        }

        #endregion

        #region Search and Filters

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadAppointments();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _currentPage = 1;
                LoadAppointments();
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAppointments != null) // Check if initialized
            {
                _currentPage = 1;
                LoadAppointments();
            }
        }

        #endregion

        #region Schedule/Edit Appointment Dialog

        private void ScheduleNew_Click(object sender, RoutedEventArgs e)
        {
            OpenAppointmentDialog(isEdit: false);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Appointment appointment)
            {
                OpenAppointmentDialog(isEdit: true, appointment);
            }
        }

        private void OpenAppointmentDialog(bool isEdit, Appointment appointment = null)
        {
            try
            {
                // Load patients into dropdown
                var patients = _patientService.GetAll()
                    .Where(p => p.Status == "Active")
                    .OrderBy(p => p.FullName)
                    .ToList();

                cmbPatient.ItemsSource = patients;

                if (isEdit && appointment != null)
                {
                    txtDialogTitle.Text = "Edit Appointment";
                    _editingAppointmentId = appointment.Id;

                    // Select patient
                    var selectedPatient = patients.FirstOrDefault(p => p.Id == appointment.PatientId);
                    cmbPatient.SelectedItem = selectedPatient;

                    // Set appointment type
                    foreach (ComboBoxItem item in cmbAppointmentType.Items)
                    {
                        if (item.Content.ToString() == appointment.Type)
                        {
                            cmbAppointmentType.SelectedItem = item;
                            break;
                        }
                    }

                    // Set date and time
                    dpAppointmentDate.SelectedDate = appointment.Time.Date;

                    string timeStr = appointment.Time.ToString("hh:mm tt");
                    foreach (ComboBoxItem item in cmbAppointmentTime.Items)
                    {
                        if (item.Content.ToString() == timeStr)
                        {
                            cmbAppointmentTime.SelectedItem = item;
                            break;
                        }
                    }

                    txtReason.Text = appointment.Condition;

                    // Set status
                    foreach (ComboBoxItem item in cmbAppointmentStatus.Items)
                    {
                        if (item.Content.ToString() == appointment.Status)
                        {
                            cmbAppointmentStatus.SelectedItem = item;
                            break;
                        }
                    }

                    btnSaveAppointment.Content = "✓ Update Appointment";
                }
                else
                {
                    txtDialogTitle.Text = "Schedule New Appointment";
                    _editingAppointmentId = null;
                    ClearAppointmentForm();
                    dpAppointmentDate.SelectedDate = DateTime.Today;
                    btnSaveAppointment.Content = "✓ Save Appointment";
                }

                overlayPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening appointment dialog: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog_Click(object sender, RoutedEventArgs e)
        {
            overlayPanel.Visibility = Visibility.Collapsed;
            ClearAppointmentForm();
            _editingAppointmentId = null;
        }

        private void cmbPatient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPatient.SelectedItem is Patient patient)
            {
                txtPatientId.Text = patient.PatientId ?? $"PT-{patient.Id:D3}";
                txtPatientContact.Text = patient.Contact ?? "N/A";
            }
        }

        private void SaveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAppointmentForm())
                return;

            try
            {
                var patient = cmbPatient.SelectedItem as Patient;
                var appointmentType = (cmbAppointmentType.SelectedItem as ComboBoxItem)?.Content.ToString();
                var date = dpAppointmentDate.SelectedDate.Value;
                var timeStr = (cmbAppointmentTime.SelectedItem as ComboBoxItem)?.Content.ToString();
                var time = DateTime.Parse(timeStr);
                var appointmentDateTime = date.Date.Add(time.TimeOfDay);
                var reason = txtReason.Text?.Trim();
                var status = (cmbAppointmentStatus.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (_editingAppointmentId.HasValue)
                {
                    // Update existing appointment
                    var appointment = _appointmentService.GetById(_editingAppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.PatientId = patient.Id;
                        appointment.PatientName = patient.FullName;
                        appointment.Type = appointmentType;
                        appointment.Time = appointmentDateTime;
                        appointment.Condition = reason;
                        appointment.Status = status;
                        appointment.Contact = patient.Contact;

                        _appointmentService.UpdateAppointment(appointment);

                        MessageBox.Show("Appointment updated successfully!",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Create new appointment
                    var appointment = new Appointment
                    {
                        PatientId = patient.Id,
                        PatientName = patient.FullName,
                        Type = appointmentType,
                        Time = appointmentDateTime,
                        Condition = reason,
                        Status = status,
                        Contact = patient.Contact,
                        CreatedBy = _currentUser?.FullName ?? "Receptionist",
                        CreatedAt = DateTime.Now
                    };

                    _appointmentService.AddAppointment(appointment);

                    // Update patient's last visit
                    patient.LastVisit = DateTime.Now;
                    _patientService.UpdatePatient(patient);

                    MessageBox.Show($"Appointment scheduled successfully!\nAppointment ID: {appointment.AppointmentId}\n\nPatient: {patient.FullName}\nDate & Time: {appointmentDateTime:MMMM dd, yyyy hh:mm tt}",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseDialog_Click(null, null);
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving appointment: {ex.Message}\n\n{ex.InnerException?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAppointmentForm()
        {
            if (cmbPatient.SelectedItem == null)
            {
                MessageBox.Show("Please select a patient.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbPatient.Focus();
                return false;
            }

            if (cmbAppointmentType.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment type.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAppointmentType.Focus();
                return false;
            }

            if (!dpAppointmentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select an appointment date.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpAppointmentDate.Focus();
                return false;
            }

            // Check if date is not in the past
            if (dpAppointmentDate.SelectedDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Appointment date cannot be in the past.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpAppointmentDate.Focus();
                return false;
            }

            if (cmbAppointmentTime.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment time.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAppointmentTime.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Please enter the reason for visit / chief complaint.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReason.Focus();
                return false;
            }

            if (cmbAppointmentStatus.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment status.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAppointmentStatus.Focus();
                return false;
            }

            return true;
        }

        private void ClearAppointmentForm()
        {
            cmbPatient.SelectedItem = null;
            txtPatientId.Clear();
            txtPatientContact.Clear();
            cmbAppointmentType.SelectedIndex = 0;
            dpAppointmentDate.SelectedDate = null;
            cmbAppointmentTime.SelectedIndex = 2; // Default to 09:00 AM
            txtReason.Clear();
            cmbAppointmentStatus.SelectedIndex = 0; // Default to CONFIRMED
        }

        #endregion

        #region Cancel Appointment

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Appointment appointment)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to cancel this appointment?\n\nPatient: {appointment.PatientName}\nDate & Time: {appointment.Time:MMMM dd, yyyy hh:mm tt}\n\nThis action cannot be undone.",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Show reason dialog
                        var reasonDialog = new Window
                        {
                            Title = "Cancellation Reason",
                            Width = 450,
                            Height = 280,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize,
                            WindowStyle = WindowStyle.ToolWindow
                        };

                        var stackPanel = new StackPanel { Margin = new Thickness(20) };

                        var label = new TextBlock
                        {
                            Text = "Please provide a reason for cancellation (optional):",
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        stackPanel.Children.Add(label);

                        var reasonTextBox = new TextBox
                        {
                            Height = 100,
                            TextWrapping = TextWrapping.Wrap,
                            AcceptsReturn = true,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Margin = new Thickness(0, 0, 0, 15)
                        };
                        stackPanel.Children.Add(reasonTextBox);

                        var buttonPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };

                        var btnCancel = new Button
                        {
                            Content = "Cancel",
                            Width = 100,
                            Height = 35,
                            Margin = new Thickness(0, 0, 10, 0)
                        };
                        btnCancel.Click += (s, args) => reasonDialog.DialogResult = false;
                        buttonPanel.Children.Add(btnCancel);

                        var btnConfirm = new Button
                        {
                            Content = "Confirm",
                            Width = 100,
                            Height = 35,
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 83, 80)),
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                        };
                        btnConfirm.Click += (s, args) => reasonDialog.DialogResult = true;
                        buttonPanel.Children.Add(btnConfirm);

                        stackPanel.Children.Add(buttonPanel);

                        reasonDialog.Content = stackPanel;

                        if (reasonDialog.ShowDialog() == true)
                        {
                            string reason = reasonTextBox.Text?.Trim();
                            _appointmentService.CancelAppointment(appointment.Id, reason);

                            MessageBox.Show("Appointment cancelled successfully.",
                                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadAppointments();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error cancelling appointment: {ex.Message}",
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion
    }

}
