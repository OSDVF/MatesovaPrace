using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;

//https://www.charlespetzold.com/blog/2012/11/The-Lesson-of-GetIntermediatePoints.html
namespace MatesovaPrace
{
    public sealed partial class SignatureDialog : ContentDialog
    {
        Dictionary<uint, Polyline> pointerDictionary = new Dictionary<uint, Polyline>();
        public SignatureDialog()
        {
            this.InitializeComponent();
        }

        void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // Get information from event arguments
            uint id = e.Pointer.PointerId;

            // If ID is in dictionary, add the points to the Polyline
            if (pointerDictionary.ContainsKey(id))
            {
                foreach (PointerPoint pointerPoint in  e.GetIntermediatePoints(sender as UIElement).Reverse())
                {
                    pointerDictionary[id].Points.Add(pointerPoint.Position);
                }
            }
        }

        void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Get information from event arguments
            uint id = e.Pointer.PointerId;
            Point point = e.GetCurrentPoint(sender as UIElement).Position;

            // Create Polyline
            Polyline polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1
            };
            polyline.Points.Add(point);

            // Add to Grid and dictionary
            SignCanvas.Children.Add(polyline);
            pointerDictionary.Add(id, polyline);
        }

        void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Get information from event arguments
            uint id = e.Pointer.PointerId;

            // If ID is in dictionary, remove it
            if (pointerDictionary.ContainsKey(id))
                pointerDictionary.Remove(id);
        }
    }
}
