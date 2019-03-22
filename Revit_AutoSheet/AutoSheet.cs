using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

using Revit_AutoSheet;

namespace RevitAutoSheet
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AutoSheet : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            DocSet docSet = new DocSet(commandData);

            //get the selected room from Selection
            foreach (ElementId e in docSet.selection.GetElementIds())
            {
                docSet.selRoom = docSet.doc.GetElement(e) as Room;
                break;
            }

            ControlWindow window = new ControlWindow(docSet);
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}
