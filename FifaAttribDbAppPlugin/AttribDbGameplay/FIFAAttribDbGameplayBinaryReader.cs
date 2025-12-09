using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FifaAttribDbAppPlugin.AttribDbGameplay
{
    /// <summary>
    /// This class reads the AttribDb/AttribDb.BIN files used in FIFA games. It will obtain the String Names of the Types.
    /// 
    /// </summary>
    public class FIFAAttribDbGameplayBinaryReader : NativeReader
    {
        public FIFAAttribDbGameplayBinaryReader(string filePath) : base(filePath)
        {
        }

        public FIFAAttribDbGameplayBinaryReader(
            byte[] attribdbbindata
            ) : base(attribdbbindata)
        {
            this.Position = 0;

            this.ReadBytes(32); // Skip header
          
        }

    }
}
