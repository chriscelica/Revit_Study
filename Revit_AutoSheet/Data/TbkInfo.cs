using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit_AutoSheet.Data
{
    public class TbkInfo
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
