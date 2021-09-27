using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace rf3lez
{
    struct RawColor 
    {
        public byte r, g, b;
    };

    public static class BitmapUtils
    {

        public static Bitmap Generate256ColorBitmap(int w, int h, byte[] paletteData, byte[] textureData)
        {
            List<RawColor> rawPalette = new List<RawColor>();
            using (var reader = new BinaryReader(new MemoryStream(paletteData)))
            {
                while (reader.BaseStream.Length != reader.BaseStream.Position) {
                    var color = reader.ReadUInt16();
                    var b = (byte)((color & 0b0111110000000000) >> 10);
                    var g = (byte)((color & 0b0000001111100000) >> 5);
                    var r = (byte)((color & 0b0000000000011111) >> 0);
                    rawPalette.Add(new RawColor { b = b, g = g, r = r });
                }
            }

            var bmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

            var palette = bmp.Palette;

            for (int i = 0; i < rawPalette.Count; i++) 
            {
                palette.Entries[i] = ColorOf(rawPalette[i]);
            }

            bmp.Palette = palette;

            var bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            // Note: This is coincidentally 8bpp, we may need to use stride here for other image types.
            System.Runtime.InteropServices.Marshal.Copy(textureData, 0, bmpData.Scan0, bmpData.Height * bmpData.Width);

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        static Color ColorOf(RawColor color) {
            //float conversionFactor = (float)(255.0 / 31.0);
            var r = (byte) color.r * 8;
            var g = (byte) color.g * 8;
            var b = (byte) color.b * 8;
            return Color.FromArgb(r, g, b);
        }

        public static byte[] Get256ColorBitmapData(Bitmap bitmap) 
        {
            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            var buffer = new byte[bmpData.Height * bmpData.Width]; // FIXME: Overflow here if stride isn't used and bpp != 1.
            Marshal.Copy(bmpData.Scan0, buffer, 0, buffer.Length);
            return buffer;
        }
    }


}