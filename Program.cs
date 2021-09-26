using System;
using System.IO;

using Newtonsoft.Json;

namespace rf3lez
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var reader = new BinaryReader(File.OpenRead("./micah_swimsuit.nsbmd")))
            {
                var nitroReader = new NitroReader();
                var parsed = nitroReader.Parse(reader);
                var serialized = JsonConvert.SerializeObject(parsed);

                File.WriteAllText("./micah.json", serialized);

                File.WriteAllBytes("./pc00_s1.bin", (parsed.Containers[1] as TextureContainer).TextureTable.Entries["PC00_S1"].Data);
                File.WriteAllBytes("./pc00_s2.bin", (parsed.Containers[1] as TextureContainer).TextureTable.Entries["PC00_S2"].Data);
            }
        }
    }
}
