using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Orion.Commands.SectionBox
{
    public class AdjustSectionBoxHandler : IExternalEventHandler
    {
        public XYZ Delta {  get; set; } = new XYZ(1.0,1.0,1.0);
        public const string CommandName = "Shrink Section Box";
        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            SectionBoxLogic.AdjustSize(doc, Delta, CommandName);
        }

        public string GetName()
        {
            return CommandName + " Event";
        }
    }


    public class SetSectionBoxHandler : IExternalEventHandler
    {
        public const string CommandName = "Set Section Box";
        private bool ViewHandlerFired = false;
        public double? Size { get; set; }
        public string Mode {  get; set; }
        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;

            SectionBoxLogic.SetSection(uidoc, ViewHandlerFired, CommandName, Mode, Size);

        }

        public string GetName()
        {
            return CommandName + " Event";
        }
    }

    public class ToggleSectionBoxHandler : IExternalEventHandler
    {
        public const string CommandName = "Toggle Section Box";

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            SectionBoxLogic.Toggle(doc, CommandName);
        }

        public string GetName()
        {
            return CommandName + " Event";
        }
    }
}
