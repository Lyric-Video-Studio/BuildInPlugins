using PluginBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalAiPlugin
{
    public class AudioTrackPayload
    {
        public AudioTrackPayload()
        {
            Speaker.Remove += (s, e) => Speakers.Remove((Speaker)s);
        }

        public string Prompt { get; set; }

        [Description("CFG (Classifier-Free Guidance) scale for generation. Higher values increase adherence to text. Default value: 1.3")]
        public float Cfg { get; set; } = 1.3f;

        public ObservableCollection<Speaker> Speakers { get; set; } = new ObservableCollection<Speaker>();

        [CustomAction("Add speaker (max 4)")]
        public void Add()
        {
            Speakers.Add(new Speaker());
        }
    }

    public class Speaker
    {
        public static event EventHandler Remove;

        [Description("Reference speaker with index in the prompt test and separate script lines with enter, like 'Speaker 1: my turn<enter>Speaker 2: no, it's my turn. Same applies when using audio file'")]
        public string Preset { get; set; }

        [Description("Audio sample file, preset will be ignored if this is used")]
        [EnableFileDrop]
        public string AudioFile { get; set; }

        [CustomAction("Delete")]
        public void Delete()
        {
            Remove?.Invoke(this, EventArgs.Empty);
        }
    }
}