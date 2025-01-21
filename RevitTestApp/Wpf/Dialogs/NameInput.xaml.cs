using Autodesk.Revit.DB;
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

namespace RevitTestApp.Wpf.Dialogs
{
    /// <summary>
    /// Interaction logic for NameInput.xaml
    /// </summary>
    public partial class NameInput : Window
    {
        public string name;
        public NameInput()
        {
            InitializeComponent();
        }

        private void CreateClick(object sender, RoutedEventArgs e)
        {
            if (textBox.Text != "")
            {
                messageLabel.Content = "";
                name = textBox.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                messageLabel.Content = "The name field is empty.";
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
