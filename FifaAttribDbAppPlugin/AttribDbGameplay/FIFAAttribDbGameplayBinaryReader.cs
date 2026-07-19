using FMT.FileTools;

namespace FifaAttribDbAppPlugin.AttribDbGameplay
{
    /// <summary>
    /// Reads the AttribDbGameplay.BIN file. Stores the raw binary data
    /// so callers can read/write FloatCurve, Array, and other non-scalar field data.
    /// </summary>
    public class FIFAAttribDbGameplayBinaryReader : NativeReader
    {
        public byte[] RawData { get; }

        public const int HeaderSize = 32;

        public FIFAAttribDbGameplayBinaryReader(string filePath) : base(filePath)
        {
            RawData = ReadBytes((int)BaseStream.Length);
        }

        public FIFAAttribDbGameplayBinaryReader(byte[] data) : base(data)
        {
            Position = 0;
            ReadBytes(HeaderSize);
            RawData = data;
        }

        public float[] ReadFloatCurve(long offset, int floatCount)
        {
            if (offset < 0 || offset + floatCount * 4 > RawData.Length)
                return Array.Empty<float>();

            var values = new float[floatCount];
            for (int i = 0; i < floatCount; i++)
                values[i] = BitConverter.ToSingle(RawData, (int)offset + i * 4);
            return values;
        }

        public float[] ReadArray(long offset)
        {
            if (offset < 0 || offset + 8 > RawData.Length)
                return Array.Empty<float>();

            var count = BitConverter.ToUInt16(RawData, (int)offset);
            if (count == 0 || offset + 8 + count * 4 > RawData.Length)
                return Array.Empty<float>();

            var values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = BitConverter.ToSingle(RawData, (int)offset + 8 + i * 4);
            return values;
        }

        public void WriteFloatCurve(long offset, float[] values)
        {
            if (offset < 0 || values == null || offset + values.Length * 4 > RawData.Length)
                return;

            for (int i = 0; i < values.Length; i++)
                BitConverter.GetBytes(values[i]).CopyTo(RawData, (int)offset + i * 4);
        }

        public void WriteArray(long offset, float[] values)
        {
            if (offset < 0 || values == null || offset + 8 + values.Length * 4 > RawData.Length)
                return;

            BitConverter.GetBytes((ushort)values.Length).CopyTo(RawData, (int)offset);
            BitConverter.GetBytes((ushort)values.Length).CopyTo(RawData, (int)offset + 2);
            BitConverter.GetBytes((uint)4).CopyTo(RawData, (int)offset + 4);
            for (int i = 0; i < values.Length; i++)
                BitConverter.GetBytes(values[i]).CopyTo(RawData, (int)offset + 8 + i * 4);
        }

        public byte[] GetRawData() => RawData;
    }
}
