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
            ViduQ2TurboTrackPayload.ModelT2V, ViduQ2TurboTrackPayload.ModelI2V, ViduQ2TurboTrackPayload.ModelStartEnd,
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
        [HideAllChildren]
        [ParentName("")]
        public ViduQ2TurboTrackPayload ViduQ2Turbo { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(Seedance2):
                        return IsSeedance2(tp);
                    case nameof(ViduQ2Turbo):
                        return IsViduQ2Turbo(tp);
                    default:
                        break;
                }

                if (IsSeedance2(tp))
                {
                    return tp.Seedance2.ShouldPropertyBeVisible(propertyName, model);
                }
                if (IsViduQ2Turbo(tp))
                {
                    return tp.ViduQ2Turbo.ShouldPropertyBeVisible(propertyName, model);
                }
            }
            return true;
        }

        public static bool IsSeedance2(TrackPayload tp)
        {
            return tp.Model != null && tp.Model.StartsWith("seedance");
        }
        public static bool IsViduQ2Turbo(TrackPayload tp)
        {
            return ViduQ2TurboTrackPayload.IsViduQ2TurboModel(tp.Model);
        }

        [CustomAction("Model info", true)]
        public void ModelInfo()
        {
            IUriLauncher.Launcher.LaunchUrl($"https://muapi.ai/playground/{Model}");
        }
    }
}
