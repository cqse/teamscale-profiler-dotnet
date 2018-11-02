using System;
using System.IO;
using System.Reflection;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Utilities to determine the machine type of a PE executable.
    /// </summary>
    internal class MachineTypeUtils
    {
        private const int ExpectedPEHead = 0x00004550;

        /// <summary>
        /// This is needed to select the correct profiler DLL to run the application against.
        /// </summary>
        public static Bitness DetermineBitness(string executablePath)
        {
            Bitness bitness = GetManagedAssemblyBitness(executablePath);
            if (bitness == Bitness.Unknown)
            {
                return GetUnmanagedAssemblyBitness(executablePath);
            }

            return bitness;
        }

        private static Bitness GetManagedAssemblyBitness(string executablePath)
        {
            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(executablePath);
                switch (assemblyName.ProcessorArchitecture)
                {
                    case ProcessorArchitecture.Amd64:

                    case ProcessorArchitecture.IA64:
                        return Bitness.Bitness64;

                    case ProcessorArchitecture.X86:
                        return Bitness.Bitness32;

                    case ProcessorArchitecture.MSIL:
                        // this is the Any CPU target in VS, i.e. it depends on the machine
                        return DetermineMachineBitness();

                    default:
                        return Bitness.Unknown;
                }
            }
            catch
            {
                // IO errors must be handled gracefully as the user might input an invalid path
                // and we don't want to crash in that case
                return Bitness.Unknown;
            }
        }

        private static Bitness DetermineMachineBitness()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                return Bitness.Bitness64;
            }
            else
            {
                return Bitness.Bitness32;
            }
        }

        /// <summary>
        /// Tries to find the "bitness" (32bit vs 64bit) of the given executable by looking at the PE header.
        /// This handles unmanaged DLLs only.
        ///
        /// Taken from https://stackoverflow.com/questions/1001404/check-if-unmanaged-dll-is-32-bit-or-64-bit
        /// </summary>
        private static Bitness GetUnmanagedAssemblyBitness(string executablePath)
        {
            try
            {
                // See http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
                // Offset to PE header is always at 0x3C.
                // The PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00,
                // followed by a 2-byte machine type field (see the document above for the enum).
                //
                using (FileStream stream = new FileStream(executablePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Seek(0x3c, SeekOrigin.Begin);
                        Int32 peOffset = reader.ReadInt32();
                        stream.Seek(peOffset, SeekOrigin.Begin);
                        UInt32 peHead = reader.ReadUInt32();

                        if (peHead != ExpectedPEHead)
                        {
                            // the PE header wasn't found
                            return Bitness.Unknown;
                        }

                        switch ((PeHeaderMachineType)reader.ReadUInt16())
                        {
                            case PeHeaderMachineType.I386:
                                return Bitness.Bitness32;

                            case PeHeaderMachineType.Amd64:
                            case PeHeaderMachineType.IA64:
                                return Bitness.Bitness64;

                            default:
                                return Bitness.Unknown;
                        }
                    }
                }
            }
            catch
            {
                // IO errors must be handled gracefully as the user might input an invalid path
                // and we don't want to crash in that case
                return Bitness.Unknown;
            }
        }

        /// <summary>
        /// Lists all possible machine types in the PE header.
        ///
        /// The enum's value corresponds to the field in the PE header for that machine type.
        /// </summary>
        public enum PeHeaderMachineType : ushort
        {
            I386 = 0x14c,
            Amd64 = 0x8664,
            IA64 = 0x200
        }

        /// <summary>
        /// Lists all possible bitness values.
        /// </summary>
        public enum Bitness
        {
            /// <summary>
            /// Bitness could not be determined.
            /// </summary>
            Unknown,

            Bitness32,
            Bitness64
        }
    }
}