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
            PushButtonData SheetsOneView = new PushButtonData
                ("SheetsOneView", "SheetsOneView", thisAssemblyPath, "RevitAutoSheet.NewSheetsOneView");
            SheetsOneView.LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconL.png"));
            SheetsOneView.Image = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconS.png"));
            SheetsOneView.ToolTip = "Create a new Sheet for every selected View";
            
            //
            PushButtonData SheetViews = new PushButtonData
                ("SheetViews", "SheetViews", thisAssemblyPath, "RevitAutoSheet.NewSheetManyViews");
            SheetViews.LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconL.png"));
            SheetViews.Image = new BitmapImage(new Uri("pack://application:,,,/RevitAutoSheet;component/image/iconS.png"));
            SheetViews.ToolTip = "Create a new Sheet with all selected Views";

            //创建记忆下拉菜单并将按钮添加进去
            SplitButtonData splitButtonSheet = new SplitButtonData("SplotButtonSheet", "Split Button!");//注解完全不知道什么时候显示
            SplitButton splitButton = panel.AddItem(splitButtonSheet) as SplitButton;
            splitButton.AddPushButton(SheetsOneView);
            splitButton.AddPushButton(SheetViews);
        }
    }
}
