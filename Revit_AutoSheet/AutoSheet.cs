using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

namespace Revit_CreateRoomElevation1
{
    public class CreateRoomElevations
    {
        [Transaction(TransactionMode.Manual)]
        [Regeneration(RegenerationOption.Manual)]
        public class mainClass : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData,
                ref string message, ElementSet elements)
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                Selection sel = uidoc.Selection;

                Room selRoom = null;

                //从选择集中获取房间
                foreach (ElementId e in sel.GetElementIds())
                {
                    selRoom = doc.GetElement(e) as Room;
                    break;
                }

                ControlWindow window = new ControlWindow(uiapp, selRoom);
                window.ShowDialog();

                return Result.Succeeded;
            }
        }
    }
}
