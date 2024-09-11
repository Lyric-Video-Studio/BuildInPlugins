namespace CroppedImagePlugin
{
    public partial class ImageCropViewTrack : ContentView
    {
        public ImageCropViewTrack()
        {
            InitializeComponent();
            BindingContextChanged += ImageCropViewTrack_BindingContextChanged;
            imageContainer.SizeChanged += ImageContainer_SizeChanged;
        }

        private void ImageContainer_SizeChanged(object sender, EventArgs e)
        {
            CheckCropInit();
        }

        private void CheckCropInit()
        {
            if (BindingContext is TrackPayload tp && !tp.Scale)
            {
                // Bindingcontext set
                if (tp.XOffset >= 0 && tp.YOffset >= 0 && tp.XOffset + tp.Width < imageContainer.Width && tp.YOffset + tp.Height < imageContainer.Height)
                {
                    if (topLeftMarker != null)
                    {
                        cropCanvas.Remove(topLeftMarker);
                        topLeftMarker.PropertyChanged -= Marker_PropertyChanged;
                    }
                    if (bottomRightMarker != null)
                    {
                        cropCanvas.Remove(bottomRightMarker);
                        bottomRightMarker.PropertyChanged -= Marker_PropertyChanged;
                    }

                    topLeftMarker = null;
                    bottomRightMarker = null;

                    SetMarkers(new Point(tp.XOffset, tp.YOffset));
                    SetMarkers(new Point(tp.XOffset + tp.Width, tp.YOffset + tp.Height));
                }
            }
        }

        private void ImageCropViewTrack_BindingContextChanged(object sender, EventArgs e)
        {
            if (BindingContext is TrackPayload tp)
            {
                void SetPictureFrameProperties()
                {
                    if (tp.Scale)
                    {
                        imageContainer.Aspect = Aspect.Fill;
                        imageContainer.WidthRequest = tp.Width;
                        imageContainer.HeightRequest = tp.Height;
                        cropCanvas.Clear();

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
                        imageContainer.Aspect = Aspect.Center;
                        imageContainer.WidthRequest = -1;
                        imageContainer.HeightRequest = -1;
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

        internal void SetItemPayload(ItemPayload ip)
        {
            void SetImagePrroperties()
            {
                imageContainer.Source = ip.Source;

                if (!string.IsNullOrEmpty(ip.Source))
                {
                }
            }

            SetImagePrroperties();

            ip.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(ItemPayload.Source))
                {
                    SetImagePrroperties();
                }
            };
        }

        private int size = 10;

        private void CheckFullCrop()
        {
            if (topLeftMarker != null && bottomRightMarker != null)
            {
                if (fullCrop == null)
                {
                    fullCrop = new Border();
                    fullCrop.Stroke = Color.FromRgb(0, 0, 0);
                    fullCrop.StrokeThickness = 2;
                    cropCanvas.Insert(0, fullCrop);
                }

                var topLeft = cropCanvas.GetLayoutBounds(topLeftMarker);
                var bottomRight = cropCanvas.GetLayoutBounds(bottomRightMarker);

                var width = bottomRight.X - topLeft.X;
                var heigth = bottomRight.Y - topLeft.Y;

                cropCanvas.SetLayoutBounds(fullCrop, new Rect(topLeft.X, topLeft.Y, width + size, heigth + size));

                if (BindingContext is TrackPayload tp)
                {
                    tp.XOffset = (int)topLeft.X;
                    tp.YOffset = (int)topLeft.Y;

                    tp.Width = (int)width + size;
                    tp.Height = (int)heigth + size;
                }
            }
        }

        internal void PointerGestureRecognizer_PointerMoved(object sender, PointerEventArgs e)
        {
            var relativeToContainerPosition = e.GetPosition(imageContainer) ?? new Point();
            if (elementToDrag != null)
            {
                var isBottom = elementToDrag == bottomRightMarker;
                var offset = isBottom ? -size : 0;
                var newRect = new Rect(relativeToContainerPosition.X + offset, relativeToContainerPosition.Y + offset, size, size);
                cropCanvas.SetLayoutBounds(elementToDrag, new Rect(relativeToContainerPosition.X + offset, relativeToContainerPosition.Y + offset, size, size));
            }
            else if (moveCenterStart != null && origTopLeft != null && origBottomRight != null)
            {
                var delta = relativeToContainerPosition - moveCenterStart.Value;

                var newTopLeft = origTopLeft.Value.Offset(delta.Width, delta.Height);
                var newBottomRight = origBottomRight.Value.Offset(delta.Width, delta.Height);

                cropCanvas.SetLayoutBounds(topLeftMarker, newTopLeft);
                cropCanvas.SetLayoutBounds(bottomRightMarker, newBottomRight);
            }
        }

        internal void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
        {
            elementToDrag = null;
            moveCenterStart = null;
            origTopLeft = null;
            origBottomRight = null;
        }

        internal void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
        {
            if (BindingContext is TrackPayload tp && !tp.Scale)
            {
                var relativeToContainerPosition = e.GetPosition(imageContainer) ?? new Point();
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
                var topLeft = cropCanvas.GetLayoutBounds(topLeftMarker);
                var bottomRight = cropCanvas.GetLayoutBounds(bottomRightMarker);

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

        private Border elementToDrag;
        private Point? moveCenterStart;
        private Rect? origTopLeft;
        private Rect? origBottomRight;

        private void SetElement(ref Border marker, Point relativeToContainerPosition, bool isBottomRight)
        {
            marker = new Border();
            marker.Stroke = Color.FromRgb(0, 0, 0);
            marker.StrokeThickness = 2;
            marker.BackgroundColor = Color.FromRgb(1, 1, 1);

            marker.PropertyChanged += Marker_PropertyChanged;
            var offset = isBottomRight ? -size : 0;

            cropCanvas.Add(marker);
            cropCanvas.SetLayoutBounds(marker, new Rect(relativeToContainerPosition.X + offset, relativeToContainerPosition.Y + offset, size, size));
        }

        private void Marker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LayoutBounds")
            {
                CheckFullCrop();
            }
        }

        private void InvertColorsChanged(object sender, CheckedChangedEventArgs e)
        {
            var color = e.Value ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0);
            if (topLeftMarker != null)
            {
                topLeftMarker.Stroke = color;
            }
            if (bottomRightMarker != null)
            {
                bottomRightMarker.Stroke = color;
            }
            if (fullCrop != null)
            {
                fullCrop.Stroke = color;
            }
        }

        public void SyncValuesToCropControl(object sender, EventArgs e)
        {
            CheckCropInit();
        }
    }
}