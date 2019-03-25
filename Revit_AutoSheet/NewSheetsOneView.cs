using System;
using System.IO;//用于获取路径
using System.Collections.Generic;
using System.Reflection;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

using Revit_AutoSheet.Data;

namespace RevitAutoSheet
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public  class NewSheetsOneView :IExternalCommand
    {
        public ElementId TbkSymbolId = null;

        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            DocSet docSet = new DocSet(commandData);

            //
            List<ElementId> Ids = new List<ElementId>();

            foreach (ElementId id in docSet.selection.GetElementIds())
            {
                View view = docSet.doc.GetElement(id) as View;
                if (view == null) continue;
                Ids.Add(id);
            }

            if (Ids.Count <= 0)
            {
                TaskDialog.Show("no id", "plz selected at least a view");
                return Result.Succeeded;
            }

            TBKSelection seleWindow = new TBKSelection(docSet,this);
            seleWindow.ShowDialog();


            foreach (ElementId id in Ids)
            {
                FamilySymbol TbkSymbol = docSet.doc.GetElement(TbkSymbolId) as FamilySymbol;
                CreateSheet(docSet, id, TbkSymbol);
            }

            return Result.Succeeded;
        }

        public ViewSheet CreateSheet(DocSet docSet,ElementId id, FamilySymbol fs)
        {

            ViewSheet viewSheet = null;
            using(Transaction tran = new Transaction(docSet.doc))
            {
                View view = docSet.doc.GetElement(id) as View;

                tran.Start("Create a New sheet of" + view.ViewName);
                // Create a sheet view
                viewSheet = ViewSheet.Create(docSet.doc, fs.Id);

                // Add passed in view onto the center of the sheet
                UV location = new UV(viewSheet.Outline.Min.U+0.4,
                                     viewSheet.Outline.Min.V+0.4);

                //viewSheet.AddView(view3D, location);
                Viewport.Create(docSet.doc, viewSheet.Id, id, new XYZ(location.U, location.V, 0));

                viewSheet.Name = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Definition.Name;
                TaskDialog.Show("idsheet", viewSheet.Id.ToString());
                tran.Commit();
            }
            TaskDialog.Show("newSheet", "create a new sheet" + viewSheet.Id.ToString());
            return viewSheet;
        }
    }
}
