using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CroppedImagePlugin
{
    public partial class ImageCropViewTrack : UserControl
    {
        public ImageCropViewTrack()
        {
            InitializeComponent();
            DataContextChanged += ImageCropViewTrack_DataContextChanged;
            imageContainer.SizeChanged += ImageContainer_SizeChanged;
        }

        private void ImageContainer_SizeChanged(object? sender, EventArgs e)
        {
            CheckCropInit();
        }

        private void CheckCropInit()
        {
            if (DataContext is TrackPayload tp && !tp.Scale)
            {
                // DataContext set

                if (topLeftMarker != null)
                {
                    cropCanvas.Children.Remove(topLeftMarker);
                    topLeftMarker.PropertyChanged -= Marker_PropertyChanged;
                }
                if (bottomRightMarker != null)
                {
                    cropCanvas.Children.Remove(bottomRightMarker);
                    bottomRightMarker.PropertyChanged -= Marker_PropertyChanged;
                }

                topLeftMarker = null;
                bottomRightMarker = null;

                SetMarkers(new Point(tp.XOffset * ScaleMultiplier, tp.YOffset * ScaleMultiplier));
                SetMarkers(new Point((tp.XOffset + tp.Width) * ScaleMultiplier, (tp.YOffset + tp.Height) * ScaleMultiplier));

                InvertColorsChanged(null, null);
            }
        }

        private TrackPayload currentTp;

        private void ImageCropViewTrack_DataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is TrackPayload tp)
            {
                currentTp = tp;

                /*void SetScaleConteiner()
                {
                    imageContainer.Stretch = Stretch.Fill;
                    imageContainer.MaxWidth = currentTp.Width;
                    imageContainer.MaxHeight = currentTp.Height;
                    imageContainer.MinWidth = currentTp.Width;
                    imageContainer.MinHeight = currentTp.Height;
                }*/

                void SetPictureFrameProperties()
                {
                    if (tp.Scale)
                    {
                        //SetScaleConteiner();
                        cropCanvas.Children.Clear();

                        if (topLeftMarker != null)
                        {
                            topLeftMarker.PropertyChanged -= Marker_PropertyChanged;
                        }
                        if (bottomRightMarker != null)
                        {
                            bottomRightMarker.PropertyChanged -= Marker_PropertyChanged;
                        }

                        topLeftMarker = null;
                        bottomRightMarker = null;
                        fullCrop = null;
                    }
                    else
                    {
                        CheckCropInit();
                    }
                }

                tp.PropertyChanged += (a, b) =>
                {
                    if (currentTp != null && currentTp.Scale)
                    {
                        if (b.PropertyName == nameof(TrackPayload.Width) || b.PropertyName == nameof(TrackPayload.Height))
                        {
                            //SetScaleConteiner();
                        }
                    }
                };

                /*tp.PropertyChanged += (a, b) =>
                {
                    if (b.PropertyName == nameof(TrackPayload.Scale))
                    {
                        SetPictureFrameProperties();
                    }

                    if (b.PropertyName == nameof(TrackPayload.Width) || b.PropertyName == nameof(TrackPayload.Height) ||
                        b.PropertyName == nameof(TrackPayload.XOffset) || b.PropertyName == nameof(TrackPayload.YOffset))
                    {
                        SetPictureFrameProperties();
                    }
                };*/

                SetPictureFrameProperties();
            }
        }

        private Border topLeftMarker;
        private Border bottomRightMarker;
        private Border fullCrop;

        private int size = 10;

        private void CheckFullCrop()
        {
            if (topLeftMarker != null && bottomRightMarker != null)
            {
                if (fullCrop == null)
                {
                    fullCrop = new Border();
                    fullCrop.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    fullCrop.BorderThickness = new Thickness(2);
                    cropCanvas.Children.Insert(0, fullCrop);
                }

                var topLeft = GetLayoutBounds(topLeftMarker);
                var bottomRight = GetLayoutBounds(bottomRightMarker);

                var width = bottomRight.X - topLeft.X;
                var heigth = bottomRight.Y - topLeft.Y;

                SetLayoutBounds(fullCrop, new Rect(topLeft.X, topLeft.Y, width + size, heigth + size));

                if (DataContext is TrackPayload tp)
                {
                    tp.XOffset = (int)(topLeft.X / ScaleMultiplier);
                    tp.YOffset = (int)(topLeft.Y / ScaleMultiplier);

                    tp.Width = (int)((width + size) / ScaleMultiplier);
                    tp.Height = (int)((heigth + size) / ScaleMultiplier);
                }
            }
        }

        public double ScaleMultiplier => imageContainer.DesiredSize.Width / originalWidth;

        internal void PointerGestureRecognizer_PointerMoved(object? sender, PointerEventArgs e)
        {
            var relativeToContainerPosition = e.GetPosition(imageContainer);
            if (elementToDrag != null)
            {
                var isBottom = elementToDrag == bottomRightMarker;
                var offset = isBottom ? -size / 2 : 0;
                SetLayoutBounds(elementToDrag, new Rect(relativeToContainerPosition.X + offset, relativeToContainerPosition.Y + offset, size, size));
            }
            else if (moveCenterStart != null && origTopLeft != null && origBottomRight != null)
            {
                var delta = relativeToContainerPosition - moveCenterStart.Value;

                var newTopLeft = origTopLeft.Value.Translate(new Vector(delta.X, delta.Y));
                var newBottomRight = origBottomRight.Value.Translate(new Vector(delta.X, delta.Y));

                SetLayoutBounds(topLeftMarker, newTopLeft);
                SetLayoutBounds(bottomRightMarker, newBottomRight);
            }

            if (currentTp != null && !currentTp.Scale && topLeftMarker != null && bottomRightMarker != null)
            {
                var topLeft = GetLayoutBounds(topLeftMarker);
                var bottomRight = GetLayoutBounds(bottomRightMarker);
                currentTp.XOffset = (int)(topLeft.Left / ScaleMultiplier);
                currentTp.YOffset = (int)(topLeft.Top / ScaleMultiplier);

                currentTp.Width = (int)((bottomRight.Right - topLeft.Left) / ScaleMultiplier);
                currentTp.Height = (int)((bottomRight.Bottom - topLeft.Top) / ScaleMultiplier);
            }
        }

        private void SetLayoutBounds(Border elementToDrag, Rect rect)
        {
            Canvas.SetLeft(elementToDrag, rect.Left);
            Canvas.SetTop(elementToDrag, rect.Top);
            Canvas.SetRight(elementToDrag, rect.Right);
            Canvas.SetBottom(elementToDrag, rect.Bottom);

            if (topLeftMarker != null && bottomRightMarker != null && DataContext is TrackPayload tp)
            {
                SetVisualizationBorder(topLeftMarker, bottomRightMarker, tp, false);
            }
        }

        internal void PointerGestureRecognizer_PointerExited(object? sender, PointerReleasedEventArgs e)
        {
            elementToDrag = null;
            moveCenterStart = null;
            origTopLeft = null;
            origBottomRight = null;
        }

        internal void PointerGestureRecognizer_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is TrackPayload tp && !tp.Scale)
            {
                var relativeToContainerPosition = e.GetPosition(imageContainer);
                SetMarkers(relativeToContainerPosition);
            }
        }

        private void SetMarkers(Point relativeToContainerPosition)
        {
            if (topLeftMarker == null)
            {
                SetElement(ref topLeftMarker, relativeToContainerPosition, false);
            }
            else if (bottomRightMarker == null)
            {
                SetElement(ref bottomRightMarker, relativeToContainerPosition, true);
                SetVisualizationBorder(topLeftMarker, bottomRightMarker, DataContext as TrackPayload);
            }
            else
            {
                var topLeft = GetLayoutBounds(topLeftMarker);
                var bottomRight = GetLayoutBounds(bottomRightMarker);

                if (topLeft.Contains(relativeToContainerPosition))
                {
                    // Drag top left
                    elementToDrag = topLeftMarker;
                }
                else if (bottomRight.Contains(relativeToContainerPosition))
                {
                    // Drag top left
                    elementToDrag = bottomRightMarker;
                }
                else
                {
                    moveCenterStart = relativeToContainerPosition;
                    origTopLeft = topLeft;
                    origBottomRight = bottomRight;
                }
            }
        }

        private Rect GetLayoutBounds(Border target)
        {
            return new Rect(new Point(Canvas.GetLeft(target), Canvas.GetTop(target)), new Point(Canvas.GetRight(target), Canvas.GetBottom(target)));
        }

        private Border elementToDrag;
        private Point? moveCenterStart;
        private Rect? origTopLeft;
        private Rect? origBottomRight;

        private void SetElement(ref Border marker, Point relativeToContainerPosition, bool isBottomRight)
        {
            marker = new Border();
            marker.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            marker.BorderThickness = new Thickness(2);
            marker.Background = new SolidColorBrush(Color.FromRgb(1, 1, 1));
            marker.Width = size;
            marker.Height = size;

            marker.PropertyChanged += Marker_PropertyChanged;
            var offset = isBottomRight ? -size : 0;

            cropCanvas.Children.Add(marker);
            Canvas.SetLeft(marker, relativeToContainerPosition.X + offset);
            Canvas.SetTop(marker, relativeToContainerPosition.Y + offset);
            Canvas.SetRight(marker, relativeToContainerPosition.X + offset + size);
            Canvas.SetBottom(marker, relativeToContainerPosition.Y + offset + size);
        }

        private Border visualizationBorder;

        public void SetVisualizationBorder(Border topLeft, Border bottomRight, TrackPayload tp, bool createNew = true)
        {
            if (visualizationBorder != null && createNew)
            {
                cropCanvas.Children.Remove(visualizationBorder);
            }

            if (createNew)
            {
                visualizationBorder = new Border();
                cropCanvas.Children.Add(visualizationBorder);
            }

            visualizationBorder!.BorderThickness = new Thickness(2);
            visualizationBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            visualizationBorder.Width = tp.Width * ScaleMultiplier;
            visualizationBorder.Height = tp.Height * ScaleMultiplier;

            Canvas.SetLeft(visualizationBorder, Canvas.GetLeft(topLeft));
            Canvas.SetTop(visualizationBorder, Canvas.GetTop(topLeft));
        }

        private void Marker_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "LayoutBounds")
            {
                CheckFullCrop();
            }
        }

        private void InvertColorsChanged(object? sender, RoutedEventArgs e)
        {
            var color = invColorsCheckbox!.IsChecked!.Value ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0);
            var brush = new SolidColorBrush(color);
            if (topLeftMarker != null)
            {
                topLeftMarker.BorderBrush = brush;
            }
            if (bottomRightMarker != null)
            {
                bottomRightMarker.BorderBrush = brush;
            }
            if (fullCrop != null)
            {
                fullCrop.BorderBrush = brush;
            }
        }

        public void SyncValuesToCropControl(object? sender, RoutedEventArgs e)
        {
            CheckCropInit();
        }

        private double originalWidth = 1;

        internal void SetItemPayload(ItemPayload ip)
        {
            void SetImageProperties()
            {
                if (ip.SourceBitmap != null)
                {
                    originalWidth = ip.SourceBitmap.Size.Width;
                }

                imageContainer.Source = ip.SourceBitmap;
                // Source has changed, must set the width and heigh to match the picture
                if (DataContext is TrackPayload trackPayload)
                {
                    if (ip.SourceBitmap != null)
                    {
                        AssingValuesFromSource(trackPayload, ip);
                    }
                    else
                    {
                        ip.PropertyChanged += (s, p) =>
                        {
                            if (p.PropertyName == nameof(ItemPayload.Source))
                            {
                                if (ip.SourceBitmap != null)
                                {
                                    AssingValuesFromSource(trackPayload, ip);
                                }
                            }
                        };
                    }
                }
                CheckCropInit();
            }
            SetImageProperties();

            /*Task.Run(() =>
            {
                try
                {
                    if (DataContext is TrackPayload tp)
                    {
                        var w = tp.Width;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            });*/

            ip.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(ItemPayload.SourceBitmap))
                {
                    SetImageProperties();
                }
            };
        }

        private void AssingValuesFromSource(TrackPayload trackPayload, ItemPayload ip)
        {
            if ((trackPayload.Width + trackPayload.XOffset > ip.SourceBitmap.Size.Width || trackPayload.Height + trackPayload.YOffset > ip.SourceBitmap.Size.Height) && !trackPayload.Scale)
            {
                trackPayload.Width = (int)(ip.SourceBitmap.Size.Width * 0.75d);
                trackPayload.Height = (int)(ip.SourceBitmap.Size.Height * 0.75d);
                trackPayload.XOffset = (int)((ip.SourceBitmap.Size.Width * 0.25d) / 2);
                trackPayload.YOffset = (int)((ip.SourceBitmap.Size.Height * 0.25d) / 2);
            }

            /*if (!trackPayload.Scale)
            {
                SetCanvasSize(cropCanvas, ip.SourceBitmap);
                SetCanvasSize(imageContainer, ip.SourceBitmap);
            }*/
        }

        /*private void SetCanvasSize(Control v, Bitmap b)
        {
            v.MaxWidth = b.Size.Width;
            v.MaxHeight = b.Size.Width;
            v.MinWidth = b.Size.Width;
            v.MinHeight = b.Size.Width;
        }*/
    }
}