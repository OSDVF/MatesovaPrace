using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using System.Diagnostics;
using MatesovaPrace.Models;
using Android.Views;

//https://www.charlespetzold.com/blog/2012/11/The-Lesson-of-GetIntermediatePoints.html
namespace MatesovaPrace
{
    public sealed partial class SignatureDialog : ContentDialog
    {
        Polyline currentPoints;
        PersonModel _model;
        public SignatureDialog()
        {
            this.InitializeComponent();
            DataContextChanged += SignatureDialog_DataContextChanged;
            _model = DataContext as PersonModel;
            Closing += DialogClosingEvent;
        }

        private void SignatureDialog_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            foreach (var child in SignCanvas.Children)
            {
                if (child is Polyline)
                {
                    SignCanvas.Children.Remove(child);
                }
            }
            _model = DataContext as PersonModel;
        }

        void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_model.Signature != null)
            {
                return;
            }
            try
            {
                // Get information from event arguments
                uint id = e.Pointer.PointerId;


                foreach (PointerPoint pointerPoint in e.GetIntermediatePoints(SignCanvas).Reverse())
                {
                    var point = pointerPoint.Position;
                    if (point.X > 0 && point.X < SignCanvas.ActualWidth && point.Y > 0 && point.Y < SignCanvas.ActualHeight)
                    {
                        currentPoints.Points.Add(point);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void DialogClosingEvent(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (args.Result == ContentDialogResult.Secondary)//Clear canvas button
            {
                args.Cancel = true;
                foreach (var child in SignCanvas.Children)
                {
                    if (child is Polyline)
                    {
                        SignCanvas.Children.Remove(child);
                    }
                }
            }
        }

        void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_model.Signature != null)
            {
                return;
            }
            try
            {
                // Get information from event arguments
                uint id = e.Pointer.PointerId;
                Point point = e.GetCurrentPoint(SignCanvas).Position;
                if (point.X > 0 && point.X < SignCanvas.ActualWidth && point.Y > 0 && point.Y < SignCanvas.ActualHeight)
                {
                    // Create Polyline
                    currentPoints = new()
                    {
                        Stroke = new SolidColorBrush(Colors.Black),
                        StrokeThickness = 2
                    };
                    currentPoints.Points.Add(point);

                    // Add to Grid and dictionary
                    SignCanvas.Children.Add(currentPoints);
                    SignCanvas.CapturePointer(e.Pointer);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_model.Signature != null)
            {
                return;
            }
            try
            {
                uint id = e.Pointer.PointerId;
                Point point = e.GetCurrentPoint(SignCanvas).Position;
                if (point.X > 0 && point.X < SignCanvas.ActualWidth && point.Y > 0 && point.Y < SignCanvas.ActualHeight)
                {
                    currentPoints = null;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            var canvasPos = SignCanvas.TransformToVisual(this).TransformPoint(new Point(0, 0));
            var dpi = Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density;
            var point = new Point(ev.GetX() / dpi, ev.GetY() / dpi) - canvasPos;
            if (point.X > 0 && point.X < SignCanvas.ActualWidth && point.Y > 0 && point.Y < SignCanvas.ActualHeight)
            {
                switch (ev.ActionMasked)
                {
                    case MotionEventActions.Move:
                        currentPoints.Points.Add(point);
                        break;
                    case MotionEventActions.Up:
                        currentPoints = null;
                        break;
                }
            }
            return base.OnInterceptTouchEvent(ev);
        }
    }
}
