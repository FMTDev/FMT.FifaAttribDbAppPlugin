using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FifaAttribDbAppPlugin.AttribDb
{
    /// <summary>
    /// This class reads the AttribDb/AttribDb.BIN files used in FIFA games. It will obtain the String Names of the Types.
    /// 
    /// </summary>
    public class FIFAAttribDbBinaryReader : NativeReader
    {
        #region Properties

        public List<FIFAAttribDbType> Types { get; } = new ();

        #endregion

        #region Constructors

        public FIFAAttribDbBinaryReader(string filePath) : base(filePath)
        {
        }

        #endregion

        public FIFAAttribDbBinaryReader(
            byte[] attribdbbindata
            , int countOfTypes = 70
            , int countOfUnk1 = 121
            ) : base(attribdbbindata)
        {
            this.Position = 0;
            Types.Clear();

            this.ReadBytes(32); // Skip header
            for (var i = 0; i < countOfTypes; i++)
            {
                Types.Add(new FIFAAttribDbType(this.ReadNullTerminatedString()));
            }
            this.Pad(16);
            for (var i = 0; i < countOfUnk1; i++)
            {
                this.ReadGuid();
                this.ReadInt();
                this.ReadInt();
            }
            this.Pad(16);

            _ = Types;
        }

    }
}
