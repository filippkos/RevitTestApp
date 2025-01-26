using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using Autodesk.Revit.DB.Visual;
using RevitTestApp.Wpf.Dialogs;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class HiddenElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                View startView = doc.ActiveView;
                NameInput nameInput = new NameInput();
                string name;

                ElementId? viewId = GetViewType(doc);

                bool? result = nameInput.ShowDialog();
                name = nameInput.name;

                if (viewId != null)
                {
                    if (result == true)
                    {
                        View3D view;

                        List<ElementId> invisibleElementsIds = GetInvisibleElementsIn(doc);

                        if (invisibleElementsIds.Count == 0)
                        {
                            TaskDialog.Show(Strings.MessageTitle, Strings.NoHiddenElementsError);
                            return Result.Succeeded;
                        }

                        using (Transaction trans1 = new Transaction(doc, "Placing a view on a sheet"))
                        {
                            try
                            {
                                trans1.Start();
                                view = View3D.CreateIsometric(doc, viewId);

                                view.Name = name;

                                trans1.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans1.RollBack();
                                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToCreate3DView}: " + ex.Message);
                                return Result.Failed;
                            }
                        }

                        Dictionary<string, Color> types;
                        try
                        {
                            types = GetUniqueElementTypesWithColors(invisibleElementsIds, doc);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(Strings.ErrorTitle, Strings.FailedToGetElementTypesAndColorsError + ex.Message);
                            return Result.Failed;
                        }

                        var elementIds = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .Cast<Element>()
                            .Where(x => x.CanBeHidden(view) && !(x is View))
                            .Select(x => x.Id)
                            .ToList();

                        using (Transaction trans = new Transaction(doc, "Hiding and unhiding"))
                        {
                            try
                            {
                                trans.Start();

                                view.HideElements(elementIds);
                                view.UnhideElements(invisibleElementsIds);

                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.RollBack();
                                TaskDialog.Show(Strings.ErrorTitle, Strings.FailedToHideElements + ex.Message);
                                return Result.Failed;
                            }
                        }

                        try
                        {
                            SetColorsTo(invisibleElementsIds, doc, view);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(Strings.ErrorTitle, Strings.FailedToSetElementColorError + ex.Message);
                            return Result.Failed;
                        }

                        return Result.Succeeded;
                    }
                    else
                    {
                        TaskDialog.Show(Strings.CancelTitle, Strings.OperationWasCancelledError);

                        return Result.Cancelled;
                    }
                }

                return Result.Failed;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.UnexpectedError}: " + ex.Message);
                return Result.Failed;
            }
        }

        public void SetColorsTo(List<ElementId> invisibleElementsIds, Document doc, View view)
        {
            try
            {
                var typesAndColors = GetUniqueElementTypesWithColors(invisibleElementsIds, doc);

                foreach (ElementId id in invisibleElementsIds)
                {
                    var type = (doc.GetElement(doc.GetElement(id).GetTypeId()) as ElementType).ToString();
                    SetElementColor(view, id, typesAndColors[type], doc);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(Strings.FailedToSetElementColorError + ex.Message, ex);
            }
        }

        public Dictionary<string, Color> GetUniqueElementTypesWithColors(List<ElementId> elementsIds, Document doc)
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(Strings.FailedToGetUniqueTypes + ex.Message, ex);
            }
        }

        public void SetElementColor(View view, ElementId elementId, Color color, Document doc)
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(Strings.FailedToSetElementColorError + ex.Message, ex);
            }
        }

        private ElementId? GetViewType(Document doc)
        {
            try
            {
                ElementId? viewFamilyTypeId = null;

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                foreach (ViewFamilyType vft in collector.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>())
                {
                    if (vft.ViewFamily == ViewFamily.ThreeDimensional) return vft.Id;
                }

                if (viewFamilyTypeId == null) TaskDialog.Show(Strings.ErrorTitle, Strings.UnableToFind3DViewTypeError);

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(Strings.FailedToGet3DViewTypeError + ex.Message, ex);
            }
        }

        private List<ElementId> GetInvisibleElementsIn(Document doc)
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(Strings.FailedToGetInvisibleElementsError + ex.Message, ex);
            }
        }
    }
}
