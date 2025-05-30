﻿using Avalonia;
using Avalonia.Controls;
using PluginBase;
using SkiaSharp;
using System.Text.Json.Nodes;
using static CroppedImagePlugin.TrackPayload;

namespace CroppedImagePlugin
{
    public class ImageCropperPlugin : IImagePlugin, IImportFromImage, IPluginEditUi
    {
        public string UniqueName => "ImageCropperBuiltInt";

        public string DisplayName => "Image crop/scale";

        public string SettingsHelpText => "Crops or scale images from source and creates new ones. Usefull as 'middle' plugin for importing images from source (track) and then cropping those for next track to use (for example, Img2Vid)";

        public string[] SettingsLinks => new[] { "www.ulti.fi" };

        public object GeneralDefaultSettings => null;

        public bool AsynchronousGeneration { get; } = false;

        public bool IsInitialized => true;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public void CloseConnection()
        {
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (JsonHelper.DeepCopy<TrackPayload>(obj) is TrackPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageTrack();
        }

        public object CopyPayloadForImageItem(object obj)
        {
            if (JsonHelper.DeepCopy<ItemPayload>(obj) is ItemPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageItem();
        }

        public IPluginBase CreateNewInstance()
        {
            return new ImageCropperPlugin();
        }

        public object DefaultPayloadForImageItem()
        {
            currentItemPayload = new ItemPayload();
            if (trackEdit != null)
            {
                // Track edit has been initialized first, set the image path
                trackEdit.SetItemPayload(currentItemPayload);
            }
            return currentItemPayload;
        }

        public object DefaultPayloadForImageTrack()
        {
            return new TrackPayload() { Height = 1024, Width = 1024 };
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName) ?? new TrackPayload();
        }

        public Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            var newTp = JsonHelper.DeepCopy<TrackPayload>(trackPayload);
            var newIp = JsonHelper.DeepCopy<ItemPayload>(itemsPayload);

            if (!string.IsNullOrEmpty(newIp?.Source) && newTp != null)
            {
                if (File.Exists(newIp.Source))
                {
                    using var img = SKBitmap.Decode(newIp.Source);

                    if (img == null)
                    {
                        return Task.FromResult(new ImageResponse { ErrorMsg = "Failed to decode source" });
                    }
                    else
                    {
                        if (newTp.Scale)
                        {
                            if (newTp.Width <= 0)
                            {
                                // Set width based on heigth
                                var scale = (double)newTp.Height / newIp.SourceBitmap.Size.Height;
                                newTp.Width = (int)(newIp.SourceBitmap.Size.Width * scale);
                            }

                            if (newTp.Height <= 0)
                            {
                                // Set width based on heigth
                                var scale = (double)newTp.Width / newIp.SourceBitmap.Size.Width;
                                newTp.Height = (int)(newIp.SourceBitmap.Size.Height * scale);
                            }

                            var info = new SKImageInfo(newTp.Width, newTp.Height, img.ColorType);
                            var output = SKImage.Create(info);
                            img.ScalePixels(output.PeekPixels(), SKSamplingOptions.Default);

                            var rot = (RotateFinalOutput)newTp.SelectedRotation;
                            var rotDeg = 0;
                            switch (rot)
                            {
                                case RotateFinalOutput.Left:
                                    rotDeg = -90;
                                    break;

                                case RotateFinalOutput.Right:
                                    rotDeg = 90;
                                    break;
                            }

                            SKImage? rotated = rotDeg != 0 ? Rotate(SKBitmap.FromImage(output), rotDeg) : null;

                            using var memStream = new MemoryStream();
                            var codec = SKCodec.Create(newIp.Source);
                            using var data = (rotated ?? output).Encode(codec.EncodedFormat, 100);
                            data.SaveTo(memStream);
                            memStream.Position = 0;
                            output.Dispose();
                            rotated?.Dispose();
                            return Task.FromResult(new ImageResponse { Image = Convert.ToBase64String(memStream.ToArray()), ImageFormat = Path.GetExtension(newIp.Source).Replace(".", ""), Success = true });
                        }
                        else
                        {
                            if (img.Width < newTp.Width)
                            {
                                return Task.FromResult(new ImageResponse { ErrorMsg = $"Cropped width ({newTp.Width}) must be smaller than source image width {img.Width}" });
                            }

                            if (img.Height < newTp.Height)
                            {
                                return Task.FromResult(new ImageResponse { ErrorMsg = $"Cropped heigth ({newTp.Height}) must be smaller than source image heigth {img.Height}" });
                            }

                            if (newTp.XOffset + newTp.Width > img.Width)
                            {
                                return Task.FromResult(new ImageResponse { ErrorMsg = $"X Offset & width ({newTp.XOffset + newTp.Width}) is greater than image width {img.Width}" });
                            }

                            if (newTp.YOffset + newTp.Height > img.Height)
                            {
                                return Task.FromResult(new ImageResponse { ErrorMsg = $"Y Offset & heigth ({newTp.XOffset + newTp.Height}) is greater than image heigth {img.Width}" });
                            }

                            var pixels = img.Pixels;

                            using var newImg = new SKBitmap(newTp.Width, newTp.Height);

                            var targetPixels = newImg.Pixels;

                            var imgWidth = img.Width;
                            var imgHeight = img.Height;
                            var targetRect = new Rect(newTp.XOffset, newTp.YOffset, newTp.Width, newTp.Height);

                            for (var y = 1; y < imgHeight - 1; y++)
                            {
                                for (var x = 0; x < imgWidth; x++)
                                {
                                    var rowOffset = y * imgWidth;

                                    if (targetRect.Contains(new Point(x, y - 1)))
                                    {
                                        try
                                        {
                                            var pix = pixels[rowOffset - imgWidth + x];

                                            var targetPixXCoord = x - newTp.XOffset;
                                            var rowOffsetTarget = (y - newTp.YOffset) * newTp.Width;
                                            var targetIndex = rowOffsetTarget - newTp.Width + targetPixXCoord;

                                            if (targetIndex < targetPixels.Length)
                                            {
                                                targetPixels[targetIndex] = pix;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            return Task.FromResult(new ImageResponse { ErrorMsg = $"Failed to manipulate pixels: {ex.Message}" });
                                        }
                                    }
                                }
                            }

                            newImg.Pixels = targetPixels;

                            using var memStream = new MemoryStream();
                            var codec = SKCodec.Create(newIp.Source);
                            if (newImg.Encode(memStream, codec.EncodedFormat, 100))
                            {
                                memStream.Position = 0;
                                return Task.FromResult(new ImageResponse { Image = Convert.ToBase64String(memStream.ToArray()), ImageFormat = Path.GetExtension(newIp.Source).Replace(".", ""), Success = true });
                            }
                            else
                            {
                                return Task.FromResult(new ImageResponse { ErrorMsg = "Failed to encode bitmap data" });
                            }
                        }
                    }
                }
                else
                {
                    return Task.FromResult(new ImageResponse { ErrorMsg = "Source file was not found" });
                }
            }
            else
            {
                return Task.FromResult(new ImageResponse { ErrorMsg = "Source was null or empty" });
            }
        }

        public static SKImage Rotate(SKBitmap bitmap, double angle)
        {
            double radians = Math.PI * angle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));
            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (var surface = new SKCanvas(rotatedBitmap))
            {
                surface.Clear();
                surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
                surface.RotateDegrees((float)angle);
                surface.Translate(-originalWidth / 2, -originalHeight / 2);
                surface.DrawBitmap(bitmap, new SKPoint());
            }
            var res = SKImage.FromBitmap(rotatedBitmap);
            rotatedBitmap.Dispose();
            bitmap.Dispose();
            return res;
        }

