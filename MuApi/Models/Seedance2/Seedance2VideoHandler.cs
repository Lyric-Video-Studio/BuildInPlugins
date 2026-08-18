using PluginBase;

namespace MuApiPlugin.Models.Seedance2
{
    internal class Seedance2VideoHandler
    {
        public static async Task<VideoResponse> GetVideo(ConnectionSettings connectionSettings, Seedance2TrackPayload trackPayload, Seedance2ItemPayload itemsPayload, string folderToSaveVideo, string model, IApiPollingPayload pollingId)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            var imageSources = Seedance2TrackPayload.SupportsImageReferences(model)
                ? itemsPayload.ImageReferences.ImageSources.Select(i => i.ImageFile)
                    .Concat(trackPayload.ImageReferences.ImageSources.Select(i => i.ImageFile))
                : [];
            var allImageSources = CollectReferenceFiles(imageSources, GetMaxImageCount(model));
            if (!allImageSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allImageSources.Error };
            }

            var audioSources = Seedance2TrackPayload.SupportsAudioReferences(model)
                ? itemsPayload.AudioReferences.AudioSources.Select(i => i.AudioFile)
                    .Concat(trackPayload.AudioReferences.AudioSources.Select(i => i.AudioFile))
                : [];
            var allAudioSources = CollectReferenceFiles(audioSources, Seedance2TrackPayload.IsSeedance25SpicyModel(model) ? 10 : 3);
            if (!allAudioSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allAudioSources.Error };
            }

            var videoSources = Seedance2TrackPayload.SupportsVideoReferences(model)
                ? itemsPayload.VideoReferences.VideoSources.Select(i => i.VideoFile)
                    .Concat(trackPayload.VideoReferences.VideoSources.Select(i => i.VideoFile))
                : [];
            var allVideoSources = CollectReferenceFiles(videoSources, GetMaxVideoCount(model));
            if (!allVideoSources.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = allVideoSources.Error };
            }

            var requirementError = GetReferenceRequirementError(model, allImageSources.Files.Count, allVideoSources.Files.Count);
            if (!string.IsNullOrWhiteSpace(requirementError))
            {
                return new VideoResponse() { Success = false, ErrorMsg = requirementError };
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
                quality = !Seedance2TrackPayload.IsMiniModel(model) && !Seedance2TrackPayload.IsSpicyModel(model)
                    ? trackPayload.Quality
                    : null,
                resolution = Seedance2TrackPayload.UsesResolutionParameter(model) ? trackPayload.Resolution : null,
                generate_audio = Seedance2TrackPayload.SupportsGenerateAudio(model) ? trackPayload.GenerateAudio : null,
                camera_fixed = Seedance2TrackPayload.SupportsCameraFixed(model) ? trackPayload.CameraFixed : null,
                high_bitrate = Seedance2TrackPayload.IsMiniModel(model) && trackPayload.HighBitrate,
                seed = Seedance2TrackPayload.IsSeedance25SpicyModel(model) ? trackPayload.Seed : null
            };

            PopulateMediaFields(request, model, trackPayload, uploadedImages, uploadedAudios, uploadedVideos);

            return await client.GetVideo(request, model, folderToSaveVideo, connectionSettings, pollingId,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }

        private static void PopulateMediaFields(GenerationRequest request, string model, Seedance2TrackPayload trackPayload,
            List<string> uploadedImages, List<string> uploadedAudios, List<string> uploadedVideos)
        {
            if (!Seedance2TrackPayload.IsSeedance25SpicyModel(model))
            {
                request.images_list = uploadedImages.Count > 0 ? uploadedImages : null;
                request.audio_files = uploadedAudios.Count > 0 ? uploadedAudios : null;
                request.video_files = uploadedVideos.Count > 0 ? uploadedVideos : null;
                return;
            }

            if (Seedance2TrackPayload.IsImageToVideoModel(model))
            {
                request.image_url = uploadedImages[0];
                if (model == Seedance2TrackPayload.Model25SpicyI2V && uploadedImages.Count > 1)
                {
                    request.last_image = uploadedImages[1];
                }
                return;
            }

            if (Seedance2TrackPayload.IsFirstLastFrameModel(model))
            {
                request.images_list = uploadedImages;
                return;
            }

            if (Seedance2TrackPayload.IsOmniReferenceModel(model))
            {
                request.images_list = uploadedImages.Count > 0 ? uploadedImages : null;
                request.videos_list = uploadedVideos.Count > 0 ? uploadedVideos : null;
                request.audios_list = uploadedAudios.Count > 0 ? uploadedAudios : null;
                request.omni_reference_task_type = trackPayload.OmniReferenceTaskType;
                return;
            }

            if (Seedance2TrackPayload.IsVideoEditModel(model))
            {
                request.video_url = uploadedVideos[0];
                request.images_list = uploadedImages.Count > 0 ? uploadedImages : null;
                request.audios_list = uploadedAudios.Count > 0 ? uploadedAudios : null;
                return;
            }

            if (Seedance2TrackPayload.IsVideoExtendModel(model))
            {
                request.video_url = uploadedVideos[0];
                request.last_image = uploadedImages.Count > 0 ? uploadedImages[0] : null;
            }
        }

        private static int GetMaxImageCount(string model)
        {
            if (Seedance2TrackPayload.IsVideoExtendModel(model))
            {
                return 1;
            }

            if (Seedance2TrackPayload.IsFirstLastFrameModel(model))
            {
                return 2;
            }

            if (Seedance2TrackPayload.IsImageToVideoModel(model) && Seedance2TrackPayload.IsSeedance25SpicyModel(model))
            {
                return model == Seedance2TrackPayload.Model25SpicyI2V ? 2 : 1;
            }

            if (Seedance2TrackPayload.IsSeedance25SpicyModel(model))
            {
                return 30;
            }

            return 9;
        }

        private static int GetMaxVideoCount(string model)
        {
            if (Seedance2TrackPayload.IsVideoEditModel(model) || Seedance2TrackPayload.IsVideoExtendModel(model))
            {
                return 1;
            }

            return Seedance2TrackPayload.IsSeedance25SpicyModel(model) ? 10 : 3;
        }

        private static string GetReferenceRequirementError(string model, int imageCount, int videoCount)
        {
            if (Seedance2TrackPayload.IsImageToVideoModel(model) && imageCount == 0)
            {
                return "Seedance image-to-video requires at least one input image";
            }

            if (Seedance2TrackPayload.IsFirstLastFrameModel(model) && imageCount != 2)
            {
                return "Seedance first-last-frame requires exactly two input images";
            }

            if ((Seedance2TrackPayload.IsVideoEditModel(model) || Seedance2TrackPayload.IsVideoExtendModel(model)) && videoCount != 1)
            {
                return "Seedance video edit/extend requires exactly one input video";
            }

            return "";
        }

        private static (bool Success, string Error, List<string> Files) CollectReferenceFiles(IEnumerable<string> additionalFiles, int maxCount)
        {
            var files = additionalFiles.Where(path => !string.IsNullOrWhiteSpace(path)).ToList();

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
