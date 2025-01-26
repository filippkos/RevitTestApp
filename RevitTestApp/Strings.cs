using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;
using System.Windows.Controls;

namespace RevitTestApp
{
    public static class Strings
    {
        public static string ErrorTitle = "Error";
        public static string CriticalErrorTitle = "Critical Error";
        public static string MessageTitle = "Message";
        public static string SuccessTitle = "Success";
        public static string CancelTitle = "Cancel";

        public static string UnexpectedError = "An unexpected error occurred during execution.";

        public static string FailedToGet2DViewsError = "Failed to retrieve 2D views.";
        public static string IsNot2DError = "The plugin can only be used for 2D views.";
        public static string IsAlreadyPlacedError = "This view is already placed on another sheet.";
        public static string FailedToGetDimensionsError = "Failed to get view dimensions.";
        public static string FailedToCreateSheetError = "Failed to create new sheet.";
        public static string TitleBlockNotFoundError = "No suitable title block was found to host the view.";
        public static string OperationWasCancelledError = "The operation was cancelled.";
        public static string FailedToCheckViewPlacementError = "Failed to check if the view is placed on a sheet.";
        public static string FailedToGetAllTitleBlocks = "Failed to retrieve title blocks.";
        public static string FailedToCreateSheetAndPlaceViewError = "Failed to create sheet and place the view.";
        public static string FailedToGetViewportsError = "Failed to retrieve viewports from view.";

        public static string UnableToFind3DViewTypeError = "Unable to find family type for 3D view.";
        public static string NoHiddenElementsError = "There are no hidden elements in the current view.";
        public static string FailedToGetElementTypesAndColorsError = "Failed to retrieve element types and colors: ";
        public static string FailedToHideElements = "Failed to hide/unhide elements: ";
        public static string FailedToGetUniqueTypes = "Failed to get unique element types and colors: ";
        public static string FailedToSetElementColorError = "Failed to set element color: ";
        public static string FailedToGet3DViewTypeError = "Failed to retrieve 3D view type: ";
        public static string FailedToGetInvisibleElementsError = "Failed to retrieve invisible elements: ";
        public static string FailedToPlaceFloorInRoom = "Failed to place floor in room";
        public static string FailedToCreate3DView = "Failed to create 3D view";

        public static string FailedToProcessRoomError = "Failed to process room";
        public static string NoFloorTypeFoundError = "No existing floor type found.";
        public static string FailedToCreateFloorTypeError = "Failed to create floor type";
        public static string FailedToFindMaterialError = "Failed to find material";
        public static string RoomBoundariesNotFoundError = "Room boundaries not found.";
        public static string SomeFloorsFoundMessage = "Some Existing Floors Found";
        public static string CannotFindOffsetError = "Cannot set 'Height Offset From Level'.";

        public static string FailedToGetLevelError = "Failed to get the level of the room.";
        public static string NoFloorPlanTypeFoundError = "No Floor Plan ViewFamilyType found.";
        public static string FailedToCreateFloorPlanError = "Failed to create a new Floor Plan.";
        public static string FailedToCreateFloorPlanForRoomError = "Failed to create floor plan for room";
        public static string FailedToCalculateCropBoxError = "Failed to calculate crop box.";
        public static string CreatingFloorPlanError = "An error occurred while creating the floor plan";
    }
}
