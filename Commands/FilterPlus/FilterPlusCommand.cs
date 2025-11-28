using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orion.Commands.FilterPlus
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class FilterPlusCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            UIApplication uiapp = commandData.Application as UIApplication;

            Orion.Views.FilterPlus.ShowOrActivate(uiapp, uidoc);

            return Result.Succeeded;
        }
    }
}
