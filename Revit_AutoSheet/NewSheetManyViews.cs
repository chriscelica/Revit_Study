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
    public class NewSheetManyViews:IExternalCommand
    {

        public ElementId TbkSymbolId = null;
        public Result Execute (ExternalCommandData commandData,ref string  message ,ElementSet elements)
        {
            DocSet docSet = new DocSet(commandData);

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

            TBKSelection seleWindow = new TBKSelection(docSet, this);
            seleWindow.ShowDialog();

            using(Transaction tran = new Transaction(docSet.doc))
            {
                tran.Start("Create a new Sheet with all selected Views");
                // Create a sheet view
                ViewSheet viewSheet = ViewSheet.Create(docSet.doc, TbkSymbolId);

                //
                double offset = 1;//
                UV viewLocation = new UV(viewSheet.Outline.Min.U + offset / 2, viewSheet.Outline.Min.V + offset / 3);
                //
                UV outLine = new UV(viewSheet.Outline.Max.U, viewSheet.Outline.Max.V);

                //
                int successNub = 0;
                int wrongNub = 0;
                string wrongViewName = "";

                foreach (ElementId id in Ids)
                {
                    try
                    {
                        View view = docSet.doc.GetElement(id) as View;

                        //
                        Viewport viewport = Viewport.Create(docSet.doc, viewSheet.Id, id, new XYZ(viewLocation.U, viewLocation.V, 0));

                        if (outLine.U - viewLocation.U < offset * 1.5)
                        {
                            if (outLine.V - viewLocation.V < offset)
                            {
                                viewLocation = new UV(viewSheet.Outline.Min.U + offset, viewSheet.Outline.Min.V + offset);
                                successNub++;
                                continue;
                            }
                            viewLocation = new UV(viewSheet.Outline.Min.U + offset / 2, viewLocation.V + offset / 2);
                            successNub++;
                            continue;
                        }
                        viewLocation = new UV(viewLocation.U + offset * 0.75, viewLocation.V);
                        successNub++;
                    }
                    catch
                    {
                        wrongNub++;
                        wrongViewName += "\n" + docSet.doc.GetElement(id).Name;
                        continue;
                    }

                }
                tran.Commit();
                docSet.uidoc.ActiveView = viewSheet;
                TaskDialog.Show("1", "Success " + successNub + " views \n " + wrongNub + " views failed:" + wrongViewName);
            }

            return Result.Succeeded;
        }

    }
}
