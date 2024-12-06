using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PluginBase;

namespace CroppedImagePlugin
{
    public partial class ImageCropViewItem : UserControl
    {
        public ImageCropViewItem()
        {
            InitializeComponent();
        }

        private void PickFile(object? sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                var res = await FilePicker.PickAsync("", fileTypes: [.. CommonConstants.ImgTypes]);

                if (res.IsSuccessful && !string.IsNullOrEmpty(res.File.Path))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is ItemPayload ip)
                        {
                            ip.Source = res.File.Path;
                        }
                    });
                }
            });
        }
    }
}