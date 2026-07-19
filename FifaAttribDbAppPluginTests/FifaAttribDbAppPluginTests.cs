using FifaAttribDbAppPlugin;
using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.FileTools;
using FMT.ServicesManagers;
using FMT.ServicesManagers.Interfaces;
using System.Text;

namespace FifaAttribDbAppPluginTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            SingletonService.RegisterInstance<IAssetManagementService, AssetManagementMockForTests>(new AssetManagementMockForTests());
        }

        [Test]
        public void ReadAttribDbAttribDbBinary()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDb.attribdb.BIN").CopyTo(msAttribDb_Data);
            new FIFAAttribDbBinaryReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplayBinary()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Data);
            new FIFAAttribDbBinaryReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplayVault()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Data);
            new FIFAAttribDbGameplayVLTReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplay()
        {
            var msAttribDb_Vlt_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Vlt_Data);
            var msAttribDb_Bin_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Bin_Data);

            var service = new FIFAAttribDbService();
            service.Load(msAttribDb_Vlt_Data.ToArray(), msAttribDb_Bin_Data.ToArray());
            _ = service.Assets;
        }

        [Test]
        public void ReadWriteAttribDbAttribDbGameplayVault()
        {
            var msAttribDb_Vlt_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Vlt_Data);
            var msAttribDb_Bin_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Bin_Data);

            var attribDbVltData = msAttribDb_Vlt_Data.ToArray();
            var attribDbBinData = msAttribDb_Bin_Data.ToArray();

            FIFAAttribDbService service = new FIFAAttribDbService();
            service.Load(attribDbVltData, attribDbBinData);

            float newValue = 0.05f;
            var movement = service.GetAssetEntry("actor/movement");
            var vanillaValue = movement.AttribDbType.Fields.First(x => x.Name == "ATTR_DribbleJogSpeed").Value;
            movement.AttribDbType.Fields.First(x => x.Name == "ATTR_DribbleJogSpeed").Value = newValue;

            var writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;

            attribDbVltData = msAttribDb_Vlt_Data.ToArray();

            //for (var i = 0; i < attribDbVltData.Length; i++)
            //{
            //    var vanillaByte = attribDbVltData[i];
            //    var writtenByte = writtenVltData[i];
            //    if (writtenByte != vanillaByte
            //        &&
            //        BitConverter.ToSingle(new ReadOnlySpan<byte>(new byte[4] { writtenVltData[i], writtenVltData[i + 1], writtenVltData[i + 2], writtenVltData[i + 3] }))
            //        != newValue
            //        )
            //    {
            //        Debug.WriteLine($"Byte at position {i} is different. Vanilla: {vanillaByte}, Written: {writtenByte}");
            //        throw new Exception($"Byte at position {i} is different. Vanilla: {vanillaByte}, Written: {writtenByte}");
            //    }
            //    else
            //    {
            //        if (attribDbVltData.Length < i + 4)
            //        {
            //            i += 4;
            //        }
            //    }
            //}

        }

        [Test]
        public void Diagnostic_DumpNrtPIndex_GameplayVLT()
        {
            var ms = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(ms);
            var vltData = ms.ToArray();

            var nrtPMarker = Encoding.ASCII.GetBytes("NrtP");
            int nrtPPos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == nrtPMarker[0] && vltData[i + 1] == nrtPMarker[1] &&
                    vltData[i + 2] == nrtPMarker[2] && vltData[i + 3] == nrtPMarker[3])
                {
                    nrtPPos = i;
                    break;
                }
            }

            Assert.That(nrtPPos, Is.GreaterThanOrEqualTo(0), "NrtP marker not found in VLT");

            TestContext.WriteLine($"NrtP marker found at offset 0x{nrtPPos:X4}");

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = nrtPPos + 4; // Skip "NrtP"

            var entryCount = reader.ReadInt();
            TestContext.WriteLine($"NrtP entry count: {entryCount}");

            var entries = new List<(uint offset, uint zero, uint size, ushort flags, ushort unknown)>();
            for (int i = 0; i < Math.Min(entryCount, 50); i++)
            {
                var binOffset = reader.ReadUInt();
                var zero = reader.ReadUInt();
                var size = reader.ReadUInt();
                var flags = reader.ReadUShort();
                var unknown = reader.ReadUShort();
                entries.Add((binOffset, zero, size, flags, unknown));
                TestContext.WriteLine($"  [{i,4}] offset=0x{binOffset:X8} zero=0x{zero:X8} size=0x{size:X4}({size}) flags=0x{flags:X4} unk=0x{unknown:X4}");
            }

            Assert.That(entries.Count, Is.EqualTo(Math.Min(entryCount, 50)));
            TestContext.WriteLine($"Dumped {entries.Count} NrtP entries");
        }

        [Test]
        public void Diagnostic_DumpBINFirstBytes_Gameplay()
        {
            var ms = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(ms);
            var binData = ms.ToArray();

            TestContext.WriteLine($"BIN file size: {binData.Length} bytes (0x{binData.Length:X4})");

            // Dump first 256 bytes in hex
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(binData.Length, 256); i += 16)
            {
                sb.Clear();
                sb.Append($"  0x{i:X4}: ");
                var hexParts = new List<string>();
                var asciiParts = new List<char>();
                for (int j = 0; j < 16 && (i + j) < binData.Length; j++)
                {
                    hexParts.Add(binData[i + j].ToString("X2"));
                    char c = (char)binData[i + j];
                    asciiParts.Add(c >= 32 && c <= 126 ? c : '.');
                }
                sb.Append(string.Join(" ", hexParts));
                sb.Append("  ");
                sb.Append(new string(asciiParts.ToArray()));
                TestContext.WriteLine(sb.ToString());
            }
        }

        [Test]
        public void Diagnostic_DumpFloatCurves_GameplayBIN()
        {
            var ms = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(ms);
            var binData = ms.ToArray();

            TestContext.WriteLine($"BIN file size: {binData.Length} bytes");

            using var reader = new NativeReader(new MemoryStream(binData));
            reader.Position = 0;

            int curveCount = 0;
            while (reader.Position < binData.Length - 8 && curveCount < 10)
            {
                long startPos = reader.Position;
                try
                {
                    ushort numPoints = reader.ReadUShort();
                    ushort capacity = reader.ReadUShort();
                    uint flags = reader.ReadUInt();

                    if (numPoints > 0 && numPoints <= 32 && numPoints == capacity && flags == 4)
                    {
                        var values = new List<float>();
                        for (int p = 0; p < numPoints; p++)
                        {
                            values.Add(reader.ReadSingle());
                        }

                        TestContext.WriteLine($"FloatCurve at 0x{startPos:X4}: count={numPoints} cap={capacity} flags={flags}");
                        TestContext.WriteLine($"  Values: [{string.Join(", ", values.Select(v => v.ToString("F4")))}]");

                        curveCount++;
                    }
                    else
                    {
                        reader.Position = startPos + 1;
                    }
                }
                catch
                {
                    reader.Position = startPos + 1;
                }
            }

            TestContext.WriteLine($"Found {curveCount} float curves in first scan");
        }

        [Test]
        public void Diagnostic_CountFieldTypes_GameplayVLT()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var service = new FIFAAttribDbService();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);

            var types = service.ReadAttribDbGameplayVlt(msVlt.ToArray());

            var typeCounts = new Dictionary<string, int>();
            int totalFields = 0;
            foreach (var type in types)
            {
                foreach (var field in type.Fields)
                {
                    var typeName = field.FieldType.ToString();
                    if (!typeCounts.ContainsKey(typeName))
                        typeCounts[typeName] = 0;
                    typeCounts[typeName]++;
                    totalFields++;
                }
            }

            TestContext.WriteLine($"Total types: {types.Count}");
            TestContext.WriteLine($"Total fields: {totalFields}");
            foreach (var kvp in typeCounts.OrderByDescending(x => x.Value))
            {
                TestContext.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        [Test]
        public void Diagnostic_DumpNpxETypeOffsets()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            // Find NpxE marker
            var npxeMarker = Encoding.ASCII.GetBytes("NpxE");
            int npxePos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == npxeMarker[0] && vltData[i + 1] == npxeMarker[1] &&
                    vltData[i + 2] == npxeMarker[2] && vltData[i + 3] == npxeMarker[3])
                {
                    npxePos = i;
                    break;
                }
            }

            Assert.That(npxePos, Is.GreaterThanOrEqualTo(0));
            TestContext.WriteLine($"NpxE marker at 0x{npxePos:X4}");

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = npxePos + 4; // Skip "NpxE"
            var countOfFields = reader.ReadInt();
            var countOfTypes = reader.ReadInt();
            reader.Pad(16);

            TestContext.WriteLine($"countOfFields={countOfFields}, countOfTypes={countOfTypes}");

            // Read per-type entries
            var typeEntries = new List<(ulong hash1, ulong hash2, uint offset1, uint offset2)>();
            for (int i = 0; i < countOfTypes; i++)
            {
                var hash1 = reader.ReadULong();
                var hash2 = reader.ReadULong();
                var offset1 = reader.ReadUInt();
                var offset2 = reader.ReadUInt();
                typeEntries.Add((hash1, hash2, offset1, offset2));
            }

            // Correlate with types
            for (int i = 0; i < Math.Min(typeEntries.Count, types.Count); i++)
            {
                var entry = typeEntries[i];
                var type = types[i];
                int curveCount = type.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2);
                int arrayCount = type.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.Array);
                int stringCount = type.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.String);

                if (curveCount > 0 || arrayCount > 0 || stringCount > 0)
                {
                    TestContext.WriteLine($"[{i,3}] {type.FolderName}/{type.Name}");
                    TestContext.WriteLine($"     hash1=0x{entry.hash1:X16} hash2=0x{entry.hash2:X16}");
                    TestContext.WriteLine($"     offset1=0x{entry.offset1:X8}({entry.offset1}) offset2=0x{entry.offset2:X8}({entry.offset2})");
                    TestContext.WriteLine($"     FloatCurves={curveCount} Arrays={arrayCount} Strings={stringCount}");

                    // Check BIN at offset1
                    if (entry.offset1 > 0 && entry.offset1 < binData.Length - 8)
                    {
                        using var binReader = new NativeReader(new MemoryStream(binData));
                        binReader.Position = entry.offset1;
                        var b0 = binReader.ReadUShort();
                        var b1 = binReader.ReadUShort();
                        var b2 = binReader.ReadUInt();
                        TestContext.WriteLine($"     BIN@offset1: u16={b0} u16={b1} u32=0x{b2:X8}");
                    }
                }
            }
        }

        [Test]
        public void Diagnostic_ScanAllNpxEAndCorrelateBIN()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            // Find NpxE
            var npxeMarker = Encoding.ASCII.GetBytes("NpxE");
            int npxePos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == npxeMarker[0] && vltData[i + 1] == npxeMarker[1] &&
                    vltData[i + 2] == npxeMarker[2] && vltData[i + 3] == npxeMarker[3])
                { npxePos = i; break; }
            }

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = npxePos + 4;
            var countOfFields = reader.ReadInt();
            var countOfTypes = reader.ReadInt();
            reader.Pad(16);

            var typeEntries = new List<(ulong hash1, ulong hash2, uint offset1, uint offset2)>();
            for (int i = 0; i < countOfTypes; i++)
            {
                typeEntries.Add((reader.ReadULong(), reader.ReadULong(), reader.ReadUInt(), reader.ReadUInt()));
            }

            // Print ALL types with their offsets and non-scalar field counts
            TestContext.WriteLine($"{"idx",4} {"Folder/Name",-45} {"off1",10} {"off2",10} {"FC",4} {"Arr",4} {"Str",4} {"total",5}");
            int cumBin = 0;
            for (int i = 0; i < Math.Min(typeEntries.Count, types.Count); i++)
            {
                var e = typeEntries[i];
                var t = types[i];
                int fc = t.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2);
                int ar = t.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.Array);
                int st = t.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.String);
                TestContext.WriteLine($"{i,4} {t.FolderName}/{t.Name,-40} {e.offset1,10} {e.offset2,10} {fc,4} {ar,4} {st,4} {t.Fields.Count,5}");
            }

            // Also try reading FloatCurves from the BIN starting at various offsets to see which ones work
            TestContext.WriteLine($"\n=== Testing offset2 as cumulative BIN data offset (after 32-byte header) ===");
            int runningOffset = 0;
            for (int i = 0; i < Math.Min(typeEntries.Count, types.Count); i++)
            {
                var e = typeEntries[i];
                var t = types[i];
                int fc = t.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2);
                if (fc == 0) continue;

                var binStart = 32 + (int)e.offset2;
                if (binStart >= binData.Length - 8) continue;

                // Try to read first FloatCurve at this position
                var p1 = BitConverter.ToUInt16(binData, binStart);
                var p2 = BitConverter.ToUInt16(binData, binStart + 2);
                var p4 = BitConverter.ToUInt32(binData, binStart + 4);
                bool validFC = p1 > 0 && p1 <= 32 && p1 == p2 && p4 == 4;

                TestContext.WriteLine($"[{i,3}] {t.FolderName}/{t.Name,-40} offset2={e.offset2,6} BIN@{binStart,6}(0x{binStart:X4}) peek: u16={p1} u16={p2} u32=0x{p4:X8} FC={validFC}");
            }

            // Also scan the entire BIN for FloatCurve signatures
            TestContext.WriteLine($"\n=== All FloatCurve signatures in BIN ===");
            for (int pos = 32; pos < binData.Length - 8;)
            {
                var c = BitConverter.ToUInt16(binData, pos);
                var cap = BitConverter.ToUInt16(binData, pos + 2);
                var flags = BitConverter.ToUInt32(binData, pos + 4);
                if (c > 0 && c <= 32 && c == cap && flags == 4)
                {
                    var vals = new List<float>();
                    for (int p = 0; p < c && pos + 8 + p * 4 + 4 <= binData.Length; p++)
                        vals.Add(BitConverter.ToSingle(binData, pos + 8 + p * 4));
                    TestContext.WriteLine($"  FC@0x{pos:X4}: count={c} [{string.Join(",", vals.Select(v => v.ToString("F4")))}]");
                    pos += 8 + c * 4;
                }
                else
                {
                    pos++;
                }
            }
        }

        [Test]
        public void Diagnostic_ReadBINSequentialByType()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var nonScalarTypes = new[] { FifaAttribDbFieldType.FloatCurve, FifaAttribDbFieldType.FloatCurve2, FifaAttribDbFieldType.Array, FifaAttribDbFieldType.String, FifaAttribDbFieldType.RawBytes };

            using var binReader = new NativeReader(new MemoryStream(binData));
            binReader.Position = 32; // Skip 32-byte header

            int typeIdx = 0;
            int totalEntries = 0;
            bool mismatch = false;

            foreach (var type in types)
            {
                var nonScalarFields = type.Fields.Where(f => nonScalarTypes.Contains(f.FieldType)).ToList();
                if (nonScalarFields.Count == 0) continue;

                long typeStart = binReader.Position;
                bool typeOk = true;

                foreach (var f in nonScalarFields)
                {
                    if (binReader.Position + 8 > binData.Length)
                    {
                        typeOk = false;
                        break;
                    }

                    long entryStart = binReader.Position;
                    var count = binReader.ReadUShort();
                    var capacity = binReader.ReadUShort();
                    var flags = binReader.ReadUInt();

                    if (count > 0 && count <= 32 && count == capacity && flags == 4)
                    {
                        binReader.Position += count * 4; // skip floats
                        totalEntries++;
                    }
                    else if (count == 0 && capacity == 0)
                    {
                        // Empty entry (valid for empty curves/arrays)
                        totalEntries++;
                    }
                    else
                    {
                        typeOk = false;
                        mismatch = true;
                        TestContext.WriteLine($"MISMATCH at type[{typeIdx}] {type.FolderName}/{type.Name} field '{f.Name}' ({f.FieldType}) at BIN 0x{entryStart:X4}: u16={count} u16={capacity} u32=0x{flags:X8}");
                        break;
                    }

                    f.BinaryFileOffset = entryStart;
                }

                if (typeOk)
                {
                    long typeSize = binReader.Position - typeStart;
                    if (nonScalarFields.Count > 0)
                        TestContext.WriteLine($"OK [{typeIdx,3}] {type.FolderName}/{type.Name,-40} {nonScalarFields.Count,3} fields, BIN 0x{typeStart:X4}-0x{binReader.Position:X4} ({typeSize} bytes)");
                }

                typeIdx++;
            }

            TestContext.WriteLine($"\nTotal entries read: {totalEntries}");
            TestContext.WriteLine($"BIN consumed: {binReader.Position - 32} of {binData.Length - 32} bytes");
            TestContext.WriteLine($"Remaining: {binData.Length - binReader.Position} bytes");
            TestContext.WriteLine($"Mismatch: {mismatch}");
        }

        [Test]
        public void Diagnostic_CorrelateNpxEOffsets()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDb.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var npxeMarker = Encoding.ASCII.GetBytes("NpxE");
            int npxePos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == npxeMarker[0] && vltData[i + 1] == npxeMarker[1] &&
                    vltData[i + 2] == npxeMarker[2] && vltData[i + 3] == npxeMarker[3])
                { npxePos = i; break; }
            }

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = npxePos + 4;
            var countOfFields = reader.ReadInt();
            var countOfTypes = reader.ReadInt();
            reader.Pad(16);

            var typeEntries = new List<(ulong hash1, ulong hash2, uint offset1, uint offset2)>();
            for (int i = 0; i < countOfTypes; i++)
                typeEntries.Add((reader.ReadULong(), reader.ReadULong(), reader.ReadUInt(), reader.ReadUInt()));

            TestContext.WriteLine($"DEBUG: types.Count={types.Count}, typeEntries.Count={typeEntries.Count}, countOfTypes={countOfTypes}");

            // Print first 5 NpxE entries and first 5 VLT type hashes
            TestContext.WriteLine("\n--- First 5 NpxE entries ---");
            for (int i = 0; i < Math.Min(5, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                TestContext.WriteLine($"NpxE[{i}]: hash1=0x{e.hash1:X16} hash2=0x{e.hash2:X16} off1={e.offset1} off2={e.offset2}");
            }
            TestContext.WriteLine("\n--- First 5 VLT types ---");
            for (int i = 0; i < Math.Min(5, types.Count); i++)
            {
                var t = types[i];
                TestContext.WriteLine($"VLT[{i}]: hashLong=0x{t.HashLong:X16} folderHash=0x{t.FolderHash:X16} name={t.FolderName}/{t.Name} DataOff={t.DataOffsetInVault} DataSz={t.DataInVault?.Length ?? 0}");
            }

            // Try to match NpxE entries to VLT types by hash1=folderHash or hash1=hashLong
            TestContext.WriteLine("\n--- Matching by hash1=FolderHash ---");
            int matchedByFolder = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.FolderHash == e.hash1);
                if (match.HashLong != 0) { matchedByFolder++; TestContext.WriteLine($"NpxE[{i}] hash1=0x{e.hash1:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFolder} of first 10");

            TestContext.WriteLine("\n--- Matching by hash2=HashLong (fileName) ---");
            int matchedByFile = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.HashLong == e.hash2);
                if (match.HashLong != 0) { matchedByFile++; TestContext.WriteLine($"NpxE[{i}] hash2=0x{e.hash2:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFile} of first 10");

            TestContext.WriteLine("\n--- Matching by hash1=HashLong (reversed) ---");
            int matchedByFileRev = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.HashLong == e.hash1);
                if (match.HashLong != 0) { matchedByFileRev++; TestContext.WriteLine($"NpxE[{i}] hash1=0x{e.hash1:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFileRev} of first 10");

            TestContext.WriteLine("\n--- Matching by hash2=FolderHash (reversed) ---");
            int matchedByFolderRev = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.FolderHash == e.hash2);
                if (match.HashLong != 0) { matchedByFolderRev++; TestContext.WriteLine($"NpxE[{i}] hash2=0x{e.hash2:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFolderRev} of first 10");
        }

        [Test]
        public void Diagnostic_CorrelateNpxEOffsets_AttribDbGameplay()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var npxeMarker = Encoding.ASCII.GetBytes("NpxE");
            int npxePos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == npxeMarker[0] && vltData[i + 1] == npxeMarker[1] &&
                    vltData[i + 2] == npxeMarker[2] && vltData[i + 3] == npxeMarker[3])
                { npxePos = i; break; }
            }

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = npxePos + 4;
            var countOfFields = reader.ReadInt();
            var countOfTypes = reader.ReadInt();
            reader.Pad(16);

            var typeEntries = new List<(ulong hash1, ulong hash2, uint offset1, uint offset2)>();
            for (int i = 0; i < countOfTypes; i++)
                typeEntries.Add((reader.ReadULong(), reader.ReadULong(), reader.ReadUInt(), reader.ReadUInt()));

            TestContext.WriteLine($"DEBUG: types.Count={types.Count}, typeEntries.Count={typeEntries.Count}, countOfTypes={countOfTypes}");

            // Print first 5 NpxE entries and first 5 VLT type hashes
            TestContext.WriteLine("\n--- First 5 NpxE entries ---");
            for (int i = 0; i < Math.Min(5, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                TestContext.WriteLine($"NpxE[{i}]: hash1=0x{e.hash1:X16} hash2=0x{e.hash2:X16} off1={e.offset1} off2={e.offset2}");
            }
            TestContext.WriteLine("\n--- First 5 VLT types ---");
            for (int i = 0; i < Math.Min(5, types.Count); i++)
            {
                var t = types[i];
                TestContext.WriteLine($"VLT[{i}]: hashLong=0x{t.HashLong:X16} folderHash=0x{t.FolderHash:X16} name={t.FolderName}/{t.Name} DataOff={t.DataOffsetInVault} DataSz={t.DataInVault?.Length ?? 0}");
            }

            // Try to match NpxE entries to VLT types by hash1=folderHash or hash1=hashLong
            TestContext.WriteLine("\n--- Matching by hash1=FolderHash ---");
            int matchedByFolder = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.FolderHash == e.hash1);
                if (match.HashLong != 0) { matchedByFolder++; TestContext.WriteLine($"NpxE[{i}] hash1=0x{e.hash1:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFolder} of first 10");

            TestContext.WriteLine("\n--- Matching by hash2=HashLong (fileName) ---");
            int matchedByFile = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.HashLong == e.hash2);
                if (match.HashLong != 0) { matchedByFile++; TestContext.WriteLine($"NpxE[{i}] hash2=0x{e.hash2:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFile} of first 10");

            TestContext.WriteLine("\n--- Matching by hash1=HashLong (reversed) ---");
            int matchedByFileRev = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.HashLong == e.hash1);
                if (match.HashLong != 0) { matchedByFileRev++; TestContext.WriteLine($"NpxE[{i}] hash1=0x{e.hash1:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFileRev} of first 10");

            TestContext.WriteLine("\n--- Matching by hash2=FolderHash (reversed) ---");
            int matchedByFolderRev = 0;
            for (int i = 0; i < Math.Min(10, typeEntries.Count); i++)
            {
                var e = typeEntries[i];
                var match = types.FirstOrDefault(t => t.FolderHash == e.hash2);
                if (match.HashLong != 0) { matchedByFolderRev++; TestContext.WriteLine($"NpxE[{i}] hash2=0x{e.hash2:X16} -> {match.FolderName}/{match.Name}"); }
            }
            TestContext.WriteLine($"Matched {matchedByFolderRev} of first 10");
        }
        [Test]
        public void Diagnostic_CheckFieldValuesAsBinPointers()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var nonScalarTypes = new[] { FifaAttribDbFieldType.FloatCurve, FifaAttribDbFieldType.FloatCurve2, FifaAttribDbFieldType.Array, FifaAttribDbFieldType.String, FifaAttribDbFieldType.RawBytes };

            TestContext.WriteLine($"VLT size: {vltData.Length} bytes, BIN size: {binData.Length} bytes");
            TestContext.WriteLine($"BIN data area: 0x20 - 0x{binData.Length - 1:X4} ({binData.Length - 32} bytes)\n");

            foreach (var type in types.Take(10))
            {
                var nonScalarFields = type.Fields.Where(f => nonScalarTypes.Contains(f.FieldType)).ToList();
                if (nonScalarFields.Count == 0) continue;

                TestContext.WriteLine($"=== {type.FolderName}/{type.Name} ===");
                TestContext.WriteLine($"  DataOffsetInVault=0x{type.DataOffsetInVault:X4} DataInVault.Length={type.DataInVault?.Length ?? 0}");

                foreach (var f in nonScalarFields)
                {
                    TestContext.WriteLine($"  Field '{f.Name}' ({f.FieldType}) at VLT 0x{f.VaultValueOffset:X4}");

                    // Read the raw 8-byte field value from VLT
                    if (f.VaultValueOffset + 8 <= vltData.Length)
                    {
                        var raw = new byte[8];
                        Array.Copy(vltData, f.VaultValueOffset, raw, 0, 8);
                        var asULong = BitConverter.ToUInt64(raw);
                        var asUInt = BitConverter.ToUInt32(raw, 0);
                        var asFloat = BitConverter.ToSingle(raw, 0);

                        TestContext.WriteLine($"    Raw bytes: {BitConverter.ToString(raw)}");
                        TestContext.WriteLine($"    As UInt64: {asULong} (0x{asULong:X16})");
                        TestContext.WriteLine($"    As UInt32: {asUInt} (0x{asUInt:X8})");
                        TestContext.WriteLine($"    As Float: {asFloat}");

                        // Check if it could be a BIN offset
                        if (asUInt < binData.Length)
                        {
                            TestContext.WriteLine($"    ** Could be BIN offset 0x{asUInt:X4} - peeking:");
                            var p1 = BitConverter.ToUInt16(binData, (int)asUInt);
                            var p2 = BitConverter.ToUInt16(binData, (int)asUInt + 2);
                            var p4 = BitConverter.ToUInt32(binData, (int)asUInt + 4);
                            TestContext.WriteLine($"       u16={p1} u16={p2} u32=0x{p4:X8}");

                            if (p1 > 0 && p1 <= 32 && p1 == p2 && p4 == 4)
                                TestContext.WriteLine($"       ** VALID FloatCurve/Array header!");
                        }

                        // Check if it's a VLT position
                        if (asUInt < vltData.Length && asUInt > 0)
                        {
                            var vltByte = vltData[asUInt];
                            TestContext.WriteLine($"    If VLT@0x{asUInt:X4}: byte=0x{vltByte:X2}");
                        }
                    }
                }
                TestContext.WriteLine("");
            }
        }
        [Test]
        public void Diagnostic_DumpNrtPAndCheckBinOffsets()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var nrtP = Encoding.ASCII.GetBytes("NrtP");
            int nrtPPos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == nrtP[0] && vltData[i + 1] == nrtP[1] &&
                    vltData[i + 2] == nrtP[2] && vltData[i + 3] == nrtP[3])
                { nrtPPos = i; break; }
            }

            Assert.That(nrtPPos, Is.GreaterThanOrEqualTo(0));
            TestContext.WriteLine($"NrtP marker at 0x{nrtPPos:X4}");

            uint chunkRawSize = BitConverter.ToUInt32(vltData, nrtPPos + 4);
            TestContext.WriteLine($"Chunk raw size field: {chunkRawSize} (0x{chunkRawSize:X8})");

            long chunkDataStart = nrtPPos + 8;
            long chunkEnd = nrtPPos + chunkRawSize;
            long chunkDataLen = chunkEnd - chunkDataStart;
            TestContext.WriteLine($"Chunk data: 0x{chunkDataStart:X4} to 0x{chunkEnd:X4} ({chunkDataLen} bytes)");
            TestContext.WriteLine($"12-byte entries possible: {chunkDataLen / 12}");
            TestContext.WriteLine($"16-byte entries possible: {chunkDataLen / 16}");

            TestContext.WriteLine($"\n=== Raw hex: first 128 bytes of chunk data (from 0x{chunkDataStart:X4}) ===");
            for (int row = 0; row < 8; row++)
            {
                int baseOff = (int)chunkDataStart + row * 16;
                if (baseOff + 16 > vltData.Length) break;
                var hex = BitConverter.ToString(vltData, baseOff, 16).Replace("-", " ");
                TestContext.WriteLine($"  0x{baseOff:X4}: {hex}");
            }

            TestContext.WriteLine($"\n=== Try 16-byte entries (12-byte PtrRef + 4-byte trailing) ===");
            {
                const ushort PtrEnd = 0;
                const ushort PtrNull = 1;
                const ushort PtrSetFixupTarget = 2;
                const ushort PtrDepRelative = 3;

                var r = new NativeReader(new MemoryStream(vltData));
                r.Position = chunkDataStart;
                var binPtrs = new List<(uint fixup, uint dest)>();
                var vltPtrs = new List<(uint fixup, uint dest)>();
                bool isVltPointer = false;
                int count = 0;

                while (r.Position + 16 <= chunkEnd)
                {
                    uint fixupOffset = r.ReadUInt();
                    ushort ptrType = r.ReadUShort();
                    ushort index = r.ReadUShort();
                    uint destination = r.ReadUInt();
                    uint trailing = r.ReadUInt();
                    count++;

                    switch (ptrType)
                    {
                        case PtrSetFixupTarget:
                            isVltPointer = index == 0;
                            TestContext.WriteLine($"  [{count,4}] SetFixupTarget idx={index} -> {(isVltPointer ? "VLT" : "BIN")} trailing=0x{trailing:X8}");
                            break;
                        case PtrDepRelative:
                        case PtrNull:
                            if (isVltPointer)
                                vltPtrs.Add((fixupOffset, destination));
                            else
                                binPtrs.Add((fixupOffset, destination));
                            break;
                        case PtrEnd:
                            TestContext.WriteLine($"  [{count,4}] PtrEnd");
                            break;
                    }
                    if (ptrType == PtrEnd) break;
                }

                TestContext.WriteLine($"\nTotal 16-byte entries: {count}");
                TestContext.WriteLine($"BIN pointers: {binPtrs.Count}");
                TestContext.WriteLine($"VLT pointers: {vltPtrs.Count}");

                TestContext.WriteLine($"\n=== All VLT Pointers (fixup -> dest) ===");
                for (int i = 0; i < vltPtrs.Count; i++)
                {
                    var p = vltPtrs[i];
                    string peekBin = "N/A";
                    if (p.dest + 8 <= (uint)binData.Length)
                    {
                        var u1 = BitConverter.ToUInt16(binData, (int)p.dest);
                        var u2 = BitConverter.ToUInt16(binData, (int)p.dest + 2);
                        var u4 = BitConverter.ToUInt32(binData, (int)p.dest + 4);
                        peekBin = $"u16={u1} u16={u2} u32=0x{u4:X8}";
                        if (u1 > 0 && u1 <= 128 && u1 == u2 && u4 == 4) peekBin += " [FloatCurve]";
                        else if (u1 == 0 && u2 == 0 && u4 == 0) peekBin += " [zeros]";
                    }
                    TestContext.WriteLine($"  [{i,4}] VLT@0x{p.fixup:X4} -> BIN@0x{p.dest:X4}: {peekBin}");
                }

                if (binPtrs.Count > 0)
                {
                    TestContext.WriteLine($"\n=== All BIN Pointers ===");
                    for (int i = 0; i < binPtrs.Count; i++)
                    {
                        var p = binPtrs[i];
                        TestContext.WriteLine($"  [{i,4}] BIN@0x{p.fixup:X4} -> 0x{p.dest:X4}");
                    }
                }
            }
        }

        [Test]
        public void Diagnostic_CorrelateVltPtrsWithFields()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var scalarTypes = new HashSet<ulong> {
                (ulong)FifaAttribDbFieldType.Int32,
                (ulong)FifaAttribDbFieldType.Float,
                (ulong)FifaAttribDbFieldType.Int64,
                (ulong)FifaAttribDbFieldType.Bool
            };

            // Step 1: Parse NrtP 16-byte entries
            var nrtPMarker = Encoding.ASCII.GetBytes("NrtP");
            int nrtPPos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == nrtPMarker[0] && vltData[i + 1] == nrtPMarker[1] &&
                    vltData[i + 2] == nrtPMarker[2] && vltData[i + 3] == nrtPMarker[3])
                { nrtPPos = i; break; }
            }
            Assert.That(nrtPPos, Is.GreaterThanOrEqualTo(0), "NrtP not found");

            using var ptrReader = new NativeReader(new MemoryStream(vltData));
            ptrReader.Position = nrtPPos + 4; // Skip "NrtP"
            var chunkSizeField = ptrReader.ReadUInt();
            long chunkEnd = nrtPPos + 4 + chunkSizeField;
            long chunkDataStart = ptrReader.Position;

            var vltPtrs = new List<(uint fixupOffset, uint destination)>();
            bool isVltPointer = false;

            while (ptrReader.Position + 16 <= chunkEnd)
            {
                uint fixupOffset = ptrReader.ReadUInt();
                ushort ptrType = ptrReader.ReadUShort();
                ushort index = ptrReader.ReadUShort();
                uint destination = ptrReader.ReadUInt();
                uint trailing = ptrReader.ReadUInt();

                switch (ptrType)
                {
                    case 2: // SetFixupTarget
                        isVltPointer = index == 0;
                        break;
                    case 3: // DepRelative
                    case 1: // PtrNull
                        if (isVltPointer)
                            vltPtrs.Add((fixupOffset, destination));
                        break;
                    case 0: // PtrEnd
                        break;
                }
                if (ptrType == 0) break;
            }

            TestContext.WriteLine($"Parsed {vltPtrs.Count} VLT pointer entries from NrtP");

            // Step 2: Build lookup from fixupOffset → BIN dest
            var fixupToBinDest = new Dictionary<uint, uint>();
            foreach (var p in vltPtrs)
                fixupToBinDest[p.fixupOffset] = p.destination;

            // Step 3: Correlate fields with PtrN entries
            TestContext.WriteLine($"\n=== Field → BIN Offset Correlation ===");
            int matchedCount = 0;
            int unmatchedNonScalar = 0;
            int totalNonScalar = 0;
            int totalScalar = 0;

            foreach (var type in types)
            {
                foreach (var field in type.Fields)
                {
                    if (scalarTypes.Contains((ulong)field.FieldType))
                    {
                        totalScalar++;
                        continue;
                    }
                    totalNonScalar++;

                    uint vltOff = (uint)field.VaultValueOffset;
                    if (fixupToBinDest.TryGetValue(vltOff, out uint binDest))
                    {
                        field.BinaryFileOffset = binDest;
                        matchedCount++;

                        string peek = "N/A";
                        if (binDest + 8 <= (uint)binData.Length)
                        {
                            var u1 = BitConverter.ToUInt16(binData, (int)binDest);
                            var u2 = BitConverter.ToUInt16(binData, (int)binDest + 2);
                            var u4 = BitConverter.ToUInt32(binData, (int)binDest + 4);
                            peek = $"u16={u1} u16={u2} u32=0x{u4:X8}";
                            if (u1 > 0 && u1 <= 128 && u1 == u2 && u4 == 4) peek += " [FloatCurve]";
                            else if (u1 == 0 && u2 == 0 && u4 == 0) peek += " [zeros]";
                        }

                        TestContext.WriteLine($"MATCHED {type.FolderName}/{type.Name}/{field.Name} ({field.FieldType}) VLT@0x{vltOff:X4} → BIN@0x{binDest:X4}: {peek}");
                    }
                    else
                    {
                        unmatchedNonScalar++;
                        TestContext.WriteLine($"UNMATCHED {type.FolderName}/{type.Name}/{field.Name} ({field.FieldType}) VLT@0x{vltOff:X4}");
                    }
                }
            }

            TestContext.WriteLine($"\n=== SUMMARY ===");
            TestContext.WriteLine($"Total fields: {totalScalar} scalar + {totalNonScalar} non-scalar = {totalScalar + totalNonScalar}");
            TestContext.WriteLine($"VLT pointer entries: {vltPtrs.Count}");
            TestContext.WriteLine($"Matched non-scalar fields: {matchedCount}/{totalNonScalar}");
            TestContext.WriteLine($"Unmatched non-scalar fields: {unmatchedNonScalar}");

            // Step 4: List unique BIN offsets to see if they form a contiguous data block
            var uniqueBinOffsets = fixupToBinDest.Values.Distinct().OrderBy(x => x).ToList();
            TestContext.WriteLine($"\nUnique BIN destinations from PtrN: {uniqueBinOffsets.Count}");
            if (uniqueBinOffsets.Count > 0)
            {
                TestContext.WriteLine($"Range: BIN@0x{uniqueBinOffsets.First():X4} - BIN@0x{uniqueBinOffsets.Last():X4}");

                // Check for gaps
                int gaps = 0;
                for (int i = 1; i < uniqueBinOffsets.Count; i++)
                {
                    // Not necessarily contiguous, just check
                }
            }

            TestContext.WriteLine($"\nKnown gap: 120 String/RawBytes fields have no PtrN entry (119 String + 1 RawBytes)");
            TestContext.WriteLine($"Expected matched: {totalNonScalar} - 120 = {totalNonScalar - 120}");
            Assert.That(matchedCount, Is.EqualTo(totalNonScalar - 120),
                $"Matched should be {totalNonScalar - 120} (all non-scalar minus 120 String/RawBytes with no PtrN)");
        }

        [Test]
        public void Validate_BinDataPopulatedInFields()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(msVlt.ToArray(), binData);

            int floatCurvesWithValues = 0;
            int emptyFC = 0;
            int arraysWithValues = 0;
            int floatsRead = 0;

            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2)
                    {
                        if (field.Value is float[] fc && fc.Length > 0)
                            floatCurvesWithValues++;
                        else
                            emptyFC++;
                    }
                    else if (field.FieldType == FifaAttribDbFieldType.Array)
                    {
                        if (field.Value is float[] arr && arr.Length > 0)
                            arraysWithValues++;
                    }
                    else if (field.FieldType == FifaAttribDbFieldType.Float)
                    {
                        if (field.Value is float)
                            floatsRead++;
                    }
                }
            }

            TestContext.WriteLine($"FloatCurves with values: {floatCurvesWithValues}");
            TestContext.WriteLine($"FloatCurves empty: {emptyFC}");
            TestContext.WriteLine($"Arrays with values: {arraysWithValues}");
            TestContext.WriteLine($"Floats read: {floatsRead}");

            // Dump raw BIN at first few FloatCurve offsets to understand the format
            TestContext.WriteLine($"\n=== Raw BIN data at FloatCurve BinaryFileOffsets ===");
            int fcShown = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        var hex = BitConverter.ToString(binData, binOff, Math.Min(32, binData.Length - binOff)).Replace("-", " ");
                        TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{binOff:X4}: {hex}");
                        if (++fcShown >= 5) break;
                    }
                }
                if (fcShown >= 5) break;
            }

            // Also dump raw BIN at first few Array offsets
            TestContext.WriteLine($"\n=== Raw BIN data at first Array BinaryFileOffsets ===");
            int arrShown = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.Array && field.BinaryFileOffset.HasValue)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        var hex = BitConverter.ToString(binData, binOff, Math.Min(32, binData.Length - binOff)).Replace("-", " ");
                        TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{binOff:X4}: {hex}");
                        if (++arrShown >= 5) break;
                    }
                }
                if (arrShown >= 5) break;
            }

            // Check specifically kick_error/balance fields
            TestContext.WriteLine($"\n=== kick_error/balance fields ===");
            var balanceEntry = service.Assets.FirstOrDefault(e => e.AttribDbType.FolderName == "kick_error" && e.AttribDbType.Name == "balance");
            if (balanceEntry != null)
            {
                foreach (var f in balanceEntry.AttribDbType.Fields)
                {
                    string valStr = f.Value?.ToString() ?? "null";
                    if (f.Value is float[] fa) valStr = $"float[{fa.Length}]";
                    TestContext.WriteLine($"  {f.Name} ({f.FieldType}) VLT@0x{f.VaultValueOffset:X4} BIN@{(f.BinaryFileOffset.HasValue ? $"0x{f.BinaryFileOffset.Value:X4}" : "none")}: {valStr}");
                }
            }

            Assert.That(floatCurvesWithValues + arraysWithValues, Is.GreaterThan(0), "Should have some FloatCurves or Arrays with values");
        }

        [Test]
        public void Diagnostic_ReadBINSequentialByType_IncludingUnknownTypes()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            var scalarTypes = new HashSet<ulong> {
                (ulong)FifaAttribDbFieldType.Int32,
                (ulong)FifaAttribDbFieldType.Float,
                (ulong)FifaAttribDbFieldType.Int64,
                (ulong)FifaAttribDbFieldType.Bool
            };

            using var binReader = new NativeReader(new MemoryStream(binData));
            binReader.Position = 32;

            int typeIdx = 0;
            int totalEntries = 0;
            bool mismatch = false;
            var typeResults = new List<(int idx, string folder, string name, int fieldCount, long start, long end, bool ok)>();

            foreach (var type in types)
            {
                var nonScalarFields = type.Fields.Where(f => !scalarTypes.Contains((ulong)f.FieldType)).ToList();
                if (nonScalarFields.Count == 0) { typeIdx++; continue; }

                long typeStart = binReader.Position;
                bool typeOk = true;
                int fieldsRead = 0;

                foreach (var f in nonScalarFields)
                {
                    if (binReader.Position + 8 > binData.Length) { typeOk = false; break; }

                    long entryStart = binReader.Position;
                    var count = binReader.ReadUShort();
                    var capacity = binReader.ReadUShort();
                    var flags = binReader.ReadUInt();

                    if (count > 0 && count <= 128 && count == capacity && flags == 4)
                    {
                        binReader.Position += count * 4;
                        totalEntries++;
                        fieldsRead++;
                    }
                    else if (count == 0 && capacity == 0 && flags == 0)
                    {
                        totalEntries++;
                        fieldsRead++;
                    }
                    else
                    {
                        TestContext.WriteLine($"MISMATCH [{typeIdx}] {type.FolderName}/{type.Name} field '{f.Name}' type={(ulong)f.FieldType} at BIN 0x{entryStart:X4}: u16={count} u16={capacity} u32=0x{flags:X8}");
                        typeOk = false;
                        mismatch = true;

                        binReader.Position = entryStart;
                        var peek = binReader.ReadBytes(Math.Min(32, (int)(binData.Length - entryStart)));
                        TestContext.WriteLine($"  Raw: {BitConverter.ToString(peek).Replace("-", " ")}");
                        break;
                    }

                    f.BinaryFileOffset = entryStart;
                }

                typeResults.Add((typeIdx, type.FolderName, type.Name, fieldsRead, typeStart, binReader.Position, typeOk));

                if (typeOk && fieldsRead > 0)
                {
                    TestContext.WriteLine($"OK [{typeIdx,3}] {type.FolderName}/{type.Name,-40} {fieldsRead,3} fields, BIN 0x{typeStart:X4}-0x{binReader.Position:X4} ({binReader.Position - typeStart} bytes)");
                }

                typeIdx++;
            }

            TestContext.WriteLine($"\n=== SUMMARY ===");
            TestContext.WriteLine($"Total entries read: {totalEntries}");
            TestContext.WriteLine($"BIN consumed: {binReader.Position - 32} of {binData.Length - 32} bytes");
            TestContext.WriteLine($"Remaining: {binData.Length - binReader.Position} bytes");
            TestContext.WriteLine($"Mismatch: {mismatch}");

            var okCount = typeResults.Count(r => r.ok);
            var failCount = typeResults.Count(r => !r.ok && r.fieldCount > 0);
            var skipCount = typeResults.Count(r => r.fieldCount == 0);
            TestContext.WriteLine($"Types OK: {okCount}, Failed: {failCount}, Skipped (no non-scalar): {skipCount}");

            if (mismatch)
            {
                var firstFail = typeResults.FirstOrDefault(r => !r.ok && r.fieldCount > 0);
                TestContext.WriteLine($"\nFirst failure at [{firstFail.idx}] {firstFail.folder}/{firstFail.name} at BIN 0x{firstFail.start:X4}");
            }
        }

        [Test]
        public void Diagnostic_TraceFloatCurveVsArray()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            var types = service.ReadAttribDbGameplayVlt(vltData);

            // Parse NrtP
            var nrtPMarker = Encoding.ASCII.GetBytes("NrtP");
            int nrtPPos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == nrtPMarker[0] && vltData[i + 1] == nrtPMarker[1] &&
                    vltData[i + 2] == nrtPMarker[2] && vltData[i + 3] == nrtPMarker[3])
                { nrtPPos = i; break; }
            }
            Assert.That(nrtPPos, Is.GreaterThanOrEqualTo(0), "NrtP not found");

            using var ptrReader = new NativeReader(new MemoryStream(vltData));
            ptrReader.Position = nrtPPos + 4;
            var chunkSizeField = ptrReader.ReadUInt();
            long chunkEnd = nrtPPos + 4 + chunkSizeField;

            var allPtrs = new List<(uint fixupOffset, ushort ptrType, ushort index, uint destination)>();
            bool isVltPointer = false;

            while (ptrReader.Position + 16 <= chunkEnd)
            {
                uint fixupOffset = ptrReader.ReadUInt();
                ushort ptrType = ptrReader.ReadUShort();
                ushort index = ptrReader.ReadUShort();
                uint destination = ptrReader.ReadUInt();
                uint trailing = ptrReader.ReadUInt();

                switch (ptrType)
                {
                    case 2: isVltPointer = index == 0; break;
                    case 3:
                    case 1:
                        allPtrs.Add((fixupOffset, ptrType, index, destination));
                        break;
                    case 0:
                        break;
                }
                if (ptrType == 0) break;
            }

            TestContext.WriteLine($"Total PtrN entries (DepRelative+PtrNull): {allPtrs.Count}");

            // Collect FloatCurve and Array fields with their BIN offsets
            var fcFields = new List<(string path, uint vltOff, long? binOff)>();
            var arrFields = new List<(string path, uint vltOff, long? binOff)>();

            foreach (var type in types)
            {
                foreach (var field in type.Fields)
                {
                    string path = $"{type.FolderName}/{type.Name}/{field.Name}";
                    if (field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2)
                        fcFields.Add((path, (uint)field.VaultValueOffset, field.BinaryFileOffset));
                    else if (field.FieldType == FifaAttribDbFieldType.Array)
                        arrFields.Add((path, (uint)field.VaultValueOffset, field.BinaryFileOffset));
                }
            }

            TestContext.WriteLine($"\nFloatCurve fields: {fcFields.Count}");
            TestContext.WriteLine($"Array fields: {arrFields.Count}");

            // Check if FloatCurve PtrN destinations differ from Array ones
            TestContext.WriteLine($"\n=== FloatCurve PtrN entries (first 5) ===");
            for (int i = 0; i < Math.Min(5, fcFields.Count); i++)
            {
                var (path, vltOff, binOff) = fcFields[i];
                TestContext.WriteLine($"  {path} VLT@0x{vltOff:X4} BIN@{(binOff.HasValue ? $"0x{binOff.Value:X4}" : "none")}");
                if (binOff.HasValue)
                {
                    var hex = BitConverter.ToString(binData, (int)binOff.Value, Math.Min(32, binData.Length - (int)binOff.Value)).Replace("-", " ");
                    TestContext.WriteLine($"    Raw BIN: {hex}");
                }
            }

            TestContext.WriteLine($"\n=== Array PtrN entries (first 5) ===");
            for (int i = 0; i < Math.Min(5, arrFields.Count); i++)
            {
                var (path, vltOff, binOff) = arrFields[i];
                TestContext.WriteLine($"  {path} VLT@0x{vltOff:X4} BIN@{(binOff.HasValue ? $"0x{binOff.Value:X4}" : "none")}");
                if (binOff.HasValue)
                {
                    var hex = BitConverter.ToString(binData, (int)binOff.Value, Math.Min(32, binData.Length - (int)binOff.Value)).Replace("-", " ");
                    TestContext.WriteLine($"    Raw BIN: {hex}");
                }
            }

            // Scan BIN for FloatCurve-like signatures: u16=count, u16=count, u32=4, then float[count]
            TestContext.WriteLine($"\n=== Scanning BIN for FloatCurve signatures (u16==u16 > 0, u32==4) ===");
            int fcSigs = 0;
            for (int off = 32; off < binData.Length - 8; off += 4)
            {
                var u1 = BitConverter.ToUInt16(binData, off);
                var u2 = BitConverter.ToUInt16(binData, off + 2);
                var u4 = BitConverter.ToUInt32(binData, off + 4);
                if (u1 > 0 && u1 <= 128 && u1 == u2 && u4 == 4)
                {
                    var neededBytes = 8 + u1 * 4;
                    if (off + neededBytes <= binData.Length)
                    {
                        var hex = BitConverter.ToString(binData, off, Math.Min(32, binData.Length - off)).Replace("-", " ");
                        TestContext.WriteLine($"  BIN@0x{off:X4}: count={u1} capacity={u2} flags=0x{u4:X8} → {hex}");
                        if (++fcSigs >= 10) break;
                    }
                }
            }
            TestContext.WriteLine($"Found {fcSigs} FloatCurve-like signatures in BIN (stopped at 10)");

            // Compare: what VLT offsets do the NrtP entries that map to BIN < 0x100 (near start) correspond to?
            TestContext.WriteLine($"\n=== PtrN entries pointing to low BIN offsets (< 0x100) ===");
            var lowBinPtrs = allPtrs.Where(p => p.destination < 0x100 && p.destination > 0).Take(10).ToList();
            foreach (var p in lowBinPtrs)
            {
                // Check if this VLT offset corresponds to a FloatCurve or Array field
                var matchingFc = fcFields.FirstOrDefault(f => f.vltOff == p.fixupOffset);
                var matchingArr = arrFields.FirstOrDefault(f => f.vltOff == p.fixupOffset);
                string fieldType = matchingFc.path != null ? "FloatCurve" : matchingArr.path != null ? "Array" : "other";
                TestContext.WriteLine($"  VLT@0x{p.fixupOffset:X4} → BIN@0x{p.destination:X4} [{fieldType}]");
            }

            // Check: are ALL FloatCurve BIN offsets identical to some pattern?
            var fcBinOffsets = fcFields.Where(f => f.binOff.HasValue).Select(f => f.binOff.Value).Distinct().OrderBy(x => x).ToList();
            var arrBinOffsets = arrFields.Where(f => f.binOff.HasValue).Select(f => f.binOff.Value).Distinct().OrderBy(x => x).ToList();
            TestContext.WriteLine($"\n=== Unique BIN offset distribution ===");
            TestContext.WriteLine($"FloatCurve unique BIN offsets: {fcBinOffsets.Count}");
            TestContext.WriteLine($"Array unique BIN offsets: {arrBinOffsets.Count}");
            if (fcBinOffsets.Count > 0)
            {
                TestContext.WriteLine($"FloatCurve BIN range: 0x{fcBinOffsets.First():X4} - 0x{fcBinOffsets.Last():X4}");
                if (fcBinOffsets.Count >= 2)
                    TestContext.WriteLine($"FloatCurve BIN spacing: first few diffs = {string.Join(", ", fcBinOffsets.Take(10).Skip(1).Select((o, i) => $"0x{o - fcBinOffsets[i]:X}"))}");
            }
            if (arrBinOffsets.Count > 0)
            {
                TestContext.WriteLine($"Array BIN range: 0x{arrBinOffsets.First():X4} - 0x{arrBinOffsets.Last():X4}");
            }

            // Now check if FloatCurve BIN offsets overlap with Array BIN offsets
            var overlap = fcBinOffsets.Intersect(arrBinOffsets).ToList();
            TestContext.WriteLine($"\nOverlapping BIN offsets (FC ∩ Array): {overlap.Count}");
            if (overlap.Count > 0)
                TestContext.WriteLine($"  First few: {string.Join(", ", overlap.Take(5).Select(o => $"0x{o:X4}"))}");

            // Read NrtP ALL entries to check for a second SetFixupTarget
            TestContext.WriteLine($"\n=== ALL PtrN entries re-scan (checking for second SetFixupTarget) ===");
            ptrReader.Position = nrtPPos + 4;
            ptrReader.ReadUInt(); // chunk size
            int entryNum = 0;
            int setFixupCount = 0;
            int depRelativeCount = 0;
            int ptrNullCount = 0;
            int ptrEndCount = 0;
            while (ptrReader.Position + 16 <= chunkEnd)
            {
                uint fixupOffset = ptrReader.ReadUInt();
                ushort ptrType = ptrReader.ReadUShort();
                ushort index = ptrReader.ReadUShort();
                uint destination = ptrReader.ReadUInt();
                uint trailing = ptrReader.ReadUInt();

                if (ptrType == 2) // SetFixupTarget
                {
                    setFixupCount++;
                    TestContext.WriteLine($"  SetFixupTarget #{setFixupCount} at entry {entryNum}: index={index}, fixupOffset=0x{fixupOffset:X4}, dest=0x{destination:X4}");
                }
                else if (ptrType == 3) depRelativeCount++;
                else if (ptrType == 1) ptrNullCount++;
                else if (ptrType == 0) { ptrEndCount++; break; }

                entryNum++;
            }
            TestContext.WriteLine($"SetFixupTarget: {setFixupCount}, DepRelative: {depRelativeCount}, PtrNull: {ptrNullCount}, PtrEnd: {ptrEndCount}");

            Assert.That(fcBinOffsets.Count, Is.GreaterThan(0), "Should have FloatCurve BIN offsets");
        }

        [Test]
        public void Diagnostic_DumpFloatCurveVsArrayRawBIN()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            TestContext.WriteLine($"BIN file size: {binData.Length} (0x{binData.Length:X4})");

            var service = new FIFAAttribDbService();
            service.Load(msVlt.ToArray(), binData);

            // Dump raw bytes at first 10 FloatCurve BIN offsets
            TestContext.WriteLine($"\n=== Raw BIN bytes at first 10 FloatCurve offsets (64 bytes each) ===");
            int fcCount = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        var len = Math.Min(64, binData.Length - binOff);
                        if (len > 0)
                        {
                            var hex = BitConverter.ToString(binData, binOff, len).Replace("-", " ");
                            var fcArr = field.Value as float[];
                            TestContext.WriteLine($"  [{fcCount}] {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{binOff:X4} Value={(fcArr != null ? $"float[{fcArr.Length}]" : "null")}");
                            TestContext.WriteLine($"    Hex: {hex}");
                        }
                        if (++fcCount >= 10) break;
                    }
                }
                if (fcCount >= 10) break;
            }

            // Dump raw bytes at first 10 Array BIN offsets
            TestContext.WriteLine($"\n=== Raw BIN bytes at first 10 Array offsets (64 bytes each) ===");
            int arrCount = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.Array && field.BinaryFileOffset.HasValue)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        var len = Math.Min(64, binData.Length - binOff);
                        if (len > 0)
                        {
                            var hex = BitConverter.ToString(binData, binOff, len).Replace("-", " ");
                            var arrArr = field.Value as float[];
                            TestContext.WriteLine($"  [{arrCount}] {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{binOff:X4} Value={(arrArr != null ? $"float[{arrArr.Length}]" : "null")}");
                            TestContext.WriteLine($"    Hex: {hex}");
                        }
                        if (++arrCount >= 10) break;
                    }
                }
                if (arrCount >= 10) break;
            }

            // Now: what if we just scan the ENTIRE BIN for count/capacity/flags patterns?
            // Count how many FloatCurve-like entries exist in the BIN (count>0, count==cap, flags==4)
            TestContext.WriteLine($"\n=== Full BIN scan for FloatCurve signatures ===");
            var sigOffsets = new List<int>();
            for (int off = 32; off < binData.Length - 8; off += 4)
            {
                var u1 = BitConverter.ToUInt16(binData, off);
                var u2 = BitConverter.ToUInt16(binData, off + 2);
                var u4 = BitConverter.ToUInt32(binData, off + 4);
                if (u1 > 0 && u1 == u2 && u4 == 4 && u1 <= 128)
                {
                    var neededBytes = 8 + u1 * 4;
                    if (off + neededBytes <= binData.Length)
                    {
                        sigOffsets.Add(off);
                    }
                }
            }
            TestContext.WriteLine($"Total FloatCurve-like signatures found: {sigOffsets.Count}");
            TestContext.WriteLine($"First 20: {string.Join(", ", sigOffsets.Take(20).Select(o => $"0x{o:X4}"))}");

            // Check: are ANY of these signatures at FloatCurve BIN offsets?
            var fcBinOffs = new HashSet<long>();
            foreach (var entry in service.Assets)
                foreach (var field in entry.AttribDbType.Fields)
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                        fcBinOffs.Add(field.BinaryFileOffset.Value);

            var matchedSigs = sigOffsets.Where(o => fcBinOffs.Contains(o)).ToList();
            TestContext.WriteLine($"\nSignatures at FloatCurve BIN offsets: {matchedSigs.Count} out of {fcBinOffs.Count} FloatCurve offsets");
            if (matchedSigs.Count > 0)
                TestContext.WriteLine($"  Matched offsets: {string.Join(", ", matchedSigs.Take(10).Select(o => $"0x{o:X4}"))}");

            // Check if signatures are ALL at Array offsets
            var arrBinOffs = new HashSet<long>();
            foreach (var entry in service.Assets)
                foreach (var field in entry.AttribDbType.Fields)
                    if (field.FieldType == FifaAttribDbFieldType.Array && field.BinaryFileOffset.HasValue)
                        arrBinOffs.Add(field.BinaryFileOffset.Value);

            var arrMatched = sigOffsets.Where(o => arrBinOffs.Contains(o)).ToList();
            TestContext.WriteLine($"Signatures at Array BIN offsets: {arrMatched.Count} out of {arrBinOffs.Count} Array offsets");

            // What about at the END of the BIN file?
            TestContext.WriteLine($"\n=== Last 128 bytes of BIN ===");
            var tailOff = Math.Max(0, binData.Length - 128);
            var tailHex = BitConverter.ToString(binData, tailOff, binData.Length - tailOff).Replace("-", " ");
            TestContext.WriteLine($"  BIN@0x{tailOff:X4}: {tailHex}");

            // Check what's really at the FIRST FloatCurve offset (0x00B4)
            TestContext.WriteLine($"\n=== Detailed look at first FloatCurve BIN@0x00B4 ===");
            for (int b = 0; b < 32; b += 2)
            {
                var u16 = BitConverter.ToUInt16(binData, 0x00B4 + b);
                TestContext.WriteLine($"  +0x{b:X2}: u16=0x{u16:X4} ({u16})");
            }
            for (int b = 0; b < 32; b += 4)
            {
                var u32 = BitConverter.ToUInt32(binData, 0x00B4 + b);
                var f32 = BitConverter.ToSingle(binData, 0x00B4 + b);
                TestContext.WriteLine($"  +0x{b:X2}: u32=0x{u32:X8} f32={f32}");
            }

            // Also: what's at offset 0 (BIN header)?
            TestContext.WriteLine($"\n=== BIN first 128 bytes ===");
            var headHex = BitConverter.ToString(binData, 0, Math.Min(128, binData.Length)).Replace("-", " ");
            TestContext.WriteLine($"  {headHex}");

            Assert.That(sigOffsets.Count, Is.GreaterThan(0), "Should find FloatCurve signatures in BIN");
        }

        [Test]
        public void Diagnostic_FloatCurveVLTInlineValues()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            TestContext.WriteLine("=== VLT inline 8-byte values vs BIN data for FloatCurve fields ===");
            int count = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2))
                    {
                        var vltOff = (int)field.VaultValueOffset;
                        var binOff = field.BinaryFileOffset.HasValue ? (int)field.BinaryFileOffset.Value : -1;

                        // Read 8-byte VLT inline value
                        var vltBytes = vltData.Skip(vltOff).Take(8).ToArray();
                        var vltU16a = BitConverter.ToUInt16(vltBytes, 0);
                        var vltU16b = BitConverter.ToUInt16(vltBytes, 2);
                        var vltU32 = BitConverter.ToUInt32(vltBytes, 4);
                        var vltU64 = BitConverter.ToUInt64(vltBytes, 0);

                        string binInfo = "N/A";
                        if (binOff >= 0 && binOff + 8 <= binData.Length)
                        {
                            var binBytes = binData.Skip(binOff).Take(16).ToArray();
                            var binU16a = BitConverter.ToUInt16(binBytes, 0);
                            var binU16b = BitConverter.ToUInt16(binBytes, 2);
                            var binU32 = BitConverter.ToUInt32(binBytes, 4);
                            // Also try reading at offset+8
                            var binAt8 = binData.Skip(binOff + 8).Take(4).ToArray();
                            var binF32At8 = BitConverter.ToSingle(binAt8, 0);
                            binInfo = $"BIN@0x{binOff:X4}: raw={BitConverter.ToString(binData, binOff, 16).Replace("-", " ")} f32@+8={binF32At8}";
                        }

                        TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name}:");
                        TestContext.WriteLine($"    VLT@0x{vltOff:X4} inline: {BitConverter.ToString(vltBytes).Replace("-", " ")}");
                        TestContext.WriteLine($"    VLT u16={vltU16a} u16={vltU16b} u32=0x{vltU32:X8} u64=0x{vltU64:X16}");
                        TestContext.WriteLine($"    {binInfo}");

                        if (++count >= 10) break;
                    }
                }
                if (count >= 10) break;
            }

            // Also dump Array VLT inline values for comparison
            TestContext.WriteLine($"\n=== VLT inline values for Array fields (first 5) ===");
            int arrCount = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.Array)
                    {
                        var vltOff = (int)field.VaultValueOffset;
                        var binOff = field.BinaryFileOffset.HasValue ? (int)field.BinaryFileOffset.Value : -1;
                        var vltBytes = vltData.Skip(vltOff).Take(8).ToArray();
                        var vltU16a = BitConverter.ToUInt16(vltBytes, 0);
                        var vltU16b = BitConverter.ToUInt16(vltBytes, 2);
                        var vltU32 = BitConverter.ToUInt32(vltBytes, 4);

                        TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name}:");
                        TestContext.WriteLine($"    VLT@0x{vltOff:X4} inline: {BitConverter.ToString(vltBytes).Replace("-", " ")}");
                        TestContext.WriteLine($"    VLT u16={vltU16a} u16={vltU16b} u32=0x{vltU32:X8}");
                        if (binOff >= 0)
                            TestContext.WriteLine($"    BIN@0x{binOff:X4}: {BitConverter.ToString(binData, binOff, 16).Replace("-", " ")}");

                        if (++arrCount >= 5) break;
                    }
                }
                if (arrCount >= 5) break;
            }

            // Now test: what if FloatCurve format is just float[] at offset+8, and count comes from VLT?
            // Try reading first FloatCurve with various counts
            TestContext.WriteLine($"\n=== Trying to read FloatCurve with VLT count hypothesis ===");
            var fcEntry = service.Assets.FirstOrDefault(e => e.AttribDbType.Fields.Any(f =>
                (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2) && f.BinaryFileOffset.HasValue));
            if (fcEntry != null)
            {
                var fcField = fcEntry.AttribDbType.Fields.First(f =>
                    (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2) && f.BinaryFileOffset.HasValue);
                var binOff = (int)fcField.BinaryFileOffset.Value;
                var vltOff = (int)fcField.VaultValueOffset;

                var vltBytes = vltData.Skip(vltOff).Take(8).ToArray();
                var vltCount = BitConverter.ToUInt16(vltBytes, 0);
                var vltCapacity = BitConverter.ToUInt16(vltBytes, 2);
                var vltFlags = BitConverter.ToUInt32(vltBytes, 4);

                TestContext.WriteLine($"  Field: {fcEntry.AttribDbType.FolderName}/{fcEntry.AttribDbType.Name}/{fcField.Name}");
                TestContext.WriteLine($"  VLT inline: count={vltCount} capacity={vltCapacity} flags=0x{vltFlags:X8}");

                // Try reading vltCount floats from BIN at offset+8
                if (vltCount > 0 && binOff + 8 + vltCount * 4 <= binData.Length)
                {
                    TestContext.WriteLine($"  Reading {vltCount} floats from BIN@0x{binOff + 8:X4}:");
                    for (int i = 0; i < vltCount; i++)
                    {
                        var f = BitConverter.ToSingle(binData, binOff + 8 + i * 4);
                        TestContext.WriteLine($"    [{i}] = {f}");
                    }
                }

                // Also try: what if count is at VLT+4 (u32)?
                var vltCountU32 = BitConverter.ToUInt32(vltBytes, 0);
                var vltCapU32 = BitConverter.ToUInt32(vltBytes, 4);
                TestContext.WriteLine($"  VLT as u32: count={vltCountU32} capacity={vltCapU32}");

                // Also try: read all floats from offset+8 until we hit the next entry or non-float
                TestContext.WriteLine($"  Scanning for float count at BIN@0x{binOff + 8:X4} up to next entry at 0x{binOff + 0x90:X4}:");
                int floatCount = 0;
                for (int i = 0; i < 34; i++) // max 34 floats in 136 bytes
                {
                    if (binOff + 8 + (i + 1) * 4 > binOff + 0x90) break;
                    var f = BitConverter.ToSingle(binData, binOff + 8 + i * 4);
                    TestContext.WriteLine($"    [{i}] offset+{(8 + i * 4):X2} = {f}");
                    floatCount++;
                }
                TestContext.WriteLine($"  Total possible floats: {floatCount}");
            }

            Assert.That(count, Is.GreaterThan(0), "Should have FloatCurve fields");
        }

        [Test]
        public void Diagnostic_FloatCurveRawVLTBytes()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();

            TestContext.WriteLine("=== Raw VLT bytes at FloatCurve field value offsets ===");
            TestContext.WriteLine($"VLT size: {vltData.Length} (0x{vltData.Length:X4})");

            // Parse VLT to get field offsets
            var msVlt2 = new MemoryStream(vltData);
            var reader = new FIFAAttribDbVLTReader(vltData);

            int count = 0;
            foreach (var type in reader.ListOfDbTypes)
            {
                foreach (var field in type.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2))
                    {
                        var vltOff = (int)field.VaultValueOffset;

                        // The VLT layout around each field:
                        // [hash 8 bytes] [value 8 bytes] [type 8 bytes]
                        // vaultValueOffset points to the value bytes
                        var valueBytes = vltData.Skip(vltOff).Take(8).ToArray();
                        var typeBytes = vltData.Skip(vltOff + 8).Take(8).ToArray();
                        var hashBytes = vltData.Skip(vltOff - 8).Take(8).ToArray();

                        // Also check bytes BEFORE the hash (might be more metadata)
                        var preHashBytes = vltData.Skip(Math.Max(0, vltOff - 24)).Take(24).ToArray();

                        TestContext.WriteLine($"  [{count}] {type.FolderName}/{type.Name}/{field.Name}");
                        TestContext.WriteLine($"    VLT@0x{vltOff:X4} hash: {BitConverter.ToString(hashBytes).Replace("-", " ")}");
                        TestContext.WriteLine($"    VLT@0x{vltOff:X4} value: {BitConverter.ToString(valueBytes).Replace("-", " ")}");
                        TestContext.WriteLine($"    VLT@0x{vltOff:X4} type: {BitConverter.ToString(typeBytes).Replace("-", " ")}");

                        // Check if value bytes have any non-zero data
                        var nonZero = valueBytes.Any(b => b != 0);
                        TestContext.WriteLine($"    Value has non-zero bytes: {nonZero}");

                        if (++count >= 15) break;
                    }
                }
                if (count >= 15) break;
            }

            // Now check: what if the FloatCurve count is encoded as part of the type hash (hashCount)?
            // Look at the type definitions that contain FloatCurve fields
            TestContext.WriteLine($"\n=== Type definitions containing FloatCurve fields ===");
            foreach (var type in reader.ListOfDbTypes)
            {
                var fcCount = type.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2);
                var arrCount = type.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.Array);
                if (fcCount > 0 || arrCount > 0)
                {
                    TestContext.WriteLine($"  {type.FolderName}/{type.Name}: {fcCount} FloatCurves, {arrCount} Arrays, {type.Fields.Count} total fields");
                    TestContext.WriteLine($"    Hashes: {string.Join(", ", type.Hashes?.Select(h => $"0x{h:X16}") ?? new[] { "null" })}");
                }
            }

            // Check: is the VLT value for Array fields also zeros?
            TestContext.WriteLine($"\n=== VLT bytes for first Array field ===");
            count = 0;
            foreach (var type in reader.ListOfDbTypes)
            {
                foreach (var field in type.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.Array)
                    {
                        var vltOff = (int)field.VaultValueOffset;
                        var valueBytes = vltData.Skip(vltOff).Take(8).ToArray();
                        TestContext.WriteLine($"  {type.FolderName}/{type.Name}/{field.Name} VLT@0x{vltOff:X4}: {BitConverter.ToString(valueBytes).Replace("-", " ")}");
                        if (++count >= 5) break;
                    }
                }
                if (count >= 5) break;
            }

            Assert.That(count, Is.GreaterThan(0), "Should have Array fields");
        }

        [Test]
        public void Diagnostic_FloatCurveAllocationAndCount()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            var fcOffsets = new List<(string path, long binOff)>();
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                    {
                        fcOffsets.Add(($"{entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name}", field.BinaryFileOffset.Value));
                    }
                }
            }

            fcOffsets.Sort((a, b) => a.binOff.CompareTo(b.binOff));

            TestContext.WriteLine($"Total FloatCurve fields: {fcOffsets.Count}");

            // Compute gaps between consecutive FloatCurve BIN offsets
            TestContext.WriteLine($"\n=== Spacing between consecutive FloatCurve BIN offsets ===");
            var gaps = new Dictionary<int, int>();
            for (int i = 1; i < fcOffsets.Count; i++)
            {
                int gap = (int)(fcOffsets[i].binOff - fcOffsets[i - 1].binOff);
                if (!gaps.ContainsKey(gap)) gaps[gap] = 0;
                gaps[gap]++;
            }
            foreach (var kvp in gaps.OrderByDescending(x => x.Value))
            {
                TestContext.WriteLine($"  Gap 0x{kvp.Key:X4} ({kvp.Key} bytes): {kvp.Value} occurrences");
            }

            // Print first 30 FloatCurve offsets to see pattern
            TestContext.WriteLine($"\n=== First 30 FloatCurve BIN offsets ===");
            for (int i = 0; i < Math.Min(30, fcOffsets.Count); i++)
            {
                long nextOff = (i + 1 < fcOffsets.Count) ? fcOffsets[i + 1].binOff : binData.Length;
                int availBytes = (int)(nextOff - fcOffsets[i].binOff);
                TestContext.WriteLine($"  [{i,3}] 0x{fcOffsets[i].binOff:X4} ({fcOffsets[i].binOff,5}) avail={availBytes} bytes ({availBytes / 4} floats) - {fcOffsets[i].path}");
            }

            // For each FloatCurve, read the first 8 bytes (prefix) and count valid floats after
            TestContext.WriteLine($"\n=== Analyzing prefix and float count for first 20 FloatCurves ===");
            for (int i = 0; i < Math.Min(20, fcOffsets.Count); i++)
            {
                var binOff = (int)fcOffsets[i].binOff;
                long nextOff = (i + 1 < fcOffsets.Count) ? fcOffsets[i + 1].binOff : binData.Length;
                int availBytes = (int)(nextOff - fcOffsets[i].binOff);

                var prefix0 = BitConverter.ToSingle(binData, binOff + 0);
                var prefix4 = BitConverter.ToSingle(binData, binOff + 4);

                // Count how many floats starting at offset+8 are valid (not NaN, not absurdly large)
                int validFloats = 0;
                for (int f = 0; f * 4 + 8 < availBytes && f * 4 + 8 + 4 <= binData.Length; f++)
                {
                    var val = BitConverter.ToSingle(binData, binOff + 8 + f * 4);
                    if (float.IsNaN(val) || float.IsInfinity(val) || Math.Abs(val) > 1e10) break;
                    validFloats++;
                }

                TestContext.WriteLine($"  [{i,3}] prefix=[{prefix0:F4}, {prefix4:F4}] validFloatsAfterPrefix={validFloats} availFloats={availBytes / 4} - {fcOffsets[i].path}");
            }

            // Try a fixed-size approach: what if FloatCurve is always 0x90 (144) bytes?
            // 144 - 8 prefix = 136 bytes / 4 = 34 floats max
            // Or maybe 14 floats: 8 + 14*4 = 64 bytes, then padded to 0x90
            TestContext.WriteLine($"\n=== Hypothesis: fixed 0x90 (144) byte slot ===");
            int slotSize = 0x90;
            int expectedCount = (slotSize - 8) / 4; // = 34
            TestContext.WriteLine($"  Slot size: {slotSize} bytes, prefix: 8 bytes, max floats: {expectedCount}");

            // Check if all FloatCurve offsets are aligned to slotSize
            int misaligned = 0;
            for (int i = 0; i < fcOffsets.Count; i++)
            {
                long offsetFromStart = fcOffsets[i].binOff - fcOffsets[0].binOff;
                if (offsetFromStart % slotSize != 0)
                {
                    misaligned++;
                    if (misaligned <= 5)
                        TestContext.WriteLine($"  MISALIGNED [{i}] offset=0x{fcOffsets[i].binOff:X4} offsetFromStart=0x{offsetFromStart:X4} not divisible by 0x{slotSize:X4}");
                }
            }
            TestContext.WriteLine($"  Misaligned: {misaligned}/{fcOffsets.Count}");

            // Also check if first FloatCurve offset is 0xB4 and 0xB4 - 0x20 (after header) is divisible by slotSize
            if (fcOffsets.Count > 0)
            {
                long firstDataOff = fcOffsets[0].binOff - 32; // minus BIN header
                TestContext.WriteLine($"  First FloatCurve data offset (from BIN start): 0x{firstDataOff:X4} ({firstDataOff})");
                TestContext.WriteLine($"  Divisible by 0x{slotSize:X4}? {firstDataOff % slotSize == 0}");
            }

            // Read 14 floats from each of first 5 and show them as curve data
            TestContext.WriteLine($"\n=== First 5 FloatCurves as 14-float curves ===");
            for (int i = 0; i < Math.Min(5, fcOffsets.Count); i++)
            {
                var binOff = (int)fcOffsets[i].binOff;
                var vals = new List<float>();
                for (int f = 0; f < 14; f++)
                {
                    if (binOff + 8 + f * 4 + 4 > binData.Length) break;
                    vals.Add(BitConverter.ToSingle(binData, binOff + 8 + f * 4));
                }
                TestContext.WriteLine($"  [{i}] {fcOffsets[i].path}: [{string.Join(", ", vals.Select(v => v.ToString("F4")))}]");
            }

            Assert.That(fcOffsets.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Diagnostic_FloatCurveSizesFromTypeLayout()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            var nonScalarTypes = new[] { FifaAttribDbFieldType.FloatCurve, FifaAttribDbFieldType.FloatCurve2, FifaAttribDbFieldType.Array, FifaAttribDbFieldType.String, FifaAttribDbFieldType.RawBytes };

            TestContext.WriteLine("=== FloatCurve sizes derived from type BIN layout ===");
            int totalFC = 0;
            var sizes = new Dictionary<int, int>();

            foreach (var entry in service.Assets)
            {
                var nonScalarFields = entry.AttribDbType.Fields
                    .Where(f => nonScalarTypes.Contains(f.FieldType) && f.BinaryFileOffset.HasValue)
                    .OrderBy(f => f.BinaryFileOffset.Value)
                    .ToList();

                if (nonScalarFields.Count == 0) continue;

                // For each non-scalar field, its size is the gap to the next field's offset
                for (int i = 0; i < nonScalarFields.Count; i++)
                {
                    var field = nonScalarFields[i];
                    if (field.FieldType != FifaAttribDbFieldType.FloatCurve && field.FieldType != FifaAttribDbFieldType.FloatCurve2)
                        continue;

                    totalFC++;
                    long fieldEnd;
                    if (i + 1 < nonScalarFields.Count)
                    {
                        fieldEnd = nonScalarFields[i + 1].BinaryFileOffset.Value;
                    }
                    else
                    {
                        // Last non-scalar field - use end of type's BIN block or just next type
                        fieldEnd = field.BinaryFileOffset.Value + 0x90; // estimate
                    }

                    int fieldSizeBytes = (int)(fieldEnd - field.BinaryFileOffset.Value);
                    int floatCount = fieldSizeBytes / 4;

                    if (!sizes.ContainsKey(floatCount)) sizes[floatCount] = 0;
                    sizes[floatCount]++;

                    if (totalFC <= 30)
                    {
                        TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{field.BinaryFileOffset.Value:X4} size={fieldSizeBytes} bytes = {floatCount} floats");
                    }
                }
            }

            TestContext.WriteLine($"\nTotal FloatCurve fields: {totalFC}");
            TestContext.WriteLine($"\nFloat count distribution:");
            foreach (var kvp in sizes.OrderByDescending(x => x.Value))
                TestContext.WriteLine($"  {kvp.Key} floats ({kvp.Key * 4} bytes): {kvp.Value} fields");

            // Now check: does the prefix (first 8 bytes) actually encode count/capacity?
            TestContext.WriteLine($"\n=== Checking if first u16 of FloatCurve data is count ===");
            int checkedCount = 0;
            var countMatches = new Dictionary<int, int>();
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        if (binOff + 8 > binData.Length) continue;

                        var u16a = BitConverter.ToUInt16(binData, binOff);
                        var u16b = BitConverter.ToUInt16(binData, binOff + 2);

                        // Check if u16a could be a count (small number, <= 128)
                        if (u16a <= 128 && !countMatches.ContainsKey(u16a)) countMatches[u16a] = 0;
                        if (u16a <= 128) countMatches[u16a]++;

                        if (++checkedCount >= 20)
                        {
                            TestContext.WriteLine($"  First u16 distribution (first 20 FCs): {string.Join(", ", countMatches.OrderByDescending(x => x.Value).Take(10).Select(x => $"{x.Key}:{x.Value}"))}");
                            break;
                        }
                    }
                }
                if (checkedCount >= 20) break;
            }

            Assert.That(totalFC, Is.GreaterThan(0));
        }

        [Test]
        public void Diagnostic_EmptyFloatCurvesLastField()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            TestContext.WriteLine("=== FloatCurve fields with NO value (empty) ===");
            int emptyCount = 0;
            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if ((field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2) && field.BinaryFileOffset.HasValue)
                    {
                        if (field.Value is not float[] arr || arr.Length == 0)
                        {
                            emptyCount++;
                            TestContext.WriteLine($"  {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}/{field.Name} BIN@0x{field.BinaryFileOffset.Value:X4} size={field.BinaryFileSize?.ToString() ?? "null"}");

                            // Check if it's the last non-scalar field
                            var nonScalarFields = entry.AttribDbType.Fields
                                .Where(f => f.BinaryFileOffset.HasValue)
                                .OrderBy(f => f.BinaryFileOffset.Value)
                                .ToList();
                            bool isLast = nonScalarFields.LastOrDefault()?.Name == field.Name;
                            TestContext.WriteLine($"    isLastNonScalar={isLast} totalNonScalar={nonScalarFields.Count}");

                            // Peek at BIN data
                            var binOff = (int)field.BinaryFileOffset.Value;
                            if (binOff + 16 <= binData.Length)
                            {
                                var hex = BitConverter.ToString(binData, binOff, 16).Replace("-", " ");
                                TestContext.WriteLine($"    BIN peek: {hex}");
                            }
                        }
                    }
                }
            }
            TestContext.WriteLine($"\nTotal empty FloatCurves: {emptyCount}");

            // Now look at NpxE entries to see if offset2 is BIN data size
            var npxeMarker = Encoding.ASCII.GetBytes("NpxE");
            int npxePos = -1;
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] == npxeMarker[0] && vltData[i + 1] == npxeMarker[1] &&
                    vltData[i + 2] == npxeMarker[2] && vltData[i + 3] == npxeMarker[3])
                { npxePos = i; break; }
            }
            Assert.That(npxePos, Is.GreaterThanOrEqualTo(0));

            using var reader = new NativeReader(new MemoryStream(vltData));
            reader.Position = npxePos + 4;
            var countOfFields = reader.ReadInt();
            var countOfTypes = reader.ReadInt();
            reader.Pad(16);

            var typeEntries = new List<(ulong hash1, ulong hash2, uint offset1, uint offset2)>();
            for (int i = 0; i < countOfTypes; i++)
                typeEntries.Add((reader.ReadULong(), reader.ReadULong(), reader.ReadUInt(), reader.ReadUInt()));

            // For the types with empty FloatCurves, show their NpxE entries
            TestContext.WriteLine($"\n=== NpxE entries for types with empty FloatCurves ===");
            foreach (var entry in service.Assets)
            {
                bool hasEmpty = entry.AttribDbType.Fields.Any(f =>
                    (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2) &&
                    f.BinaryFileOffset.HasValue && (f.Value is not float[] arr || arr.Length == 0));
                if (!hasEmpty) continue;

                var types2 = service.Assets;
                var typeIdx = types2.IndexOf(entry);
                if (typeIdx >= 0 && typeIdx < typeEntries.Count)
                {
                    var ne = typeEntries[typeIdx];
                    int fcCount = entry.AttribDbType.Fields.Count(f => f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2);
                    TestContext.WriteLine($"  [{typeIdx}] {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}: off1=0x{ne.offset1:X8} off2=0x{ne.offset2:X8} FloatCurves={fcCount}");
                }
            }

            // Also check: for each type with FloatCurves, does offset2 = total BIN data size for non-scalar fields?
            TestContext.WriteLine($"\n=== Checking offset2 vs BIN data size for types with FloatCurves ===");
            foreach (var entry in service.Assets)
            {
                int fcCount = entry.AttribDbType.Fields.Count(f =>
                    (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2) && f.BinaryFileOffset.HasValue);
                if (fcCount == 0) continue;

                var types2 = service.Assets;
                var typeIdx = types2.IndexOf(entry);
                if (typeIdx < 0 || typeIdx >= typeEntries.Count) continue;

                var ne = typeEntries[typeIdx];
                var nonScalarFields = entry.AttribDbType.Fields
                    .Where(f => f.BinaryFileOffset.HasValue)
                    .OrderBy(f => f.BinaryFileOffset.Value)
                    .ToList();

                if (nonScalarFields.Count == 0) continue;

                long firstBin = nonScalarFields.First().BinaryFileOffset.Value;
                long lastBin = nonScalarFields.Last().BinaryFileOffset.Value;
                long computedSize = lastBin - firstBin + (nonScalarFields.Last().BinaryFileSize ?? 0);

                TestContext.WriteLine($"  [{typeIdx}] {entry.AttribDbType.FolderName}/{entry.AttribDbType.Name}: off1=0x{ne.offset1:X8} off2=0x{ne.offset2:X8} firstBin=0x{firstBin:X4} computedTotal=0x{computedSize:X4}({computedSize}) off2==computed?{ne.offset2 == (uint)computedSize}");

                if (typeIdx >= 5) break;
            }

            Assert.That(true);
        }

        [Test]
        public void Diagnostic_ReadFloatCurvesWith14Count()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            int totalFC = 0;
            int withValues = 0;
            int empty = 0;
            var allCounts = new Dictionary<int, int>();

            foreach (var entry in service.Assets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (field.FieldType == FifaAttribDbFieldType.FloatCurve || field.FieldType == FifaAttribDbFieldType.FloatCurve2)
                    {
                        totalFC++;
                        if (field.Value is float[] arr && arr.Length > 0)
                        {
                            withValues++;
                            if (!allCounts.ContainsKey(arr.Length)) allCounts[arr.Length] = 0;
                            allCounts[arr.Length]++;
                        }
                        else
                        {
                            empty++;
                        }
                    }
                }
            }

            TestContext.WriteLine($"Total FloatCurve fields: {totalFC}");
            TestContext.WriteLine($"With values: {withValues}");
            TestContext.WriteLine($"Empty: {empty}");
            TestContext.WriteLine($"Value length distribution:");
            foreach (var kvp in allCounts.OrderByDescending(x => x.Value))
                TestContext.WriteLine($"  Length {kvp.Key}: {kvp.Value} fields");

            Assert.That(withValues, Is.GreaterThan(0), "Should have FloatCurves with values");
        }

        [Test]
        public void Validate_FloatCurveModificationAndBinWriteBack()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(msVlt.ToArray(), binData);

            var fcField = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .First(f => (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2)
                            && f.BinaryFileOffset.HasValue && f.Value is float[] arr && arr.Length > 0);

            var originalValues = (float[])fcField.Value;
            TestContext.WriteLine($"Original field: {fcField.Name}, {originalValues.Length} floats");
            TestContext.WriteLine($"  First 5: [{string.Join(", ", originalValues.Take(5).Select(v => v.ToString("F4")))}]");

            var modifiedValues = new float[originalValues.Length];
            Array.Copy(originalValues, modifiedValues, originalValues.Length);
            modifiedValues[0] = 999.0f;
            modifiedValues[1] = 888.0f;
            fcField.Value = modifiedValues;

            var modifiedBin = service.GetModifiedBinaryData();
            Assert.That(modifiedBin, Is.Not.Null);

            var binOff = (int)fcField.BinaryFileOffset.Value;
            var readBack0 = BitConverter.ToSingle(modifiedBin, binOff);
            var readBack1 = BitConverter.ToSingle(modifiedBin, binOff + 4);
            TestContext.WriteLine($"Modified BIN: [{readBack0:F4}, {readBack1:F4}, ...]");
            Assert.That(readBack0, Is.EqualTo(999.0f));
            Assert.That(readBack1, Is.EqualTo(888.0f));

            for (int i = 2; i < originalValues.Length; i++)
            {
                var readBack = BitConverter.ToSingle(modifiedBin, binOff + i * 4);
                Assert.That(readBack, Is.EqualTo(originalValues[i]), $"Float at index {i} should be unchanged");
            }

            TestContext.WriteLine("BIN write-back validation passed");
        }

        [Test]
        public void Validate_ArrayModificationAndBinWriteBack()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(msVlt.ToArray(), binData);

            var arrField = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .First(f => f.FieldType == FifaAttribDbFieldType.Array
                            && f.BinaryFileOffset.HasValue && f.Value is float[] arr && arr.Length > 0);

            var originalValues = (float[])arrField.Value;
            TestContext.WriteLine($"Original Array field: {arrField.Name}, {originalValues.Length} floats");

            var modifiedValues = new float[originalValues.Length];
            Array.Copy(originalValues, modifiedValues, originalValues.Length);
            modifiedValues[0] = 777.0f;
            arrField.Value = modifiedValues;

            var modifiedBin = service.GetModifiedBinaryData();
            var binOff = (int)arrField.BinaryFileOffset.Value;

            var arrCount = BitConverter.ToUInt16(modifiedBin, binOff);
            var arrCap = BitConverter.ToUInt16(modifiedBin, binOff + 2);
            var arrFlags = BitConverter.ToUInt32(modifiedBin, binOff + 4);
            var firstVal = BitConverter.ToSingle(modifiedBin, binOff + 8);

            TestContext.WriteLine($"BIN header: count={arrCount} cap={arrCap} flags=0x{arrFlags:X8}");
            TestContext.WriteLine($"BIN first float: {firstVal:F4}");

            Assert.That(arrCount, Is.EqualTo((ushort)modifiedValues.Length));
            Assert.That(arrCap, Is.EqualTo((ushort)modifiedValues.Length));
            Assert.That(arrFlags, Is.EqualTo((uint)4));
            Assert.That(firstVal, Is.EqualTo(777.0f));

            for (int i = 1; i < originalValues.Length; i++)
            {
                var readBack = BitConverter.ToSingle(modifiedBin, binOff + 8 + i * 4);
                Assert.That(readBack, Is.EqualTo(originalValues[i]), $"Float at index {i} should be unchanged");
            }

            TestContext.WriteLine("Array BIN write-back validation passed");
        }

        [Test]
        public void Validate_VltRoundTripWithModifiedFields()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var attribDbVltData = msVlt.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(attribDbVltData, msBin.ToArray());

            var floatField = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .First(f => f.FieldType == FifaAttribDbFieldType.Float && f.Value is float);

            var originalFloat = (float)floatField.Value;
            floatField.Value = 42.0f;

            var writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;

            var vltPos = (int)floatField.VaultValueOffset;
            var writtenFloat = BitConverter.ToSingle(writtenVltData, vltPos);
            TestContext.WriteLine($"Original: {originalFloat}, Written: {writtenFloat}");
            Assert.That(writtenFloat, Is.EqualTo(42.0f));

            var intField = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .FirstOrDefault(f => f.FieldType == FifaAttribDbFieldType.Int32 && f.Value is int);

            if (intField != null)
            {
                var originalInt = (int)intField.Value;
                intField.Value = 999;
                writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;
                var writtenInt = BitConverter.ToInt32(writtenVltData, (int)intField.VaultValueOffset);
                TestContext.WriteLine($"Int original: {originalInt}, Written: {writtenInt}");
                Assert.That(writtenInt, Is.EqualTo(999));
            }

            TestContext.WriteLine("VLT roundtrip with modified fields passed");
        }

        [Test]
        public void Diagnostic_StringRawBytesInvestigation()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var vltData = msVlt.ToArray();
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
            var binData = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(vltData, binData);

            var stringFields = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .Where(f => f.FieldType == FifaAttribDbFieldType.String)
                .ToList();

            var rawBytesFields = service.Assets
                .SelectMany(e => e.AttribDbType.Fields)
                .Where(f => f.FieldType == FifaAttribDbFieldType.RawBytes)
                .ToList();

            TestContext.WriteLine($"String fields: {stringFields.Count}");
            TestContext.WriteLine($"RawBytes fields: {rawBytesFields.Count}");

            TestContext.WriteLine("\n=== Sample String fields (first 20) ===");
            foreach (var f in stringFields.Take(20))
            {
                TestContext.WriteLine($"  {f.Name} | Hash=0x{f.Hash:X16} | VltOff=0x{f.VaultValueOffset:X4} | BINOff={f.BinaryFileOffset} | Parent={f.ParentEntry?.Name}");
            }

            TestContext.WriteLine("\n=== Raw VLT bytes for String field values ===");
            var valueDistribution = new Dictionary<long, int>();
            foreach (var f in stringFields)
            {
                int off = (int)f.VaultValueOffset;
                if (off + 8 <= vltData.Length)
                {
                    var bytes = new byte[8];
                    Array.Copy(vltData, off, bytes, 0, 8);
                    var asUlong = BitConverter.ToUInt64(bytes);
                    if (!valueDistribution.ContainsKey((long)asUlong))
                        valueDistribution[(long)asUlong] = 0;
                    valueDistribution[(long)asUlong]++;
                }
            }

            TestContext.WriteLine("Value distribution:");
            foreach (var kvp in valueDistribution.OrderByDescending(x => x.Value).Take(20))
            {
                TestContext.WriteLine($"  0x{kvp.Key:X16} ({kvp.Key}): {kvp.Value} fields");
            }

            TestContext.WriteLine("\n=== Search for 4-marker patterns in VLT (string table markers) ===");
            var knownMarkers = new[] { "StbT", "StTb", "StrT", "StRt", "Stng", "Srng", "ErtS", "NpeDP", "NpxE", "NrtP" };
            foreach (var marker in knownMarkers)
            {
                var markerBytes = Encoding.ASCII.GetBytes(marker);
                for (int i = 0; i < vltData.Length - 4; i++)
                {
                    if (vltData[i] == markerBytes[0] && vltData[i + 1] == markerBytes[1] &&
                        vltData[i + 2] == markerBytes[2] && vltData[i + 3] == markerBytes[3])
                    {
                        TestContext.WriteLine($"  Found '{marker}' at VLT offset 0x{i:X4}");
                    }
                }
            }

            TestContext.WriteLine("\n=== All 4-byte ASCII markers in VLT file ===");
            for (int i = 0; i < vltData.Length - 4; i++)
            {
                if (vltData[i] >= 0x41 && vltData[i] <= 0x5A &&
                    vltData[i + 1] >= 0x41 && vltData[i + 1] <= 0x5A &&
                    vltData[i + 2] >= 0x41 && vltData[i + 2] <= 0x5A &&
                    vltData[i + 3] >= 0x41 && vltData[i + 3] <= 0x5A)
                {
                    var marker = Encoding.ASCII.GetString(vltData, i, 4);
                    TestContext.WriteLine($"  VLT@0x{i:X4}: {marker}");
                }
            }

            TestContext.WriteLine("\n=== All 4-byte ASCII markers in BIN file ===");
            for (int i = 0; i < binData.Length - 4; i++)
            {
                if (binData[i] >= 0x41 && binData[i] <= 0x5A &&
                    binData[i + 1] >= 0x41 && binData[i + 1] <= 0x5A &&
                    binData[i + 2] >= 0x41 && binData[i + 2] <= 0x5A &&
                    binData[i + 3] >= 0x41 && binData[i + 3] <= 0x5A)
                {
                    var marker = Encoding.ASCII.GetString(binData, i, 4);
                    TestContext.WriteLine($"  BIN@0x{i:X4}: {marker}");
                }
            }

            TestContext.WriteLine("\n=== NpxE entry details ===");
            var reader = new FIFAAttribDbVLTReader(vltData);
            TestContext.WriteLine($"  Total types: {reader.ListOfDbTypes.Count}");

            TestContext.WriteLine("\n=== RawBytes field details ===");
            foreach (var f in rawBytesFields)
            {
                int off = (int)f.VaultValueOffset;
                if (off + 8 <= vltData.Length)
                {
                    var bytes = new byte[8];
                    Array.Copy(vltData, off, bytes, 0, 8);
                    TestContext.WriteLine($"  {f.Name}: VltOff=0x{off:X4} value=[{BitConverter.ToString(bytes).Replace("-", " ")}] Parent={f.ParentEntry?.Name}");
                }
            }

            TestContext.WriteLine("\n=== Check if String VLT values could be offsets into full VLT data ===");
            foreach (var f in stringFields.Take(10))
            {
                int off = (int)f.VaultValueOffset;
                if (off + 8 <= vltData.Length)
                {
                    var bytes = new byte[8];
                    Array.Copy(vltData, off, bytes, 0, 8);
                    var asInt32 = BitConverter.ToInt32(bytes);
                    if (asInt32 > 0 && asInt32 + 8 <= vltData.Length)
                    {
                        var preview = BitConverter.ToString(vltData, asInt32, Math.Min(32, vltData.Length - asInt32)).Replace("-", " ");
                        TestContext.WriteLine($"  {f.Name}: value={asInt32} -> VLT@0x{asInt32:X4}: {preview}");
                    }
                }
            }
            //arrField.Value = modifiedValues;

            //var modifiedBin = service.GetModifiedBinaryData();
            //var binOff = (int)arrField.BinaryFileOffset.Value;

            //var arrCount = BitConverter.ToUInt16(modifiedBin, binOff);
            //var arrCap = BitConverter.ToUInt16(modifiedBin, binOff + 2);
            //var arrFlags = BitConverter.ToUInt32(modifiedBin, binOff + 4);
            //var firstVal = BitConverter.ToSingle(modifiedBin, binOff + 8);

            //TestContext.WriteLine($"BIN header: count={arrCount} cap={arrCap} flags=0x{arrFlags:X8}");
            //TestContext.WriteLine($"BIN first float: {firstVal:F4}");

            //Assert.That(arrCount, Is.EqualTo((ushort)modifiedValues.Length));
            //Assert.That(arrCap, Is.EqualTo((ushort)modifiedValues.Length));
            //Assert.That(arrFlags, Is.EqualTo((uint)4));
            //Assert.That(firstVal, Is.EqualTo(777.0f));

            //for (int i = 1; i < originalValues.Length; i++)
            //{
            //    var readBack = BitConverter.ToSingle(modifiedBin, binOff + 8 + i * 4);
            //    Assert.That(readBack, Is.EqualTo(originalValues[i]), $"Float at index {i} should be unchanged");
            //}

            TestContext.WriteLine("Array BIN write-back validation passed");
        }

        //[Test]
        //public void Validate_VltRoundTripWithModifiedFields()
        //{
        //    var msVlt = new MemoryStream();
        //    EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
        //    var msBin = new MemoryStream();
        //    EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
        //    var attribDbVltData = msVlt.ToArray();

        //    var service = new FIFAAttribDbService();
        //    service.Load(attribDbVltData, msBin.ToArray());

        //    var floatField = service.Assets
        //        .SelectMany(e => e.AttribDbType.Fields)
        //        .First(f => f.FieldType == FifaAttribDbFieldType.Float && f.Value is float);

        //    var originalFloat = (float)floatField.Value;
        //    floatField.Value = 42.0f;

        //    var writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;

        //    var vltPos = (int)floatField.VaultValueOffset;
        //    var writtenFloat = BitConverter.ToSingle(writtenVltData, vltPos);
        //    TestContext.WriteLine($"Original: {originalFloat}, Written: {writtenFloat}");
        //    Assert.That(writtenFloat, Is.EqualTo(42.0f));

        //    var intField = service.Assets
        //        .SelectMany(e => e.AttribDbType.Fields)
        //        .FirstOrDefault(f => f.FieldType == FifaAttribDbFieldType.Int32 && f.Value is int);

        //    if (intField != null)
        //    {
        //        var originalInt = (int)intField.Value;
        //        intField.Value = 999;
        //        writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;
        //        var writtenInt = BitConverter.ToInt32(writtenVltData, (int)intField.VaultValueOffset);
        //        TestContext.WriteLine($"Int original: {originalInt}, Written: {writtenInt}");
        //        Assert.That(writtenInt, Is.EqualTo(999));
        //    }

        //    TestContext.WriteLine("VLT roundtrip with modified fields passed");
        //}

        //[Test]
        //public void Diagnostic_StringRawBytesInvestigation()
        //{
        //    var msVlt = new MemoryStream();
        //    EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
        //    var vltData = msVlt.ToArray();
        //    var msBin = new MemoryStream();
        //    EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);
        //    var binData = msBin.ToArray();

        //    var service = new FIFAAttribDbService();
        //    service.Load(vltData, binData);

        //    var stringFields = service.Assets
        //        .SelectMany(e => e.AttribDbType.Fields)
        //        .Where(f => f.FieldType == FifaAttribDbFieldType.String)
        //        .ToList();

        //    var rawBytesFields = service.Assets
        //        .SelectMany(e => e.AttribDbType.Fields)
        //        .Where(f => f.FieldType == FifaAttribDbFieldType.RawBytes)
        //        .ToList();

        //    TestContext.WriteLine($"String fields: {stringFields.Count}");
        //    TestContext.WriteLine($"RawBytes fields: {rawBytesFields.Count}");

        //    TestContext.WriteLine("\n=== Sample String fields (first 20) ===");
        //    foreach (var f in stringFields.Take(20))
        //    {
        //        TestContext.WriteLine($"  {f.Name} | Hash=0x{f.Hash:X16} | VltOff=0x{f.VaultValueOffset:X4} | BINOff={f.BinaryFileOffset} | Parent={f.ParentEntry?.Name}");
        //    }

        //    TestContext.WriteLine("\n=== Raw VLT bytes for String field values ===");
        //    var valueDistribution = new Dictionary<long, int>();
        //    foreach (var f in stringFields)
        //    {
        //        int off = (int)f.VaultValueOffset;
        //        if (off + 8 <= vltData.Length)
        //        {
        //            var bytes = new byte[8];
        //            Array.Copy(vltData, off, bytes, 0, 8);
        //            var asUlong = BitConverter.ToUInt64(bytes);
        //            if (!valueDistribution.ContainsKey((long)asUlong))
        //                valueDistribution[(long)asUlong] = 0;
        //            valueDistribution[(long)asUlong]++;
        //        }
        //    }

        //    TestContext.WriteLine("Value distribution:");
        //    foreach (var kvp in valueDistribution.OrderByDescending(x => x.Value).Take(20))
        //    {
        //        TestContext.WriteLine($"  0x{kvp.Key:X16} ({kvp.Key}): {kvp.Value} fields");
        //    }

        //    TestContext.WriteLine("\n=== Search for 4-marker patterns in VLT (string table markers) ===");
        //    var knownMarkers = new[] { "StbT", "StTb", "StrT", "StRt", "Stng", "Srng", "ErtS", "NpeDP", "NpxE", "NrtP" };
        //    foreach (var marker in knownMarkers)
        //    {
        //        var markerBytes = Encoding.ASCII.GetBytes(marker);
        //        for (int i = 0; i < vltData.Length - 4; i++)
        //        {
        //            if (vltData[i] == markerBytes[0] && vltData[i + 1] == markerBytes[1] &&
        //                vltData[i + 2] == markerBytes[2] && vltData[i + 3] == markerBytes[3])
        //            {
        //                TestContext.WriteLine($"  Found '{marker}' at VLT offset 0x{i:X4}");
        //            }
        //        }
        //    }

        //    TestContext.WriteLine("\n=== All 4-byte ASCII markers in VLT file ===");
        //    for (int i = 0; i < vltData.Length - 4; i++)
        //    {
        //        if (vltData[i] >= 0x41 && vltData[i] <= 0x5A &&
        //            vltData[i + 1] >= 0x41 && vltData[i + 1] <= 0x5A &&
        //            vltData[i + 2] >= 0x41 && vltData[i + 2] <= 0x5A &&
        //            vltData[i + 3] >= 0x41 && vltData[i + 3] <= 0x5A)
        //        {
        //            var marker = Encoding.ASCII.GetString(vltData, i, 4);
        //            TestContext.WriteLine($"  VLT@0x{i:X4}: {marker}");
        //        }
        //    }

        //    TestContext.WriteLine("\n=== All 4-byte ASCII markers in BIN file ===");
        //    for (int i = 0; i < binData.Length - 4; i++)
        //    {
        //        if (binData[i] >= 0x41 && binData[i] <= 0x5A &&
        //            binData[i + 1] >= 0x41 && binData[i + 1] <= 0x5A &&
        //            binData[i + 2] >= 0x41 && binData[i + 2] <= 0x5A &&
        //            binData[i + 3] >= 0x41 && binData[i + 3] <= 0x5A)
        //        {
        //            var marker = Encoding.ASCII.GetString(binData, i, 4);
        //            TestContext.WriteLine($"  BIN@0x{i:X4}: {marker}");
        //        }
        //    }

        //    TestContext.WriteLine("\n=== NpxE entry details ===");
        //    var reader = new FIFAAttribDbVLTReader(vltData);
        //    TestContext.WriteLine($"  Total types: {reader.ListOfDbTypes.Count}");

        //    TestContext.WriteLine("\n=== RawBytes field details ===");
        //    foreach (var f in rawBytesFields)
        //    {
        //        int off = (int)f.VaultValueOffset;
        //        if (off + 8 <= vltData.Length)
        //        {
        //            var bytes = new byte[8];
        //            Array.Copy(vltData, off, bytes, 0, 8);
        //            TestContext.WriteLine($"  {f.Name}: VltOff=0x{off:X4} value=[{BitConverter.ToString(bytes).Replace("-", " ")}] Parent={f.ParentEntry?.Name}");
        //        }
        //    }

        //    TestContext.WriteLine("\n=== Check if String VLT values could be offsets into full VLT data ===");
        //    foreach (var f in stringFields.Take(10))
        //    {
        //        int off = (int)f.VaultValueOffset;
        //        if (off + 8 <= vltData.Length)
        //        {
        //            var bytes = new byte[8];
        //            Array.Copy(vltData, off, bytes, 0, 8);
        //            var asInt32 = BitConverter.ToInt32(bytes);
        //            if (asInt32 > 0 && asInt32 + 8 <= vltData.Length)
        //            {
        //                var preview = BitConverter.ToString(vltData, asInt32, Math.Min(32, vltData.Length - asInt32)).Replace("-", " ");
        //                TestContext.WriteLine($"  {f.Name}: value={asInt32} -> VLT@0x{asInt32:X4}: {preview}");
        //            }
        //            else
        //            {
        //                TestContext.WriteLine($"  {f.Name}: value={asInt32} (out of range or zero)");
        //            }
        //        }
        //    }
        //}

        [Test]
        public void TestModifyArrayAndFloatCurveWithoutCorruption()
        {
            var msVlt = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msVlt);
            var msBin = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msBin);

            var originalVlt = msVlt.ToArray();
            var originalBin = msBin.ToArray();

            var service = new FIFAAttribDbService();
            service.Load(originalVlt, originalBin);

            var movement = service.GetAssetEntry("actor/movement");
            Assert.That(movement, Is.Not.Null);

            var allFields = service.Assets.SelectMany(x => x.AttribDbType.Fields).ToList();

            var arrayField = allFields.FirstOrDefault(f => f.FieldType == FifaAttribDbFieldType.Array && f.BinaryFileOffset.HasValue);
            var curveField = allFields.FirstOrDefault(f => (f.FieldType == FifaAttribDbFieldType.FloatCurve || f.FieldType == FifaAttribDbFieldType.FloatCurve2) && f.BinaryFileOffset.HasValue);

            Assert.That(arrayField, Is.Not.Null);
            Assert.That(curveField, Is.Not.Null);

            var originalArrayValues = (float[])((float[])arrayField.Value).Clone();
            var originalCurveValues = (float[])((float[])curveField.Value).Clone();

            // Modify array value
            var modifiedArrayValues = (float[])originalArrayValues.Clone();
            modifiedArrayValues[0] += 5.5f;
            arrayField.Value = modifiedArrayValues;

            // Modify curve value
            var modifiedCurveValues = (float[])originalCurveValues.Clone();
            modifiedCurveValues[0] += 10.5f;
            curveField.Value = modifiedCurveValues;

            // Ensure the parent entry is dirty
            Assert.That(arrayField.ParentEntry.IsModified, Is.True);

            // Generate modified binary
            var modifiedBin = service.GetModifiedBinaryData();
            Assert.That(modifiedBin, Is.Not.Null);
            Assert.That(modifiedBin.Length, Is.EqualTo(originalBin.Length));

            // Verify modifications in the generated BIN
            // Array verification: count and capacity should match, flags at offset + 4 should NOT be overwritten (they should match original)
            int arrayOff = (int)arrayField.BinaryFileOffset.Value;
            var originalFlags = BitConverter.ToUInt32(originalBin, arrayOff + 4);
            var modifiedFlags = BitConverter.ToUInt32(modifiedBin, arrayOff + 4);
            Assert.That(modifiedFlags, Is.EqualTo(originalFlags), "Array flags at offset + 4 were corrupted!");

            var modifiedArrayValInBin = BitConverter.ToSingle(modifiedBin, arrayOff + 8);
            Assert.That(modifiedArrayValInBin, Is.EqualTo(modifiedArrayValues[0]));

            // Curve verification: first element modified, rest unchanged
            int curveOff = (int)curveField.BinaryFileOffset.Value;
            var modifiedCurveValInBin = BitConverter.ToSingle(modifiedBin, curveOff);
            Assert.That(modifiedCurveValInBin, Is.EqualTo(modifiedCurveValues[0]));
        }
    }
}
