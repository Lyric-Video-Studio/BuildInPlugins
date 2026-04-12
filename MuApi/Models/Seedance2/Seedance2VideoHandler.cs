using PluginBase;

namespace MuApiPlugin.Models.Seedance2
{
    internal class Seedance2VideoHandler
    {
        public static async Task<VideoResponse> GetVideo(ConnectionSettings connectionSettings, Seedance2TrackPayload trackPayload, Seedance2ItemPayload itemsPayload, string folderToSaveVideo, string model)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            var allImageSources = CollectReferenceFiles(itemsPayload.ImageReferences.ImageSources.Select(i => i.ImageFile), 9);
            if (!allImageSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allImageSources.Error };
            }

            var allAudioSources = CollectReferenceFiles(itemsPayload.AudioReferences.AudioSources.Select(i => i.AudioFile), 3);
            if (!allAudioSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allAudioSources.Error };
            }

            var allVideoSources = CollectReferenceFiles(itemsPayload.VideoReferences.VideoSources.Select(i => i.VideoFile), 3);
            if (!allVideoSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allAudioSources.Error };
            }

            var client = new Client();
            var uploadedImages = new List<string>();
            var uploadedAudios = new List<string>();
            var uploadedVideos = new List<string>();

            try
            {
                foreach (var imageSource in allImageSources.Files)
                {
                    uploadedImages.Add(await client.UploadFile(imageSource, connectionSettings, MuApiVideoPlugin._cancellationToken));
                }

                foreach (var audioSource in allAudioSources.Files)
                {
                    uploadedAudios.Add(await client.UploadFile(audioSource, connectionSettings, MuApiVideoPlugin._cancellationToken));
                }

                foreach (var videoSource in allVideoSources.Files)
                {
                    uploadedVideos.Add(await client.UploadFile(videoSource, connectionSettings, MuApiVideoPlugin._cancellationToken));
                }
            }
            catch (OperationCanceledException)
            {
                return new VideoResponse() { Success = false, ErrorMsg = "User cancelled" };
            }
            catch (Exception ex)
            {
                return new VideoResponse() { Success = false, ErrorMsg = ex.Message };
            }

            var request = new GenerationRequest()
            {
                prompt = $"{itemsPayload.Prompt} {trackPayload.Prompt}".Trim(),
                aspect_ratio = trackPayload.AspectRatio,
                duration = itemsPayload.Duration,
                quality = trackPayload.Quality,
                images_list = uploadedImages.Count > 0 ? uploadedImages : null,
                audio_files = uploadedAudios.Count > 0 ? uploadedAudios : null, 
                video_files = uploadedVideos.Count > 0 ? uploadedVideos : null,
            };

            return await client.GetVideo(request, model, folderToSaveVideo, connectionSettings, itemsPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }        

        private static (bool Success, string Error, List<string> Files) CollectReferenceFiles(IEnumerable<string> additionalFiles, int maxCount)
        {
            var files = new List<string>();

            void AddIfAny(string path)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    files.Add(path);
                }
            }

            foreach (var file in additionalFiles)
            {
                AddIfAny(file);
            }

            if (files.Count > maxCount)
            {
                return (false, $"Too many references. Maximum supported count is {maxCount}.", []);
            }

            foreach (var file in files)
            {
                var absolute = WorkspaceSettings.GetAbsolutePath(file);
                if (string.IsNullOrWhiteSpace(absolute) || !File.Exists(absolute))
                {
                    return (false, $"Reference file not found: {file}", []);
                }
            }

            return (true, "", files);
        }
    }
}
