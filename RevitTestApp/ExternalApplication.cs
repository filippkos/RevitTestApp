using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace RevitTestApp
{
    class ExternalApplication : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("Test Commands");

            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData task1ButtonData = new PushButtonData("Button1", "View TitleBlock", path, "RevitTestApp.Commands.ViewTitleBlock");
            PushButtonData task2ButtonData = new PushButtonData("Button2", "HiddenElements \n3DView", path, "RevitTestApp.Commands.HiddenElements");
            PushButtonData task3ButtonData = new PushButtonData("Button3", "Room Floors", path, "RevitTestApp.Commands.RoomFloors");
            PushButtonData task4ButtonData = new PushButtonData("Button4", "Room Plans", path, "RevitTestApp.Commands.RoomPlans");

            RibbonPanel panel = application.CreateRibbonPanel("Test Commands", "Commands");

            Uri imagePath1 = new Uri("pack://application:,,,/RevitTestApp;component/Resources/Icons/command1.png", UriKind.RelativeOrAbsolute);
            Uri imagePath2 = new Uri("pack://application:,,,/RevitTestApp;component/Resources/Icons/command2.png", UriKind.RelativeOrAbsolute);
            Uri imagePath3 = new Uri("pack://application:,,,/RevitTestApp;component/Resources/Icons/command3.png", UriKind.RelativeOrAbsolute);
            Uri imagePath4 = new Uri("pack://application:,,,/RevitTestApp;component/Resources/Icons/command4.png", UriKind.RelativeOrAbsolute);
            BitmapImage image1 = new BitmapImage(imagePath1);
            BitmapImage image2 = new BitmapImage(imagePath2);
            BitmapImage image3 = new BitmapImage(imagePath3);
            BitmapImage image4 = new BitmapImage(imagePath4);

            PushButton? pushButton1 = panel.AddItem(task1ButtonData) as PushButton;
            PushButton? pushButton2 = panel.AddItem(task2ButtonData) as PushButton;
            PushButton? pushButton3 = panel.AddItem(task3ButtonData) as PushButton;
            PushButton? pushButton4 = panel.AddItem(task4ButtonData) as PushButton;

            pushButton1.LargeImage = image1;
            pushButton2.LargeImage = image2;
            pushButton3.LargeImage = image3;
            pushButton4.LargeImage = image4;

            return Result.Succeeded;
        }
    }
}