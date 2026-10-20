# Renderer comparison — 20 October 2026

The current macOS renderer was compared pixel by pixel with the installed official osu! assemblies at nine INVERSION timestamps: 1000, 32775, 34190, 116395, 178556, 187633, 270500, 305500 and 323476 ms.

Both implementations received identical exported commands, assets, timestamps and a 2732 × 1536 viewport. The reference used LegacyStoryboardDecoder and DrawableStoryboardSprite through osu.Framework's Metal backend. Storybrew used EditorOsbSprite.Draw and BrewLib's OpenGL renderer. The comparison excluded editor chrome, hitobjects and gameplay dimming. RGB measurements used the original image resolution, without registration or colour adjustment.

The initial reference harness incorrectly used TextureStore's default scale adjustment. Those captures were discarded; the corrected harness uses scaleAdjust: 1, matching official DrawableStoryboard.

Across the sampled frames, mean absolute RGB error was 0.40–0.90 out of 255, and over 99.8% of pixels differed by at most three channel levels. Exact agreement was only 4–25%; these results establish neither pixel identity nor universal compatibility. Larger localized differences remain, including a maximum channel error of 142 at the ending transition. A second comparison using the original export in both renderers produced similarly close results.

The regenerated storyboard also differs from the original when both use the official renderer, particularly in individual spectrum bar heights. This is a generation discrepancy and is not resolved by matching display scale. Full-frame scores are dominated by the background and do not certify font-generation or FFT parity. Animations, videos, loops, triggers and skin fallback were not covered by this fixture.

The font/import/texture-upload tests passed (21 tests). The full suite recorded 51 passes and one existing Boolean-loop failure. The macOS font backend supports built-in effects; custom effects implemented against System.Drawing and GetMapsetBitmap remain Windows-only. Skia font output is not guaranteed to reproduce GDI rasterization exactly.

Reference implementation:

- https://github.com/ppy/osu/blob/master/osu.Game/Storyboards/Drawables/DrawableStoryboard.cs
- https://github.com/ppy/osu/blob/master/osu.Game/Storyboards/Drawables/DrawableStoryboardSprite.cs

Tested official assembly SHA-256 hashes:

- osu.Game.dll: ac2dd187bcf4f556412f46b1135f44fc33e0093cec7e0e0a9664568be263df10
- osu.Framework.dll: de0ef71a9cbfdf7b680342d69f649db1c5d38178730783d8a44832fe56e76d74
