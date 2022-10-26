/*
 * Copyright (c) 2021- Chronos "phantombeta" Ouroboros
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SkoutLib;

public enum CompressionKind : byte {
    Copy  = 0,
    RLE   = 1,
    LZRLE = 2,
}

[StructLayout (LayoutKind.Sequential, Pack = 1)]
internal ref struct CmpHeader {
    public const int HeaderLength = 4 + 4 + 2;

    public CompressionKind CompressionMode;
    public byte FileType;
    public (byte, byte) Ident;
    public uint Length;
    public ushort UncompressedBytes;
}

[StructLayout (LayoutKind.Sequential, Pack = 1)]
internal struct BitEntryHeader {
    public uint Id;
    public uint Offset;
    public uint Length;
    public uint Hash;
    public byte FileType;
}

public readonly struct BitEntry {
    public readonly uint Id { get; init; }
    public readonly uint Hash { get; init; }
    public readonly byte FileType { get; init; }

    public readonly CompressionKind CompressionMode { get; init; }

    public readonly (byte, byte) Ident { get; init; }
    public readonly ushort UncompressedBytes { get; init; }

    public readonly byte [] Bytes { get; init; }
}

public delegate void InvalidMagicDelegate (ReadOnlySpan<byte> magic);

public unsafe struct BitFile {
    public readonly ref struct ReadResult {
        public enum ValueType {
            Success,
            InvalidMagic,
            MalformedFile,
            UnsupportedCompression,
        }

        private readonly ValueType valueType { get; init; }

        private readonly BitFile bitFile { get; init; }
        private readonly ReadOnlySpan<byte> magicBytes { get; init; }
        private readonly byte compressionFormat { get; init; }

        internal ReadResult (BitFile file) {
            valueType = ValueType.Success;

            bitFile = file;
            magicBytes = null;
            compressionFormat = 0;
        }

        internal ReadResult (ReadOnlySpan<byte> magic) {
            Debug.Assert (magic.Length == 4);

            valueType = ValueType.InvalidMagic;

            magicBytes = magic;
            bitFile = default;
            compressionFormat = 0;
        }

        internal ReadResult (byte compFormat) {
            valueType = ValueType.UnsupportedCompression;

            compressionFormat = compFormat;
            bitFile = default;
            magicBytes = null;
        }

        internal static ReadResult NewMalformedFile () => new () {
            valueType = ValueType.MalformedFile,
        };

        public void Match (
            Action<BitFile> successAction,
            InvalidMagicDelegate invalidMagicAction,
            Action invalidFile,
            Action<byte> unsupportedCompression
        ) {
            switch (valueType) {
                case ValueType.Success:
                    successAction (bitFile);
                    break;

                case ValueType.InvalidMagic:
                    invalidMagicAction (magicBytes);
                    break;

                case ValueType.MalformedFile:
                    invalidFile ();
                    break;

                case ValueType.UnsupportedCompression:
                    unsupportedCompression (compressionFormat);
                    break;

                default:
                    throw new NotImplementedException ();
            }
        }
    }

    public ushort Revision;
    public List<BitEntry> Entries { get; private init; }

    private BitFile (ushort rev, BitEntry [] entries) {
        Revision = rev;
        Entries = new (entries);
    }

    public BitFile (ushort rev) {
        Revision = rev;
        Entries = new ();
    }

    #region ================== Static methods

    #region Reading

    private static bool DecompressCopy (ReadOnlySpan<byte> inBytes, CmpHeader header, out byte [] bytesOut) {
        bytesOut = new byte [header.Length];
        inBytes.Slice (CmpHeader.HeaderLength, (int) header.Length).CopyTo (bytesOut);

        return true;
    }

    private static bool DecompressRLE (ReadOnlySpan<byte> inBytes, CmpHeader header, out byte [] bytesOut) {
        var ret = new byte [header.Length];

        // Skip the header
        inBytes = inBytes [CmpHeader.HeaderLength..];

        inBytes [..header.UncompressedBytes].CopyTo (ret);
        inBytes = inBytes [header.UncompressedBytes..];

        var outBytes = ret.AsSpan (header.UncompressedBytes);
        do {
            int inLength;
            int outLength;

            var op = inBytes [0];
            if (op >= 0x80) { // RLE
                outLength = op - 0x7D;
                inLength = 2;

                var value = inBytes [1];
                outBytes [..outLength].Fill (value);
            } else { // Raw copy
                outLength = op + 1;
                inLength = outLength + 1;

                inBytes [1..(outLength + 1)].CopyTo (outBytes [0..outLength]);
            }

            inBytes = inBytes [inLength..];
            outBytes = outBytes [outLength..];
        } while (outBytes.Length > 0);

        bytesOut = ret;
        return true;
    }

    private static bool DecompressLZRLE (ReadOnlySpan<byte> inBytes, CmpHeader header, out byte [] bytesOut) {
        var ret = new byte [header.Length];

        // Skip the header
        inBytes = inBytes [CmpHeader.HeaderLength..];

        inBytes [..header.UncompressedBytes].CopyTo (ret);
        inBytes = inBytes [header.UncompressedBytes..];

        var retSpan = ret.AsSpan (header.UncompressedBytes);
        var outBytes = retSpan;
        var bytesCount = 0;
        do {
            int inLength;
            int outLength;

            var op = inBytes [0];
            if ((op & 0x80) != 0) {
                if ((op & 0x40) != 0) { // RLE mode
                    inLength = 2;
                    outLength = op - 0xBD;

                    var value = inBytes [1];
                    outBytes [..outLength].Fill (value);
                } else {
                    outLength = op - 0x7C;
                    inLength = 3;

                    // Distance to copy from (out - distance)
                    var distance = BitConverter.ToUInt16 (inBytes [1..]);

                    retSpan.Slice (bytesCount - distance, outLength).CopyTo (outBytes [..outLength]);
                }
            } else {
                outLength = op + 1;
                inLength = outLength + 1;

                inBytes.Slice (1, outLength).CopyTo (outBytes [..outLength]);
            }

            inBytes = inBytes [inLength..];
            outBytes = outBytes [outLength..];
            bytesCount += outLength;
        } while (outBytes.Length > 0);

        bytesOut = ret;
        return true;
    }

    private static BitEntryHeader ReadEntryHeader (ReadOnlySpan<byte> inBytes) {
        return new BitEntryHeader {
            Id = BitConverter.ToUInt32 (inBytes),
            Offset = BitConverter.ToUInt32 (inBytes [4..8]),
            Length = BitConverter.ToUInt32 (inBytes [8..12]),
            Hash = BitConverter.ToUInt32 (inBytes [12..16]),
            FileType = inBytes [16],
        };
    }

    private static CmpHeader ReadCmpHeader (ReadOnlySpan<byte> bytes) {
        return new CmpHeader {
            CompressionMode = (CompressionKind) bytes [0],
            FileType = bytes [1],
            Ident = (bytes [2], bytes [3]),
            Length = BitConverter.ToUInt32 (bytes [4..8]),
            UncompressedBytes = BitConverter.ToUInt16 (bytes [8..10]),
        };
    }

    public static ReadResult ReadFile (ReadOnlySpan<byte> inBytes) {
        var magic = inBytes [..4];
        if (magic [0] != 'B' || magic [1] != 'I' || magic [2] != 'T' || magic [3] != 'P')
            return new (magic);

        var headerRevision = BitConverter.ToUInt16 (inBytes [4..]);
        var headerEntryCount = BitConverter.ToUInt32 (inBytes [6..]);

        var entries = new BitEntry [headerEntryCount];

        var directoryBytes = inBytes.Slice (10, (int) (sizeof (BitEntryHeader) * headerEntryCount));
        for (int i = 0; i < headerEntryCount; i++) {
            var entryHeader = ReadEntryHeader (directoryBytes);
            directoryBytes = directoryBytes [sizeof (BitEntryHeader)..];

            if (entryHeader.Offset >= inBytes.Length)
                return ReadResult.NewMalformedFile ();

            var entryInput = inBytes [(int) entryHeader.Offset..];
            var cmpHeader = ReadCmpHeader (entryInput);

            if (cmpHeader.UncompressedBytes > cmpHeader.Length)
                return ReadResult.NewMalformedFile ();

            if (entryHeader.FileType != cmpHeader.FileType)
                return ReadResult.NewMalformedFile ();

            byte [] entryBytes;
            bool bytesValid;

            var compressionFormat = entryInput [0];
            switch (compressionFormat) {
                case 0: bytesValid = DecompressCopy (entryInput, cmpHeader, out entryBytes); break;
                case 1: bytesValid = DecompressRLE (entryInput, cmpHeader, out entryBytes); break;
                case 2: bytesValid = DecompressLZRLE (entryInput, cmpHeader, out entryBytes); break;

                default: return new (compressionFormat);
            }

            if (!bytesValid)
                return ReadResult.NewMalformedFile ();

            entries [i] = new () {
                Id = entryHeader.Id,
                Hash = entryHeader.Hash,
                FileType = entryHeader.FileType,

                CompressionMode = cmpHeader.CompressionMode,

                Ident = cmpHeader.Ident,
                UncompressedBytes = cmpHeader.UncompressedBytes,

                Bytes = entryBytes,
            };
        }

        return new (new BitFile (headerRevision, entries));
    }

    #endregion

    #region Writing

    private static (bool, int) FindRun (ReadOnlySpan<byte> bytesToCompress, int minRunLen) {
        Debug.Assert (bytesToCompress.Length > 0);

        var repeatStart = 0;
        var repeatLen = 1;
        var prevByte = bytesToCompress [0];

        var i = 1;
        for (; i < bytesToCompress.Length; i++) {
            var b = bytesToCompress [i];

            if (repeatStart >= 0 && b == prevByte) {
                repeatLen++;

                if (repeatStart != 0 && repeatLen >= minRunLen) {
                    i = repeatStart;
                    break;
                }
            } else if (b == prevByte) {
                repeatStart = i - 1;
                repeatLen++;
            } else {
                if (repeatStart == 0 && repeatLen >= minRunLen)
                    break;

                repeatStart = -1;
                repeatLen = 1;
            }

            prevByte = b;
        }

        if (repeatStart == 0 && repeatLen >= minRunLen)
            return (true, repeatLen);

        return (false, i);
    }

    private static void CompressRLE ([NotNull] BinaryWriter writer, ReadOnlySpan<byte> bytesToCompress) {
        const int CopyMaxLen = 128;
        const int RleMinLen = 3;
        const int RleMaxLen = 130;
        int maxLen = Math.Max (CopyMaxLen, RleMaxLen);

        while (bytesToCompress.Length > 0) {
            var (isRepeat, len) = FindRun (bytesToCompress [..Math.Min (bytesToCompress.Length, maxLen)], RleMinLen);

            if (isRepeat) {
                Debug.Assert (len >= RleMinLen);

                len = Math.Min (len, RleMaxLen);

                writer.Write ((byte) (0x7D + len));
                writer.Write (bytesToCompress [0]);
            } else {
                len = Math.Min (len, CopyMaxLen);
                writer.Write ((byte) (len - 1));
                writer.Write (bytesToCompress [..len]);
            }

            bytesToCompress = bytesToCompress [len..];
        }
    }

    private static void CompressLZRLE ([NotNull] BinaryWriter writer, ReadOnlySpan<byte> bytesToCompress) {
        // TODO: Implement the LZ part of LZRLE compression.
        const int CopyMaxLen = 128;
        const int LzMinLen = 4;
        const int LzMaxLen = 67;
        const int RleMinLen = 3;
        const int RleMaxLen = 66;
        var maxLen = Math.Max (Math.Max (CopyMaxLen, LzMaxLen), RleMaxLen);

        while (bytesToCompress.Length > 0) {
            var (isRepeat, len) = FindRun (bytesToCompress [..Math.Min (bytesToCompress.Length, maxLen)], RleMinLen);

            if (isRepeat) {
                Debug.Assert (len >= RleMinLen);

                len = Math.Min (len, RleMaxLen);

                writer.Write ((byte) (0xBD + len));
                writer.Write (bytesToCompress [0]);
            } else {
                len = Math.Min (len, CopyMaxLen);
                writer.Write ((byte) (len - 1));
                writer.Write (bytesToCompress [..len]);
            }

            bytesToCompress = bytesToCompress [len..];
        }
    }

    public static void WriteFile ([NotNull] Stream outStream, BitFile bitFile, bool leaveOpen = false) {
        if (!outStream.CanSeek)
            throw new ArgumentException ("The stream is not seekable.", nameof (outStream));
        if (!outStream.CanWrite)
            throw new ArgumentException ("The stream is not writable.", nameof (outStream));

        using var writer = new BinaryWriter (outStream, Encoding.ASCII, leaveOpen);
        writer.Write ("BITP".AsSpan ());
        writer.Write (bitFile.Revision);
        writer.Write ((uint) bitFile.Entries.Count);

        var directoryStart = writer.BaseStream.Position;
        writer.Seek (bitFile.Entries.Count * sizeof (BitEntryHeader), SeekOrigin.Current);

        var entryHeaders = new BitEntryHeader [bitFile.Entries.Count];

        for (int i = 0; i < bitFile.Entries.Count; i++) {
            var entry = bitFile.Entries [i];
            ref var header = ref entryHeaders [i];

            header.Id = entry.Id;
            header.Offset = (uint) writer.BaseStream.Position;
            header.Hash = entry.Hash;
            header.FileType = entry.FileType;

            writer.Write ((byte) entry.CompressionMode);
            writer.Write (entry.FileType);
            writer.Write (entry.Ident.Item1);
            writer.Write (entry.Ident.Item2);
            writer.Write ((uint) entry.Bytes.Length);
            writer.Write (entry.UncompressedBytes);

            writer.Write (entry.Bytes.AsSpan (0, entry.UncompressedBytes));

            var bytesToCompress = entry.Bytes.AsSpan (entry.UncompressedBytes);
            switch (entry.CompressionMode) {
                case CompressionKind.Copy:
                    writer.Write (bytesToCompress);
                    break;

                case CompressionKind.RLE:
                    CompressRLE (writer, bytesToCompress);
                    break;

                case CompressionKind.LZRLE:
                    CompressLZRLE (writer, bytesToCompress);
                    break;

                default:
                    throw new NotImplementedException ();
            }

            header.Length = (uint) (writer.BaseStream.Position - header.Offset);
        }

        writer.Seek ((int) directoryStart, SeekOrigin.Begin);
        foreach (var header in entryHeaders) {
            writer.Write (header.Id);
            writer.Write (header.Offset);
            writer.Write (header.Length);
            writer.Write (header.Hash);
            writer.Write (header.FileType);
        }

        writer.Flush ();
        writer.Close ();
    }

    #endregion

    #endregion
}
