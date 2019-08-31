﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    using PointFunc = Func<DynamicStatistics.PlotPoints, DynamicStatistics.PlotPoint, long, Point>;

    public partial class PlotterPresenter : UserControl
    {
        int AxisOffset = 6;
        bool IsBaseRedrawNeeded;

        Typeface Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        public PlotterPresenter()
        {
            InitializeComponent();
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            CtlPresenter.SizeChanged += PlotterPresenter_SizeChanged;
        }

        private void PlotterPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsBaseRedrawNeeded = true;
        }

        public void Draw(List<NetworkDataModel> models, NetworkDataModel selectedModel)
        {
            var sw = Stopwatch.StartNew();

            if (IsBaseRedrawNeeded)
            {
                DrawPlotter();
                IsBaseRedrawNeeded = true;
            }

            CtlPresenter.Clear();

            foreach (var model in models)
            {
                if (!model.IsEnabled)
                {
                    continue;
                }

                Vanish(model.DynamicStatistics.PercentData, GetPointPercentData);
                Vanish(model.DynamicStatistics.CostData, GetPointCostData);

                DrawData(model.DynamicStatistics.PercentData, Tools.Draw.GetColor(220, model.Color), GetPointPercentData, false);
                DrawData(model.DynamicStatistics.CostData, Tools.Draw.GetColor(150, model.Color), GetPointCostData, true);
            }

            if (selectedModel != null && selectedModel.DynamicStatistics.PercentData.Count > 0)
            {
                DrawLabel(selectedModel.DynamicStatistics.PercentData, selectedModel.Color);
            }

            CtlPresenter.Update();

            sw.Stop();
            RenderTime.Plotter = sw.Elapsed.Ticks;
        }

        private void DrawPlotter()
        {
            CtlBase.Clear();

            var penBlack = Tools.Draw.GetPen(Colors.Black);
            var penLightGray = Tools.Draw.GetPen(Colors.LightGray);

            double step = (ActualWidth - AxisOffset) / 10;
            double y = ActualHeight - AxisOffset - AxisOffset / 2;
            double x = 0;
            for (x = 0; x < 11; ++x)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)(AxisOffset + step * x), (float)y), Points.Get((float)(AxisOffset + step * x), 0));
                CtlBase.DrawLine(penBlack, Points.Get((float)(AxisOffset + step * x), (float)y), Points.Get((float)(AxisOffset + step * x), (float)(y + AxisOffset)));
            }

            step = (ActualHeight - AxisOffset) / 10;
            x = AxisOffset / 2;
            for (y = 0; y < 11; ++y)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)x, (float)(ActualHeight - AxisOffset - step * y)), Points.Get(ActualWidth, (float)(ActualHeight - AxisOffset - step * y)));
                CtlBase.DrawLine(penBlack, Points.Get((float)x, (float)(ActualHeight - AxisOffset - step * y)), Points.Get((float)(x + AxisOffset), (float)(ActualHeight - AxisOffset - step * y)));
            }

            CtlBase.DrawLine(penBlack, Points.Get(AxisOffset, 0), Points.Get(AxisOffset, ActualHeight));
            CtlBase.DrawLine(penBlack, Points.Get(0, ActualHeight - AxisOffset), Points.Get(ActualWidth, ActualHeight - AxisOffset));
        }

        private void DrawData(DynamicStatistics.PlotPoints data, Color color, PointFunc func, bool isRect)
        {
            if (data == null || data.FirstOrDefault() == null)
            {
                return;
            }

            var pen = Tools.Draw.GetPen(color);
            var brush = Tools.Draw.GetBrush(color);
            
            var f = data[0];
            var l = data.Last();

            var ticks = l.Item2 - f.Item2;

            var prev = f;
            foreach (var p in data)
            {
                var pp = func(data, p, ticks);
                CtlPresenter.DrawLine(pen, func(data, prev, ticks), pp);

                if (isRect)
                {
                    CtlPresenter.DrawRectangle(brush, pen, Rects.Get(pp.X - 6 / 2, pp.Y - 6 / 2, 6, 6));
                }
                else
                {
                    CtlPresenter.DrawEllipse(brush, pen, Points.Get(pp.X, pp.Y), 7 / 2, 7 / 2);
                }

                prev = p;
            }
        }

        private void DrawLabel(DynamicStatistics.PlotPoints data, Color color)
        {     
            var text = new FormattedText(TimeSpan.FromTicks(data.Last().Item2 - data[0].Item2).ToString(@"hh\:mm\:ss") + " / " + Converter.DoubleToText(data.Last().Item1, "N6") + " %", Culture.Current, FlowDirection.LeftToRight, Font, 10, Tools.Draw.GetBrush(color), Render.PixelsPerDip);
            CtlPresenter.DrawText(text, Points.Get((ActualWidth - AxisOffset - text.Width) / 2, ActualHeight - AxisOffset - 20));
        }

        private Point GetPointPercentData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? AxisOffset : AxisOffset + (ActualWidth - AxisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - AxisOffset) * (1 - (point.Item1 / 100));

            return Points.Get((int)px, (int)py);
        }

        private Point GetPointCostData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? AxisOffset : AxisOffset + (ActualWidth - AxisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - AxisOffset) * (1 - Math.Min(1, point.Item1));

            return Points.Get((int)px, (int)py);
        }

        /*
        private void Vanish(DynamicStatistics.PlotPoints data, PointFunc func)
        {
            int vanishArea = 16;

            if (data.Count > 10)
            {
                var pointsToRemove = new List<DynamicStatistics.PlotPoint>();
                var totolTicks = data.Last().Item2 - data[0].Item2;

                for (int i = 0; i < data.Count * 0.8; ++i)
                {
                    var ticks = data.Last().Item2 - data[0].Item2;
                    var p0 = func(data, data[i], ticks);
                    var p1 = func(data, data[i + 1], ticks);
                    var p2 = func(data, data[i + 2], ticks);

                    if ((Math.Abs(p0.X - p2.X) < vanishArea && Math.Abs(p0.Y - p2.Y) < vanishArea) &&
                        (Math.Abs(p0.X - p1.X) < vanishArea && Math.Abs(p0.Y - p1.Y) < vanishArea) &&
                        (Math.Abs(p1.X - p2.X) < vanishArea && Math.Abs(p1.Y - p2.Y) < vanishArea))
                    {
                        pointsToRemove.Add(data[i + 1]);
                    }
                    else
                    {
                        if ((p0.X == p1.X && p1.X == p2.X && Math.Abs(p0.Y - p1.Y) < vanishArea) || (p0.Y == p1.Y && p1.Y == p2.Y && Math.Abs(p0.X - p1.X) < vanishArea))
                        {
                            pointsToRemove.Add(data[i + 1]);
                        }
                    }
                }

                pointsToRemove.ForEach(p => data.Remove(p));
            }
        }
        */

        private void Vanish(DynamicStatistics.PlotPoints data, PointFunc func)
        {
            const int vanishArea = 14;

            while (true)
            {
                if (data.Count <= 10)
                {
                    return;
                }

                var pointsToRemove = new List<DynamicStatistics.PlotPoint>();

                for (int i = 0; i < data.Count - 10; ++i)
                {
                    var ticks = data.Last().Item2 - data[0].Item2;
                    var p0 = func(data, data[i], ticks);
                    var p1 = func(data, data[i + 1], ticks);
                    var p2 = func(data, data[i + 2], ticks);

                    if (Math.Abs(Angle(p0, p1) - Angle(p1, p2)) < Math.PI / 720D) // 90
                    {
                        pointsToRemove.Add(data[i + 1]);
                    }
                    else
                    {
                        if (//Math.Abs(p0.X - p2.X) < vanishArea && Math.Abs(p0.Y - p2.Y) < vanishArea &&
                            Math.Abs(p0.X - p1.X) < vanishArea && Math.Abs(p0.Y - p1.Y) < vanishArea)// &&
                            //Math.Abs(p1.X - p2.X) < vanishArea && Math.Abs(p1.Y - p2.Y) < vanishArea)
                        {
                            pointsToRemove.Add(data[i + 1]);
                        }
                        else
                        {
                            if ((p0.X == p1.X && p1.X == p2.X && Math.Abs(p0.Y - p1.Y) < vanishArea) || (p0.Y == p1.Y && p1.Y == p2.Y && Math.Abs(p0.X - p1.X) < vanishArea))
                            {
                                pointsToRemove.Add(data[i + 1]);
                            }
                        }
                    }
                }

                if (!pointsToRemove.Any())
                {
                    return;
                }

                pointsToRemove.ForEach(p =>
                {
                    if (data.Count > 10)
                    {
                        data.Remove(p);
                    }
                    else
                    {
                        return;
                    }
                });
            }
        }

        private double Angle(Point p1, Point p2)
        {
            return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }
    }
}
