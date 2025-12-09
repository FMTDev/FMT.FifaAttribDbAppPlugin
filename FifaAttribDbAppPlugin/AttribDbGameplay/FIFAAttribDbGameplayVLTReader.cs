using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FifaAttribDbAppPlugin.AttribDbGameplay
{
    public class FIFAAttribDbGameplayVLTReader : NativeReader
    {
        public FIFAAttribDbGameplayVLTReader(string filePath) : base(filePath)
        {
        }

        public FIFAAttribDbGameplayVLTReader(byte[] vltData) : base(vltData)
        {
            this.Position = 0;
            this.ReadBytes(32); // Skip header
            this.ReadLong();
            this.ReadLong(); // Version ??
            // Position 48
            this.ReadGuid();
            this.ReadInt();
            this.ReadInt();
            this.ReadNullTerminatedString(); //attribdb.vlt
            this.ReadNullTerminatedString(); //attribdb.bin
            this.Pad(16);
            this.ReadBytes(32);
        }
    }
}
