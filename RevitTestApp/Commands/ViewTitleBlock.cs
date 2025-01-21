using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTestApp.Wpf.Dialogs;
using System.Windows;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ViewTitleBlock : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            PlanSelection selection = new PlanSelection(GetAll2DViews(doc));
            bool? result = selection.ShowDialog();
            if (result == true)
            {
                View? selectedView = selection.SelectedPlan;

                if (!Is2DView(selectedView)) return ShowErrorAndFail(Strings.ErrorTitle, Strings.IsNot2DErrorDescription);

                if (!IsViewNotOnSheet(selectedView, doc)) return ShowErrorAndFail(Strings.ErrorTitle, Strings.IsAlreadyPlacedErrorDescription);

                var viewDimensions = GetViewDimensions(selectedView);
                if (viewDimensions == null) return ShowErrorAndFail(Strings.ErrorTitle, Strings.FaildToGetDimensionsErrorDescription);

                double scaledViewArea = viewDimensions.Value.Width * viewDimensions.Value.Height;
                Element? optimalTitleBlock = FindOptimalTitleBlock(doc, viewDimensions.Value);

                if (optimalTitleBlock != null)
                {
                    return CreateSheetAndPlaceView(doc, selectedView, optimalTitleBlock, viewDimensions.Value);
                }

                TaskDialog.Show(Strings.MessageTitle, Strings.TitleBlockNotFoundErrorDescription);

                return Result.Succeeded;
            }
            else
            {
                TaskDialog.Show(Strings.CancelTitle, Strings.OperationWasCancelledDescription);

                return Result.Cancelled;
            }
        }

        public static List<View> GetAll2DViews(Document doc)
        {
            // Собираем все виды
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && 
                    (v.ViewType == ViewType.FloorPlan ||
                     v.ViewType == ViewType.CeilingPlan ||
                     v.ViewType == ViewType.Section ||
                     v.ViewType == ViewType.Elevation ||
                     v.ViewType == ViewType.Detail))
                .ToList();

            return views;
        }

        private bool Is2DView(View view) => view is ViewPlan || view is ViewSection || view is ViewDrafting;

        private bool IsViewNotOnSheet(View view, Document doc)
        {
            var viewPorts = GetViewPortsFromView(view, doc);
            return viewPorts.All(viewport => viewport?.SheetId.Value == -1);
        }

        private Result ShowErrorAndFail(string title, string message)
        {
            TaskDialog.Show(title, message);
            return Result.Failed;
        }

        private (double Width, double Height)? GetViewDimensions(View view)
        {
            BoundingBoxXYZ viewBoundingBox = view.get_BoundingBox(null);
            if (viewBoundingBox == null) return null;

            double width = (viewBoundingBox.Max.X - viewBoundingBox.Min.X) / view.Scale;
            double height = (viewBoundingBox.Max.Y - viewBoundingBox.Min.Y) / view.Scale;
            return (width, height);
        }

        private Element? FindOptimalTitleBlock(Document doc, (double Width, double Height) viewDimensions)
        {
            double scaledWidth = viewDimensions.Width;
            double scaledHeight = viewDimensions.Height;
            double? optimalSheetArea = null;
            Element? optimalTitleBlock = null;

            var titleBlocks = AllTitleBlocksIn(doc);

            foreach (Element titleBlock in titleBlocks)
            {
                double sheetWidth = titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH).AsDouble();
                double sheetHeight = titleBlock.get_Parameter(BuiltInParameter.SHEET_HEIGHT).AsDouble();

                if (scaledWidth <= sheetWidth && scaledHeight <= sheetHeight)
                {
                    double area = sheetWidth * sheetHeight;
                    if (optimalSheetArea == null || area < optimalSheetArea)
                    {
                        optimalSheetArea = area;
                        optimalTitleBlock = titleBlock;
                    }
                }
            }

            return optimalTitleBlock;
        }

        private List<Element> AllTitleBlocksIn(Document doc)
        { 
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType()
                .ToList();
        }

        private List<Element> AllLevelsIn(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .ToList();
        }

        private Result CreateSheetAndPlaceView(Document doc, View view, Element titleBlock, (double Width, double Height) viewDimensions)
        {
            using (Transaction trans = new Transaction(doc, "Placing a view on a sheet"))
            {
                trans.Start();

                ElementId? titleBlockTypeId = (titleBlock as FamilyInstance)?.Symbol.Id;
                ViewSheet newSheet = ViewSheet.Create(doc, titleBlockTypeId);

                if (newSheet == null) return ShowErrorAndFail(Strings.ErrorTitle, Strings.FailedToCreateSheetErrorDescription);

                double sheetWidth = titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH).AsDouble();
                double sheetHeight = titleBlock.get_Parameter(BuiltInParameter.SHEET_HEIGHT).AsDouble();

                XYZ viewPosition = new XYZ((sheetWidth - viewDimensions.Width) / 2, (sheetHeight - viewDimensions.Height) / 2, 0);
                Viewport.Create(doc, newSheet.Id, view.Id, viewPosition);

                TaskDialog.Show(Strings.SuccessTitle, $"The view '{view.Name}' was placed on the sheet '{newSheet.SheetNumber}' ({titleBlock.Name}).");

                trans.Commit();
                return Result.Succeeded;
            }
        }

        private List<Viewport?> GetViewPortsFromView(View view, Document doc)
        {
            var dependentElements = view.GetDependentElements(null);
            return dependentElements
                .Select(id => doc.GetElement(id) as Viewport)
                .Where(vp => vp != null)
                .ToList();
        }
    }
}