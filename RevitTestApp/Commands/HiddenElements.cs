using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using Autodesk.Revit.DB.Visual;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class HiddenElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View startView = doc.ActiveView;

            ElementId? viewId = GetViewType(doc);

            if (viewId != null)
            {
                View3D view;
                
                List<ElementId> invisibleElementsIds = GetInvisibleElementsIn(doc);

                if (invisibleElementsIds.Count == 0)
                {
                    TaskDialog.Show(Strings.MessageTitle, Strings.NoHiddenElementsDescription);
                    return Result.Succeeded;
                }

                using (Transaction trans1 = new Transaction(doc, "Placing a view on a sheet"))
                {
                    trans1.Start();
                    view = View3D.CreateIsometric(doc, viewId);
                    view.Name = "Hidden objects 3D view";

                    trans1.Commit();
                }

                Dictionary<string, Color> types = GetUniqueElementTypesWithColors(invisibleElementsIds, doc);

                var elementIds = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Cast<Element>()
                    .Where(x => x.CanBeHidden(view) && !(x is View))
                    .Select(x => x.Id)
                    .ToList();

                using (Transaction trans = new Transaction(doc, "Hiding and unhiding"))
                {
                    trans.Start();

                    view.HideElements(elementIds);
                    view.UnhideElements(invisibleElementsIds);

                    trans.Commit();
                }

                SetColorsTo(invisibleElementsIds, doc, view);
                return Result.Succeeded;
            }

            return Result.Failed;
        }

        public void SetColorsTo(List<ElementId> invisibleElementsIds, Document doc, View view)
        {
            var typesAndColors = GetUniqueElementTypesWithColors(invisibleElementsIds, doc);

            foreach (ElementId id in invisibleElementsIds)
            {
                var type = (doc.GetElement(doc.GetElement(id).GetTypeId()) as ElementType).ToString();
                SetElementColor(view, id, typesAndColors[type], doc);

            }

        }

        public Dictionary<string, Color> GetUniqueElementTypesWithColors(List<ElementId> elementsIds, Document doc)
        {
            Dictionary<string, Color> elementTypeColorMap = new Dictionary<string, Color>();

            foreach (ElementId elementId in elementsIds)
            {
                Element element = doc.GetElement(elementId);
                ElementId typeId = element.GetTypeId();
                string? type = (doc.GetElement(typeId) as ElementType).ToString();

                if (type == null) continue; 

                if (typeId == ElementId.InvalidElementId) continue; 

                if (elementTypeColorMap.ContainsKey(type)) continue;

                Random random = new Random(Guid.NewGuid().GetHashCode());
                Color randomColor = new Color((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));

                elementTypeColorMap[type] = randomColor;
            }

            return elementTypeColorMap;
        }

        public void SetElementColor(View view, ElementId elementId, Color color, Document doc)
        {
            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();

            overrideSettings.SetProjectionLineColor(color);
            overrideSettings.SetSurfaceBackgroundPatternColor(color);
            overrideSettings.SetSurfaceBackgroundPatternId(FillPatternElement.GetFillPatternElementByName(doc, FillPatternTarget.Drafting, "<Solid fill>").Id);

            using (Transaction tx = new Transaction(doc, "Override Element Color"))
            {
                tx.Start();
                view.SetElementOverrides(elementId, overrideSettings);
                tx.Commit();
            }
        }

        private ElementId? GetViewType(Document doc)
        {
            ElementId? viewFamilyTypeId = null;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            foreach (ViewFamilyType vft in collector.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>())
            {
                if (vft.ViewFamily == ViewFamily.ThreeDimensional) return vft.Id;
            }

            if (viewFamilyTypeId == null) TaskDialog.Show(Strings.ErrorTitle, Strings.UnableToFind3DViewTypeErrorDescription);

            return null;
        }

        private List<ElementId> GetInvisibleElementsIn(Document doc)
        {
            View activeView = doc.ActiveView;
            List<ElementId> elementsIds = new List<ElementId>();

            elementsIds = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(x => x.IsHidden(activeView))
                .Select(x => x.Id)
                .ToList();

            return elementsIds;
        }
    }
}