using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit_AutoSheet.Data;


namespace RevitAutoSheet
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class TBKSelection : Window
    {
        DocSet DocSet = new DocSet();
        NewSheetsOneView _oneView = null;
        NewSheetManyViews _manyView = null;

        public TBKSelection(DocSet docSet, NewSheetsOneView oneView)
        {
            InitializeComponent();
            _oneView = oneView;
            DocSet = docSet;
            Load();
        }

        public TBKSelection(DocSet docSet, NewSheetManyViews manyView)
        {
            InitializeComponent();
            _manyView = manyView;
            DocSet = docSet;
            Load();
        }

        private void Load()
        {
            FilteredElementCollector collector = new FilteredElementCollector(DocSet.doc);
            collector.OfClass(typeof(FamilySymbol));
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
            List<TbkInfo> infos = new List<TbkInfo>();
            foreach (Element element in collector)
            {
                FamilySymbol Tbk = element as FamilySymbol;
                if (Tbk != null)
                {
                    TbkInfo info = new TbkInfo { Name = Tbk.Name, Id = Tbk.Id };
                    infos.Add(info);
                }
            }
            this.ListView.ItemsSource = infos;
;        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            TbkInfo info = this.ListView.SelectedItem as TbkInfo;
            if (_oneView != null) _oneView.TbkSymbolId = info.Id;
            if (_manyView != null) _manyView.TbkSymbolId = info.Id;

            this.Close();
        }
    }
}
