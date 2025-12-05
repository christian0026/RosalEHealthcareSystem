using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class TemplateSelectionDialog : Window
    {
        private readonly PrescriptionTemplateService _templateService;
        private List<PrescriptionTemplate> _allTemplates;

        public PrescriptionTemplate SelectedTemplate { get; private set; }

        public TemplateSelectionDialog(PrescriptionTemplateService templateService)
        {
            InitializeComponent();

            _templateService = templateService;

            Loaded += TemplateSelectionDialog_Loaded;
        }

        private void TemplateSelectionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            try
            {
                // Use GetAllTemplates and filter for active ones
                // Adjust this based on your actual PrescriptionTemplateService methods
                var templates = _templateService.GetAllTemplates();

                // Filter for active templates if IsActive property exists
                _allTemplates = templates
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.UsageCount)
                    .ThenByDescending(t => t.LastUsedAt)
                    .ToList();

                DisplayTemplates(_allTemplates);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading templates:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayTemplates(List<PrescriptionTemplate> templates)
        {
            icTemplates.ItemsSource = templates;
            pnlEmpty.Visibility = templates.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                DisplayTemplates(_allTemplates);
                return;
            }

            var filtered = _allTemplates.Where(t =>
                (t.TemplateName?.ToLower().Contains(searchText) ?? false) ||
                (t.PrimaryDiagnosis?.ToLower().Contains(searchText) ?? false) ||
                (t.ConditionSeverity?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            DisplayTemplates(filtered);
        }

        private void Template_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var template = border?.Tag as PrescriptionTemplate;

            if (template != null)
            {
                SelectedTemplate = template;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}