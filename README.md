# RevitTestApp

This Revit plugin provides four commands to enhance project workflows. The commands include generating title block views, showing hidden elements in 3D views, creating floors for rooms, and generating floor plans based on room geometry.

## Ribbon Tab
The plugin creates a new tab in the Revit ribbon named Test Commands, containing a panel with four buttons:

* View TitleBlock
* HiddenElements 3DView
* Room Floors
* Room Plans

Each button is associated with its respective command, and the plugin also includes custom icons for a better user interface.

## Commands Overview

#### 1. View Title Block
The plugin calculates the optimal title block size and place the view on it.
#### 2. Hidden Elements in 3D View
Creates a 3D view highlighting previously hidden elements on active view. The plugin hides all visible elements and un-hides hidden ones, applying specific colors for element types.
#### 3. Room Floors
Creates floors based on room boundaries for a specific level. The plugin identifies all rooms on the level named L1 and creates floors that match room boundaries.
Floors are created using appropriate floor types.
#### 4. Room Plans
Generates floor plans for rooms on a specific level. The plugin identifies all rooms on the level named L1 and creates a floor plan for each room with additional boundaries.
Floor plans are created with a predefined offset.

## Installation

* Clone or download the repository containing the plugin.
* Build the solution in Visual Studio.
* Place the resulting .dll and associated resources in the appropriate folder for Revit add-ins.
* Add an .addin file to the Revit add-in directory pointing to the plugin .dll.
