# Lyric Video Studio AI Plugins for Image, Video and Audio Generation

`BuildInPlugins` is the built-in and example plugin collection for [Lyric Video Studio](https://lyricvideo.studio/), the AI-powered desktop video editor positioned on the official site for musicians and content creators. It shows how Lyric Video Studio connects to modern GenAI providers for `text-to-image`, `image-to-image`, `text-to-video`, `image-to-video`, `AI voice`, `text-to-speech`, and `AI music generation` workflows inside a frame-accurate, timeline-based editor.

If you are looking for:

- AI video generator plugins for Lyric Video Studio
- text-to-image and image-to-video integrations
- GenAI media automation examples in C#
- sample plugins for creative tools, creator platforms, or AI product teams this is the repo.

## What This Repository Is

This folder serves two jobs:

1. It contains the plugins bundled with some Lyric Video Studio distributions.
2. It acts as a real-world reference implementation for developers building their own plugins on top of the `PluginInterface` contracts.

For end users, these plugins unlock cloud AI generation inside Lyric Video Studio, so AI outputs can land directly on the project timeline instead of being shuffled between separate tools.

For developers and GenAI businesses, this repo is a practical plugin SDK example showing how to wire provider APIs into a timeline-based creative app with typed payloads, dynamic settings UIs, validation, progress updates, content upload helpers, and track/item level prompting.

## Official Product Positioning

Across the main site and AI feature pages, Lyric Video Studio is presented as:

- a frame-accurate lyric and music video editor
- an AI-powered desktop video editor for musicians and content creators
- a local-plus-cloud workflow that supports bring-your-own-key provider integrations
- a tool where generated media drops straight into the timeline for fast iteration

The official AI pages also highlight support for `25+ AI models` across `11 service providers` plus local generation options, making this repository relevant both to creative end users and to teams researching multi-provider GenAI product architecture.

## Who This Is For

- Creators who want AI image, video, narration, or music generation directly inside Lyric Video Studio
- Steam users who need to install external AI plugins manually
- Product teams building GenAI media workflows or white-label creative tooling
- C# developers who want example implementations for image, video, and audio plugin architecture
- AI startups and agencies that need repeatable content pipelines instead of one-off demos

## Included AI Plugin Projects

The source tree currently includes plugin projects for the following providers and use cases.

| Folder | Provider / Plugin | Main modality |
| --- | --- | --- |
| `A1111TxtToImgPlugin` | Automatic1111 | Local text-to-image |
| `A1111Img2ImgPlugin` | Automatic1111 | Local image-to-image and frame-based workflows |
| `Bfl` | Black Forest Labs | AI image generation and image editing workflows |
| `CroppedImagePlugin` | Built-in utility | Crop and scale source images for downstream AI pipelines |
| `Elevenlabs` | ElevenLabs | AI speech, narration, and music generation |
| `FalAi` | fal.ai | Multi-model image, video, and audio generation |
| `Google` | Google | Gemini image, Imagen, Veo video, TTS, and Lyria music workflows |
| `KlingAi` | Kling AI | Image and video generation, plus lip sync workflows |
| `LTX` | LTX Video | AI video generation |
| `LumaAiDreamMachine` | Luma AI Dream Machine | Image-to-video, generation upscaling, and add-audio workflows |
| `Minimax` | MiniMax | Image, video, speech, and music generation |
| `MistralAI` | Mistral AI | AI image generation |
| `MuApi` | MuApi | Image and video generation across multiple hosted models |
| `MusicGpt` | MusicGPT | AI music and speech generation |
| `OpenAiTxtToImgPlugin` | OpenAI | AI image generation |
| `RunwayMl` | Runway | AI video generation and upscale workflows |
| `StabilityAiTxtToImgPlugin` | Stability AI | Text-to-image |
| `StabilityAiImgToVidPlugin` | Stability AI | Image-to-video |
| `Wan` | Alibaba Cloud WAN / Model Studio | AI video generation |

## Notable Model Coverage in the Current Source

Several plugins expose multiple model families behind one integration. Examples visible in this repo include:

- Google: `Gemini`, `Imagen`, `Veo`, `Lyria`
- Runway: `gen4.5`, `act_two`, `upscale_v1`, `gen4_aleph`, `gen4_turbo`, `gen3a_turbo`
- MuApi: `GPT Image 2`, `Midjourney V8`, `Seedance 2`, `Happy Horse 1`, `Vidu Q2 Turbo`
- FalAi: Veo 3.1, Veo 3.1 Fast, Veo 3.1 Image to Video, Veo 3.1 Fast Image to Video, Veo 3.1 Reference to Video, Veo 3.1 First Last Frame to Video, MiniMax Hailuo 2.3 Fast Standard Image to Video, MiniMax Hailuo 2.3 Fast Pro Image to Video, Wan 2.7 Text to Video, Wan 2.7 Image to Video, Wan 2.7 Reference to Video, Wan 2.7 Edit Video, Wan 2.6 Text to Video, Wan 2.6 Image to Video, Wan 2.5 Preview Text to Video, Wan 2.5 Preview Image to Video, Wan Alpha, Kling O3 Pro Text to Video, Kling O3 Pro Image to Video, Kling V3 Pro Motion Control, Kling AI Avatar V2 Pro, Kling V2.6 Pro Text to Video, Kling V2.6 Pro Image to Video, Kling O1 Image to Video, Kling V2.6 Pro Motion Control, Kling V2.6 Standard Motion Control, Kling V2.5 Turbo Pro Image to Video, Kling V2.5 Turbo Pro Text to Video, LTX Video 2 Text to Video Fast, LTX Video 2 Text to Video, LTX Video 2 Image to Video Fast, LTX Video 2 Image to Video, PixVerse V6 Text to Video, PixVerse V6 Image to Video, PixVerse V5.6 Text to Video, PixVerse V5.6 Image to Video, ByteDance DreamActor V2, ByteDance Seedance 1.5 Pro Text to Video, ByteDance Seedance 1.5 Pro Image to Video, ByteDance OmniHuman 1.5, SeedVR Video Upscale, Lucy Edit Pro, Decart Lucy Restyle, Editto, One to All Animation 1.3B, One to All Animation 14B, Creatify Aurora, ByteDance Seedance 2.0 Image to Video, ByteDance Seedance 2.0 Text to Video, ByteDance Seedance 2.0 Reference to Video, Alibaba Happy Horse Text to Video, Alibaba Happy Horse Image to Video, Alibaba Happy Horse Reference to Video, Alibaba Happy Horse Video Edit, Z Image Turbo, Ovis Image, HiDream I1 Full, GLM Image, ImagineArt 1.5 Pro Preview Text to Image, Qwen Image 2512, Qwen Image Edit 2511, Imagen 4 Preview, Wan 2.2 A14B Text to Image, Wan 2.5 Preview Text to Image, Wan 2.5 Preview Image to Image, ByteDance Seedream V5 Lite Text to Image, ByteDance Seedream V5 Lite Edit, ByteDance Seedream V4.5 Text to Image, ByteDance Seedream V4 Text to Image, ByteDance Seedream V4 Edit, GPT Image 2, GPT Image 2 Edit, GPT Image 1.5, GPT Image 1.5 Edit, GPT Image 1 Mini, GPT Image 1 Mini Edit, VibeVoice 7B, VibeVoice
- MiniMax: Hailuo 2.3, video and image, speech and music model variants

That makes this repo especially useful if you are researching how to support multiple AI providers and multiple media modalities inside one product.

## Why End Users Use These Plugins

- Generate AI images, AI videos, voiceovers, and music without leaving Lyric Video Studio
- Generate directly to the timeline instead of exporting and re-importing assets between apps
- Reuse prompts and settings at track level and item level
- Mix local generation with hosted GenAI services in a BYOK setup
- Import from images, video frames, lyrics, media references, or existing generation IDs depending on the plugin
- Keep production workflows in one timeline instead of juggling separate tools
- Combine AI workflows with Lyric Video Studio’s musician-first editing model, including lyric sync, multi-track editing, vertical exports, and 4K output

## Why Developers and GenAI Teams Use This Repo

- The `PluginInterface` project gives you typed contracts for `IImagePlugin`, `IVideoPlugin`, and `IAudioPlugin`
- Create your own plugins and join Lyric Video Studio ecosystem. Keep the revenue from users, Lyric Video Studio has no part in it :)
- Plugin settings and payloads are reflected dynamically into the app UI from public serializable objects
- The examples show validation, async generation, cancellation, progress reporting, file reference tracking, and settings persistence
- Several plugins demonstrate multi-model routing, provider-specific payload visibility, and content upload requirements
- The codebase is useful for SaaS teams, internal creative tooling teams, and AI product engineers building extensible media generation systems

