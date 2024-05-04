using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Mygod.Edge.Tool.LibTwoTribes;
using Mygod.Edge.Tool.LibTwoTribes.Util;
using _3DTools;
using System.Collections.Generic;

namespace Mygod.Edge.Tool
{
    public sealed partial class ModelWindow
    {
        private string ModelName;

        public void SetModelName(string modelName)
        {
            this.ModelName = modelName;
        }

        Dictionary<string, Vector3D> offsets = new Dictionary<string, Vector3D>();
        public ModelWindow(string modelName)
        {
            ModelName = modelName;
            offsets.Add("platform", new Vector3D(0, -1, 0));
            offsets.Add("platform_active", new Vector3D(0, -1, 0));
            offsets.Add("platform_edges_active", new Vector3D(0, -1, 0));
            offsets.Add("platform_small", new Vector3D(0, -1, 0));
            offsets.Add("platform_active_small", new Vector3D(0, -1, 0));
            offsets.Add("platform_edges_active_small", new Vector3D(0, -1, 0));
            offsets.Add("switch", new Vector3D(0, -0.5, 0));
            offsets.Add("switch_ghost", new Vector3D(0, -0.5, 0));
            offsets.Add("prism_shadow", new Vector3D(0, 0.71, 0));
            offsets.Add("shrinker_tobig", new Vector3D(0, 0.5, 0));
            offsets.Add("shrinker_tomini", new Vector3D(0, 0.5, 0));

            InitializeComponent();
        }

        public bool DrawChildModels, DebugMode;

