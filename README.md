# Dino Crisis 3 Model Viewer

<img width="1917" height="1011" alt="Screenshot 2026-10-03 180736" src="https://github.com/user-attachments/assets/b2046eea-a340-4381-b0b9-707d3d6b505d" />

An unofficial Windows tool for browsing, viewing, and extracting assets from the original Xbox version of Dino Crisis 3.

**Status: experimental.** Asset formats are still being reverse-engineered. Some models, animations, and materials may display incorrectly.

## Features

- Open ITK archives, including nested archives.
- Search files and filter by asset type.
- View supported XOM models with matching XOA animations and XTX textures.
- Play, pause, loop, and scrub animation clips.
- Inspect skeletons, wireframes, and individual meshes.
- Export models, skeletons, and animations to GLB for Blender.
- Export supported XTX textures to PNG.
- Convert supported XSP sound banks to PCM WAV.
- Extract original files, including XSS music and BIK videos.
- Export selected files, filtered results, or the entire asset library.

## Requirements

- 64-bit Windows.
- .NET Framework 4.5 or later.
- Your own extracted Dino Crisis 3 game files.

Python is bundled. No separate Python installation or internet connection is required.

## Download and Installation

Download the latest viewer ZIP from this repository’s **Releases** page.

1. Extract the entire ZIP into a folder.
2. Launch `Dino Crisis 3 Model Viewer.exe`.
3. Keep the bundled runtime and Python scripts beside the EXE.

Do not launch the EXE directly from inside a ZIP or WinRAR window.

## Viewing Models

1. Click **Open ITK** and select an archive such as `pack_01.itk`.
2. Allow the archive to finish unpacking on its first opening.
3. Select **Models** in the file-type filter.
4. Search for a model or enter `*.xom`.
5. Select its XOM file in the asset library.

The viewer automatically loads same-name XOA and XTX files from the model’s folder when available.

Choose an animation from the clip list and press **Play**. Enable **Loop clip** for repeated playback.

You can also use **Open folder** to browse extracted game files.

## Controls

| Control | Action |
|---|---|
| Left-drag | Orbit |
| Right-drag | Pan |
| Mouse wheel | Zoom |
| Frame model | Fit the model in the viewport |
| Space | Play or pause |
| Timeline | Scrub the animation |
| Frame buttons | Step backward or forward |
| Skeleton / Wireframe | Toggle inspection overlays |

## Exporting to Blender

1. Open the original XOM model.
2. Click **Save GLB**.
3. In Blender, choose **File → Import → glTF 2.0**.
4. Select the exported GLB.

The GLB includes supported geometry, skinning, textures, and matching animation clips.

Manual display overrides in the viewer are not baked into exported GLBs.

After updating the viewer, reopen the original XOM and export again to apply converter fixes. Existing GLBs do not update automatically.

## Exporting Other Assets

Click **Export assets**, select the export scope, and choose a format:

| Option | Result |
|---|---|
| Original files | Copies assets without conversion |
| XTX → PNG | Exports supported texture images |
| XSP → PCM WAV | Exports supported sound-bank entries |

Exports preserve relative folder paths and include an `export-report.json` listing successful exports, skipped files, and errors.

Music and videos are outside `pack_01.itk`. Use **Open folder** on the game’s extracted `data` folder to locate them.

## Known Limitations

- Animation playback is not guaranteed to match the original game.
- Animation timing currently assumes 30 FPS.
- Fractional source key times are preserved, but interpolation between exported samples remains approximate.
- Non-transform animation channels are not yet reproduced.
- Some models have unresolved animation-node mappings or unsupported geometry.
- Alternate animation filenames are not automatically matched.
- Original shader effects, including some normal, environment, and detail layers, are not reproduced.
- XSS music supports original-file extraction only.
- BIK videos are extracted without conversion to MP4.
- Audio, video, and standalone texture previews are not implemented.
- Eight tested XSP banks contain incomplete ADPCM blocks and currently require original-file extraction.


## Reporting Problems

Please include:

- Viewer version.
- Model and animation filenames.
- Clip number and approximate time or frame.
- A screenshot or short recording.
- Whether the problem occurs in the viewer, Blender, or both.
- Any error message or relevant export-report entry.

A comparison with the same animation in-game is especially useful.

## Building from Source

The C# source and build script are in the `source` folder.

On a compatible Windows development environment, run:

    powershell -ExecutionPolicy Bypass -File source/build.ps1

Keep the Python converter scripts and bundled runtime beside the resulting EXE.

## Disclaimer

This is an unofficial project and is not affiliated with or endorsed by Capcom.

No game files are included.
