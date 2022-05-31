﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class MatrixPresenter : UserControl
    {
        private static readonly Typeface s_font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private readonly FormattedText _formatOutput = new FormattedText("Output", Culture.Current, FlowDirection.LeftToRight, s_font, 10, Brushes.Black, Render.PixelsPerDip);
        private readonly FormattedText _formatInput = new FormattedText("Input", Culture.Current, FlowDirection.LeftToRight, s_font, 10, Brushes.Black, Render.PixelsPerDip);

        private readonly Dictionary<string, FormattedText> _classesFormatText = new Dictionary<string, FormattedText>();

        private readonly Pen _penSilver = Tools.Draw.GetPen(Colors.Silver);

        private List<string> _classes;

        public MatrixPresenter()
        {
            InitializeComponent();
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        bool IsClassesChanged(List<string> classes)
        {
            if (_classes == null || _classes.Count != classes.Count)
            {
                return true;
            }

            for (int c = 0; c < classes.Count; ++c)
            {
                if (classes[c] != _classes[c])
                {
                    return true;
                }
            }

            return false;
        }

        void InitClassesFmtText(List<string> classes)
        {
            _classesFormatText.Clear();
            for (int i = 0; i < classes.Count; ++i)
            {
                _classesFormatText[classes[i]] = new FormattedText(classes[i], Culture.Current, FlowDirection.LeftToRight, s_font, 7, Brushes.Black, Render.PixelsPerDip);
            }
        }

        public void DrawBase(ErrorMatrix matrix)
        {
            CtlBase.Clear();

            int size = 9;
            long axisOffset = 12;
 
            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    CtlBase.DrawRectangle(null, _penSilver, Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                }
            }

            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var text = _classesFormatText[matrix.Classes[x]];
                CtlBase.DrawText(text, Points.Get(axisOffset + x * size + (size - text.Width) / 2, 1 + axisOffset + matrix.Input.Length * size));
            }

            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var text = _classesFormatText[matrix.Classes[y]];
                CtlBase.DrawText(text, Points.Get(1 + axisOffset + matrix.Output.Length * size + (size - text.Width) / 2, axisOffset + y * size));
            }
        
            CtlBase.DrawText(_formatOutput, Points.Get(axisOffset + (matrix.Output.Length * size - _formatOutput.Width) / 2, axisOffset - _formatOutput.Height - 1));            
            CtlBase.DrawText(_formatInput, Points.Get(-axisOffset - (matrix.Input.Length * size - _formatInput.Width) / 1, axisOffset - _formatInput.Height - 1), -90);

            CtlBase.Update();
        }

        public void Draw(ErrorMatrix matrix)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (IsClassesChanged(matrix.Classes))
            {
                InitClassesFmtText(matrix.Classes);
                DrawBase(matrix);
            }

            _classes = matrix.Classes;
            CtlPresenter.Clear();
            
            int size = 9;
            long goodMax = 1;
            long badMax = 1;
            long axisOffset = 12;
            long bound = 30;

            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    if (x == y)
                    {
                        goodMax = Math.Max(goodMax, matrix.Matrix[x, y]);
                    }
                    else
                    {
                        badMax = Math.Max(badMax, matrix.Matrix[x, y]);
                    }
                }
            }

            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    if (matrix.Matrix[y, x] > 0)
                    {
                        var value = (double)matrix.Matrix[y, x] / (double)(x == y ? goodMax : badMax);
                        var color = Tools.Draw.GetColorDradient(Colors.LightGray, x == y ? Colors.Green : Colors.Red, 255, value);
                        var brush = Tools.Draw.GetBrush(color);
                        CtlPresenter.DrawRectangle(brush, _penSilver, Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                    }
                }
            }
        
            long outputMax = Math.Max(matrix.Output.Max(), 1);
            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, matrix.Output[x] > matrix.Input[x] ? Colors.Red : matrix.Output[x] < matrix.Input[x] ? Colors.Blue : Colors.Green, 100, (double)matrix.Output[x] / (double)outputMax);
                var brush = Tools.Draw.GetBrush(color);
                CtlPresenter.DrawRectangle(brush, _penSilver, Rects.Get(axisOffset + x * size, 10 + axisOffset + matrix.Input.Length * size, size, (int)(bound * (double)matrix.Output[x] / (double)outputMax)));
            }

            long inputMax = Math.Max(matrix.Input.Max(), 1);
            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, Colors.Green, 100, (double)matrix.Input[y] / (double)inputMax);
                var brush = Tools.Draw.GetBrush(color);
                CtlPresenter.DrawRectangle(brush, _penSilver, Rects.Get(11 + axisOffset + matrix.Output.Length * size, axisOffset + y * size, (int)(bound * (double)matrix.Input[y] / (double)inputMax), size));
            }

            CtlPresenter.Update();
        }

        public void Clear()
        {
            CtlPresenter.Clear();
        }
    }

    public class ErrorMatrix : ListNode<ErrorMatrix>
    {
        public long[] Input;
        public long[] Output;
        public long[,] Matrix;

        public List<string> Classes;

        public long Count { get; private set; }

        public ErrorMatrix(List<string> classes)
        {
            Classes = classes;
            var c = Classes.Count;

            Input = new long[c];
            Output = new long[c];
            Matrix = new long[c, c];
        }

        public void AddData(int input, int output)
        {
            ++Input[input];
            ++Output[output];
            ++Matrix[input, output];
            ++Count;
        }

        public void ClearData()
        {
            Array.Clear(Input, 0, Input.Length);
            Array.Clear(Output, 0, Output.Length);
            
            for (int y = 0; y < Input.Length; ++y)
            {
                for (int x = 0; x < Output.Length; ++x)
                {
                    Matrix[x, y] = 0;
                }
            }

            Count = 0;
        }
    }
}
