using System;
using System.Collections.Generic;
using System.IO;

namespace rf3lez
{
    public class NitroReader
    {
        private Dictionary<string, IContainerReader> ContainerReaderMap  = new System.Collections.Generic.Dictionary<string, IContainerReader> {
            { "TEX0", new TextureContainerReader() },
            { "MDL0", new ModelContainerReader() },
        };

        public NitroFile Parse(BinaryReader reader) {
            NitroFile file = new NitroFile();
            file.MagicNumber = reader.ReadBytes(4);
            file.ByteOrderMark = reader.ReadUInt16();
            Utils.Assert(file.ByteOrderMark == 0xFEFF, "expected a little endian BOM here");
            file.Version = reader.ReadUInt16();
            Utils.Expect(file.Version == 2, "expected version to be 2");
            file.FileSizeBytes = reader.ReadUInt32();
            file.HeaderSize = reader.ReadUInt16();
            Utils.Expect(file.HeaderSize == 16, "expected header size to be 16");
            file.SubfileCount = reader.ReadUInt16();

            for(int i = 0; i < file.SubfileCount; i++) {
                file.SubfileOffsets.Add(reader.ReadUInt32());
            }

            foreach(var offset in file.SubfileOffsets) {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var magicNumber = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4)); 
                Utils.Assert(
                    ContainerReaderMap.TryGetValue(magicNumber, out IContainerReader containerReader), 
                    $"expected to have a container reader for '{magicNumber}'");

                file.Containers.Add(containerReader.Read(offset, reader));
            }
            
            return file;
        }
    }

    public interface IContainerReader
    {
        // nb: these assume we've already read the magic number 
        INitroContainer Read(uint containerOffset, BinaryReader reader);
    }

    public class ModelContainerReader : IContainerReader
    {
        public INitroContainer Read(uint containerOffset, BinaryReader reader)
        {
            return new ModelContainer() 
            {
                Offset = containerOffset,
                FileSize = reader.ReadUInt32(),
            };
        }
    }

    public class TextureContainerReader : IContainerReader
    {
        public INitroContainer Read(uint containerOffset, BinaryReader reader)
        {
            void ExpectPosition(string offset) 
            {
                var offsetInt = int.Parse(offset, System.Globalization.NumberStyles.HexNumber) + containerOffset;
                Utils.Expect(reader.BaseStream.Position == offsetInt, because: "this is the provided offset in the docs");
            }

            var container = new TextureContainer();
            container.Offset = containerOffset;

            ExpectPosition("04");
            reader.ReadBytes(8); // Unknown x 8

            ExpectPosition("0C");
            UInt32 block1LenShr3 = reader.ReadUInt16();         // block1_len_shr_3
            container.TextureLength = (block1LenShr3 << 3);

            container.TextureTableOffset = reader.ReadUInt16(); // textures_off

            reader.ReadBytes(4); // Unknown x 4 
            
            container.TextureOffset = reader.ReadUInt32(); // block1_off

            // from vg-resource
            reader.ReadBytes(4);
            container.StripedTextureU32Length = (uint) (reader.ReadUInt16()) << 3;
            reader.ReadUInt16();
            reader.ReadUInt32();

            container.StripedTextureU32Offset = reader.ReadUInt32();
            container.StripedTextureU16Offset = reader.ReadUInt32();

            reader.ReadBytes(4); // Unknown x 4

            UInt32 block4LenShr3 = reader.ReadUInt16();
            container.PaletteDataLength = block4LenShr3 << 3;

            reader.ReadBytes(2); // Unknown x 2

            ExpectPosition("34");
            container.PaletteTableOffset = reader.ReadUInt32();
            container.PaletteDataOffset = reader.ReadUInt32();

            // Now populate the name lists.
            // First, textures:
            // FIXME: Roll this into a method. This is a bit "here be dragons."
            reader.BaseStream.Seek(containerOffset + container.TextureTableOffset, SeekOrigin.Begin);
            var textureList = new NameList<TextureInfo>();
            reader.ReadBytes(1);
            textureList.NamesCount = reader.ReadByte();
            textureList.NameListSizeBytes = reader.ReadUInt16();

            // Skip the unknown table bit.
            reader.ReadBytes(8);
            reader.ReadBytes(4 * textureList.NamesCount);

            textureList.ElementSize = reader.ReadUInt16();
            textureList.DataSectionSize = reader.ReadUInt16();

            List<TextureInfo> textures = new List<TextureInfo>();
            for(int i = 0; i < textureList.NamesCount; i++) 
            {
                var textureInfo = new TextureInfo();
                uint textureOffset = reader.ReadUInt16();
                textureInfo.TextureOffset = (textureOffset << 3);
                textureInfo.AbsoluteTextureOffset = textureInfo.TextureOffset + containerOffset + container.TextureOffset;
                textureInfo.TextureImageParams = reader.ReadUInt16();
                textureInfo.MaybeOffset = reader.ReadUInt32(); 
                textures.Add(textureInfo);
            }

            foreach(var tex in textures) {
                var name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(16)).TrimEnd('\0');
                textureList.Entries.Add(name, tex);
            }

            var startOfTextureData = containerOffset + container.TextureOffset;
            for (int i = 0; i < textures.Count; i++) 
            {
                var tex = textures[i];
                var textureLength = Utils.FormatToLength(tex.TextureImageParams, out int imageType, out int width, out int height);

                tex.Type = imageType;
                tex.Width = width;
                tex.Height = height;

                reader.BaseStream.Seek(tex.AbsoluteTextureOffset, SeekOrigin.Begin);

                tex.Data = reader.ReadBytes(textureLength);
            }

            container.TextureTable = textureList;

            // Then, palettes.
            reader.BaseStream.Seek(containerOffset + container.PaletteTableOffset, SeekOrigin.Begin);
            var paletteList = new NameList<PaletteInfo>();
            reader.ReadBytes(1); // Unknown bit
            paletteList.NamesCount = reader.ReadByte(); // Names
            paletteList.NameListSizeBytes = reader.ReadUInt16();

            // Skip the unknown table bit.
            reader.ReadBytes(8);
            reader.ReadBytes(4 * paletteList.NamesCount);

            paletteList.ElementSize = reader.ReadUInt16();
            Utils.Expect(paletteList.ElementSize == 4, because: "constant size");
            paletteList.DataSectionSize = reader.ReadUInt16();

            List<PaletteInfo> palettes = new List<PaletteInfo>();
            var paletteLenBytes = paletteList.NamesCount == 0 ? 0 : container.PaletteDataLength / paletteList.NamesCount;
            Utils.Expect(paletteLenBytes == 512, "256color, 16bpp bgr");

            for(int i = 0; i < paletteList.NamesCount; i++) 
            {
                var paletteInfo = new PaletteInfo();
                UInt32 offsetShr3 = reader.ReadUInt32();
                paletteInfo.Offset = ((offsetShr3) << 3) + container.PaletteDataOffset;
                paletteInfo.AbsolutePaletteOffset = paletteInfo.Offset + + containerOffset;

                palettes.Add(paletteInfo);
            }

            foreach(var pal in palettes) {
                var name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(16)).TrimEnd('\0');
                paletteList.Entries.Add(name, pal);
            }

            // Set data spans for palettes.
            for(int i = 0; i < palettes.Count; i++) {
                reader.BaseStream.Seek(containerOffset + palettes[i].Offset, SeekOrigin.Begin);
                palettes[i].Data = reader.ReadBytes((int) paletteLenBytes);
            }

            container.PaletteTable = paletteList;

            return container;
        }
    }

}