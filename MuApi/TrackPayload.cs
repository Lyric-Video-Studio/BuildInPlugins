using Avalonia.Platform.Storage;
using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public event EventHandler ModelChanged;
        private string model = Seedance2TrackPayload.ModelT2V;

        [PropertyComboOptions([Seedance2TrackPayload.ModelT2V, Seedance2TrackPayload.ModelI2V, Seedance2TrackPayload.ModelOmniRef,
            Seedance2TrackPayload.ModelT2V480p, Seedance2TrackPayload.ModelI2V480p])]
        public string Model
        {
            get => model;
            set
            {
                var notifi = IPayloadPropertyVisibility.UserInitiatedSet && model != value;
                model = value;

                if (notifi)
                {
                    ModelChanged?.Invoke(this, null);
                }
            }
        }

        [HideAllChildren]
        [ParentName("")]
        public Seedance2TrackPayload Seedance2 { get; set; } = new Seedance2TrackPayload();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(Seedance2):
                        return IsSeedance2(tp);
                    default:
                        break;
                }

                if (IsSeedance2(tp))
                {
                    return tp.Seedance2.ShouldPropertyBeVisible(propertyName, model);
                }
            }
            return true;
        }

        public static bool IsSeedance2(TrackPayload tp)
        {
            return tp.Model != null && tp.Model.StartsWith("seedance");
        }

        [CustomAction("Model info", true)]
        public void ModelInfo()
        {
            IUriLauncher.Launcher.LaunchUrl($"https://muapi.ai/playground/{Model}");
        }
    }
}
