using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using System;
using Autodesk.Revit.DB.Architecture;
using System.Windows;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RoomFloors : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                string levelName = "L1";

                List<Room> collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .Where(x => (x as Room).Level.Name == levelName)
                    .Cast<Room>()
                    .ToList();

                if (collector.Count > 0)
                {
                    foreach (Room room in collector)
                    {
                        try
                        {
                            FloorType? type = CreateFloorType(doc, room.Name);
                            if (type != null)
                            {
                                PlaceFloor(doc, room, type);
                            }
                            else
                            {
                                TaskDialog.Show(Strings.ErrorTitle, Strings.NoFloorTypeFoundError);
                                return Result.Failed;
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToProcessRoomError} '{room.Name}': {ex.Message}");
                            return Result.Failed;
                        }
                    }
                    return Result.Succeeded;
                }

                TaskDialog.Show(Strings.ErrorTitle, $"No existing rooms on {levelName} found.");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.CriticalErrorTitle, $"{Strings.UnexpectedError}: {ex.Message}");
                return Result.Failed;
            }
        }

        private FloorType? CreateFloorType(Document doc, string name)
        {
            try
            {
                FloorType? existingFloorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .OfType<FloorType>()
                    .FirstOrDefault(ft => ft.FamilyName == "Floor");

                if (existingFloorType == null) { return null; }

                using (Transaction tx = new Transaction(doc, "Create New Floor Type"))
                {
                    tx.Start();

                    ElementId newFloorTypeId = existingFloorType.Duplicate($"{name} Floor").Id;
                    FloorType? newFloorType = doc.GetElement(newFloorTypeId) as FloorType;

                    if (newFloorType != null)
                    {
                        Material? baseMaterial = FloorMaterial("Concrete, Cast-in-Place gray", doc);
                        Material? laminateMaterial = FloorMaterial("Laminate, Ivory, Matte", doc);

                        if (baseMaterial != null && laminateMaterial != null)
                        {
                            CompoundStructure structure = newFloorType.GetCompoundStructure();
                            if (structure != null)
                            {
                                IList<CompoundStructureLayer> layers = structure.GetLayers();
                                if (layers.Count > 0)
                                {
                                    CompoundStructureLayer concreteLayer = new CompoundStructureLayer(
                                        0.3,
                                        MaterialFunctionAssignment.Finish1,
                                        baseMaterial.Id
                                     );
                                    layers.Insert(0, concreteLayer);
                                    CompoundStructureLayer laminateLayer = new CompoundStructureLayer(
                                      0.1,
                                      MaterialFunctionAssignment.Finish1,
                                      laminateMaterial.Id
                                   );
                                    layers.Insert(0, laminateLayer);

                                    structure.SetLayers(layers);
                                    structure.SetNumberOfShellLayers(ShellLayerType.Exterior, 2);
                                    newFloorType.SetCompoundStructure(structure);
                                }
                            }
                        }
                    }
                    tx.Commit();
                    return newFloorType;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToCreateFloorTypeError}: {ex.Message}");
                return null;
            }
        }

        private Material? FloorMaterial(string name, Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                            .OfClass(typeof(Material))
                            .OfType<Material>()
                            .FirstOrDefault(m => m.Name == name);
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToFindMaterialError} '{name}': {ex.Message}");
                return null;
            }
        }

        private void PlaceFloor(Document doc, Room room, FloorType floorType)
        {
            try
            {
                List<ElementId> existingFloorsList = new List<ElementId>();

                SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
                IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(options);

                if (boundaries == null || boundaries.Count == 0)
                {
                    TaskDialog.Show(Strings.ErrorTitle, Strings.RoomBoundariesNotFoundError);
                    return;
                }

                List<Curve> curveList = new List<Curve>();
                foreach (BoundarySegment segment in boundaries[0])
                {
                    curveList.Add(segment.GetCurve());
                }

                CurveLoop profile = new CurveLoop();
                foreach (Curve curve in curveList)
                {
                    profile.Append(curve);
                }
                List<CurveLoop> loopList = new List<CurveLoop> { profile };

                using (Transaction tx = new Transaction(doc, "Create Floor"))
                {
                    tx.Start();
                    Floor floor = Floor.Create(doc, loopList, floorType.Id, room.LevelId);
                    Parameter offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);

                    if (offsetParam != null && !offsetParam.IsReadOnly)
                    {
                        offsetParam.Set(1);
                    }
                    else
                    {
                        TaskDialog.Show(Strings.ErrorTitle, Strings.CannotFindOffsetError);
                    }

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToPlaceFloorInRoom} '{room.Name}': {ex.Message}");
            }
        }
    }
}
