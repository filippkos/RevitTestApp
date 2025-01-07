using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Controls;

namespace RevitTestApp
{
    public static class Strings
    {
        public static string ErrorTitle = "Error";
        public static string MessageTitle = "Message";
        public static string SuccessTitle = "Success";

        public static string IsNot2DErrorDescription = "The plugin can only be used for 2D views.";
        public static string IsAlreadyPlacedErrorDescription = "This view is already placed on another sheet.";
        public static string FaildToGetDimensionsErrorDescription = "Failed to get view dimensions.";
        public static string FailedToCreateSheetErrorDescription = "Failed to create new sheet.";
        public static string TitleBlockNotFoundErrorDescription = "No suitable title block was found to host the view.";

        public static string UnableToFind3DViewTypeErrorDescription = "Unable to find family type for 3D view.";
        public static string NoHiddenElementsDescription = "There are no hidden elements in the current view.";

        public static string NoFloorTypeFoundErrorDescription = "No existing floor type found.";
        public static string RoomBoundariesNotFoundErrorDescription = "Room boundaries not found.";
        public static string SomeFloorsFoundMessage = "Some Existing Floors Found";
        public static string CannotFindOffsetErrorDescription = "Cannot set 'Height Offset From Level'.";

        public static string FailedToGetLevelErrorDescription = "Failed to get the level of the room.";
        public static string NoFloorPlanTypeFoundErrorDescription = "No Floor Plan ViewFamilyType found.";
        public static string FailedToCreateFloorPlanErrorDescription = "Failed to create a new Floor Plan.";
        public static string FailedToCalculateCropRegionErrorDescription = "Failed to calculate crop region.";
    }
}