## Installation for Lyric Video Studio Users

Lyric Video Studio is distributed through multiple channels on the official site:

- direct Windows installer
- Microsoft Store
- Steam
- Microsoft Store LITE edition

### Direct installer builds

The official download pages position the direct installer as the fastest update path. If you installed Lyric Video Studio directly from `lyricvideo.studio`, updating the app is generally enough to get the newest bundled plugin set.

### Microsoft Store builds

Microsoft Store builds already include built-in plugins. If the site announces new plugins and your Store build has not caught up yet, you can update from this repository’s release bundle.

### Steam builds

The official plugin page explicitly states that the Steam version does not ship with built-in plugins. Steam users typically need to install external AI plugins manually because of Steam policy constraints around AI-related integrations and external paid services.

### LITE edition

The official features page says the Microsoft Store LITE edition limits plugin access to a default set and does not include local generative AI. That means cloud plugin availability and local-model expectations differ from the full app.

### How to install plugin releases

1. Download the latest plugin release bundle from: `https://github.com/Lyric-Video-Studio/BuildInPlugins/releases`
2. Extract the zip
3. Copy the plugin files into the `plugins` folder next to the Lyric Video Studio executable
4. Or set a custom plugin directory in Lyric Video Studio settings and point the app to that folder
5. Restart Lyric Video Studio

