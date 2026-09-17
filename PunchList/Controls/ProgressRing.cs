using System;
using System.Windows;
using System.Windows.Media;

namespace PunchList.Controls
{
    /// <summary>
    /// Lightweight, hardware-accelerated circular progress ring.
    /// Track is drawn using TrackBrush (default subtle border grey).
    /// Progress arc is drawn using IndicatorBrush (accent color).
    /// </summary>
    public class ProgressRing : FrameworkElement
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(
                nameof(Progress),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(
                nameof(TrackBrush),
                typeof(Brush),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IndicatorBrushProperty =
            DependencyProperty.Register(
                nameof(IndicatorBrush),
                typeof(Brush),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public Brush IndicatorBrush
        {
            get => (Brush)GetValue(IndicatorBrushProperty);
            set => SetValue(IndicatorBrushProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= StrokeThickness || size <= 0) return;

            double radius = (size - StrokeThickness) / 2.0;
            Point center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);

            // 1. Draw track circle (subtle grey #2A2A2E)
            if (TrackBrush != null)
            {
                var trackPen = new Pen(TrackBrush, StrokeThickness);
                trackPen.Freeze();
                dc.DrawEllipse(null, trackPen, center, radius, radius);
            }

            // 2. Draw progress arc (accent color #6E7AA8)
            double progress = Math.Clamp(Progress, 0.0, 1.0);
            if (progress <= 0.005 || IndicatorBrush == null) return;

            var indicatorPen = new Pen(IndicatorBrush, StrokeThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            indicatorPen.Freeze();

            if (progress >= 0.995)
            {
                // Full circle
                dc.DrawEllipse(null, indicatorPen, center, radius, radius);
                return;
            }

            // Draw arc from -90 deg (12 o'clock) clockwise
            double sweepAngle = progress * 360.0;
            double startAngle = -90.0;
            double endAngle = startAngle + sweepAngle;

            double radStart = startAngle * Math.PI / 180.0;
            double radEnd = endAngle * Math.PI / 180.0;

            Point startPoint = new Point(center.X + radius * Math.Cos(radStart), center.Y + radius * Math.Sin(radStart));
            Point endPoint = new Point(center.X + radius * Math.Cos(radEnd), center.Y + radius * Math.Sin(radEnd));

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(startPoint, false, false);
                ctx.ArcTo(endPoint, new Size(radius, radius), 0, sweepAngle > 180.0, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();

            dc.DrawGeometry(null, indicatorPen, geometry);
        }
    }
}
