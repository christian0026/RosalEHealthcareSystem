using RosalEHealthcare.Core.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class TemplateSelectionDialog : Window
    {
        private List<PrescriptionTemplate> _templates;
        public PrescriptionTemplate SelectedTemplate { get; private set; }

        public TemplateSelectionDialog(List<PrescriptionTemplate> templates)
        {
            InitializeComponent();
            _templates = templates;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Add severity color property for display
            var templatesWithColor = new List<object>();
            foreach (var template in _templates)
            {
                templatesWithColor.Add(new
                {
                    template.Id,
                    template.TemplateName,
                    template.PrimaryDiagnosis,
                    template.SecondaryDiagnosis,
                    template.ConditionSeverity,
                    template.UsageCount,
                    SeverityColor = GetSeverityColor(template.ConditionSeverity),
                    Template = template
                });
            }

            icTemplates.ItemsSource = templatesWithColor;
        }

        private Brush GetSeverityColor(string severity)
        {
            return severity switch
            {
                "Mild" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Moderate" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "Severe" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }

        private void Template_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.Tag != null)
            {
                var templateWrapper = element.Tag;
                var templateProperty = templateWrapper.GetType().GetProperty("Template");
                if (templateProperty != null)
                {
                    SelectedTemplate = templateProperty.GetValue(templateWrapper) as PrescriptionTemplate;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}