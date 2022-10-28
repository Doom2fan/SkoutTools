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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ChronosLib;

namespace SkoutLib;

public readonly struct ST_PalColor {
    public const int ColCount = 256;
    public const int PalLength = ColCount * 3;
    public const string PalIdStartString = "FFFF04";

    public readonly byte R { get; init; }
    public readonly byte G { get; init; }
    public readonly byte B { get; init; }

    public static void ReadPalette (ReadOnlySpan<byte> bytes, Span<ST_PalColor> colors) {
        Debug.Assert (bytes.Length == PalLength);
        Debug.Assert (colors.Length == ColCount);

        for (int i = 0; i < ColCount; i++) {
            colors [i] = new () {
                R = bytes [i * 3 + 0],
                G = bytes [i * 3 + 1],
                B = bytes [i * 3 + 2],
            };
        }
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool TryGetPaletteNumFromId (uint id, out byte palNum) {
        const uint palIdBase = 0xFFFF0400;
        const uint palIdMask = 0xFFFFFF00;

        if ((id & palIdMask) != palIdBase) {
            palNum = default;
            return false;
        }

        palNum = (byte) (id & ~palIdMask);
        return true;
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int ToIntARGB () => (0xFF << 24) | (R << 16) | (G << 8) | B;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int ToIntARGB (byte alpha) => (alpha << 24) | (R << 16) | (G << 8) | B;
}

public ref struct SkoutTexFile {
    public enum Kind {
        Indexed,
        IndexedAlpha,
        IndexedId0Trans,
        ARGB1555,
        ARGB4444,
        ARGB8888,

        Unknown,
    }

    public interface TexFileDecoder {
        /// <summary>Whether the converter's backing already exists or is created when decoding starts.
        /// If the backing is created on demand, <see cref="Width"/> and <see cref="Height"/> are not necessary.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        public bool OnDemand { get; }

        /// <summary>Gets the image/converter's width.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        public int Width { get; }
        /// <summary>Gets the image/converter's height.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        public int Height { get; }

        /// <summary>Called when the decoding starts.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        public void StartDecode (uint width, uint height, Kind kind);

        /// <summary>Called when the decoding is done.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        public void EndDecode ();

        /// <summary>Sets the specified pixel in the image to the specified color.
        /// This should be marked with MethodImpl.AggressiveInlining.</summary>
        /// <param name="x">The pixel's X coordinates.</param>
        /// <param name="y">The pixel's Y coordinates.</param>
        /// <param name="color">The color to set the pixel to.</param>
        public void SetPixel (int x, int y, int color);
    }

    private const int MipInfoStart = sizeof (ushort) * 5;
    private const int MipInfoLen = sizeof (uint);

    public ushort Width { get; private init; }
    public ushort Height { get; private init; }
    public ushort MipCount { get; private init; }

    public ushort Data1 { get; private init; }
    public ushort Data2 { get; private init; }

    public ReadOnlySpan<byte> Bytes { get; private init; }
    public ReadOnlySpan<ST_PalColor> Palette { get; private init; }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private uint GetMipOffset (int mipLevel) {
        var mipStart = MipInfoStart + MipInfoLen * mipLevel;
        return BitConversion.LittleEndian.ToUInt32 (Bytes [mipStart..]);
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public Kind GetKind () {
        if (Palette.Length > 0) {
            // TODO: Add support for different kinds of paletted image.
            if (Data1 == 0 && Data2 == 0)
                return Kind.Indexed;
            else if (Data1 == 1 && Data2 == 0)
                return Kind.IndexedId0Trans;
            else if (Data1 == 2 && Data2 == 0)
                return Kind.IndexedAlpha;
        } else {
            if (Data2 == 2 && Data1 == 0)
                return Kind.ARGB1555;
            else if (Data2 == 4 && Data1 == 0)
                return Kind.ARGB4444;
            else if (Data2 == 8 && Data1 == 0)
                return Kind.ARGB8888;
        }

        return Kind.Unknown;
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private uint GetRawPixelSize () {
        switch (GetKind ()) {
            case Kind.Indexed: return 1;
            case Kind.IndexedId0Trans: return 1;
            case Kind.IndexedAlpha: return 2;
            case Kind.ARGB1555: return 2;
            case Kind.ARGB4444: return 2;
            case Kind.ARGB8888: return 4;

            case Kind.Unknown: return 0;

            default: throw new NotImplementedException ();
        }
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private uint GetRawMipLength (int mipLevel) {
        var (width, height) = GetMipSize (mipLevel);
        return width * height * GetRawPixelSize ();
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public (uint Width, uint Height) GetMipSize (int mipLevel) {
        if (mipLevel < 0 || mipLevel >= MipCount)
            throw new ArgumentOutOfRangeException (nameof (mipLevel));

        var width = (uint) Width;
        var height = (uint) Height;
        for (int i = 0; i < mipLevel; i++) {
            width /= 2;
            height /= 2;
        }

        return (width, height);
    }

    public void DecodeImageData<TConverter> (ref TConverter converter, int mipLevel)
        where TConverter : struct, TexFileDecoder {
        if (mipLevel < 0 || mipLevel >= MipCount)
            throw new ArgumentOutOfRangeException (nameof (mipLevel));

        var mipRes = GetMipSize (mipLevel);

        if (!converter.OnDemand && (converter.Width != mipRes.Width || converter.Height != mipRes.Height))
            throw new ArgumentException ("The converter is not the correct size.", nameof (converter));

        var kind = GetKind ();
        var mipOffs = GetMipOffset (mipLevel);
        var rawPixelSize = GetRawPixelSize ();
        var mipBytes = Bytes.Slice ((int) mipOffs, (int) (mipRes.Width * mipRes.Height * rawPixelSize));

        converter.StartDecode (mipRes.Width, mipRes.Height, kind);
        try {
            for (int y = 0; y < mipRes.Height; y++) {
                var rawColStart = y * mipRes.Width * rawPixelSize;
                for (int x = 0; x < mipRes.Width; x++) {
                    var rawPixel = mipBytes.Slice ((int) (rawColStart + x * rawPixelSize), (int) rawPixelSize);

                    converter.SetPixel (x, y, kind switch {
                        Kind.Indexed => Palette [rawPixel [0]].ToIntARGB (),
                        Kind.IndexedAlpha => Palette [rawPixel [0]].ToIntARGB (rawPixel [1]),
                        Kind.IndexedId0Trans => Palette [rawPixel [0]].ToIntARGB (rawPixel [0] != 0 ? byte.MaxValue : byte.MinValue),
                        Kind.ARGB1555 => SkoutUtils.DecodeARGB1555 (BitConversion.LittleEndian.ToUInt16 (rawPixel)),
                        Kind.ARGB4444 => SkoutUtils.DecodeARGB4444 (BitConversion.LittleEndian.ToUInt16 (rawPixel)),
                        Kind.ARGB8888 => BitConversion.LittleEndian.ToInt32 (rawPixel),
                        _ => throw new NotImplementedException (),
                    });
                }
            }
        } finally {
            converter.EndDecode ();
        }
    }

    public static SkoutTexFile ReadTex (ReadOnlySpan<byte> bytes, ReadOnlySpan<ST_PalColor> palette) {
        if (palette.Length != 0 && palette.Length != ST_PalColor.ColCount)
            throw new ArgumentException ("Invalid palette");

        const string exceptionMessage = "Malformed texture file";
        if (bytes.Length < MipInfoStart)
            throw new ArgumentException (exceptionMessage);

        var mipCount = BitConversion.LittleEndian.ToUInt16 (bytes [4..6]);
        var mipLength = mipCount * (sizeof (ushort) * 2);

        if (bytes.Length < MipInfoStart + mipLength + sizeof (ushort))
            throw new ArgumentException (exceptionMessage);

        var tex = new SkoutTexFile () {
            Width = BitConversion.LittleEndian.ToUInt16 (bytes [0..2]),
            Height = BitConversion.LittleEndian.ToUInt16 (bytes [2..4]),
            MipCount = mipCount,

            Data1 = BitConversion.LittleEndian.ToUInt16 (bytes [6..8]),
            Data2 = BitConversion.LittleEndian.ToUInt16 (bytes [8..10]),

            Bytes = bytes,
            Palette = palette,
        };

        if (tex.Width < 1 || tex.Height < 1 || tex.MipCount < 1)
            throw new ArgumentException (exceptionMessage);

        if (tex.GetKind () == Kind.Unknown)
            throw new ArgumentException ("Unknown texture format");

        for (var i = 0; i < tex.MipCount; i++) {
            if (tex.GetMipOffset (i) + tex.GetRawMipLength (i) > bytes.Length)
                throw new ArgumentException (exceptionMessage);
        }

        return tex;
    }
}
