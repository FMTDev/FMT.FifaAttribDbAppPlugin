using FMT.FileTools;
using System.Text;

namespace FifaAttribDbAppPlugin.AttribDb
{
    public class FIFAAttribDbVLTReader : NativeReader
    {
        public List<FIFAAttribDbType> ListOfDbTypes = new List<FIFAAttribDbType>();

        public FIFAAttribDbVLTReader(string filePath) : base(filePath)
        {
        }

        public FIFAAttribDbVLTReader(byte[] vltData) : base(vltData)
        {
            try
            {
                this.Position = 0;
                this.ReadBytes(32); // Skip header
                this.ReadBytes(4); // Skip header
                var typeCount = this.ReadInt() * 2; // Type Count
                this.ReadBytes(8); // Skip header
                this.ReadBytes(16); // Skip header
                var unknownInt = this.ReadInt();
                var unknownInt2 = this.ReadInt();
                var attribdbvltString = this.ReadNullTerminatedString(); // attribdb.vlt
                var attribdbbinString = this.ReadNullTerminatedString(); // attribdb.bin
                if (!attribdbbinString.Equals("attribdb.bin", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("FifaAttribDbVlt didn't read correctly");
                this.Pad(16);
                this.ReadInt();
                this.ReadInt();

                Dictionary<long, ulong> vaultValueOffsetToFieldHash = new Dictionary<long, ulong>();
                Dictionary<ulong, Dictionary<ulong, FIFAAttribDbField>> fieldHashToDbFieldByType = new Dictionary<ulong, Dictionary<ulong, FIFAAttribDbField>>();

                var countOfFields = -1;
                var countOfTypes = -1;
                var positionBeforeSearch = this.Position;
                var npxePosition = -1L;
                while (npxePosition == -1)
                {
                    var fourBytes = this.ReadBytes(4);
                    if (UTF8Encoding.UTF8.GetString(fourBytes).Equals("NpxE", StringComparison.OrdinalIgnoreCase))
                    {
                        npxePosition = this.Position - 4;
                        countOfFields = this.ReadInt();
                        countOfTypes = this.ReadInt();
                        break;
                    }
                }
                this.Position = positionBeforeSearch;

                if (npxePosition < 0)
                    return;

                while (this.Position < npxePosition && ListOfDbTypes.Count < countOfTypes)
                {
                    try
                    {
                        var fileNameHash = this.ReadULong();
                        if (fileNameHash == 0)
                            break;

                        long startOfData = this.Position;
                        this.Position = startOfData;
                        // File Name -> Balance
                        //fileNameHash = this.ReadULong(); // Hash D9 76 8E 74 05 8D FD 7F = Balance
                        var fileName = FIFAAttribDbFileNameHashDictionary.FileNameHash.TryGetValue(fileNameHash, out var fn) ? fn : $"Unknown_{fileNameHash:X16}";
                        // Folder Name -> Kick_Error
                        var folderNameHash = this.ReadULong(); // Hash 82 7A B2 FD 55 85 99 44 = Kick_Error
                        var folderName = FIFAAttribDbFileNameHashDictionary.FileNameHash.ContainsKey(folderNameHash) ? FIFAAttribDbFileNameHashDictionary.FileNameHash[folderNameHash] : $"Unknown_{folderNameHash}";

                        fieldHashToDbFieldByType.Add(folderNameHash + fileNameHash, new Dictionary<ulong, FIFAAttribDbField>());


                        var hashInOtherFileQuestionMark = this.ReadLong(); // hash in other file????
                        var fieldCount = this.ReadInt();
                        var alwaysZero1 = this.ReadInt();
                        var fieldCount2 = this.ReadInt();
                        var unkCount = this.ReadUShort();
                        var hashCount = this.ReadUShort();

                        List<byte[]> hashBytes = new List<byte[]>();
                        for (var iHash = 0; iHash < hashCount; iHash++)
                        {
                            hashBytes.Add(this.ReadBytes(8));
                        }

                        List<ulong> hashes = new List<ulong>();
                        for (var iHash = 0; iHash < hashCount; iHash++)
                        {
                            var uLongHash = BitConverter.ToUInt64(hashBytes[iHash]);
#if DEBUG
                            if (uLongHash == 10368380999018116831)
                            {

                            }
#endif
                            hashes.Add(uLongHash);
                        }

#if DEBUG
                        if (hashes.Count != 0)
                        {

                        }


#endif

                        var unk6 = this.ReadLong();
                        List<FIFAAttribDbField> fields = new();

                        for (var iField = 0; iField < fieldCount; iField++)
                        {
                            var fieldHashHex = this.ReadBytes(8);
                            var fieldHash = BitConverter.ToUInt64(fieldHashHex);
#if DEBUG
                            if (fieldHash == 17011006820318417859)
                            {

                            }
                            if (fieldHash == 10368380999018116831)
                            {

                            }
#endif
                            var fieldName = FieldNameHashLoader.Load().TryGetValue(fieldHash, out var name) ? name : $"Unknown_{fieldHash:X16}";
                            var vaultValueOffset = this.Position;
                            vaultValueOffsetToFieldHash.Add(vaultValueOffset, fieldHash);
                            var fieldValue = this.ReadBytes(8);
                            var fieldType = this.ReadLong();

                            object fieldValueObj = null;

                            switch ((FifaAttribDbFieldType)fieldType)
                            {
                                case FifaAttribDbFieldType.Float:
                                    fieldValueObj = BitConverter.ToSingle(new ReadOnlySpan<byte>(fieldValue));
                                    break;
                                case FifaAttribDbFieldType.Int32:
                                    fieldValueObj = BitConverter.ToInt32(new ReadOnlySpan<byte>(fieldValue));
                                    break;
                                case FifaAttribDbFieldType.Int64:
                                    fieldValueObj = BitConverter.ToInt64(new ReadOnlySpan<byte>(fieldValue));
                                    break;
                                case FifaAttribDbFieldType.Array:
                                    break;
                                case FifaAttribDbFieldType.FloatCurve:
                                    break;
                            }

                            // Create DbField
                            var dbField = new FIFAAttribDbField(fieldName, fieldValueObj, fieldHash, fieldType, vaultValueOffset);
                            fields.Add(dbField);
                            fieldHashToDbFieldByType[folderNameHash + fileNameHash].Add(fieldHash, dbField);
                        }

                        if (true)
                        {
                        }

                        long sizeOfData = this.Position - startOfData;

                        long positionBeforeReadingData = this.Position;
                        this.Position = startOfData;
                        var dataInVault = this.ReadBytes((int)sizeOfData);

                        this.Position = positionBeforeReadingData;
                        var dbType = new FIFAAttribDbType(fileName, fileNameHash, folderName, folderNameHash, startOfData, dataInVault, fields);
                        dbType.Hashes = hashes;
                        ListOfDbTypes.Add(dbType);
                    }
                    catch
                    {

                    }

                }

                if (true)
                {

                }

                this.Pad(16);
                var endOfTypeDefinitionOffset = this.Position;

                this.Position += 4; // npxe
                this.Position += 12;
                //var npxe = this.ReadULong(); // 23709384994894
                //var countOfTypesAgain = this.ReadUInt();
                this.Pad(16);

                for (var i = 0; i < countOfTypes; i++)
                {
                    var hash1 = this.ReadULong();
                    var hash2 = this.ReadULong();
                    var countOrOffset1 = this.ReadUInt();
                    var countOrOffset2 = this.ReadUInt();
                }

                this.Pad(16);
                this.ReadUInt(); // NrtP

                _ = this.Position;
                //var sizeOfArrayData = this.ReadInt();
                //for (var i =0; i < sizeOfArrayData; i++)
                //{
                //    _ = this.ReadUInt(); // Offset
                //    _ = this.ReadUInt(); // Type
                //    _ = this.ReadULong(); // Offset2
                //}
                //while (this.Position < this.Length - 16)
                //{
                //    _ = this.ReadUInt(); // Offset
                //    _ = this.ReadUInt(); // Type
                //    _ = this.ReadULong(); // Offset2
                //}

                if (true)
                {

                }
            }
            catch
            {

            }

        }
    }
}
