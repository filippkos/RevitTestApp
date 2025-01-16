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
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using RevitTestApp.Commands;

namespace RevitTestApp.Wpf.Dialogs
{
    /// <summary>
    /// Interaction logic for PlanSelection.xaml
    /// </summary>
    public partial class PlanSelection : Window
    {
        private IList<View> plansList;

        // Свойство для хранения выбранного элемента
        public View? SelectedPlan;

        public PlanSelection(IList<View> plans)
        {
            InitializeComponent();
            plansList = plans;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in plansList)
            {
                plans.Items.Add(item);
            }
            plans.Text = "Select a Plan";
            plans.DisplayMemberPath = "Name";
        }

        private void CreateClick(object sender, RoutedEventArgs e)
        {
            if (plans.SelectedItem != null)
            {
                SelectedPlan = plans.SelectedItem as View;
                DialogResult = true;
                Close();
            }
            else
            {
                plans.Text = "You have not selected a plan.";
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close(); 
        }
    }
}
