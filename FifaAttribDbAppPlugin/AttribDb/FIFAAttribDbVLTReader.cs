using FMT.FileTools;

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
            this.Position = 0;
            this.ReadBytes(16); // Skip header
            this.ReadBytes(16); // Skip header
            this.ReadBytes(16); // Skip header
            this.ReadBytes(16); // Skip header
            this.ReadBytes(8); // Skip header
            this.ReadNullTerminatedString(); // attribdb.vlt
            this.ReadNullTerminatedString(); // attribdb.bin
            this.Pad(16);
            this.ReadInt();
            this.ReadInt();

            for (var i = 0; i < 80; i++)
            {
                var startOfData = this.Position;
                this.Position = startOfData;
                // File Name -> Balance
                var fileNameHash = this.ReadULong(); // Hash D9 76 8E 74 05 8D FD 7F = Balance
                var fileName = FIFAAttribDbFileNameHashDictionary.FileNameHash.TryGetValue(fileNameHash, out var fn) ? fn : $"Unknown_{fileNameHash:X16}";
                // Folder Name -> Kick_Error
                var folderNameHash = this.ReadULong(); // Hash 82 7A B2 FD 55 85 99 44 = Kick_Error
                var folderName = FIFAAttribDbFileNameHashDictionary.FileNameHash[folderNameHash];
                this.ReadLong(); // 0 data
                var unk1 = this.ReadInt();
                var unk2 = this.ReadInt();
                var unk3 = this.ReadInt();
                var unkCount = this.ReadUShort();
                var unkCount2 = this.ReadUShort();
                //var unk5 = this.ReadLong();
                List<ulong> hashes = new List<ulong>();
                for (var iHash = 0; iHash < unkCount2; iHash++)
                {
                    hashes.Add(this.ReadULong());
                }
                //for (var iHash = 0; iHash < unkCount2; iHash++)
                //{
                //    hashes.Add(this.ReadULong());
                //}
                var unk6 = this.ReadLong();
                List<FIFAAttribDbField> fields = new();
                for (var iField = 0; iField < unk1; iField++)
                {
                    var fieldHash = this.ReadULong();
#if DEBUG
                    if (fieldHash == 17011006820318417859)
                    {

                    }
#endif
                    var fieldName = FieldNameHashLoader.Load().TryGetValue(fieldHash, out var name) ? name : $"Unknown_{fieldHash:X16}";
                    var fieldValue = this.ReadBytes(8);
                    var fieldType = this.ReadLong();
                    fields.Add(new FIFAAttribDbField(fieldName, fieldValue, fieldHash, fieldType));
                }

                if (true)
                {
                }

                ListOfDbTypes.Add(new FIFAAttribDbType(fileName, fileNameHash, folderName, folderNameHash, fields));
            }

            if (true)
            {

            }

            // 108112
        }
    }
}
