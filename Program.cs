using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

namespace rf3lez
{
    enum PackAction 
    {
        None,
        Unpack,
        Pack
    };

    class Program
    {
        static void Main(string[] args)
        {
            var dump = new Command("dump", "dump a nsbmd file")
            {
                new Option<FileInfo>("--source", description: "source NSBMD file to dump textures from") { IsRequired = true },
                new Option<DirectoryInfo>("--output", getDefaultValue: () => new DirectoryInfo("."), description: "destination directory to output to") { IsRequired = true },
                new Option<bool>("--json", description: "whether to output in JSON format"),
            };

            var insert = new Command("insert", "insert a texture to a nsbmd file")
            {
                new Option<FileInfo>("--source", description: "source NSBMD file to use as a template") { IsRequired = true },
                // TODO: Re-insert the palette too
                new Option<FileInfo>("--texture", description: "the updated texture, as a 256 color bmp. palette data will be ignored") { IsRequired = true },
                new Option<FileInfo>("--output", description: "the output NSBMD file path") { IsRequired = true },
                // TODO: Infer name from filename here?
                new Option<string>("--name", description: "the texture name to replace, as it appears in the section (ie: PC00_S2)") { IsRequired = true },
            };

            var root = new RootCommand("NSBMD Texture Dumper")
            {
                dump, 
                insert
            };

            dump.Handler = CommandHandler.Create<FileInfo, DirectoryInfo, bool>((source, output, json) => {

                using (var reader = new BinaryReader(source.OpenRead()))
                {
                    var nitroReader = new NitroReader();
                    var nitroFile = nitroReader.Parse(reader);

                    if (!output.Exists) {
                        output.Create();
                    }

                    if (json) 
                    {
                        var jsonText = JsonConvert.SerializeObject(nitroFile, Formatting.Indented);
                        var destinationFile = Path.Combine(output.FullName, source.Name + ".json");
                        File.WriteAllText(destinationFile, jsonText);
                    } else 
                    {
                        var textureContainers = nitroFile.Containers.Where(c => c.MagicNumber == "TEX0");
                        var textures = textureContainers.SelectMany((t) => (t as TextureContainer).TextureTable.Entries).ToList();
                        var palettes = textureContainers.SelectMany((t) => (t as TextureContainer).PaletteTable.Entries).ToList();

                        // HACK: Try to guess which palettes are associated with which textures.
                        Utils.Expect(textures.Count == palettes.Count, "we want to try to assign palettes to textures sequentially");

                        var bitmaps = new Dictionary<string, Bitmap>();
                        for(int i = 0; i < textures.Count; i++) 
                        {
                            var paletteIndex = palettes.Count != textures.Count ? 0 : i;
                            var texture = textures[i];
                            
                            if (texture.Value.Type == 4) 
                            {
                                bitmaps.Add(texture.Key, 
                                    BitmapUtils.Generate256ColorBitmap(
                                        texture.Value.Width, 
                                        texture.Value.Height, 
                                        palettes[paletteIndex].Value.Data,
                                        texture.Value.Data
                                    ));
                            } 
                            else 
                            {
                                // For now only caring about type 4 - 256color indexed.
                                // (since unpacking code for each image type needs to be specific to that format.)
                                throw new NotSupportedException($"Texture type {texture.Value.Type} is not supported.");
                            }
                        }

                        // Save each bitmap.
                        foreach (var bitmap in bitmaps)
                        {
                            var bmpDestiation = new FileInfo(Path.Combine(output.FullName, $"{bitmap.Key}.bmp"));
                            bitmap.Value.Save(bmpDestiation.FullName);
                        }
                    }
                }

            });

            insert.Handler = CommandHandler.Create<FileInfo, FileInfo, FileInfo, string>((source, texture, output, name) => {
                Utils.Assert(source.FullName != output.FullName, "ensure we don't overwrite the source");

                var sourceCopy = File.ReadAllBytes(source.FullName);

                using (var reader = new BinaryReader(source.OpenRead()))
                {
                    var nitroReader = new NitroReader();
                    var nitroFile = nitroReader.Parse(reader);

                    var bitmapData = Bitmap.FromFile(texture.FullName) as Bitmap;
                    var bitmapPixelData = BitmapUtils.Get256ColorBitmapData(bitmapData);

                    var textureContainers = nitroFile.Containers.Where(c => c.MagicNumber == "TEX0");

                    var matchedTexture = textureContainers
                        .SelectMany((a) => (a as TextureContainer).TextureTable.Entries)
                        .Where((t) => t.Key == name)
                        .Select((p) => p.Value)
                        .SingleOrDefault();

                    if (matchedTexture == null)
                        throw new ArgumentException($"Could not find texture '{name}' in table. \n" + 
                            "(Hint: Texture names are case sensitive, and are the same as the file name for the .bmp file created in 'dump'.)");

                    bitmapPixelData.CopyTo(sourceCopy, matchedTexture.AbsoluteTextureOffset);

                    File.WriteAllBytes(output.FullName, sourceCopy);
                }
                
            });

            root.Invoke(args);
        }
    }
}
