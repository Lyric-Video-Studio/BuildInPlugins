using Avalonia.Platform.Storage;
using MuApiPlugin.Models.HappyHorse1;
using MuApiPlugin.Models.Seedance2;
using MuApiPlugin.Models.ViduQ2Turbo;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public event EventHandler ModelChanged;
        private string model = Seedance2TrackPayload.ModelT2V;

        [PropertyComboOptions([Seedance2TrackPayload.ModelT2V, Seedance2TrackPayload.ModelI2V, Seedance2TrackPayload.ModelOmniRef,
            HappyHorse1TrackPayload.ModelT2V1080p, HappyHorse1TrackPayload.ModelI2V1080p, HappyHorse1TrackPayload.ModelT2V720p, HappyHorse1TrackPayload.ModelI2V720pp,
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
        public HappyHorse1TrackPayload HappyHorse1 { get; set; } = new();

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
                    case nameof(HappyHorse1):
                        return IsHappyHorse1(tp);
                    case nameof(ViduQ2Turbo):
                        return IsViduQ2Turbo(tp);
                    default:
                        break;
                }

                if (IsSeedance2(tp))
                {
                    return tp.Seedance2.ShouldPropertyBeVisible(propertyName, model);
                }

                if (IsHappyHorse1(tp))
                {
                    return tp.HappyHorse1.ShouldPropertyBeVisible(propertyName, model);
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

        public static bool IsHappyHorse1(TrackPayload tp)
        {
            return HappyHorse1TrackPayload.IsHappyHorseModel(tp.Model);
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