        public void Draw(string path, Matrix3D? parentMatrix = null)
        {
            ESO eso;
            do
            {
                var matrix = GetMatrix(eso = ESO.FromFile(path));
                if (parentMatrix.HasValue) matrix *= parentMatrix.Value;
                foreach (var model in eso.Models)
                {
                    BitmapImage image;
                    var material = new DiffuseMaterial(Brushes.White);
                    var geom = new MeshGeometry3D
                    {
                        Positions = new Point3DCollection(model.Vertices.Select(AssetHelper.ConvertVertex)),
                        Normals = new Vector3DCollection(model.Normals.Select(AssetHelper.ConvertVector))
                    };

                    var ema = EMA.FromFile(Path.Combine(MainWindow.Edge.ModelsDirectory,
                                                        model.MaterialAsset + ".ema"));
                    if (ema.Textures.Length > 0 && model.HasTexCoords)
                    {
                        geom.TextureCoordinates = new PointCollection(model.TexCoords.Select(ConvertTexCoord));
                        var etx = ETX.FromFile(Path.Combine(MainWindow.Edge.TexturesDirectory,
                                                            ema.Textures[0].Asset + ".etx"));
                        image = etx.GetBitmap().GetBitmapImage();
                        Viewport2DVisual3D.SetIsVisualHostMaterial(material, true);
                    }
                    else image = new BitmapImage();
                    if (!parentMatrix.HasValue && model.HasColors)
                        MessageBox.Show(this, Localization.ModelColorWarning + Environment.NewLine + Localization.ModelColorWarningDetails,
                                        Localization.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    for (var i = model.Vertices.Count - 1; i >= 0; i--) geom.TriangleIndices.Add(i);
                    var transform = new MatrixTransform3D(matrix);
                    Model.Children.Add(new Viewport2DVisual3D
                    { Geometry = geom, Material = material,
                        Visual = new Image { Source = image }, Transform = transform });
                    if (!DebugMode) continue;
                    var lines = new ScreenSpaceLines3D { Color = Colors.Red, Transform = transform };
                    Model.Children.Add(lines);
                    var k = 0;
                    while (k < model.Vertices.Count)
                    {
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k]));
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k + 1]));
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k + 1]));
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k + 2]));
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k + 2]));
                        lines.Points.Add(AssetHelper.ConvertVertex(model.Vertices[k]));
                        k += 3;
                    }
                }
                if (!DrawChildModels)
                {
                    return;
                }

                if (!eso.Header.NodeChild.IsZero())
                {
                    Draw(Path.Combine(Path.GetDirectoryName(path), eso.Header.NodeChild + ".eso"), matrix);
                }

                path = Path.Combine(Path.GetDirectoryName(path), eso.Header.NodeSibling + ".eso");
            }
            while (!eso.Header.NodeSibling.IsZero());
        }

        public void DrawElement(string name, Point3D16 pos, Matrix3D? parentMatrix = null)
        {
            string path = Path.Combine(MainWindow.Edge.ModelsDirectory, AssetUtil.CrcFullName(name, "models", false) + ".eso");

            ESO eso;

            Vector3D offset = new Vector3D(pos.X + 0.5f, pos.Z + 0.5f, pos.Y + 0.5f);
            if (offsets.ContainsKey(name)) {
                offset += offsets[name];
            }

            do
            {
                Matrix3D matrix = GetMatrixAt(eso = ESO.FromFile(path), new Point3D(offset.X, offset.Y, offset.Z));
                if (parentMatrix.HasValue)
                {
                    matrix *= parentMatrix.Value;
                }
                //matrix.Translate(offset);

                foreach (ESOModel model in eso.Models)
                {
                    BitmapImage image;
                    DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(new Color() { A = 255, R = 128, G = 128, B = 128}));

                    MeshGeometry3D geom = new MeshGeometry3D
                    {
                        Positions = new Point3DCollection(model.Vertices.Select(AssetHelper.ConvertVertex)),
                        Normals = new Vector3DCollection(model.Normals.Select(AssetHelper.ConvertVector))
                    };

                    EMA ema = EMA.FromFile(Path.Combine(MainWindow.Edge.ModelsDirectory, model.MaterialAsset + ".ema"));
                    if (ema.Textures.Length > 0 && model.HasTexCoords)
                    {
                        geom.TextureCoordinates = new PointCollection(model.TexCoords.Select(ConvertTexCoord));
                        ETX etx = ETX.FromFile(Path.Combine(MainWindow.Edge.TexturesDirectory,
                                                            ema.Textures[0].Asset + ".etx"));
                        image = etx.GetBitmap().GetBitmapImage();
                        Viewport2DVisual3D.SetIsVisualHostMaterial(material, true);
                    }
                    else
                    {
                        image = new BitmapImage();
                    }

                    for (int i = model.Vertices.Count - 1; i >= 0; i--)
                    {
                        geom.TriangleIndices.Add(i);
                    }

                    MatrixTransform3D transform = new MatrixTransform3D(matrix);
                    Model.Children.Add(new Viewport2DVisual3D
                    {
                        Geometry = geom,
                        Material = material,
                        Visual = new Image { Source = image },
                        Transform = transform
                    });
                }

                if (!eso.Header.NodeChild.IsZero())
                {
                    Draw(Path.Combine(Path.GetDirectoryName(path), eso.Header.NodeChild + ".eso"), matrix);
                }

                path = Path.Combine(Path.GetDirectoryName(path), eso.Header.NodeSibling + ".eso");
            }
            while (!eso.Header.NodeSibling.IsZero());
        }

        private static Matrix3D GetMatrix(ESO eso)
        {
            var matrix = new Matrix3D();
            matrix.Scale(AssetHelper.ConvertVector(eso.Header.Scale));
            matrix.Scale(new Vector3D(eso.Header.ScaleXYZ, eso.Header.ScaleXYZ, eso.Header.ScaleXYZ));
            matrix.Rotate(new Quaternion(new Vector3D(1, 0, 0), eso.Header.Rotate.X * AssetHelper.ToDegree));
            matrix.Rotate(new Quaternion(new Vector3D(0, 1, 0), eso.Header.Rotate.Y * AssetHelper.ToDegree));
            matrix.Rotate(new Quaternion(new Vector3D(0, 0, 1), eso.Header.Rotate.Z * AssetHelper.ToDegree));
            matrix.Translate(AssetHelper.ConvertVector(eso.Header.Translate));
            return matrix;
        }

        private static Matrix3D GetMatrixAt(ESO eso, Point3D pos)
        {
            var matrix = new Matrix3D();
            matrix.Translate(AssetHelper.ConvertVector(eso.Header.Translate) + new Vector3D(pos.X, pos.Y, pos.Z));
            matrix.ScaleAt(AssetHelper.ConvertVector(eso.Header.Scale), pos);
            matrix.ScaleAt(new Vector3D(eso.Header.ScaleXYZ, eso.Header.ScaleXYZ, eso.Header.ScaleXYZ), pos);
            matrix.RotateAt(new Quaternion(new Vector3D(1, 0, 0), eso.Header.Rotate.X * AssetHelper.ToDegree), pos);
            matrix.RotateAt(new Quaternion(new Vector3D(0, 1, 0), eso.Header.Rotate.Y * AssetHelper.ToDegree), pos);
            matrix.RotateAt(new Quaternion(new Vector3D(0, 0, 1), eso.Header.Rotate.Z * AssetHelper.ToDegree), pos);
            return matrix;
        }

        public void ApplyAnimation(string path, bool loop = true)
        {
            var ean = EAN.FromFile(path);
            var transforms = new Transform3DGroup();
            AxisAngleRotation3D x, y, z;
            transforms.Children.Add(new ScaleTransform3D(ean.BlockScaleX.DefaultValue, ean.BlockScaleY.DefaultValue,
                                                         ean.BlockScaleZ.DefaultValue));
            transforms.Children.Add(new RotateTransform3D(
                z = new AxisAngleRotation3D(new Vector3D(0, 0, 1), ean.BlockRotateZ.DefaultValue * ToDegree)));
            transforms.Children.Add(new RotateTransform3D(
                y = new AxisAngleRotation3D(new Vector3D(0, 1, 0), ean.BlockRotateY.DefaultValue * ToDegree)));
            transforms.Children.Add(new RotateTransform3D(
                x = new AxisAngleRotation3D(new Vector3D(1, 0, 0), ean.BlockRotateX.DefaultValue * ToDegree)));
            transforms.Children.Add(new TranslateTransform3D(ean.BlockTranslateX.DefaultValue,
                                                             ean.BlockTranslateY.DefaultValue,
                                                             ean.BlockTranslateZ.DefaultValue));
            //foreach (var child in Model.Children) child.Transform = transforms;
            Model.Children[Model.Children.Count - 1].Transform = transforms;
            var repeatBehavior = loop ? RepeatBehavior.Forever : new RepeatBehavior(1);
            transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleZProperty,
                                                  GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleZ));
            transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleYProperty,
                                                  GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleY));
            transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleXProperty,
                                                  GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleX));
            z.BeginAnimation(AxisAngleRotation3D.AngleProperty,
                             GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateZ, ToDegree));
            y.BeginAnimation(AxisAngleRotation3D.AngleProperty,
                             GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateY, ToDegree));
            x.BeginAnimation(AxisAngleRotation3D.AngleProperty,
                             GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateX, ToDegree));
            transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetZProperty,
                GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateZ));
            transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetYProperty,
                GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateY));
            transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetXProperty,
                GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateX));
        }

        public void ApplyAnimationToElement(string name)
        {
            string path;
            if (File.Exists(name))
            {
                path = name;
            }
            else
            {
                path = Path.Combine(MainWindow.Edge.ModelsDirectory, AssetUtil.CrcFullName(name, "models", false) + ".ean"); // maybe also look for children
            }

            EAN ean;
            do
            {
                ean = EAN.FromFile(path);

                Matrix3D last = Model.Children[Model.Children.Count - 1].Transform.Value;
                Point3D c = new Point3D(last.OffsetX, last.OffsetY, last.OffsetZ);
                Transform3DGroup transforms = new Transform3DGroup();
                AxisAngleRotation3D x = new AxisAngleRotation3D(new Vector3D(1, 0, 0), ean.BlockRotateX.DefaultValue * ToDegree);
                AxisAngleRotation3D y = new AxisAngleRotation3D(new Vector3D(0, 1, 0), ean.BlockRotateY.DefaultValue * ToDegree);
                AxisAngleRotation3D z = new AxisAngleRotation3D(new Vector3D(0, 0, 1), ean.BlockRotateZ.DefaultValue * ToDegree);
                //if (name.Equals("prism_shadow"))
                //{
                //    transforms.Children.Add(new ScaleTransform3D(new Vector3D(1, 0.01, 1), new Point3D(pos.X, pos.Z, pos.Y)));
                //}
                transforms.Children.Add(new ScaleTransform3D(new Vector3D(ean.BlockScaleX.DefaultValue, ean.BlockScaleY.DefaultValue, ean.BlockScaleZ.DefaultValue)));
                transforms.Children.Add(new RotateTransform3D(z));
                transforms.Children.Add(new RotateTransform3D(y));
                transforms.Children.Add(new RotateTransform3D(x));
                transforms.Children.Add(new TranslateTransform3D(ean.BlockTranslateX.DefaultValue, ean.BlockTranslateY.DefaultValue, ean.BlockTranslateZ.DefaultValue));
                transforms.Children.Add(new TranslateTransform3D(c.X, c.Y, c.Z));

                //transforms.Children.Add(Model.Children[Model.Children.Count - 1].Transform);
                Model.Children[Model.Children.Count - 1].Transform = transforms;

                RepeatBehavior repeatBehavior = RepeatBehavior.Forever;
                transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleZProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleZ));
                transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleYProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleY));
                transforms.Children[0].BeginAnimation(ScaleTransform3D.ScaleXProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockScaleX));
                z.BeginAnimation(AxisAngleRotation3D.AngleProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateZ, ToDegree));
                y.BeginAnimation(AxisAngleRotation3D.AngleProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateY, ToDegree));
                x.BeginAnimation(AxisAngleRotation3D.AngleProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockRotateX, ToDegree));
                transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetZProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateZ));
                transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetYProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateY));
                transforms.Children[4].BeginAnimation(TranslateTransform3D.OffsetXProperty, GetAnimation(repeatBehavior, ean.Header.Duration, ean.BlockTranslateX));

                if (!ean.Header.NodeChild.IsZero())
                {
                    ApplyAnimationToElement(Path.Combine(Path.GetDirectoryName(path), ean.Header.NodeChild + ".ean"));
                }

                path = Path.Combine(Path.GetDirectoryName(path), ean.Header.NodeSibling + ".ean");
            } while (!ean.Header.NodeSibling.IsZero());
            
        }

        public void AnimateMovingPlatform(MovingPlatform p)
        {
            Transform3DGroup transforms = new Transform3DGroup();
            TranslateTransform3D transform = new TranslateTransform3D();

            transforms.Children.Add(Model.Children[Model.Children.Count - 1].Transform);
            transforms.Children.Add(transform);

            // Repeating animation (everything after LoopStartIndex)
            RepeatBehavior behavior = p.LoopStartIndex == 0 ? new RepeatBehavior(1) : RepeatBehavior.Forever;
            DoubleAnimationUsingKeyFrames animationX = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = behavior };
            DoubleAnimationUsingKeyFrames animationY = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = behavior };
            DoubleAnimationUsingKeyFrames animationZ = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = behavior };

            Point3D16 start = p.Waypoints[0].Position;
            TimeSpan timer = TimeSpan.Zero;
            for (int i = p.LoopStartIndex == 0 ? 0 : p.LoopStartIndex - 1; i < p.Waypoints.Count; i++)
            {
                Waypoint w = p.Waypoints[i];
                Point3D16 pos = w.Position - start;

                timer += TimeSpan.FromSeconds(w.TravelTime / 30D);
                animationX.KeyFrames.Add(new LinearDoubleKeyFrame(pos.X, timer));
                animationY.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Z, timer));
                animationZ.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Y, timer));

                timer += TimeSpan.FromSeconds(w.PauseTime / 30D);
                animationX.KeyFrames.Add(new LinearDoubleKeyFrame(pos.X, timer));
                animationY.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Z, timer));
                animationZ.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Y, timer));
            }

            // Non-repeating start animation (everything before LoopStartIndex)
            if (p.LoopStartIndex > 1)
            {
                DoubleAnimationUsingKeyFrames startX = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = new RepeatBehavior(1) };
                DoubleAnimationUsingKeyFrames startY = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = new RepeatBehavior(1) };
                DoubleAnimationUsingKeyFrames startZ = new DoubleAnimationUsingKeyFrames() { RepeatBehavior = new RepeatBehavior(1) };

                timer = TimeSpan.Zero;
                for (int i = 0; i < p.LoopStartIndex - 1; i++)
                {
                    Waypoint w = p.Waypoints[i];
                    Point3D16 pos = w.Position - start;

                    timer += TimeSpan.FromSeconds(w.TravelTime / 30D);
                    startX.KeyFrames.Add(new LinearDoubleKeyFrame(pos.X, timer));
                    startY.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Z, timer));
                    startZ.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Y, timer));

                    timer += TimeSpan.FromSeconds(w.PauseTime / 30D);
                    startX.KeyFrames.Add(new LinearDoubleKeyFrame(pos.X, timer));
                    startY.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Z, timer));
                    startZ.KeyFrames.Add(new LinearDoubleKeyFrame(pos.Y, timer));
                }

                animationX.BeginTime = timer;
                animationY.BeginTime = timer;
                animationZ.BeginTime = timer;

                transform.BeginAnimation(TranslateTransform3D.OffsetXProperty, startX);
                transform.BeginAnimation(TranslateTransform3D.OffsetYProperty, startY);
                transform.BeginAnimation(TranslateTransform3D.OffsetZProperty, startZ);
            }

            transform.BeginAnimation(TranslateTransform3D.OffsetXProperty, animationX);
            transform.BeginAnimation(TranslateTransform3D.OffsetYProperty, animationY);
            transform.BeginAnimation(TranslateTransform3D.OffsetZProperty, animationZ);

            Model.Children[Model.Children.Count - 1].Transform = transforms;
        }

        private static TwoTribesAnimation GetAnimation(RepeatBehavior repeatBehavior, float duration,
                                                                  KeyframeBlock block, double k = 1)
        {
            //var animation = new DoubleAnimationUsingKeyFrames
            //    { RepeatBehavior = repeatBehavior, Duration = new Duration(TimeSpan.FromSeconds(duration / 30.0)) };
            //animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(block.DefaultValue * k, TimeSpan.Zero));
            //foreach (var keyframe in block.Keyframes) animation.KeyFrames.Add(
            //    new LinearDoubleKeyFrame(keyframe.Value * k, TimeSpan.FromSeconds(keyframe.Time / 30.0)));
            //return animation;
            return new TwoTribesAnimation(block, k)
            { RepeatBehavior = repeatBehavior, Duration = new Duration(TimeSpan.FromSeconds(duration / 30D)) };
        }

        private const double ToDegree = 180 / Math.PI;

        private static Point ConvertTexCoord(Vec2 vec)
        {
            double x = Math.Abs(vec.X - 1) > 1e-4 ? vec.X % 1 : 1, y = Math.Abs(vec.Y - 1) > 1e-4 ? vec.Y % 1 : 1;
            return new Point(x < 0 ? x + 1 : x, y < 0 ? y + 1 : y);
        }

        private bool dragging;
        private Point mouseDown;
        private Point3D mouseDownPosition;

        private void StartDragging(object sender, MouseButtonEventArgs e)
        {
            Grid.CaptureMouse();
            dragging = true;
            mouseDown = e.GetPosition(Grid);
            mouseDownPosition = Camera.Position;
        }

        private void Dragging(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                return;
            }

            Point position = e.GetPosition(Grid);
            double deltaX = mouseDown.X - position.X, deltaY = mouseDown.Y - position.Y;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Camera.Position = new Point3D(mouseDownPosition.X + (deltaX + deltaY) * Camera.FieldOfView / 450,
                                              mouseDownPosition.Y,
                                              mouseDownPosition.Z + (deltaY - deltaX) * Camera.FieldOfView / 450);
            }
            else if (e.RightButton == MouseButtonState.Pressed) Camera.Position =
                new Point3D(mouseDownPosition.X, mouseDownPosition.Y - deltaY * Camera.FieldOfView / 450, mouseDownPosition.Z);
        }

        private void StopDragging(object sender, MouseButtonEventArgs e)
        {
            Grid.ReleaseMouseCapture();
            Dragging(sender, e);
            dragging = false;
        }

        private void Zoom(object sender, MouseWheelEventArgs e)
        {
            Camera.FieldOfView -= (double) e.Delta / Mouse.MouseWheelDeltaForOneLine;
        }

        private void SaveAsImage(object sender, RoutedEventArgs e)
        {
            RenderTargetBitmap bmp = new RenderTargetBitmap((int) View.ActualWidth, (int) View.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(View);
            BitmapFrame frame = BitmapFrame.Create(bmp);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);

            using (var stream = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", this.ModelName + ".png")))
            {
                encoder.Save(stream);
            }
        }

        public void Clear(object sender, RoutedEventArgs e)
        {
            Model.Children.Clear();
        }

        private void ModelWindowClosed(object sender, EventArgs e)
        {
            MainWindow.ModelWindow = null;
        }
    }

    [ValueConversion(typeof(Color), typeof(SolidColorBrush))]
    public sealed class SolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Color)) return null;
            return new SolidColorBrush((Color) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            return brush?.Color;
        }
    }
}
