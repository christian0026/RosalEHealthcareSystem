using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
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
        private User _currentUser;

        private int _currentPage = 1;
        private int _pageSize = 10;
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
            _currentUser = SessionManager.CurrentUser;
        }

        private void LoadInitialData()
        {
            try
            {
                LoadSummaryCards();
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message + "\n\n" + ex.InnerException?.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Summary Cards

        private void LoadSummaryCards()
        {
            try
            {
                var allAppointments = _db.Appointments.ToList();

                var total = allAppointments.Count;
                var confirmed = allAppointments.Count(a => a.Status == "CONFIRMED");
                var pending = allAppointments.Count(a => a.Status == "PENDING");
                var cancelled = allAppointments.Count(a => a.Status == "CANCELLED");

                CardTotalAppointments.Value = total.ToString("N0");
                CardConfirmed.Value = confirmed.ToString("N0");
                CardPending.Value = pending.ToString("N0");
                CardCancelled.Value = cancelled.ToString("N0");

                CardTotalAppointments.TrendText = "All appointments";
                CardConfirmed.TrendText = "Ready for consultation";
                CardPending.TrendText = pending > 0 ? "Awaiting confirmation" : "All confirmed";
                CardCancelled.TrendText = cancelled > 0 ? "Cancelled appointments" : "No cancellations";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading summary: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Appointments with Pagination

        private void LoadAppointments()
        {
            try
            {
                string keyword = txtSearch.Text?.Trim();
                DateTime? date = dpDate.SelectedDate;
                string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string timeSlot = (cmbTimeSlot.SelectedItem as ComboBoxItem)?.Content?.ToString();

                _totalRecords = _appointmentService.GetFilteredCount(keyword, date, status, timeSlot);
                _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);

                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                var appointments = _appointmentService.GetPaged(_currentPage, _pageSize, keyword, date, status, timeSlot);
                dgAppointments.ItemsSource = appointments;

                UpdatePaginationInfo();
                BuildPaginationButtons();
                LoadSummaryCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                txtPaginationInfo.Text = "Showing " + start + "-" + end + " of " + _totalRecords + " appointments";
            }
        }

        private void BuildPaginationButtons()
        {
            paginationPanel.Children.Clear();

            var btnPrevious = new Button
            {
                Content = "❮❮ Previous",
                Width = 100,
                Height = 40,
                IsEnabled = _currentPage > 1,
                Margin = new Thickness(4, 0, 4, 0)
            };
            btnPrevious.Click += (s, e) => { _currentPage--; LoadAppointments(); };
            paginationPanel.Children.Add(btnPrevious);

            int startPage = Math.Max(1, _currentPage - 2);
            int endPage = Math.Min(_totalPages, _currentPage + 2);

            if (startPage > 1)
            {
                AddPageButton(1);
                if (startPage > 2)
                {
                    paginationPanel.Children.Add(new TextBlock { Text = "...", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
                }
            }

            for (int i = startPage; i <= endPage; i++)
            {
                AddPageButton(i);
            }

            if (endPage < _totalPages)
            {
                if (endPage < _totalPages - 1)
                {
                    paginationPanel.Children.Add(new TextBlock { Text = "...", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
                }
                AddPageButton(_totalPages);
            }

            var btnNext = new Button
            {
                Content = "Next ❯❯",
                Width = 100,
                Height = 40,
                IsEnabled = _currentPage < _totalPages,
                Margin = new Thickness(4, 0, 4, 0)
            };
            btnNext.Click += (s, e) => { _currentPage++; LoadAppointments(); };
            paginationPanel.Children.Add(btnNext);
        }

        private void AddPageButton(int pageNumber)
        {
            var btn = new Button
            {
                Content = pageNumber.ToString(),
                Width = 40,
                Height = 40,
                Margin = new Thickness(4, 0, 4, 0),
                Tag = pageNumber,
                FontWeight = pageNumber == _currentPage ? FontWeights.Bold : FontWeights.Normal,
                Background = pageNumber == _currentPage ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.White
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
            if (dgAppointments != null)
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
                if (isEdit && appointment != null)
                {
                    txtDialogTitle.Text = "Edit Appointment";
                    _editingAppointmentId = appointment.Id;

                    txtFullName.Text = appointment.PatientName;
                    txtContact.Text = appointment.Contact;
                    dpBirthDate.SelectedDate = appointment.BirthDate;
                    txtEmail.Text = appointment.Email;
                    txtAddress.Text = appointment.Address;

                    foreach (ComboBoxItem item in cmbGender.Items)
                    {
                        if (item.Content.ToString() == appointment.Gender)
                        {
                            cmbGender.SelectedItem = item;
                            break;
                        }
                    }

                    foreach (ComboBoxItem item in cmbAppointmentType.Items)
                    {
                        if (item.Content.ToString() == appointment.Type)
                        {
                            cmbAppointmentType.SelectedItem = item;
                            break;
                        }
                    }

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
                    btnSaveAppointment.Content = "✓ Update Appointment";
                }
                else
                {
                    txtDialogTitle.Text = "Schedule New Appointment";
                    _editingAppointmentId = null;
                    ClearAppointmentForm();
                    dpAppointmentDate.SelectedDate = DateTime.Today;
                    btnSaveAppointment.Content = "✓ Schedule Appointment";
                }

                overlayPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening appointment dialog: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog_Click(object sender, RoutedEventArgs e)
        {
            overlayPanel.Visibility = Visibility.Collapsed;
            ClearAppointmentForm();
            _editingAppointmentId = null;
        }

        private void SaveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAppointmentForm())
                return;

            try
            {
                var fullName = txtFullName.Text.Trim();
                var contact = txtContact.Text.Trim();
                var birthDate = dpBirthDate.SelectedDate.Value;
                var gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                var email = txtEmail.Text?.Trim();
                var address = txtAddress.Text.Trim();
                var appointmentType = (cmbAppointmentType.SelectedItem as ComboBoxItem)?.Content.ToString();
                var date = dpAppointmentDate.SelectedDate.Value;
                var timeStr = (cmbAppointmentTime.SelectedItem as ComboBoxItem)?.Content.ToString();
                var time = DateTime.Parse(timeStr);
                var appointmentDateTime = date.Date.Add(time.TimeOfDay);
                var reason = txtReason.Text?.Trim();

                if (_editingAppointmentId.HasValue)
                {
                    var appointment = _appointmentService.GetById(_editingAppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.PatientName = fullName;
                        appointment.Contact = contact;
                        appointment.BirthDate = birthDate;
                        appointment.Gender = gender;
                        appointment.Email = email;
                        appointment.Address = address;
                        appointment.Type = appointmentType;
                        appointment.Time = appointmentDateTime;
                        appointment.Condition = reason;

                        _appointmentService.UpdateAppointment(appointment);
                        MessageBox.Show("Appointment updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    var appointment = new Appointment
                    {
                        PatientName = fullName,
                        Contact = contact,
                        BirthDate = birthDate,
                        Gender = gender,
                        Email = email,
                        Address = address,
                        Type = appointmentType,
                        Time = appointmentDateTime,
                        Condition = reason,
                        Status = "PENDING", // Default status
                        CreatedBy = _currentUser?.FullName ?? "Receptionist",
                        CreatedAt = DateTime.Now
                    };
                    _appointmentService.AddAppointment(appointment);

                    MessageBox.Show("Appointment scheduled successfully!\n\nAppointment ID: " + appointment.AppointmentId + "\nPatient: " + fullName + "\nDate & Time: " + appointmentDateTime.ToString("MMMM dd, yyyy hh:mm tt") + "\nStatus: PENDING",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseDialog_Click(null, null);
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving appointment: " + ex.Message + "\n\n" + ex.InnerException?.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAppointmentForm()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter patient's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFullName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContact.Text))
            {
                MessageBox.Show("Please enter contact number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContact.Focus();
                return false;
            }

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select birth date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }

            if (cmbGender.SelectedItem == null)
            {
                MessageBox.Show("Please select gender.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbGender.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Please enter complete address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAddress.Focus();
                return false;
            }

            if (cmbAppointmentType.SelectedItem == null)
            {
                MessageBox.Show("Please select appointment type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAppointmentType.Focus();
                return false;
            }

            if (!dpAppointmentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select appointment date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpAppointmentDate.Focus();
                return false;
            }

            if (dpAppointmentDate.SelectedDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Appointment date cannot be in the past.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpAppointmentDate.Focus();
                return false;
            }

            if (cmbAppointmentTime.SelectedItem == null)
            {
                MessageBox.Show("Please select appointment time.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAppointmentTime.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Please enter reason for visit / chief complaint.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReason.Focus();
                return false;
            }

            return true;
        }

        private void ClearAppointmentForm()
        {
            txtFullName.Clear();
            txtContact.Clear();
            dpBirthDate.SelectedDate = null;
            cmbGender.SelectedItem = null;
            txtEmail.Clear();
            txtAddress.Clear();
            cmbAppointmentType.SelectedIndex = 0;
            dpAppointmentDate.SelectedDate = null;
            cmbAppointmentTime.SelectedIndex = 2;
            txtReason.Clear();
        }

        #endregion

        #region Cancel Appointment

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Appointment appointment)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to cancel this appointment?\n\nPatient: " + appointment.PatientName + "\nDate & Time: " + appointment.Time.ToString("MMMM dd, yyyy hh:mm tt") + "\n\nThis action cannot be undone.",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _appointmentService.CancelAppointment(appointment.Id, "Cancelled by receptionist");
                        MessageBox.Show("Appointment cancelled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAppointments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error cancelling appointment: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion
    }
}