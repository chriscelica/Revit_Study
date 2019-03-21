using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI.Selection;

namespace Revit_AutoSheet
{
    /// <summary>
    /// A set include UIapp, UIdoc,Doc, SelRoom and Selection
    /// </summary>
    public  class DocSet
    {
        /// <summary>
        /// the UIApplication of Revit
        /// </summary>
        public UIApplication uiapp { get; set; }

        /// <summary>
        /// the UIDocument of Revit;
        /// </summary>
        public UIDocument uidoc { get; set; }

        /// <summary>
        /// the Document of Revit
        /// </summary>
        public Document doc { get; set; }

        /// <summary>
        /// the Selected Room
        /// </summary>
        public Room selRoom { get; set; }

        /// <summary>
        /// the Selection of Revit
        /// </summary>
        public Selection selection { get; set; }

        /// <summary>
        /// Initializes a new instance of the DocSet class by a ExternalCommandData.
        /// and the Selected Room is null
        /// </summary>
        /// <param name="commandData"></param>
        public DocSet(ExternalCommandData commandData)
        {
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            doc = uidoc.Document;
            selection = uidoc.Selection;
            selRoom = null;
        }

        /// <summary>
        /// Initializes a new null instance of the DocSet class
        /// </summary>
        public DocSet()
        {
            uiapp = null;
            uidoc = null;
            doc = null;
            selection = null;
            selRoom = null;
        }
    }
}
