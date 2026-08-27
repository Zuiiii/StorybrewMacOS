#!/bin/sh
set -eu

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
artifacts="$repo_root/artifacts"
publish_dir="$artifacts/publish-osx-arm64"
app_dir="$artifacts/Storybrew.app"
contents="$app_dir/Contents"

"$repo_root/scripts/fetch-macos-native.sh"
rm -rf "$publish_dir" "$app_dir"

dotnet publish "$repo_root/editor/editor.csproj" -c Release -r osx-arm64 --self-contained false -o "$publish_dir"
mkdir -p "$contents/MacOS" "$contents/Resources"
cp -R "$publish_dir/." "$contents/MacOS/"

sed \
  -e "s|@EXECUTABLE@|StorybrewEditor|g" \
  -e "s|@VERSION@|1.97|g" \
  "$repo_root/editor/Resources/macOS/Info.plist.in" > "$contents/Info.plist"

chmod +x "$contents/MacOS/StorybrewEditor"
file "$contents/MacOS/StorybrewEditor"
for library in libSDL2-2.0.0.dylib libbass.dylib libbass_fx.dylib; do
    lipo -info "$contents/MacOS/$library"
done

echo "Built $app_dir"
