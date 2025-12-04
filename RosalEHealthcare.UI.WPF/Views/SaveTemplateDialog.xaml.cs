using System;
using System.Windows;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class SaveTemplateDialog : Window
    {
        public string TemplateName { get; private set; }
        public string TemplateDescription { get; private set; }

        public SaveTemplateDialog()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                txtTemplateName.Focus();
            };
        }

        private void TxtTemplateName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSave_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }

            // Hide error when typing
            txtError.Visibility = Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var templateName = txtTemplateName.Text?.Trim();

            // Validation
            if (string.IsNullOrEmpty(templateName))
            {
                txtError.Text = "Please enter a template name.";
                txtError.Visibility = Visibility.Visible;
                txtTemplateName.Focus();
                return;
            }

            if (templateName.Length < 3)
            {
                txtError.Text = "Template name must be at least 3 characters.";
                txtError.Visibility = Visibility.Visible;
                txtTemplateName.Focus();
                return;
            }

            if (templateName.Length > 100)
            {
                txtError.Text = "Template name must be less than 100 characters.";
                txtError.Visibility = Visibility.Visible;
                txtTemplateName.Focus();
                return;
            }

            TemplateName = templateName;
            TemplateDescription = txtDescription.Text?.Trim();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}