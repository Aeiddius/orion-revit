using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orion.Commands.LevelSectioner
{
    public static class LevelSectionerLogic
    {
        public static Result SectionBetweenLevels(
            Document doc,
            Level fromLevel,
            Level toLevel,
            double xPos,
            double xNeg,
            double yPos,
            double yNeg,
            string commandName)
        {

            View3D view3D = doc.GetElement(new ElementId(3378071)) as View3D;

            Level level_5 = doc.GetElement(new ElementId(1363877)) as Level;
            Level level_6 = doc.GetElement(new ElementId(1365008)) as Level;

            double minZ = level_5.Elevation;
            double maxZ = level_6.Elevation;

            double extentsX = 500.0;
            double extentsY = 500.0;

            BoundingBoxXYZ bbox = new BoundingBoxXYZ()
            {
                Min = new XYZ(-extentsX, -extentsY, minZ),
                Max = new XYZ(extentsX, extentsY, maxZ)
            };

            bbox.Transform = Transform.Identity;

            using (Transaction tx = new Transaction(doc, "new box"))
            {
                tx.Start();
                view3D.SetSectionBox(bbox);
                tx.Commit();
            }

            return Result.Succeeded;
        }

        public static Result TestBoundingBox(
            Document doc,
            double xPos,
            double xNeg,
            double yPos,
            double yNeg,
            string commandName)
        {
            return Result.Succeeded;
        }

    }


}
