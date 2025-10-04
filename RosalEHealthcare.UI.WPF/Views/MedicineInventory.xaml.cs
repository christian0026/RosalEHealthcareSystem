using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class MedicineInventory : UserControl
    {
        public MedicineInventory()
        {
            InitializeComponent();

            // Optional: quick sample data so the DataGrid is not empty while you build UI
            var sample = new List<MedicineItem>
            {
                new MedicineItem { Name="Amoxicillin 500mg", ID="MED-001", Category="Antibiotics", Stock=150, Price="₱15.50", Expiry="Dec 2025", Status="Available" },
                new MedicineItem { Name="Paracetamol 500mg", ID="MED-002", Category="Pain Relief", Stock=45, Price="₱2.50", Expiry="Nov 2025", Status="Available" },
                new MedicineItem { Name="Ibuprofen 400mg", ID="MED-003", Category="Pain Relief", Stock=12, Price="₱8.75", Expiry="Sep 2025", Status="Out of Stock" },
                new MedicineItem { Name="Vitamin C 500mg", ID="MED-005", Category="Vitamins", Stock=0, Price="₱12.75", Expiry="Jan 2025", Status="Out of Stock" },
            };

            dgMedicines.ItemsSource = sample;
        }
    }

    // Simple view-model for sample display (move to separate file later)
    public class MedicineItem
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Category { get; set; }
        public int Stock { get; set; }
        public string Price { get; set; }
        public string Expiry { get; set; }
        public string Status { get; set; }
    }
}
