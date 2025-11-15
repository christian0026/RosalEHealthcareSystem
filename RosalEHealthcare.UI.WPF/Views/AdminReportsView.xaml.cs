using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminReportsView : UserControl
    {
        public static string[] DailyLabels => new[] { "New", "Follow-ups", "Completed", "Cancelled" };

        public AdminReportsView()
        {
            InitializeComponent();
        }
    }
}