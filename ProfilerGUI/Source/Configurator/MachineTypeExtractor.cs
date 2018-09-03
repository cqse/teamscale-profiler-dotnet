using System;
using System.IO;

namespace ProfilerGUI.Source.Configurator
{
    // TODO (FS) make internal?
    class MachineTypeExtractor
    {
        private const int ExpectedPEHead = 0x00004550;

        /// <summary>
        /// Taken from https://stackoverflow.com/questions/1001404/check-if-unmanaged-dll-is-32-bit-or-64-bit
        /// </summary>
        // TODO (FS): please add a few words about what this does
        internal static MachineType GetExecutableType(string executablePath)
        {
            // See http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
            // Offset to PE header is always at 0x3C.
            // The PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00,
            // followed by a 2-byte machine type field (see the document above for the enum).
            //
            using (FileStream stream = new FileStream(executablePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0x3c, SeekOrigin.Begin);
                Int32 peOffset = reader.ReadInt32();
                stream.Seek(peOffset, SeekOrigin.Begin);
                UInt32 peHead = reader.ReadUInt32();

                if (peHead != ExpectedPEHead)
                {
                    throw new Exception("Can't find PE header");
                }

                return (MachineType)reader.ReadUInt16();
            }
        }

        public enum MachineType : ushort
        {
            Unknown = 0x0,
            I386 = 0x14c,
            Amd64 = 0x8664,
            IA64 = 0x200
        }
    }
}
