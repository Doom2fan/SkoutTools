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
using System.IO;
using CommandLine;
using SkoutLib;

namespace SkoutTools;

internal partial class BitUtils {
    [Verb ("bit-list", HelpText = "Lists the data and contents of a bit file")]
    internal class ListOptions {
        [Value (0, MetaName = "input file", Required = true, HelpText = "The BIT archive to list.")]
        public string InputFile { get; set; }

        [Option ('i', HelpText = "Prints detailed info about the files in the archive.")]
        public bool FileInfo { get; set; }
    }

    internal int ListBit (ListOptions options) {
        options.InputFile = Path.GetFullPath (options.InputFile);
        if (!File.Exists (options.InputFile)) {
            Console.WriteLine ($"The specified input path does not exist.");
            return 1;
        }
        if (Directory.Exists (options.InputFile)) {
            Console.WriteLine ($"The specified input path is a directory.");
            return 1;
        }

        if (!ReadInputBit (options.InputFile, out var fileBytes))
            return 1;

        var retVal = 0;
        BitFile.ReadFile (fileBytes).Match (
            bitFile => retVal = ListBit_DoList (options, bitFile),
            magic => {
                Console.WriteLine ($"Invalid \"magic\": \"{magic.ToString ()}\".");
                retVal = 2;
            },
            () => {
                Console.WriteLine ("Malformed file.");
                retVal = 3;
            },
            compFormat => {
                Console.WriteLine ($"Invalid compression format \"{compFormat}\".");
                retVal = 4;
            }
        );

        return retVal;
    }

    private int ListBit_DoList (ListOptions options, BitFile bitFile) {
        Console.WriteLine ($"Revision: {bitFile.Revision}\nEntries count: {bitFile.Entries.Count}");
        foreach (var entry in bitFile.Entries) {
            Console.Write ($"Id {entry.Id:X8} | Hash {entry.Hash:X8} | File type {entry.FileIdent}");

            if (options.FileInfo)
                ListBit_PrintExtraInfo (options, entry);

            Console.WriteLine ();
        }

        return 0;
    }

    private void ListBit_PrintExtraInfo (ListOptions options, BitEntry entry) {
        var ident = entry.FileIdent;

        if (SkoutIdents.IsTexture (ident, out var texPalette)) {
            try {
                var texFile = SkoutTexFile.ReadTex (entry.Bytes, texPalette == -1 ? null : GetPalette ((byte) texPalette));

                Console.Write (" (Texture, ");
                switch (texFile.GetKind ()) {
                    case SkoutTexFile.Kind.Indexed:
                        Console.Write ($"Palette #{texPalette:X2}");
                        break;
                    case SkoutTexFile.Kind.IndexedId0Trans:
                        Console.Write ($"Palette #{texPalette:X2} + Index 0 binary transparency");
                        break;
                    case SkoutTexFile.Kind.IndexedAlpha:
                        Console.Write ($"Palette #{texPalette:X2} + Alpha");
                        break;
                    case SkoutTexFile.Kind.ARGB1555:
                        Console.Write ("16-bit (ABGR1555)");
                        break;
                    case SkoutTexFile.Kind.ARGB4444:
                        Console.Write ("16-bit (ABGR4444)");
                        break;
                    case SkoutTexFile.Kind.ARGB8888:
                        Console.Write ("32-bit (ABGR8888)");
                        break;

                    default:
                        Console.Write ("[UNRECOGNIZED TEXTURE KIND]");
                        break;
                }
                Console.Write (")");
            } catch (ArgumentException e) {
                Console.Write (" [Error: ");
                Console.Write (e.Message);
                Console.Write ("]");
            }
        } else if (ident == SkoutIdents.IdentPalette && ST_PalColor.TryGetPaletteNumFromId (entry.Id, out var palNum))
            Console.Write ($" (Palette #{palNum:X2})");
        else
            Console.Write (" (Unrecognized file type)");
    }
}