Lyric Video Studio scans:

- the app-local `plugins` folder
- the extra plugin folder defined in app settings

If a plugin depends on API keys, open plugin settings inside the app and add the required credentials before generating content.

### Update guidance from the official FAQ

- Direct installer users: update the app when notified
- Microsoft Store full/trial users: wait for the next Store update or refresh plugins from this repo
- Steam users: refresh plugins from this repo
- LITE users: plugin availability follows the LITE release cycle and edition limits

### Regeneration tip from the official plugin page

For many cloud plugins, if you want a fresh result instead of re-fetching the previous one, clear the stored `polling id` before generating again.

## Building and Publishing the Plugins

This repository includes a `publish.bat` script that publishes the plugin interface and selected plugin projects.

If you want to build manually, each plugin is its own `.csproj` under this folder. Example pattern:

```powershell
dotnet publish .\PluginInterface\PluginBase.csproj -c Release -o publish\plugins
dotnet publish .\Google\GooglePlugin.csproj -c Release -o publish\plugins
```

## Creating Your Own Lyric Video Studio Plugin

Start with `BuildInPlugins\PluginInterface`.

The developer flow is:

1. Reference `PluginBase.csproj` or the published plugin interface assembly
2. Implement one or more of `IImagePlugin`, `IVideoPlugin`, or `IAudioPlugin`
3. Define serializable settings, track payloads, and item payloads
4. Publish your plugin assembly and copy it under a scanned plugin folder
5. Restart the app and test initialization inside plugin settings

This repo is especially helpful because it includes examples for:

- hosted API integrations
- local tool integrations
- image, video, and audio plugin types
- prompt-based generation
- reference-media workflows
- generation polling and resume behavior
- dynamic per-model setting visibility

## Important Notes

- Not every source project is guaranteed to be included in every release bundle
- Some app editions may whitelist only a subset of plugins
- Many plugins require provider-side credits, API keys, or both
- Some video providers need publicly reachable media URLs, which Lyric Video Studio can help supply through its content upload flow
- The official site describes the cloud integrations as BYOK: Lyric Video Studio does not provide bundled provider credits or third-party access tokens
- The FAQ says plugin keys are stored in Windows Credential Manager and are not logged in plain text

## Related Links

- Main site: `https://lyricvideo.studio/`
- AI landing page: `https://lyricvideo.studio/generate-ai/`
- Plugin help page inside the app points to: `https://lyricvideo.studio/plugins/`
- Features: `https://lyricvideo.studio/features/`
- Download & pricing: `https://lyricvideo.studio/buy/`
- FAQ: `https://lyricvideo.studio/f-a-q/`
- Releases: `https://github.com/Lyric-Video-Studio/BuildInPlugins/releases`
- Plugin interface examples: `BuildInPlugins\PluginInterface`

If you want Lyric Video Studio to function as an extensible AI video editor, AI music workflow tool, or GenAI media production frontend, this repository is the reference implementation for that plugin layer.
