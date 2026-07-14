#!/usr/bin/env bash
set -euo pipefail

# Cross-platform built-in plugin publisher.
#
# Usage:
#   ./BuildInPlugins/publish.sh <output-directory> [runtime-id]
#
# Examples:
#   ./BuildInPlugins/publish.sh ../publish_linux_licensed/plugins linux-x64
#   ./BuildInPlugins/publish.sh ../publish_win_plugins win-x64
#
# Signing:
#   Set BUILD_SIGNING_CERT_BASE64 to the base64 PFX used by the existing
#   Windows workflow, and BUILD_SIGNING_CERT_PASSWORD to the PFX password.
#   Set BUILD_PLUGIN_SIGNING=false to skip signing.

PLUGIN_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <output-directory> [runtime-id]" >&2
  exit 2
fi

output_dir="$1"
rid="${2:-linux-x64}"
sign_plugins="${BUILD_PLUGIN_SIGNING:-true}"
sign_base64="${BUILD_SIGNING_CERT_BASE64:-}"
sign_password="${BUILD_SIGNING_CERT_PASSWORD:-}"
signing_pfx="${BUILD_SIGNING_CERT_PFX:-}"

publish_plugin() {
  local project="$1"
  local name
  name="$(basename "$project" .csproj)"
  echo "Building $name"
  dotnet publish "$PLUGIN_DIR/$project" \
    -c Release \
    -p:RuntimeIdentifier="$rid" \
    -p:RuntimeIdentifierOverride="$rid" \
    /maxcpucount:1 \
    -o "$output_dir"
}

sign_plugin_outputs() {
  if [[ "$sign_plugins" == "false" ]]; then
    echo "Skipping built-in plugin signing."
    return 0
  fi

  if ! command -v osslsigncode >/dev/null 2>&1; then
    echo "ERROR: osslsigncode is required to sign built-in plugin DLLs." >&2
    exit 1
  fi

  if [[ -z "$sign_password" ]]; then
    echo "ERROR: BUILD_SIGNING_CERT_PASSWORD must be set to sign built-in plugins." >&2
    exit 1
  fi

  local temp_pfx=""

  if [[ -z "$signing_pfx" ]]; then
    if [[ -z "$sign_base64" ]]; then
      echo "ERROR: BUILD_SIGNING_CERT_BASE64 must be set to sign built-in plugins." >&2
      exit 1
    fi

    temp_pfx="$(mktemp "${TMPDIR:-/tmp}/lvs-buildinplugins-signing.XXXXXX.pfx")"
    signing_pfx="$temp_pfx"
    printf '%s' "$sign_base64" | base64 -d > "$signing_pfx"
    chmod 600 "$signing_pfx"
  elif [[ ! -f "$signing_pfx" ]]; then
    echo "ERROR: BUILD_SIGNING_CERT_PFX does not exist: $signing_pfx" >&2
    exit 1
  fi

  echo "Signing built-in plugin DLLs..."
  while IFS= read -r -d '' dll; do
    local signed_file="${dll}.signed"
    osslsigncode sign \
      -pkcs12 "$signing_pfx" \
      -pass "$sign_password" \
      -h sha256 \
      -n "Lyric Video Studio Built-in Plugin" \
      -in "$dll" \
      -out "$signed_file"
    mv "$signed_file" "$dll"
  done < <(find "$output_dir" -maxdepth 1 -type f -name '*.dll' -print0)

  if [[ -n "$temp_pfx" ]]; then
    rm -f "$temp_pfx"
  fi
}

rm -rf "$output_dir"
mkdir -p "$output_dir"

publish_plugin "PluginInterface/PluginBase.csproj"
publish_plugin "CroppedImagePlugin/CroppedImagePlugin.csproj"
publish_plugin "A1111Img2ImgPlugin/A1111Img2ImgPlugin.csproj"
publish_plugin "A1111TxtToImgPlugin/A1111TxtToImgPlugin.csproj"
publish_plugin "OpenAiTxtToImgPlugin/OpenAiTxtToImgPlugin.csproj"
publish_plugin "StabilityAiImgToVidPlugin/StabilityAiImgToVidPlugin.csproj"
publish_plugin "StabilityAiTxtToImgPlugin/StabilityAiTxtToImgPlugin.csproj"
publish_plugin "LumaAiDreamMachine/LumaAiDreamMachinePlugin.csproj"
publish_plugin "RunwayMl/RunwayMlPlugin.csproj"
publish_plugin "Bfl/BflAiTxtToImgPlugin.csproj"
publish_plugin "Minimax/MinimaxPlugin.csproj"
publish_plugin "MusicGpt/MusicGptPlugin.csproj"
publish_plugin "Wan/WanPlugin.csproj"
publish_plugin "Elevenlabs/ElevenLabsPlugin.csproj"
publish_plugin "MistralAI/MistralAiTxtToImgPlugin.csproj"
publish_plugin "FalAi/FalAiPlugin.csproj"
publish_plugin "Google/GooglePlugin.csproj"
publish_plugin "LTX/LTXPlugin.csproj"
publish_plugin "MuApi/MuApiPlugin.csproj"

sign_plugin_outputs

echo "Built-in plugins published: $output_dir"
