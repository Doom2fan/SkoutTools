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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using SkoutLib;

namespace SkoutTools;

internal static class ST_ProgramData {
    public static readonly string ProgramDir = Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);
    public static readonly string PalettesDir = Path.Combine (ProgramDir, "palettes/");

    private static Dictionary<byte, ST_PalColor []> globalPalettes;
    public static Dictionary<byte, ST_PalColor []> GlobalPalettes {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get {
            if (globalPalettes is not null)
                return globalPalettes;

            globalPalettes = new ();

            var dirInfo = new DirectoryInfo (PalettesDir);
            if (!dirInfo.Exists)
                return globalPalettes;

            var enumInfo = new EnumerationOptions () {
                AttributesToSkip = FileAttributes.Directory | FileAttributes.Device | FileAttributes.System,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Win32,
                RecurseSubdirectories = false,
                ReturnSpecialDirectories = false,
            };

            Span<byte> bytes = stackalloc byte [ST_PalColor.PalLength];
            var nameStartString = ST_PalColor.PalIdStartString;
            foreach (var file in dirInfo.EnumerateFiles (nameStartString + "*.*", SearchOption.TopDirectoryOnly)) {
                if (file.Length != ST_PalColor.PalLength)
                    continue;

                var name = Path.GetFileNameWithoutExtension (file.Name.AsSpan ());
                ReadOnlySpan<char> idSpan;
                if (name.Length == 2)
                    idSpan = name;
                else if (name.Length == nameStartString.Length + 2)
                    idSpan = name.Slice (6, 2);
                else
                    continue;

                if (!byte.TryParse (idSpan, NumberStyles.AllowHexSpecifier, null, out var id))
                    continue;

                using var fs = file.OpenRead ();
                fs.Read (bytes);

                var colArr = new ST_PalColor [ST_PalColor.ColCount];
                ST_PalColor.ReadPalette (bytes, colArr);

                globalPalettes [id] = colArr;
            }

            return globalPalettes;
        }
    }
}

[AttributeUsage (AttributeTargets.Struct)]
internal class NonCopyableAttribute : Attribute { }
