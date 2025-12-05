using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Orion.Commands.SectionBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orion.Commands.LevelSectioner
{
    public class LevelSectionerHandler : IExternalEventHandler
    {
        public const string CommandName = "Level Sectioner";
        public Level fromLevel { get; set; }
        public Level toLevel { get; set; }
        public double xPos { get; set; }
        public double xNeg { get; set; }
        public double yPos { get; set; }
        public double yNeg { get; set; }


        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            LevelSectionerLogic.SectionBetweenLevels(
                doc,
                fromLevel,
                toLevel,
                xPos,
                xNeg,
                yPos,
                yNeg,
                CommandName);
        }

        public string GetName()
        {
            return CommandName + " Event";
        }
    }


    public class LevelSectionerTestHandler : IExternalEventHandler
    {
        public const string CommandName = "Level Sectioner Testing";
        public double xPos { get; set; }
        public double xNeg { get; set; }
        public double yPos { get; set; }
        public double yNeg { get; set; }


        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            LevelSectionerLogic.TestBoundingBox(
                doc,
                xPos,
                xNeg,
                yPos,
                yNeg,
                CommandName);
        }

        public string GetName()
        {
            return CommandName + " Event";
        }
    }
}
