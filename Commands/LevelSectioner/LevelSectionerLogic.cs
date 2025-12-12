using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
            List<int> except = [1365138];

            List<Level> levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OrderBy(level => (level as Level).Elevation)
                .Where(level => !except.Contains(level.Id.IntegerValue))
                .Cast<Level>()
                .ToList();

            int fromIndex = levels.FindIndex(l => l.Id == fromLevel.Id);
            int toIndex = levels.FindIndex(l => l.Id == toLevel.Id);

            // Check view 3d family type
            ViewFamilyType viewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            for (int i = fromIndex; i < toIndex; i++)
            {
                Level bottomLevel = levels[i];
                Level topLevel = levels[i + 1];

                // Set bbox
                BoundingBoxXYZ bbox = new()
                {
                    Min = new XYZ(-xNeg, -yNeg, bottomLevel.Elevation),
                    Max = new XYZ(xPos, yPos, topLevel.Elevation+1),
                    Transform = Transform.Identity
                };

                // Commit to Revit
                using (Transaction tx = new Transaction(doc, "new box"))
                {
                    tx.Start();
                    // Create new view 3d
                    View3D newView = View3D.CreateIsometric(doc, viewType.Id);

                    // Set name
                    newView.Name = $"SECTIONED - {bottomLevel.Name} 3D Section View ";

                    // Set box
                    newView.SetSectionBox(bbox);
                    tx.Commit();
                }

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
            View3D view3D = doc.ActiveView as View3D;


            BoundingBoxXYZ viewBbox = view3D.GetSectionBox();



            BoundingBoxXYZ bbox = new BoundingBoxXYZ()
            {
                Min = new XYZ(-xNeg, -yNeg, viewBbox.Min.Z),
                Max = new XYZ(xPos, yPos, viewBbox.Max.Z)
            };


            using (Transaction tx = new Transaction(doc, "new box"))
            {
                tx.Start();
                view3D.IsSectionBoxActive = true;
                view3D.SetSectionBox(bbox);
                tx.Commit();
            }

            return Result.Succeeded;
        }

    }


}
