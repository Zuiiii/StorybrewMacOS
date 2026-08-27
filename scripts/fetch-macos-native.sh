#!/bin/sh
set -eu

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
target_dir="$repo_root/native/osx-arm64"
temp_dir=$(mktemp -d)
trap 'rm -rf "$temp_dir"' EXIT INT TERM

mkdir -p "$target_dir"

curl -fsSL 'https://www.un4seen.com/files/bass24-osx.zip' -o "$temp_dir/bass.zip"
curl -fsSL 'https://www.un4seen.com/files/z/0/bass_fx24-osx.zip' -o "$temp_dir/bass_fx.zip"

echo 'dfadd6238896b02b144b2870655fb9ee2445fc84d5c00f7e1c56faf9343ce59c  bass.zip' > "$temp_dir/checksums"
echo 'eb9e496da229cdd73dc51d16c7e3fd17c7400a8125c2be2feb88eee1ac68dc68  bass_fx.zip' >> "$temp_dir/checksums"
(cd "$temp_dir" && shasum -a 256 -c checksums)

unzip -q -o -j "$temp_dir/bass.zip" libbass.dylib -d "$target_dir"
unzip -q -o -j "$temp_dir/bass_fx.zip" libbass_fx.dylib -d "$target_dir"

for library in "$target_dir/libbass.dylib" "$target_dir/libbass_fx.dylib"; do
    if ! lipo -archs "$library" | tr ' ' '\n' | grep -qx arm64; then
        echo "$library does not contain arm64 code" >&2
        exit 1
    fi
done

sdl_library=""
for candidate in /opt/homebrew/lib/libSDL2-2.0.0.dylib /usr/local/lib/libSDL2-2.0.0.dylib; do
    if [ -f "$candidate" ]; then sdl_library="$candidate"; break; fi
done
if [ -z "$sdl_library" ]; then
    echo "SDL2 is required to build the app bundle (install it with: brew install sdl2)" >&2
    exit 1
fi
chmod u+w "$target_dir/libSDL2-2.0.0.dylib" 2>/dev/null || true
cp "$sdl_library" "$target_dir/libSDL2-2.0.0.dylib"
if ! lipo -archs "$target_dir/libSDL2-2.0.0.dylib" | tr ' ' '\n' | grep -qx arm64; then
    echo "$sdl_library does not contain arm64 code" >&2
    exit 1
fi

echo "Installed native macOS libraries in $target_dir"
