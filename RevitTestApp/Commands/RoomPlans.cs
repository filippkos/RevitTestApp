using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;

namespace RevitTestApp.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RoomPlans : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                View activeView = doc.ActiveView;
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
                            CreateFloorPlan(doc, room, 2);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(Strings.ErrorTitle, $"{Strings.FailedToCreateFloorPlanForRoomError} '{room.Name}': {ex.Message}");
                        }
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Strings.ErrorTitle, $"{Strings.UnexpectedError}: {ex.Message}");
                return Result.Failed;
            }
        }

        public void CreateFloorPlan(Document doc, Room room, double offsetInFeet)
        {
            using (Transaction tx = new Transaction(doc, "Create Floor Plan with Crop Region"))
            {
                try
                {
                    tx.Start();

                    Level? level = doc.GetElement(room.LevelId) as Level;
                    if (level == null)
                    {
                        throw new Exception(Strings.FailedToGetLevelError);
                    }

                    ViewPlan? existingPlan = new FilteredElementCollector(doc)
                       .OfClass(typeof(ViewPlan))
                       .Cast<ViewPlan>()
                       .FirstOrDefault(vp => vp.Name == $"{room.Name} Plan");

                    if (existingPlan != null)
                    {
                        TaskDialog.Show(Strings.MessageTitle, $"A Floor Plan for '{room.Name}' already exists.");
                        tx.RollBack();
                        return;
                    }

                    ViewFamilyType? floorPlanType = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

                    if (floorPlanType == null)
                    {
                        throw new Exception(Strings.NoFloorPlanTypeFoundError);
                    }

                    ViewPlan newFloorPlan = ViewPlan.Create(doc, floorPlanType.Id, level.Id);
                    if (newFloorPlan == null)
                    {
                        throw new Exception(Strings.FailedToCreateFloorPlanError);
                    }

                    SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
                    IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(options);

                    if (boundaries == null || boundaries.Count == 0)
                    {
                        throw new Exception(Strings.RoomBoundariesNotFoundError);
                    }

                    BoundingBoxXYZ cropBox = CalculateCropBox(boundaries, offsetInFeet);
                    if (cropBox == null)
                    {
                        throw new Exception(Strings.FailedToCalculateCropBoxError);
                    }

                    newFloorPlan.CropBox = cropBox;
                    newFloorPlan.CropBoxActive = true;
                    newFloorPlan.CropBoxVisible = true;
                    newFloorPlan.Name = $"{room.Name} Plan";

                    tx.Commit();

                    TaskDialog.Show(Strings.SuccessTitle, $"{room.Name} Plan created with a crop region.");
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    TaskDialog.Show(Strings.ErrorTitle, $"{Strings.CreatingFloorPlanError} {ex.Message}");
                }
            }
        }

        private BoundingBoxXYZ CalculateCropBox(IList<IList<BoundarySegment>> boundaries, double offsetInFeet)
        {
            try
            {
                XYZ min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                XYZ max = new XYZ(double.MinValue, double.MinValue, double.MinValue);

                foreach (IList<BoundarySegment> boundary in boundaries)
                {
                    foreach (BoundarySegment segment in boundary)
                    {
                        Curve curve = segment.GetCurve();
                        XYZ start = curve.GetEndPoint(0);
                        XYZ end = curve.GetEndPoint(1);

                        min = new XYZ(Math.Min(min.X, start.X), Math.Min(min.Y, start.Y), Math.Min(min.Z, start.Z));
                        max = new XYZ(Math.Max(max.X, end.X), Math.Max(max.Y, end.Y), Math.Max(max.Z, end.Z));
                    }
                }

                double offset = offsetInFeet;

                min = new XYZ(min.X - offset, min.Y - offset, min.Z);
                max = new XYZ(max.X + offset, max.Y + offset, max.Z);

                BoundingBoxXYZ cropBox = new BoundingBoxXYZ
                {
                    Min = min,
                    Max = max
                };

                return cropBox;
            }
            catch (Exception ex)
            {
                throw new Exception($"{Strings.FailedToCalculateCropBoxError}: {ex.Message}");
            }
        }
    }
}