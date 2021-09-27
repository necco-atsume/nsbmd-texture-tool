# NSBMD Texture Tool
[![Build status](https://ci.appveyor.com/api/projects/status/hri9qra1k3s6yp4x?svg=true)](https://ci.appveyor.com/project/corick/nsbmd-texture-tool)

## Tool to edit textures in Nintendo DS .nsbmd and .nsbtx models and textures.
This is based on file format documentation from https://github.com/scurest/nsbmd_docs

### Limitations:
This was designed for a specific romhack, so note that it only supports one of the eight or so possible image formats. You may need to tweak this a bit before it will round-trip your specific nsbmd file. 

## Usage:
First, install the [https://dotnet.microsoft.com/download/dotnet/5.0/runtime](.NET Core 5.0 Runtime)

### Dump Textures As BMPs
`nsbmd-texture-tool.exe dump --source (source file) --output (destination folder) [--json]`
Takes a nsbmd file (--source) and outputs 256-color .BMPs of each of the individual textures to the output directory (--output).

`--json` will output debug data as a JSON file instead of bitmaps. 

example: `./nsbmd-texture-tool.exe dump --source model.nsbmd --output ./exported/`

### Import Textures From BMPs
`nsbmd-texture-tool.exe import --source (source file) --texture (texture bmp) --output (copy) --name (texture name)`
Takes a nsbmd file (--source) and a bitmap (--texture), then re-inserts the texture in place, copying over the texture with the provided name (--name), and saves the new copy to a new file (--output).
Note that the `--name` option is case-sensitive and corresponds to the name of the texture to overwrite in the file. You can determine what this name should be from the name of the bitmap file (for example, if you are importing a bitmap with the name 'TEX01.bmp", you would pass `--name "TEX01").

You can re-insert the modified file using a program like [Tinke](https://github.com/pleonex/tinke)

example: `./nsbmd-texture-tool.exe --source model.nsbmd --texture edited/TEX01.bmp --output model-updated.nsbmd --name TEX01`

## Editing the Output Files:
You will need to use an image editor that supports editing 256 color bitmaps.
Aesprite, or other similar pixel art editors should work.
Note that currently the palette isn't updated when replacing a texture.

## Compiling
Clone the repository, and run `dotnet build`.

💖 claire em 💖
