using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;//用于处理图标图片

using System;
using System.IO;//用于获取路径
using System.Collections.Generic;
using System.Reflection;

namespace Revit_AutoSheet
{
    public class CsNewTab : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication app)
        {
            AddRibbonSampler(app);
            return Result.Succeeded;
        }

        public void AddRibbonSampler(UIControlledApplication app)
        {
            RibbonPanel panel = null;         
            try
            {
                app.CreateRibbonTab("ACID");
            }
            catch { }
            panel = app.CreateRibbonPanel("ACID", "AutoElevation");

            //图片的生成操作必须为Resource!!!!!!!!!
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(thisAssemblyPath);

            PushButtonData autoViewData = new PushButtonData
                ("AutoView", "AutoView", thisAssemblyPath, "RevitAutoSheet.AutoView");

            PushButton autoView = panel.AddItem(autoViewData) as PushButton;
            //图标
            autoView.LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconL.png"));
            autoView.Image = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconS.png"));
            //注解
            autoView.ToolTip = "Create a new FloorPlan, CeilingPlan, and four Elevations for the Selected Room" +
                                 "\n the Elevations will be create at the center of the \"X\" of the room";

            //

            //
            PushButtonData SheetOneViewData = new PushButtonData
                ("SheetOneView", "SheetOneView", thisAssemblyPath, "RevitAutoSheet.NewSheetsOneView");

            PushButton SheetOneView = panel.AddItem(SheetOneViewData) as PushButton;

            SheetOneView.LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconL.png"));
            SheetOneView.Image = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconS.png"));

            SheetOneView.ToolTip = "sheet";
        }
    }
}
