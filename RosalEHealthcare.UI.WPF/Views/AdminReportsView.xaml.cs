using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminReportsView : UserControl
    {
        public static readonly string[] DailyLabels = new[] { "New", "Follow-up", "Completed", "Cancelled" };

        public AdminReportsView()
        {
            InitializeComponent();
        }
    }
}