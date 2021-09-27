using System;
using System.Collections.Generic;
using System.IO;

// TODO: Rename, To match docs...
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;

namespace rf3lez {

    public class NitroFile
    {
        public u8[] MagicNumber { get; set; }

        public u16 ByteOrderMark  { get; set; }

        public u16 Version { get; set; }

        public u32 FileSizeBytes { get; set; }
        
        public u16 HeaderSize { get; set; }

        public u16 SubfileCount { get; set; }

        public List<u32> SubfileOffsets { get; set; } = new List<u32>();

        public List<INitroContainer> Containers { get; set; } = new List<INitroContainer>();
    }

    public interface INitroContainer {
        public string MagicNumber { get; }
        UInt32 Offset { get; set; }
    };

    public interface IDataBlockLike { 
        byte[] Data { get; set; }
    }

    public class NameList<T> where T: IDataBlockLike {
        public u8 NamesCount { get; set; }
        public u16 NameListSizeBytes { get; set; }

        public u16 ElementSize { get; set; }
        public u16 DataSectionSize { get; set; }

        public Dictionary<string, T> Entries  { get; set; } = new Dictionary<string, T>();
    }

    public class TextureInfo : IDataBlockLike {
        public byte[] Data { get; set; }

        public u32 TextureOffset { get; set; }
        public u32 AbsoluteTextureOffset { get; set; }
        public u16 TextureImageParams { get; set; }

        public u32 MaybeOffset { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int Type { get; set; }
    }

    public class PaletteInfo : IDataBlockLike {
        public byte[] Data { get; set; }
        public u32 Offset { get; set; }
        public u32 AbsolutePaletteOffset { get; set; }
    }

    // We don't care about any of the model contents so just, skip it.
    public class ModelContainer : INitroContainer
    {
        public string MagicNumber => "MDL0";
        public UInt32 Offset { get; set; }
        public u32 FileSize { get; set; }
    }

    public class TextureContainer : INitroContainer 
    {
        public string MagicNumber => "TEX0";
        public UInt32 Offset { get; set; }

        public u16 TextureTableOffset { get; set; }

        public u32 TextureLength { get; set; } // u16
        public u32 TextureOffset { get; set; }

        // Unused for this tex.
        public u32 StripedTextureU32Length { get; set; } // u16
        public u32 StripedTextureU32Offset { get; set; }

        public u32 StripedTextureU16Length => StripedTextureU32Length / 2;
        public u32 StripedTextureU16Offset { get; set; }

        public u32 PaletteTableOffset { get; set; }

        public u32 PaletteDataLength { get; set; } // u16   
        public u32 PaletteDataOffset { get; set; } 

        public NameList<PaletteInfo> PaletteTable { get; set; } = new NameList<PaletteInfo>();

        public NameList<TextureInfo> TextureTable { get; set; } = new NameList<TextureInfo>();
    }



}