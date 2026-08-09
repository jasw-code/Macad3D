#load "_Common.csx"

#r "PresentationCore"
#r "PresentationFramework"
#r "System.Xaml"
#r "WindowsBase"

using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using System.IO;
using System.Windows.Media;
using System.Windows.Controls;

int windowWidth = 800;
int windowHeight = 600;

var count = Args.Count();

if (count == 0)
{
	Printer.Line("Usage: ViewXaml <xaml_file>");
	return -1;
}

try
{
	var path = Args[0];

	var staThread = new Thread(() => _ViewXaml(path));
	staThread.SetApartmentState(ApartmentState.STA);
	staThread.Start();
	staThread.Join();
}
catch (Exception e)
{
	Printer.Error(e.Message);
	return -1;
}
return 0;


/***************************************************************/

void _ViewXaml(string path)
{
	if (!File.Exists(path))
	{
		Printer.Error($"XAML file not found: {path}");
		return;
	}

	try
	{
		using (var fileStream = File.OpenRead(path))
		{
			var drawing = (DrawingGroup)XamlReader.Load(fileStream);

			// Create and run WPF application
			var app = new Application();

			var window = new Window
			{
				Title = $"XAML Viewer - {Path.GetFileName(path)}",
				Width = windowWidth,
				Height = windowHeight,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Background = System.Windows.Media.Brushes.White
			};

			// Create DrawingImage for the DrawingGroup
			var drawingImage = new Image
			{
				Source = new DrawingImage(drawing),
				Stretch = Stretch.Uniform
			};

			// Transform group for Zoom and Pan
			var scaleTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
			var translateTransform = new System.Windows.Media.TranslateTransform(0, 0);
			var transformGroup = new System.Windows.Media.TransformGroup();
			transformGroup.Children.Add(scaleTransform);
			transformGroup.Children.Add(translateTransform);
			drawingImage.RenderTransform = transformGroup;

			// Canvas for Zoom/Pan
			var canvas = new Canvas
			{
				Background = System.Windows.Media.Brushes.White,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			canvas.Children.Add(drawingImage);

			// Pan variables
			bool isPanning = false;
			Point panStart = new Point(0, 0);
			double currentTranslateX = 0;
			double currentTranslateY = 0;
			double currentZoom = 1.0;
			double initialZoom = 1.0;
			double initialTranslateX = 0;
			double initialTranslateY = 0;

			// Calculate and apply Zoom-to-Fit
			Action ApplyZoomToFit = () =>
			{
				var bounds = drawing.Bounds;
				double drawingWidth = bounds.Width > 0 ? bounds.Width : 400;
				double drawingHeight = bounds.Height > 0 ? bounds.Height : 300;

				double availableWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : window.ActualWidth;
				double availableHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : window.ActualHeight;

				double scaleX = availableWidth / drawingWidth;
				double scaleY = availableHeight / drawingHeight;
				double zoom = Math.Min(scaleX, scaleY) * 0.95; // 95% für etwas Rand

				if (zoom > 0)
				{
					currentZoom = zoom;

					// Center the drawing in the canvas using Canvas position
					double scaledWidth = drawingWidth * zoom;
					double scaledHeight = drawingHeight * zoom;
					double canvasLeft = (availableWidth - scaledWidth) / 2;
					double canvasTop = (availableHeight - scaledHeight) / 2;

					Canvas.SetLeft(drawingImage, canvasLeft);
					Canvas.SetTop(drawingImage, canvasTop);

					scaleTransform.ScaleX = currentZoom;
					scaleTransform.ScaleY = currentZoom;
					translateTransform.X = 0;
					translateTransform.Y = 0;
				}
			};

			// Zoom-to-Fit nach dem Laden anwenden
			window.Loaded += (s, e) =>
			{
				ApplyZoomToFit();
			};

			// Mouse wheel event: Zoom with mouse position tracking
			canvas.MouseWheel += (s, e) =>
			{
				e.Handled = true;
				Point mouseCanvasPos = e.GetPosition(canvas);

				// Get current image position
				double currentLeft = Canvas.GetLeft(drawingImage);
				double currentTop = Canvas.GetTop(drawingImage);

				// World coordinates of mouse pointer in image space (before zoom)
				double mouseWorldX = (mouseCanvasPos.X - currentLeft) / currentZoom;
				double mouseWorldY = (mouseCanvasPos.Y - currentTop) / currentZoom;

				// Calculate new zoom
				double zoomFactor = e.Delta > 0 ? 1.1 : 0.909; // +10% or -10%
				double newZoom = currentZoom * zoomFactor;

				currentZoom = newZoom;

				// Calculate new image position to keep mouse world point fixed
				double newLeft = mouseCanvasPos.X - (mouseWorldX * newZoom);
				double newTop = mouseCanvasPos.Y - (mouseWorldY * newZoom);

				// Update transforms and position
				scaleTransform.ScaleX = currentZoom;
				scaleTransform.ScaleY = currentZoom;
				Canvas.SetLeft(drawingImage, newLeft);
				Canvas.SetTop(drawingImage, newTop);

				// Reset translate (use Canvas position instead)
				currentTranslateX = 0;
				currentTranslateY = 0;
				translateTransform.X = 0;
				translateTransform.Y = 0;
			};

			// LMB click: Start pan
			canvas.MouseLeftButtonDown += (s, e) =>
			{
				isPanning = true;
				panStart = e.GetPosition(canvas);
				canvas.Cursor = System.Windows.Input.Cursors.Hand;
				System.Windows.Input.Mouse.Capture(canvas);
				e.Handled = true;
			};

			// Mouse move: Pan
			canvas.MouseMove += (s, e) =>
			{
				if (isPanning)
				{
					Point currentPos = e.GetPosition(canvas);
					double deltaX = (currentPos.X - panStart.X);
					double deltaY = (currentPos.Y - panStart.Y);

					// Update canvas position for panning
					double newLeft = Canvas.GetLeft(drawingImage) + deltaX;
					double newTop = Canvas.GetTop(drawingImage) + deltaY;

					Canvas.SetLeft(drawingImage, newLeft);
					Canvas.SetTop(drawingImage, newTop);

					panStart = currentPos;
				}
			};

			// LMB release: Stop pan
			canvas.MouseLeftButtonUp += (s, e) =>
			{
				isPanning = false;
				canvas.Cursor = System.Windows.Input.Cursors.Arrow;
				System.Windows.Input.Mouse.Capture(null);
			};

			// RMB: Reset zoom and pan
			canvas.MouseRightButtonUp += (s, e) =>
			{
				ApplyZoomToFit();
			};

			// Close window on Escape key
			window.KeyDown += (s, e) =>
			{
				if (e.Key == Key.Escape)
				{
					window.Close();
					e.Handled = true;
				}
			};

			window.Content = canvas;
			app.Run(window);
		}
	}
	catch (XamlParseException ex)
	{
		Printer.Error($"XAML parsing error: {ex.Message}");
	}
	catch (Exception ex)
	{
		Printer.Error($"Error: {ex.Message}");
	}

}
