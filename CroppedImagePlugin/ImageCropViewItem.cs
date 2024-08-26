namespace CroppedImagePlugin
{
    public partial class ImageCropViewItem : ContentView
    {
        public ImageCropViewItem()
        {
            InitializeComponent();
        }

        private void PickFile(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var res = await FilePicker.PickAsync(PickOptions.Images);

                if (!string.IsNullOrEmpty(res.FullPath))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (BindingContext is ItemPayload ip)
                        {
                            ip.Source = res.FullPath;
                        }
                    });
                }
            });
        }
    }
}