        public Task<string> Initialize(object settings)
        {
            return Task.FromResult("");
        }

        public Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            return Task.FromResult(Array.Empty<string>());
        }

        public Task<string> TestInitialization()
        {
            return Task.FromResult("");
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            currentItemPayload = new ItemPayload() { Source = imgSource };
            if (trackEdit != null)
            {
                // Track edit has been initialized first, set the image path
                trackEdit.SetItemPayload(currentItemPayload);
            }
            return currentItemPayload;
        }

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (payload is ItemPayload ip)
            {
                if (string.IsNullOrEmpty(ip.Source))
                {
                    return (false, "No source");
                }

                if (!File.Exists(ip.Source))
                {
                    return (false, $"Source file {ip.Source} missing");
                }
            }
            return (true, "");
        }

        private ImageCropViewItem itemEdit;

        // I want to actually edit the image offsets in track mode, so that is diable with some tricks
        private ItemPayload currentItemPayload;

        public UserControl GetItemPayloadEditingUi(object payload)
        {
            if (payload is ItemPayload ip)
            {
                currentItemPayload = ip;
                var cv = itemEdit ?? new ImageCropViewItem();
                itemEdit = cv;
                cv.DataContext = payload;

                if (trackEdit != null)
                {
                    // Track edit has been initialized first, set the image path
                    trackEdit.SetItemPayload(currentItemPayload);
                }

                return cv;
            }
            return null;
        }

        private ImageCropViewTrack trackEdit;

        public UserControl GetTrackPayloadEditingUi(object payload)
        {
            if (payload is TrackPayload tp)
            {
                var cv = trackEdit ?? new ImageCropViewTrack();
                trackEdit = cv;
                cv.DataContext = tp;

                if (currentItemPayload != null)
                {
                    // Item payload was initialized first, set it to view to enable user friendly cropping
                    trackEdit.SetItemPayload(currentItemPayload);
                }

                return cv;
            }
            return null;
        }

        private ImageCropViewTrack trackEditOverride;

        public UserControl GetTrackOverridePayloadEditingUi(object payload)
        {
            if (payload is TrackPayload tp)
            {
                var cv = trackEditOverride ?? new ImageCropViewTrack();
                trackEditOverride = cv;
                cv.DataContext = tp;

                return cv;
            }
            return null;
        }

        public void ViewClosed()
        {
            trackEditOverride = null;
            trackEdit = null;
            currentItemPayload = null;
            itemEdit = null;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<ItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<TrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return null;
        }

        public string TextualRepresentation(object itemPayload)
        {
            return "";
        }

        public void SetPayloads(object trackpayload, object trackOverridePayload, object itemPayload)
        {
            // Not used, DataContext set to UserCOntrol instances is enough for this plugin
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageItem();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageTrack(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageItem(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                return new List<string>() { ip.Source };
            }

            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            // No need to do anything
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == ip.Source)
                    {
                        ip.Source = newPath[i];
                    }
                }
            }
        }
    }
}