using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using Autodesk.Revit.DB.Architecture;
using System.Reflection.Emit;
using System.Windows;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RoomFloors : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View startView = doc.ActiveView;
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
                    FloorType? type = CreateFloorType(doc, room.Name);
                    if (type != null)
                    {
                        PlaceFloor(doc, room, type);
                    }
                    else
                    {
                        TaskDialog.Show(Strings.ErrorTitle, Strings.NoFloorTypeFoundErrorDescription);
                        return Result.Failed;
                    }
                }
                return Result.Succeeded;
            }
            TaskDialog.Show(Strings.ErrorTitle, $"No existing rooms on {levelName} found.");
            return Result.Failed;
        }

        private FloorType? CreateFloorType(Document doc, string name)
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

                                double totalThickness = 0.0;

                                foreach (CompoundStructureLayer layer in layers)
                                {
                                    totalThickness += layer.Width;
                                }

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

        private Material? FloorMaterial(string name, Document doc)
        {
            return new FilteredElementCollector(doc)
                        .OfClass(typeof(Material))
                        .OfType<Material>()
                        .FirstOrDefault(m => m.Name == name);
        }

        private void PlaceFloor(Document doc, Room room, FloorType floorType)
        {
            List<ElementId> existingFloorsList = [];

            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(options);

            if (boundaries == null || boundaries.Count == 0)
            {
                TaskDialog.Show(Strings.ErrorTitle, Strings.RoomBoundariesNotFoundErrorDescription);
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
            List<CurveLoop> loopList = new List<CurveLoop>();
            loopList.Add(profile);

            List<Floor> floorCollector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(Floor))
                .OfCategory(BuiltInCategory.OST_Floors)
                .Cast<Floor>()
                .ToList();

            foreach (Floor existingFloor in floorCollector)
            {
                GeometryElement geometry = existingFloor.get_Geometry(new Options());
                foreach (GeometryObject geoObj in geometry)
                {
                    Solid? solid = geoObj as Solid;
                    if (solid != null)
                    {
                        foreach (Curve curve in curveList)
                        {
                            SolidCurveIntersection intersection = solid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions());
                            if (intersection.SegmentCount > 0)
                            {
                                existingFloorsList.Add(existingFloor.Id);
                                break;
                            }
                        }
                    }
                }
            }

            if (existingFloorsList.Count > 0)
            {
                TaskDialogResult result = TaskDialog.Show(
                    Strings.SomeFloorsFoundMessage,
                    $"Some Existing Floors ({existingFloorsList.Count}) Found In The {room.Name}. Do you want to replace it?",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                );

                if (result == TaskDialogResult.Yes)
                {
                    using (Transaction tx = new Transaction(doc, "Delete Existing Floors"))
                    {
                        tx.Start();
                        doc.Delete(existingFloorsList);
                        tx.Commit();
                    }
                }
                else
                {
                    return;
                }
            }

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
                    TaskDialog.Show(Strings.ErrorTitle, Strings.CannotFindOffsetErrorDescription);
                }

                tx.Commit();
            }
        }
    }
}