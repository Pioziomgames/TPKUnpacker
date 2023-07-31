using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace TPKUnpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 )
            {
                Console.WriteLine("TPKUnpacker 1.0 by Pio");
                Console.WriteLine("Extracts Sol Trigger's TPK files");

                Console.WriteLine("\nPress any key to exit");
                Console.ReadKey();
                return;
            }

            TPKFile tpk;
            using (BinaryReader reader = new(File.OpenRead(args[0])))
            {
                int magic = reader.ReadInt32();

                if (magic == 811227738)
                {
                    int uncompressedSize = reader.ReadInt32();

                    int reserved = reader.ReadInt32();

                    byte[] compressedData = reader.ReadBytes((int)reader.BaseStream.Length - 12);

                    byte[] decompressedData = DecompressZlibData(compressedData, uncompressedSize);

                    using (BinaryReader ureader = new(new MemoryStream(decompressedData)))
                    {
                        tpk = new(ureader);
                    }
                }
                else
                {
                    reader.BaseStream.Position = 0;
                    tpk = new(reader);
                }
                
            }
            string OutDir = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]));
            Directory.CreateDirectory(OutDir);
            for (int i = 0; i < tpk.Entries.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(OutDir, tpk.Entries[i].Name), tpk.Entries[i].Data);
            }
        }
        
        private static byte[] DecompressZlibData(byte[] compressedData, int uncompressedSize)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var inflater = new InflaterInputStream(compressedStream))
            {
                byte[] decompressedData = new byte[uncompressedSize];
                inflater.Read(decompressedData, 0, uncompressedSize);
                return decompressedData;
            }
        }
    }
    public class TPKFile
    {
        public List<TPKEntry> Entries { get; set; }
        public struct TPKEntry
        {
            public string Name { get; set; }
            public byte[] Data { get; set; }
            public TPKEntry(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
        }
        public TPKFile(BinaryReader reader)
        {
            Entries = new List<TPKEntry>();
            int Value = reader.ReadInt32();
            int FileCount = reader.ReadInt32();
            reader.BaseStream.Position -= 8;
            int[] Offsets = new int[FileCount];
            string[] Names = new string[FileCount];
            for (int i = 0; i < FileCount; i++)
            {
                reader.BaseStream.Position += 8;
                Offsets[i] = reader.ReadInt32();
                Names[i] = new String(reader.ReadChars(20)).Replace("\0","");
            }
            for (int i = 0; i < FileCount;i++)
            {
                if (Offsets[i] != 0)
                {
                    reader.BaseStream.Position = Offsets[i];
                    int Size = (int)reader.BaseStream.Length - Offsets[i];
                    if (i != FileCount - 1)
                    {
                        int ii = i + 1;
                        int end = 0;
                        while (end == 0 && ii < FileCount)
                        {
                            end = Offsets[ii];
                            ii++;
                        }
                    }
                    byte[] Data = reader.ReadBytes(Size);
                    Entries.Add(new(Names[i], Data));
                }
            }
        }
    }

}