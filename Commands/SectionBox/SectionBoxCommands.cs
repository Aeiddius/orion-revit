using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Orion.Views;
using System.Windows.Interop;


namespace Orion.Commands.SectionBox
{


    [TransactionAttribute(TransactionMode.Manual)]
    internal class MenuSectionBoxCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            UIApplication uiapp = commandData.Application as UIApplication;
          
            Orion.Views.SectionMenu.ShowOrActivate(uiapp, uidoc);

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class SetSectionBoxCommand : IExternalCommand
    {
        private const string CommandName = "Set Section Box";
        private bool viewHandlerFired = false;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;


            // Get 3D View
            SectionBoxLogic.SetSection(uidoc, viewHandlerFired, CommandName, "elements");

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class ToggleSectionBoxCommand : IExternalCommand
    {
        private const string CommandName = "Toggle Section Box";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc.ActiveView.ViewType != ViewType.ThreeD) return Result.Cancelled;

            return SectionBoxLogic.Toggle(doc, CommandName);
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class GrowSectionBoxCommand : IExternalCommand
    {
        private const string CommandName = "Grow Section Box";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc.ActiveView.ViewType != ViewType.ThreeD) return Result.Cancelled;

            return SectionBoxLogic.AdjustSize(doc, new XYZ(1.0 , 1.0, 1.0), CommandName);
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class ShrinkSectionBoxCommand : IExternalCommand
    {
        private const string CommandName = "Shrink Section Box";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc.ActiveView.ViewType != ViewType.ThreeD) return Result.Cancelled;

            return SectionBoxLogic.AdjustSize(doc, new XYZ(-1.0, -1.0, -1.0), CommandName);
        }
    }
}
