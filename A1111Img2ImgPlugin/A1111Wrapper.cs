using PluginBase;
using System.Diagnostics;

namespace A1111ImgToImgPlugin
{
    public class A1111Wrapper
    {
        // TODO: Prepare this for intanciating, so less static thinngs
        // Keep process static. Rememebr that the uri can be to server / internets, so process might not be needed

        private static Process testA111Process;
        private Client client;

        public Client Client => client;

        public void StartA1111(ConnectionSettings settings)
        {
            if (testA111Process != null && !testA111Process.HasExited)
            {
                return;
            }

            var path = settings.A1111Executable.Replace("\"", "");

            if (path.EndsWith(".bat"))
            {
                var content = JsonHelper.ReadAllText(path);
                path = path.Replace(".bat", "modified.bat");

                var split = content.Split('\n');

                for (int i = 0; i < split.Length; i++)
                {
                    if (split[i].StartsWith("set COMMANDLINE_ARGS"))
                    {
                        split[i] += $" --api --nowebui {settings.A1111Args}";
                    }
                }

                File.WriteAllLines(path, split);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = Path.GetDirectoryName(path);
            startInfo.FileName = Path.GetFileName(path);

            startInfo.UseShellExecute = true;
            testA111Process = Process.Start(startInfo);
        }

        public async Task<bool> PingConnection(ConnectionSettings settings)
        {
            var firstRes = await EnsureApiAvailableAsync(settings);

            if (firstRes)
            {
                return true;
            }

            var success = false;
            var index = 0;

            while (!success && index < 5)
            {
                try
                {
                    var resp = await client.Get_memory_sdapi_v1_memory_getAsync();
                    return resp != null;
                }
                catch (Exception)
                {
                }

                await Task.Delay(2000);
                index++;
            }

            return success;
        }

        private bool isInitializing = false;

        private async Task<bool> EnsureApiAvailableAsync(ConnectionSettings settings)
        {
            while (isInitializing)
            {
                await Task.Delay(200);
            }

            isInitializing = true;

            if (client == null)
            {
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60 * 3);
                client = new Client(settings.A1111Url, httpClient);
            }

            try
            {
                var resp = await client.Get_memory_sdapi_v1_memory_getAsync();
                if (resp != null)
                {
                    isInitializing = false;
                    return true;
                }
            }
            catch (Exception)
            {
            }

            if (testA111Process == null || testA111Process.HasExited)
            {
                try
                {
                    StartA1111(settings);
                    await Task.Delay(10000);
                }
                catch (Exception)
                {
                }
                finally
                {
                    isInitializing = false;
                }
            }

            isInitializing = false;

            return false;
        }

        public async Task<ImageResponse> GetImgToImg(StableDiffusionProcessingImg2Img payload, ConnectionSettings settings, string checkpoint)
        {
            await EnsureApiAvailableAsync(settings);

            try
            {
                if (payload.Seed == 0)
                {
                    payload.Seed = new Random().Next(int.MinValue, int.MaxValue);
                }

                if (!string.IsNullOrEmpty(checkpoint))
                {
                    var config = await client.Get_config_sdapi_v1_options_getAsync();

                    if (config.Sd_model_checkpoint as string != checkpoint)
                    {
                        config.Sd_model_checkpoint = checkpoint;
                        try
                        {
                            await client.Set_config_sdapi_v1_options_postAsync(config);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                var resp = await client.Img2imgapi_sdapi_v1_img2img_postAsync(payload);

                var par = resp.Parameters.ToString();

                // Bit dirty

                var seed = "";

#pragma warning disable CS0168 // Variable is declared but never used
                try
                {
                    var parList = par.Trim('{').Trim('{').Trim('}').Trim('}').Split('\n');
                    seed = parList.FirstOrDefault(s => s.Contains("seed")).Split(':')[1].Trim('"').Trim('\r').Trim(',');
                }
                catch (Exception e)
                {
                    // This did not work out, leave it be
                }
#pragma warning restore CS0168 // Variable is declared but never used

                return new ImageResponse { Success = true, Image = resp.Images.First(), Params = new List<(string, string)> { ("seed", seed) }, ImageFormat = "png" };
            }
            catch (Exception ex)
            {
                return new ImageResponse { Success = false, ErrorMsg = ex.Message };
            }
        }

        internal void CloseConnection()
        {
            if (client != null)
            {
                client = null;
            }

            if (testA111Process != null)
            {
                testA111Process.CloseMainWindow();
            }
        }

        public async Task<string[]> GetSelectionForProperty(string property, ConnectionSettings settings)
        {
            string[] result = null;

            var retryIndex = 3;
            Exception lastException = null;

            while (retryIndex >= 0 && result == null)
            {
                retryIndex--;

                try
                {
                    switch (property)
                    {
                        case nameof(TrackPayload.Upscaler):
                            var resp = await client.Get_upscalers_sdapi_v1_upscalers_getAsync();
                            return resp.Select(x => x.Name).ToArray();

                        case nameof(StableDiffusionProcessingImg2Img.Sampler_name):
                            await EnsureApiAvailableAsync(settings);
                            result = (await client.Get_samplers_sdapi_v1_samplers_getAsync()).Select(s => s.Name).ToArray();
                            break;

                        case nameof(StableDiffusionProcessingImg2Img.Refiner_checkpoint):
                        case nameof(TrackPayload.Sd_model_checkpoint):
                            await EnsureApiAvailableAsync(settings);
                            result = (await client.Get_sd_models_sdapi_v1_sd_models_getAsync()).Select(s => s.Model_name).ToArray();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                /*case "Upscaler":
                    /*var resp = await _wrapper.Client.Get_latent_upscale_modes_sdapi_v1_latent_upscale_modes_getAsync();
                    var lista = resp.Select(x => x.Name).ToArray();
                    return lista;
                return ["Lanczos", "Nearest", "DAT x2", "DAT x3", "DAT x4", "ESGRAN_4x", "LDSR", "R-ESRGAN 4x+", "R-ESRGAN 4x+ Anime6B", "ScuNET PSNR", "SwinIR 4x"];*/
            }

            if (lastException != null)
            {
                throw lastException;
            }

            return result;
        }
    }
}