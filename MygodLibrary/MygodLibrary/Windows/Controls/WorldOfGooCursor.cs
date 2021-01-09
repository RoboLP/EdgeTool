namespace Mygod.Windows.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// 粘粘世界鼠标。
    /// </summary>
    public class WorldOfGooCursor : Decorator
    {
        static WorldOfGooCursor()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(WorldOfGooCursor), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// 初始化 WorldOfGooCursor 类的新实例。
        /// </summary>
        public WorldOfGooCursor()
        {
            refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(1 / 60.0), DispatcherPriority.Render,
                                               Refresh, Dispatcher);
            new Thread(Shrink) {IsBackground = true}.Start();
        }

        /// <summary>
        /// 前景色的附加属性。
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground",
            typeof(Color), typeof(WorldOfGooCursor), new PropertyMetadata(Colors.Black));
        /// <summary>
        /// 边框色的附加属性。
        /// </summary>
        public static readonly DependencyProperty BorderProperty = DependencyProperty.Register("Border",
            typeof(Color), typeof(WorldOfGooCursor), new PropertyMetadata(Color.FromRgb(0xb8, 0xb8, 0xb8)));
        /// <summary>
        /// 呼气后半径的附加属性。
        /// </summary>
        public static readonly DependencyProperty ExhaledRadiusProperty = DependencyProperty.Register("ExhaledRadius",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(9.0));
        /// <summary>
        /// 吸气后半径的附加属性。
        /// </summary>
        public static readonly DependencyProperty InhaledRadiusProperty = DependencyProperty.Register("InhaledRadius",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(10.0));
        /// <summary>
        /// 边框厚度的附加属性。
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register("BorderThickness",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(3.0));
        /// <summary>
        /// 呼吸时长的附加属性。
        /// </summary>
        public static readonly DependencyProperty BreathDurationProperty = DependencyProperty.Register("BreathDuration",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(20.0 / 9));
        /// <summary>
        /// 长度的附加属性。
        /// </summary>
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register("Length",
            typeof(int), typeof(WorldOfGooCursor), new PropertyMetadata(80));
        /// <summary>
        /// 收缩率的附加属性。
        /// </summary>
        public static readonly DependencyProperty ShrinkRateProperty = DependencyProperty.Register("ShrinkRate",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(200.0));
        /// <summary>
        /// 是否使用贝赛尔曲线的附加属性。
        /// </summary>
        public static readonly DependencyProperty UseBezierCurveProperty = DependencyProperty.Register("UseBezierCurve",
            typeof(bool), typeof(WorldOfGooCursor), new PropertyMetadata(true));
        /// <summary>
        /// 全屏显示的附加属性。
        /// </summary>
        public static readonly DependencyProperty FullscreenModeProperty = DependencyProperty.Register("Fullscreen",
            typeof(bool), typeof(WorldOfGooCursor), new PropertyMetadata(false, FullscreenModeChanged));
        /// <summary>
        /// 暂停移动的附加属性。
        /// </summary>
        public static readonly DependencyProperty PausedProperty = DependencyProperty.Register("Paused",
            typeof(bool), typeof(WorldOfGooCursor), new PropertyMetadata(false));

        /// <summary>
        /// 获取或设置前景色。
        /// </summary>
        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); } 
            set { SetValue(ForegroundProperty, value); }
        }
        /// <summary>
        /// 获取或设置边框色。
        /// </summary>
        public Color Border
        {
            get { return (Color)GetValue(BorderProperty); }
            set { SetValue(BorderProperty, value); }
        }
        /// <summary>
        /// 获取或设置呼气后的半径。
        /// </summary>
        public double ExhaledRadius
        {
            get { return (double)GetValue(ExhaledRadiusProperty); }
            set { SetValue(ExhaledRadiusProperty, value); }
        }
        /// <summary>
        /// 获取或设置吸气后的半径。
        /// </summary>
        public double InhaledRadius
        {
            get { return (double)GetValue(InhaledRadiusProperty); }
            set { SetValue(InhaledRadiusProperty, value); }
        }
        /// <summary>
        /// 获取或设置边框厚度。
        /// </summary>
        public double BorderThickness
        {
            get { return (double)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }
        /// <summary>
        /// 获取或设置呼吸间隔。
        /// </summary>
        public double BreathDuration
        {
            get { return (double)GetValue(BreathDurationProperty); }
            set { SetValue(BreathDurationProperty, value); }
        }
        /// <summary>
        /// 获取或设置长度。
        /// </summary>
        public int Length
        {
            get { return (int)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }
        /// <summary>
        /// 获取或设置收缩率。
        /// </summary>
        public double ShrinkRate
        {
            get { return (double)GetValue(ShrinkRateProperty); }
            set { SetValue(ShrinkRateProperty, value); }
        }
        /// <summary>
        /// 获取或设置是否使用贝赛尔曲线。
        /// </summary>
        public bool UseBezierCurve
        {
            get { return (bool)GetValue(UseBezierCurveProperty); }
            set { SetValue(UseBezierCurveProperty, value); }
        }
        /// <summary>
        /// 获取或设置是否在全屏模式下。在全屏模式下获取鼠标坐标的方法将被重写，而且不会自动刷新。
        /// </summary>
        public bool FullscreenMode
        {
            get { return (bool)GetValue(FullscreenModeProperty); }
            set { SetValue(FullscreenModeProperty, value); }
        }
        /// <summary>
        /// 获取或设置是否暂停鼠标。暂停后动画仍将继续但鼠标位置将不再改变。
        /// </summary>
        public bool Paused
        {
            get { return (bool) GetValue(PausedProperty); }
            set { SetValue(PausedProperty, value); }
        }

        private readonly LinkedList<Point> points = new LinkedList<Point>();
        private readonly DispatcherTimer refreshTimer;

        private static void FullscreenModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cursor = (WorldOfGooCursor) d;
            if ((bool) e.NewValue) cursor.refreshTimer.Stop();
            else cursor.refreshTimer.Start();
        }

        private void Shrink()
        {
            double shrinkRate = 400;
            while (true)
            {
                Dispatcher.Invoke(() =>
                {
                    var current = GetMousePoint();
                    if (points.Count > 0)
                    {
                        var last = points.Last.Value;
                        double disx = current.X - last.X, disy = current.Y - last.Y,
                               distance = Math.Sqrt(disx * disx + disy * disy);
                        var dis = (int) Math.Floor(distance);
                        for (var i = 0; i < dis; i++) AddPoint(last.X + disx / dis * i, last.Y + disy / dis * i);
                    }
                    AddPoint(current);
                    shrinkRate = ShrinkRate;
                });
                Thread.Sleep(TimeSpan.FromSeconds(1 / shrinkRate));
            }
        }

        private void Refresh(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private Point GetMousePoint()
        {
            if (Paused) return points.Count > 0 ? points.Last() : new Point(100, 100);
            if (!FullscreenMode) return Mouse.GetPosition(this);
            var point = System.Windows.Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void AddPoint(Point point)
        {
            var length = Length;
            if (UseBezierCurve && length > 1030) length = 1030;
            while (points.Count >= length) points.RemoveFirst();
            points.AddLast(point);
        }
        private void AddPoint(double x, double y)
        {
            AddPoint(new Point(x, y));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (points.Count == 0) return;
            var size = ((InhaledRadius - ExhaledRadius) * Math.Cos(2 * Math.PI * DateTime.Now.Ticks / 10000000 / BreathDuration)
                       + InhaledRadius + ExhaledRadius) / 2;
            var end = points.Last;
            var offset = 1;
            if (end.Previous != null)
                while (end.Previous.Previous != null && (points.Last.Value - end.Previous.Previous.Value).Length < 1)
                {
                    end = end.Previous;
                    offset++;
                }
            var pointOffset = new Point();
            if (FullscreenMode)
            {
                var window = this.FindVisualParent<Window>();
                pointOffset = new Point(window.Left, window.Top);
            }
            if (UseBezierCurve) BezierDraw(drawingContext, offset, size, pointOffset);
            else NormalDraw(drawingContext, end, offset, size, pointOffset);
        }

        private void NormalDraw(DrawingContext drawingContext, LinkedListNode<Point> end, int offset, double size, Point pointOffset)
        {
            var current = points.First;
            var i = 0;
            drawingContext.PushOpacity((double) Border.A / 256);
            var brush = new SolidColorBrush(Color.FromRgb(Border.R, Border.G, Border.B));
            if (BorderThickness > 0) while (current != null && current.Previous != end)
            {
                var radius = ((double)(i++ + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(brush, null, (Point) (current.Value - pointOffset),
                                           radius + BorderThickness, radius + BorderThickness);
                current = current.Next;
            }
            drawingContext.Pop();
            current = points.First;
            i = 0;
            drawingContext.PushOpacity((double) Foreground.A / 256);
            brush = new SolidColorBrush(Color.FromRgb(Foreground.R, Foreground.G, Foreground.B));
            while (current != null && current.Previous != end)
            {
                var radius = ((double)(i++ + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(brush, null, (Point)(current.Value - pointOffset), radius, radius);
                current = current.Next;
            }
            drawingContext.Pop();
        }

        private void BezierDraw(DrawingContext drawingContext, int offset, double size, Point pointOffset)
        {
            var curve = BezierCurve.Bezier2D(points.Take(points.Count - offset + 1).ToArray(), points.Count - offset + 1);
            if (BorderThickness > 0)
            {
                drawingContext.PushOpacity((double)Border.A / 256);
                var borderBrush = new SolidColorBrush(Color.FromRgb(Border.R, Border.G, Border.B));
                for (var i = 0; i < curve.Length; i++)
                {
                    var radius = ((double)(i + offset) / points.Count * 3 + 1) * size / 4;
                    drawingContext.DrawEllipse(borderBrush, null, (Point)(curve[i] - pointOffset),
                                               radius + BorderThickness, radius + BorderThickness);
                }
                drawingContext.Pop();
            }
            drawingContext.PushOpacity((double)Foreground.A / 256);
            var brush = new SolidColorBrush(Color.FromRgb(Foreground.R, Foreground.G, Foreground.B));
            for (var i = 0; i < curve.Length; i++)
            {
                var radius = ((double)(i + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(brush, null, (Point)(curve[i] - pointOffset), radius, radius);
            }
            drawingContext.Pop();
        }
    }

    static class BezierCurve
    {
        private static readonly List<List<BigInteger>> CombinationsLookup = new List<List<BigInteger>>();
        private static readonly List<List<double>> CombinationsResults = new List<List<double>>();

        private static double GetCombination(int n, int i)
        {
            // return Factorial(n) / (Factorial(i) * Factorial(n - i));
            if (i > n >> 1) i = n - i;
            while (n >= CombinationsLookup.Count)
            {
                CombinationsLookup.Add(new List<BigInteger> { 1 });
                CombinationsResults.Add(new List<double> { 1 });
            }
            var list = CombinationsLookup[n];
            var results = CombinationsResults[n];
            while (i >= list.Count)
            {
                list.Add(list[list.Count - 1] * (n - list.Count + 1) / list.Count);
                results.Add((double) list[list.Count - 1]);
            }
            return results[i];
        }

        // Calculate Bernstein basis
        private static double Bernstein(int n, int i, double t)
        {
            double ti; /* t^i */
            double tni; /* (1 - t)^i */
            /* Prevent problems with pow */
            if (Math.Abs(t) < 1e-4 && i == 0) ti = 1.0;
            else ti = Math.Pow(t, i);

            if (n == i && Math.Abs(t - 1.0) < 1e-4) tni = 1.0;
            else tni = Math.Pow((1 - t), (n - i));
            return GetCombination(n, i) * ti * tni;
        }

        public static Point[] Bezier2D(Point[] b, int cpts)
        {
            var p = new Point[cpts];
            double t = 0, step = 1.0 / (cpts - 1);
            for (var j = 0; j != cpts; j++)
            {
                if ((1.0 - t) < 5e-6) t = 1.0;
                p[j].X = 0.0;
                p[j].Y = 0.0;
                for (var i = 0; i != b.Length; i++)
                {
                    var basis = Bernstein(b.Length - 1, i, t);
                    p[j].X += basis * b[i].X;
                    p[j].Y += basis * b[i].Y;
                }
                t += step;
            }
            return p;
        }
    }
}
