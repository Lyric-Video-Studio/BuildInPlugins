using MuApiPlugin.Models.GeminiOmni;
using PluginBase;

namespace MuApiPlugin
{
    public class AudioTrackPayload : IPayloadPropertyVisibility
    {
        public event EventHandler ModelChanged;
        private string model = GeminiOmniAudioTrackPayload.ModelAudioProfile;

        [PropertyComboOptions([GeminiOmniAudioTrackPayload.ModelAudioProfile])]
        public string Model
        {
            get => model;
            set
            {
                var notify = IPayloadPropertyVisibility.UserInitiatedSet && model != value;
                model = value;

                if (notify)
                {
                    ModelChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [HideAllChildren]
        [ParentName("")]
        public GeminiOmniAudioTrackPayload GeminiOmniAudio { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is not AudioTrackPayload typedPayload)
            {
                return true;
            }

            return propertyName switch
            {
                nameof(GeminiOmniAudio) => IsGeminiOmniAudio(typedPayload),
                _ when IsGeminiOmniAudio(typedPayload) => typedPayload.GeminiOmniAudio.ShouldPropertyBeVisible(propertyName, model),
                _ => true
            };
        }

        public static bool IsGeminiOmniAudio(AudioTrackPayload payload)
        {
            return GeminiOmniAudioTrackPayload.IsGeminiOmniAudioModel(payload.Model);
        }

        [CustomAction("Model info", true)]
        public void ModelInfo()
        {
            IUriLauncher.Launcher.LaunchUrl($"https://muapi.ai/playground/{Model}");
        }
    }
}
