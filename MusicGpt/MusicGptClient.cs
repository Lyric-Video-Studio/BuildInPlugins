using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicGptPlugin
{
    internal class MusicGptClient
    {
        internal static async Task<AudioResponse> GenerateAudio(string generationId, string prompt, string musicStyle, string lyrics, bool makeInstrumental, bool vocal_only, string voice_id,
            string folderToSaveAudio, 
            ConnectionSettings connectionSettings, MusicGptItemPayload musicGptItemPayload, 
            Action saveAndRefreshCallback, Action<string> textualProgress)
        {
            throw new NotImplementedException();
        }
    }
}
