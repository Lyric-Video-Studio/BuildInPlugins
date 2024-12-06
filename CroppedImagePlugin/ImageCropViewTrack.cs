using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

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
                if (tp.XOffset >= 0 && tp.YOffset >= 0 && tp.XOffset + tp.Width < imageContainer.Width && tp.YOffset + tp.Height < imageContainer.Height)
                {
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

                    SetMarkers(new Point(tp.XOffset, tp.YOffset));
                    SetMarkers(new Point(tp.XOffset + tp.Width, tp.YOffset + tp.Height));
                }
            }
        }

        private void ImageCropViewTrack_DataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is TrackPayload tp)
            {
                void SetPictureFrameProperties()
                {
                    if (tp.Scale)
                    {
                        imageContainer.Stretch = Stretch.Fill;
                        imageContainer.Width = tp.Width;
                        imageContainer.Height = tp.Height;
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
                        imageContainer.Stretch = Stretch.None;
                        CheckCropInit();
                    }
                }

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
                    tp.XOffset = (int)topLeft.X;
                    tp.YOffset = (int)topLeft.Y;

                    tp.Width = (int)width + size;
                    tp.Height = (int)heigth + size;
                }
            }
        }

        internal void PointerGestureRecognizer_PointerMoved(object? sender, PointerEventArgs e)
        {
            var relativeToContainerPosition = e.GetPosition(imageContainer);
            if (elementToDrag != null)
            {
                var isBottom = elementToDrag == bottomRightMarker;
                var offset = isBottom ? -size : 0;
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
        }

        private void SetLayoutBounds(Border elementToDrag, Rect rect)
        {
            Canvas.SetLeft(elementToDrag, rect.Left);
            Canvas.SetTop(elementToDrag, rect.Top);
            Canvas.SetRight(elementToDrag, rect.Right);
            Canvas.SetBottom(elementToDrag, rect.Bottom);
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

            marker.PropertyChanged += Marker_PropertyChanged;
            var offset = isBottomRight ? -size : 0;

            cropCanvas.Children.Add(marker);
            Canvas.SetLeft(marker, relativeToContainerPosition.X + offset);
            Canvas.SetTop(marker, relativeToContainerPosition.Y + offset);
            Canvas.SetRight(marker, relativeToContainerPosition.X + offset + size);
            Canvas.SetBottom(marker, relativeToContainerPosition.Y + offset + size);
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
            var color = invColorsCheckbox.IsChecked.Value ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0);
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

        internal void SetItemPayload(ItemPayload ip)
        {
            void SetImageProperties()
            {
                imageContainer.Source = ip.SourceBitmap;    
            }
            SetImageProperties();
            ip.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(ItemPayload.SourceBitmap))
                {
                    SetImageProperties();
                }
            };
        }
    }
}