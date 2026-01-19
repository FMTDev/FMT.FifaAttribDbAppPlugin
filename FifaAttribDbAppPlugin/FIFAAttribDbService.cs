using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.Core.Readers.FIFA;
using FMT.FileTools;
using FMT.Hash;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public class FIFAAttribDbService : IFIFAAttribDbService
    {
        public FIFAAttribDbService()
        {
        }


        public List<FIFAAttribDbAssetEntry> Types { get; } = new List<FIFAAttribDbAssetEntry>();

        public void ReadAttribDbBinary(byte[] data)
        {
            var reader = new FIFAAttribDbBinaryReader(data);
        }

        public void ReadAttribDbVlt(byte[] data)
        {

        }

        public void ReadAttribDbGameplayBinary(byte[] data)
        {
            var reader = new FIFAAttribDbGameplayBinaryReader(data);
        }

        public List<FIFAAttribDbType> ReadAttribDbGameplayVlt(byte[] data)
        {
            return new FIFAAttribDbGameplayVLTReader(data).ListOfDbTypes;
        }

        public void Load(byte[] vaultData)
        {
            var types = ReadAttribDbGameplayVlt(vaultData);
            foreach (var type in types)
            {
                var entry = new FIFAAttribDbAssetEntry
                {
                    AttribDbType = type,
                    Name = type.Name,
                    Type = "FIFA AttribDbGameplay",
                    Size = 0,
                    OriginalSize = 0,
                    Sha1 = Sha1.Create(Encoding.UTF8.GetBytes(type.Name))
                };
                Types.Add(entry);
            }

            _ = types;
        }

    }

    public static class FieldNameHashLoader
    {
        private static Dictionary<ulong, string>? _fieldNameHashes;

        public static Dictionary<ulong, string> Load()
        {
            if (_fieldNameHashes != null) return _fieldNameHashes;

            var ms = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("FifaAttribDbAppPlugin.FieldNameHashes.csv").CopyTo(ms);
            ms.Position = 0;
            return Load(ms);
        }

        public static Dictionary<ulong, string> Load(MemoryStream ms)
        {
            if (_fieldNameHashes != null) return _fieldNameHashes;

            var dict = new Dictionary<ulong, string>();

            File.WriteAllBytes("tempf.bin", ms.ToArray());
            foreach (var line in File.ReadLines("tempf.bin"))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("string,"))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 2)
                    continue;

                string name = parts[0].Trim();
                string hexBytes = parts[1].Trim();

                // Parse space-separated hex bytes into a byte array
                byte[] bytes = hexBytes
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => byte.Parse(b, NumberStyles.HexNumber))
                    .ToArray();

                if (bytes.Length != 8)
                    throw new InvalidDataException($"Hash for '{name}' does not contain 8 bytes.");

                // Convert little-endian bytes to ulong
                ulong key = BitConverter.ToUInt64(bytes, 0);

                dict[key] = name;
            }

            _fieldNameHashes = dict;
            return _fieldNameHashes;
        }
    }

}
