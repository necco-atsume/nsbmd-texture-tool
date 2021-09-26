using System;

namespace rf3lez
{
    public static class Utils {
        public static void Assert(bool val, string because = "expected statement to be true")
        {
            if (!val) throw new System.Exception($"Assertion failed: ${because}");
        }

        public static void Expect(bool val, string because = "expected statement to be true")
        {
            if (!val) Console.WriteLine("Possible data inconsistency: ${because}");
        }

        readonly static int[] BitDepths = new[] { 0, 8, 2, 4, 8, 2, 8, 16 };

        public static int FormatToLength(ushort p, out int imageType, out int width, out int height)
        {
            /*
            ushort widthMask  = 0b0000_111_000_0000_0_0;
            var widthShift = 9;
            ushort heightMask = 0b0000_000_111_0000_0_0;
            var heightShift = 6;
            ushort imageTypeMask = 0b0000_000_000_1111_0_0;
            var imageTypeShift = 2;
            */
            //                       -cfffhhhwwww---- 
            ushort widthMask     = 0b0000000001110000;
            ushort heightMask    = 0b0000001110000000;
            ushort imageTypeMask = 0b0001110000000000;
            var widthShift = 4;
            var heightShift = 7;
            var imageTypeShift = 10;

            width = 8 << ((p & widthMask) >> widthShift);
            height = 8 << ((p & heightMask) >> heightShift);
            imageType = (p & imageTypeMask) >> imageTypeShift;

            return (BitDepths[imageType] * width * height) / 8;
        }
    }
}