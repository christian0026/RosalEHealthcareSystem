using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class SaveTemplateDialog : Window
    {
        public string TemplateName { get; private set; }

        public SaveTemplateDialog()
        {
            InitializeComponent();
            txtTemplateName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTemplateName.Text))
            {
                MessageBox.Show("Please enter a template name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTemplateName.Focus();
                return;
            }

            TemplateName = txtTemplateName.Text.Trim();
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