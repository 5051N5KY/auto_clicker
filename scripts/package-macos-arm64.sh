#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/AutoKeyPresser.Mac/AutoKeyPresser.Mac.csproj"
PUBLISH="$ROOT/AutoKeyPresser.Mac/bin/Release/net8.0/osx-arm64/publish"
OUTPUT="$ROOT/dist/MacSilicon"
APP="$OUTPUT/AutoKeyPresser.app"

dotnet publish "$PROJECT" -c Release -r osx-arm64 --self-contained true --configfile "$ROOT/NuGet.Config"
rm -rf "$APP" "$OUTPUT/AutoKeyPresser-macOS-arm64.zip"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH/"* "$APP/Contents/MacOS/"
cp "$ROOT/AutoKeyPresser.Mac/Packaging/Info.plist" "$APP/Contents/Info.plist"
cp "$ROOT/AutoKeyPresser.Mac/Assets/AppIcon.icns" "$APP/Contents/Resources/AppIcon.icns"
chmod +x "$APP/Contents/MacOS/AutoKeyPresser"

# Ad-hoc signing is suitable for local/test builds. Developer ID signing and
# notarization can be added later when Apple credentials are available.
codesign --force --deep --sign - --entitlements "$ROOT/AutoKeyPresser.Mac/Packaging/AutoKeyPresser.entitlements" "$APP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$OUTPUT/AutoKeyPresser-macOS-arm64.zip"
echo "Created $OUTPUT/AutoKeyPresser-macOS-arm64.zip"
