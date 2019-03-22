using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

using Revit_AutoSheet;

namespace RevitAutoSheet
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class ControlWindow : Window
    {
        /// <summary>
        /// the Document set from Revit
        /// </summary>
        DocSet DocSet = new DocSet();

        /// <summary>
        /// the SoaNumber of the selected room
        /// (a ecample Parameter without BuiltInParam)
        /// </summary>
        string _SoANumber = "";

        public ControlWindow(DocSet docSet)
        {
            if(docSet == null)
            {
                TaskDialog.Show("doc null", "can't get the Document of Revit.");
                return;
            }
            InitializeComponent();
            DocSet = docSet;
            if (docSet.selRoom == null)
                labRoomId.Content = "N/A";
            else
                labRoomId.Content = docSet.selRoom.Id.ToString();
        }

        /// <summary>
        /// 运行按钮的点击事件 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            # region//获取各种偏移量并判断数据有效性
            double viewOffset = 0;//平面视图的偏移量
            double elevationOffset = 0;//里面的偏移量  暂时不支持"L"型房间 她的生成边框线的法向量不稳定
            double FloorThickness = 0;//地面楼板的厚度 用于将立面的底边框上抬到楼板上 used to move the bottom of Elevation up the floor
            bool isViewOffset = Double.TryParse(tbViewOffset.Text, out viewOffset);
            bool isElevationOffset = Double.TryParse(tbElevationOffset.Text, out elevationOffset);
            bool isFloorThickness = Double.TryParse(tbFloorThickness.Text, out FloorThickness);

            //判断输入数值是否有效
            if (!isViewOffset)
            {
                TaskDialog.Show("ViewOffsetWrong", "Pleace set a right Number of ViewOffset");
                return;
            }
            if (!isElevationOffset)
            {
                TaskDialog.Show("ElevationOffsetWrong", "Pleace set a right Number of ElevationOffset");
                return;
            }
            if (!isElevationOffset)
            {
                TaskDialog.Show("FloorThicknessWrong", "Pleace set a right Number of FloorThickness");
                return;
            }

            //无选择房间时重选 Re selecte a new room if selected room is null
            if (DocSet.selRoom == null)
            {
                TaskDialog.Show("selRoomIsNull", "Pleace selecet a room");
                this.Hide();
                DocSet.selRoom = DocSet.doc.GetElement(DocSet.selection.PickObject(ObjectType.Element, "Pleace selecet a room")) as Room;
                if (DocSet.selRoom == null)
                {
                    TaskDialog.Show("RoomWrong", "the Element you selecet is not a ROOM");
                    this.ShowDialog();
                    return;
                }
            }

            //获取房间的SoA Number  get the SOANumber of the selected room
            _SoANumber = " - A";
            try
            {
                Parameter SoANuber = DocSet.selRoom.LookupParameter("SoA Number");
                _SoANumber += SoANuber.AsString().Replace(".", "-");
            }
            catch
            {
                TaskDialog.Show("soanumber null", "Cant find the SoA Number");
                _SoANumber = null;
            }
            #endregion

            //实际执行
            View3D view3d = Create3DView();
            CreateNewViewPlan(viewOffset,view3d);
            CreateElevations(elevationOffset, FloorThickness);
           
           this.Close();
        }

        /// <summary>
        /// 为选中房间创建专门的floorPlan和ceilingPlan
        /// Ctrate a New FloorPlan and CeilingPlan for the selected selRoom
        /// </summary>
        /// <param name="viewOffseet"></param>
        /// <param name="view3d"></param>
        public void CreateNewViewPlan(double viewOffseet,View3D view3d)
        {
            //过滤所有的ViewFamilyType
            var classFilter = new ElementClassFilter(typeof(ViewFamilyType));
            FilteredElementCollector collector = new FilteredElementCollector(DocSet.doc);
            collector = collector.WherePasses(classFilter);
            ViewPlan view = null;

            using (Transaction tran = new Transaction(DocSet.doc))
            {
                foreach (ViewFamilyType viewFamilyType in collector)
                {
                    //当类型为FloorPlan或者CeilingPlan时创建同类型视图
                    if (viewFamilyType.ViewFamily == ViewFamily.FloorPlan
                        || viewFamilyType.ViewFamily == ViewFamily.CeilingPlan)
                    {
                        tran.Start("Creat view of type " + viewFamilyType.ViewFamily);
                        //创建视图时需要 视图类型ID 相关标高ID
                        view = ViewPlan.Create(DocSet.doc, viewFamilyType.Id, DocSet.selRoom.LevelId);

                        //TaskDialog.Show("CreatLevelView", "A new level's view has been Created");

                        view.Name = DocSet.selRoom.Name;//生成平面的名称

                        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION).Set(1);
                        view.AreAnalyticalModelCategoriesHidden = false;
                        view.PartsVisibility = PartsVisibility.ShowPartsAndOriginal;
                        view.Scale = 50;
                        view.CropBoxActive = true;
                        view.CropBoxVisible = true;

                        string viewName = "PLAN ";
                        view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set(DocSet.selRoom.Name);

                        if (viewFamilyType.ViewFamily == ViewFamily.CeilingPlan)
                        {
                            PlanViewRange range = view.GetViewRange();
                            range.SetLevelId(PlanViewPlane.TopClipPlane, DocSet.selRoom.UpperLimit.Id);
                            range.SetLevelId(PlanViewPlane.ViewDepthPlane, DocSet.selRoom.UpperLimit.Id);
                            range.SetLevelId(PlanViewPlane.CutPlane, DocSet.selRoom.LevelId);
                            range.SetLevelId(PlanViewPlane.BottomClipPlane, DocSet.selRoom.LevelId);
                            range.SetOffset(PlanViewPlane.CutPlane, 7.874);
                            range.SetOffset(PlanViewPlane.BottomClipPlane, 7.874);
                            view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set(DocSet.selRoom.Name);
                            view.SetViewRange(range);

                            viewName = "RCP ";
                            view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set(DocSet.selRoom.Name + " - RCP");

                        }
                        viewName += _SoANumber + "_" + DocSet.selRoom.Level.Name;
                        view.ViewName = viewName;
                        tran.Commit();
                        ChangeViewFitRoom(view, tran, viewOffseet);
                    }
                }
            }
        }

        /// <summary>
        /// 在房间X的中心创建四个方向的立面
        /// Create four Elevations on the center of the "X" of the selRoom
        /// </summary>
        /// <param name="elevationOffset"></param>
        /// <param name="FloorThickness"></param>
        public void CreateElevations(double elevationOffset,double FloorThickness)
        {
            int i = 0;//循环用

            //获取立面的familytype     Get the familyType of Elevation
            FilteredElementCollector collector = new FilteredElementCollector(DocSet.doc);
            collector.OfClass(typeof(ViewFamilyType));

            var viewFamilyTypes = from elem in collector
                                  let type = elem as ViewFamilyType
                                  where type.ViewFamily == ViewFamily.Elevation
                                  select type;

            ElementId viewTypeId;
            if (viewFamilyTypes.Count() > 0)
                viewTypeId = viewFamilyTypes.First().Id;
            else
                return;


            using (Transaction tran = new Transaction(DocSet.doc))
            {
                //房间的"X"的交点
                LocationPoint pt = DocSet.selRoom.Location as LocationPoint;

                tran.Start("newElvation");
                ElevationMarker marker = ElevationMarker.CreateElevationMarker(DocSet.doc, viewTypeId, pt.Point, 50);
                for (; i < 4; i++)
                {
                    ViewSection sv = marker.CreateElevation(DocSet.doc, DocSet.doc.ActiveView.Id, i);

                    //设定立面的 远剪裁偏移
                    sv.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).SetValueString("10000");

                    //设定每个立面的名称
                    XYZ normal = null;//法向量
                    string elevationName = "ELE -";
                    switch (i)
                    {
                        case 0:
                            elevationName += " West " + _SoANumber;
                            normal = new XYZ(-1, 0, 0);
                            sv.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set("ELEVATION WEST");
                            break;
                        case 1:
                            elevationName += " North" + _SoANumber;
                            normal = new XYZ(0, 1, 0);
                            sv.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set("ELEVATION NORTH");
                            break;
                        case 2:
                            elevationName += " East" + _SoANumber;
                            normal = new XYZ(1, 0, 0);
                            sv.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set("ELEVATION EAST");
                            break;
                        case 3:
                            elevationName += " South" + _SoANumber;
                            normal = new XYZ(0, -1, 0);
                            sv.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set("ELEVATION SOUTH");
                            break;
                    }
                    sv.ViewName = elevationName;

                    //不能删 必须先保存修改才能获取上面的元素
                    tran.Commit();
                    tran.Start("change elevation crop shape");
                    
                    //小指型房间专用修改
                    if (cbSpRoom.IsChecked == true)
                    {
                        if (i == 1 || i == 2)
                            normal = -normal;
                        spRoomElevationChange(sv, elevationOffset, normal, FloorThickness);
                    }
                    else
                    {
                        //修改立面底边的高度
                        XYZ pt1 = null;
                        XYZ pt2 = null;
                        XYZ pt3 = null;
                        XYZ pt4 = null;
                        sv.CropBoxActive = true;
                        ViewCropRegionShapeManager vcrShanpMgr = sv.GetCropRegionShapeManager();
                        CurveLoop loop = vcrShanpMgr.GetCropShape().First();
                        CurveLoopIterator iterator = loop.GetCurveLoopIterator();

                        //分辨点的位置
                        while (iterator.MoveNext())
                        {
                            Curve curve = iterator.Current;
                            XYZ pt0 = curve.GetEndPoint(0);
                            if (-1 < pt0.Z - pt.Point.Z && pt0.Z - pt.Point.Z < 1) 
                            {
                                if (pt1 == null)
                                    pt1 = pt0;
                                else pt2 = pt0;
                            }

                            else
                            {
                                if (pt3 == null)
                                    pt3 = pt0;
                                else pt4 = pt0;
                            }
                        }

                        //重新生成一个边界框
                        //TaskDialog.Show("1", pt1.ToString() + "\n" + pt2.ToString() + "\n" + pt3.ToString() + "\n" + pt4.ToString());
                        pt1 = new XYZ(pt1.X, pt1.Y, pt1.Z + FloorThickness / 300);
                        pt2 = new XYZ(pt2.X, pt2.Y, pt1.Z);

                        Line lineBottom = Line.CreateBound(pt1, pt2);
                        Line lineRight = Line.CreateBound(pt2, pt4);
                        Line lineTop = Line.CreateBound(pt4, pt3);
                        Line lineLeft = Line.CreateBound(pt3, pt1);

                        CurveLoop profile = new CurveLoop();
                        profile.Append(lineBottom);
                        profile.Append(lineRight);
                        profile.Append(lineTop);
                        profile.Append(lineLeft);

                        profile = CurveLoop.CreateViaOffset(profile, elevationOffset / 300, -normal);
                        vcrShanpMgr.SetCropShape(profile);
                    }
                }

                tran.Commit();
            }
        }

        /// <summary>
        /// 重选按钮
        /// the reSelect button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            DocSet.selRoom = DocSet.doc.GetElement(DocSet.selection.PickObject(ObjectType.Element, "Pleace selecet a room")) as Room;
            if (DocSet.selRoom == null)
            {
                TaskDialog.Show("RoomWrong", "the Element you selecet is not a ROOM");
                labRoomId.Content = "N/A";
                this.ShowDialog();
                return;
            }
            else
            {
                labRoomId.Content = DocSet.selRoom.Id.ToString();
                this.ShowDialog();
            }
        }

        /// <summary>
        /// 用于"碰瓷检测"的方法
        /// the funtion used to get the elements around a point by a solid
        /// </summary>
        /// <param name="pt0"></param>
        /// <returns></returns>
        public FilteredElementCollector GetElementCollectorAroundPoint(XYZ pt0)
        {
            //存放返回值的list
            IList<Element> list = new List<Element>();

            //"碰瓷"方块的边长
            double dBoxLength = 0.3;

            XYZ pt1 = new XYZ(pt0.X - dBoxLength / 2, pt0.Y - dBoxLength / 2, pt0.Z);
            XYZ pt2 = new XYZ(pt0.X + dBoxLength / 2, pt0.Y - dBoxLength / 2, pt0.Z);
            XYZ pt3 = new XYZ(pt0.X + dBoxLength / 2, pt0.Y + dBoxLength / 2, pt0.Z);
            XYZ pt4 = new XYZ(pt0.X - dBoxLength / 2, pt0.Y + dBoxLength / 2, pt0.Z);

            Line lineBottom = Line.CreateBound(pt1, pt2);
            Line lineRight = Line.CreateBound(pt2, pt3);
            Line lineTop = Line.CreateBound(pt3, pt4);
            Line lineLeft = Line.CreateBound(pt4, pt1);

            CurveLoop profile = new CurveLoop();
            profile.Append(lineBottom);
            profile.Append(lineRight);
            profile.Append(lineTop);
            profile.Append(lineLeft);

            List<CurveLoop> loops = new List<CurveLoop>();
            loops.Add(profile);

            //拉伸生成方块
            XYZ vector = new XYZ(0, 0, 1);//拉伸方向 
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, vector, dBoxLength);

            FilteredElementCollector collector = new FilteredElementCollector(DocSet.doc);
            ElementIntersectsSolidFilter solidFilter = new ElementIntersectsSolidFilter(solid);
            collector.WherePasses(solidFilter);

            return collector;
        }

        /// <summary>
        /// 小指"L"型房间专用的立面修改 
        /// the function of the "L" Room
        /// </summary>
        /// <param name="viewSection">the Elevation need to be change</param>
        /// <param name="elevationOffset"></param>
        /// <param name="normal">the normal vector of the curveloop of the Elevation's border</param>
        /// <param name="FloorThickness"></param>
        public void spRoomElevationChange(ViewSection viewSection, double elevationOffset, XYZ normal,double FloorThickness)
        {
            FilteredElementCollector collector = null;
            ViewCropRegionShapeManager vcrShanpMgr = viewSection.GetCropRegionShapeManager();
            //获取已经创建的默认立面的剖面框边界
            CurveLoop loop = vcrShanpMgr.GetCropShape().First();
            //TaskDialog.Show("!", "Is a spRoom!");
            //截面框四边遍历器
            CurveLoopIterator iterator = loop.GetCurveLoopIterator();

            Curve curve = null;//每条边界的曲线

            //准备用于创建曲线的点
            XYZ pt1 = null;
            XYZ pt2 = null;
            XYZ pt3 = null;
            XYZ pt4 = null;
            XYZ pt5 = null;

            //以房间中心为原点 东边为X正方向 北边为Y正方向
            //The location point of the room is the origin and the east is positive X and the north is positive Y
            int IsXLine = 1;//曲线是在x轴1 在y轴-1  if the curve on X axis set 1 ,if on Y axis set -1
            int IsAxisDirection = 1;//是轴正方向1 负方向-1 

            XYZ point1 = null;//截面框边起点
            XYZ point2 = null;//截面框边终点
            XYZ pointBottomCk = null;//与矮墙碰撞点
            XYZ pointBottom = null;//不与矮墙碰撞点
            bool isFindThePoint = false;//检测是否成功得到与矮墙碰撞点
            while (iterator.MoveNext())
            {
                //TaskDialog.Show("!", "22is a spRoom!");
                //每条边的两端点上升150后再进行"碰瓷检测"
                curve = iterator.Current;
                point1 = curve.GetEndPoint(0);
                point2 = curve.GetEndPoint(1);
                point1 = new XYZ(point1.X, point1.Y, point1.Z + FloorThickness / 350);//!!!!偷了点小懒!!!!
                point2 = new XYZ(point2.X, point2.Y, point2.Z + FloorThickness / 350);

                collector = GetElementCollectorAroundPoint(point1);
                foreach (Element elem in collector)
                {
                    Wall wall = elem as Wall;
                    //获取墙的高度约束，没有值的就是矮墙
                    //不等高且和小矮墙相交的框边为底边
                    if (wall == null || point1.Z != point2.Z) continue;//没得到墙时或得到的边是竖边时continue
                    if (wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsValueString() == "Unconnected"
                        || wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsValueString() == "未连接")
                    {
                        pointBottomCk = point1;
                        pointBottom = point2;
                        isFindThePoint = true;
                        break;
                    }
                }
                if (isFindThePoint) break;//找到所要的点后跳出while
                //TaskDialog.Show("!", "33is a spRoom!");
                collector = GetElementCollectorAroundPoint(point2);
                foreach (Element elem in collector)
                {
                    Wall wall = elem as Wall;
                    //不等高且和小矮墙相交的框边为底边
                    if (wall == null || point1.Z != point2.Z) continue;
                    if (wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsValueString() == "Unconnected"
                        || wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsValueString() == "未连接")
                    {
                        pointBottomCk = point2;
                        pointBottom = point1;
                        isFindThePoint = true;
                        break;
                    }
                }
                if (isFindThePoint) break;//找到所要的点后跳出while
            }

            if (pointBottomCk != null && pointBottom != null)
            {
                //TaskDialog.Show("!!", "pointCK：" + pointBottomCk.ToString() + "\npointB：" + pointBottom.ToString());

                pt2 = pointBottom;

                if (-0.1 < pointBottomCk.X - pointBottom.X && pointBottomCk.X - pointBottom.X < 0.1)
                    IsXLine = 0;
                if ((IsXLine == 1 && pointBottomCk.X - pointBottom.X < 0) || (IsXLine == 0 && pointBottomCk.Y - pointBottom.Y < 0))
                    IsAxisDirection = -1;
                //TaskDialog.Show("+-", "XLine:" + IsXLine.ToString() + "Axis:" + IsAxisDirection.ToString());

                pt1 = new XYZ(pointBottomCk.X + (2.5 * IsAxisDirection * IsXLine), pointBottomCk.Y + (2.5 * IsAxisDirection * (1 - IsXLine)), pointBottomCk.Z);
                pt2 = new XYZ(pt1.X, pt1.Y, pt1.Z + 11.5);
                pt3 = new XYZ(pt2.X - (3.35 * IsAxisDirection * IsXLine), pt2.Y - (3.4 * IsAxisDirection * (1 - IsXLine)), pt2.Z);
                pointBottom = new XYZ(pointBottom.X - (0 * IsAxisDirection * IsXLine), pointBottom.Y - (0 * IsAxisDirection * (1 - IsXLine)), pointBottom.Z);
                pt5 = new XYZ(pointBottom.X, pointBottom.Y, pointBottom.Z + 9.15);
                pt4 = new XYZ(pt3.X, pt3.Y, pt5.Z);

                //TaskDialog.Show("point", "pt2:" + pt2.ToString() + "\npt3:" + pt3.ToString());
                Line line1 = Line.CreateBound(pointBottom, pt1);
                Line line2 = Line.CreateBound(pt1, pt2);
                Line line3 = Line.CreateBound(pt2, pt3);
                Line line4 = Line.CreateBound(pt3, pt4);
                Line line5 = Line.CreateBound(pt4, pt5);
                Line line6 = Line.CreateBound(pt5, pointBottom);

                CurveLoop profile = new CurveLoop();
                profile.Append(line1);
                profile.Append(line2);
                profile.Append(line3);
                profile.Append(line4);
                profile.Append(line5);
                profile.Append(line6);

                //TaskDialog.Show("new Loop", "A new Loop has been Created");
                //profile = CurveLoop.CreateViaOffset(profile, elevationOffset / 300, normal);//!!!!偷了点小懒!!!!不同方向的L房间偏移结果可能相反，需要重新修改法向量改变
                vcrShanpMgr.SetCropShape(profile);
            }

            else
            {
                TaskDialog.Show("noooooooo!", "Cant find the CKpoint");
            }

        }

        /// <summary>
        /// 建立新的3D视图，并将其剖面框设为选择房间的大小
        /// create a new 3D view and change the section box fix the selected room
        /// </summary>
        /// <returns></returns>
        public View3D Create3DView()
        {
            using (Transaction tran = new Transaction(DocSet.doc))
            {
                var collector = new FilteredElementCollector(DocSet.doc).OfClass(typeof(ViewFamilyType));
                var viewFamilyTypes = from elem in collector
                                      let type = elem as ViewFamilyType
                                      where type.ViewFamily == ViewFamily.ThreeDimensional
                                      select type;

                tran.Start("Create A new 3Dview");
                View3D view = View3D.CreateIsometric(DocSet.doc, viewFamilyTypes.First().Id);

                //设定一个新的截面框
                BoundingBoxXYZ boxXyz = new BoundingBoxXYZ();
                XYZ max = DocSet.selRoom.get_BoundingBox(view).Max;
                XYZ min = DocSet.selRoom.get_BoundingBox(view).Min;
                boxXyz.Max = new XYZ(max.X + 2, max.Y + 2, max.Z);
                boxXyz.Min = new XYZ(min.X - 2, min.Y - 2, min.Z - 0.5);

                view.OrientTo(new XYZ (1,1,-1));//将3D视角转到左上
                view.SetSectionBox(boxXyz);

                string name = "ISO ";
                name += _SoANumber;
                view.ViewName = name;
                var par = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION).Set(DocSet.selRoom.Name + " - ISOMETRIC");
                tran.Commit();
                //view.SetOrientation(new ViewOrientation3D )

                return view;
            }
        }

        /// <summary>
        /// 调整viewPlan的尺寸并贴合选中房间 
        /// change a viewPlan to fix the selected room
        /// </summary>
        /// <param name="view"></param>
        /// <param name="tran"></param>
        /// <param name="viewOffseet"></param>
        public void ChangeViewFitRoom(ViewPlan view,Transaction tran,double viewOffseet)
        {
            if (view == null)
            {
                TaskDialog.Show("viewIsNull", "Can't find the type of View.");
                return;
            }

            //获得并设定房间的边界设定，并获取其边界集
            DocSet.uidoc.ActiveView = view;
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;//房间边界设定，能变更获取的边界位置
            IList<IList<BoundarySegment>> segments = DocSet.selRoom.GetBoundarySegments(opt);

            if (segments == null)
            {
                TaskDialog.Show("segementsIsNull", "can't get the BoundarySegment of room");
                return;
            }


            ViewCropRegionShapeManager vcrShanpMgr = view.GetCropRegionShapeManager();

            CurveLoop loop = new CurveLoop();
            foreach (IList<BoundarySegment> segmentList in segments)
            {
                foreach (BoundarySegment segment in segmentList)
                {

                    Curve curve = segment.GetCurve();
                    loop.Append(curve);
                }


                bool cropValid = vcrShanpMgr.IsCropRegionShapeValid(loop);
                if (cropValid)
                {
                    //默认矩形
                    //TaskDialog.Show("cropValid", "the crop is shape Valid");
                    tran.Start("change the view crop region");
                    vcrShanpMgr.SetCropShape(loop);
                    tran.Commit();
                    tran.Start("Remove Crop Region Shape");
                    vcrShanpMgr.RemoveCropRegionShape();
                    tran.Commit();
                    //TaskDialog.Show("ChangeView", "ChangeViewdone");
                    break;
                }
            }

            tran.Start("loop offset");
            //TaskDialog.Show("!!!", "changeloop!");
            loop = CurveLoop.CreateViaOffset(vcrShanpMgr.GetCropShape().First(), -1 * viewOffseet / 300, new XYZ(0, 0, 1));
            vcrShanpMgr.SetCropShape(loop);
            tran.Commit();

            DocSet.uidoc.ActiveView = view;
        }

        //
        public  void CreateSheetView(View3D view3D)
        {
            // Get an available title block from document
            FilteredElementCollector collector = new FilteredElementCollector(DocSet.doc);
            collector.OfClass(typeof(FamilySymbol));
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);

            FamilySymbol fs = collector.FirstElement() as FamilySymbol;
            if (fs == null)
            {
                TaskDialog.Show("1", "can not find fs");
                return;
            }
            using (Transaction t = new Transaction(DocSet.doc, "Create a new ViewSheet"))
            {
                t.Start();
                try
                {
                    // Create a sheet view
                    ViewSheet viewSheet = ViewSheet.Create(DocSet.doc, fs.Id);
                    if (null == viewSheet)
                    {
                        throw new Exception("Failed to create new ViewSheet.");
                    }

                    // Add passed in view onto the center of the sheet
                    UV location = new UV((viewSheet.Outline.Max.U - viewSheet.Outline.Min.U) / 2,
                                         (viewSheet.Outline.Max.V - viewSheet.Outline.Min.V) / 2);

                    //viewSheet.AddView(view3D, location);
                    Viewport.Create(DocSet.doc, viewSheet.Id, view3D.Id, new XYZ(location.U, location.V, 0));

                    viewSheet.Name = "123456adasqwe";
                    TaskDialog.Show("idsheet", viewSheet.Id.ToString());

                    t.Commit();
                }
                catch
                {
                    t.RollBack();
                }
            }
        }
    }
}
