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
using System.IO;
using CommandLine;
using SkoutLib;

namespace SkoutTools;

internal partial class BitUtils {
    [Verb ("bit-extract", HelpText = "Extracts a bit file")]
    internal class ExtractOptions {
        [Value (0, MetaName = "input file", Required = true, HelpText = "The BIT archive to extract.")]
        public string InputFile { get; set; }

        [Option ('o', Default = null, HelpText = "Sets the directory to extract the archive to.")]
        public string OutputDirectory { get; set; }

        [Option ('r', SetName = "raw", Default = false, HelpText = "Extracts the files inside archive as-is.")]
        public bool Raw { get; set; }

        [Option (SetName = "conv", Default = false, HelpText = "Aborts on unknown file types.")]
        public bool AbortOnUnknownFiles { get; set; }
    }

    internal int ExtractBit (ExtractOptions options) {
        options.InputFile = Path.GetFullPath (options.InputFile);
        if (options.OutputDirectory is not null)
            options.OutputDirectory = Path.GetFullPath (options.OutputDirectory);
        else {
            var outFolderName = Path.GetFileNameWithoutExtension (options.InputFile) + "_Extracted";
            options.OutputDirectory = Path.Combine (Path.GetDirectoryName (options.InputFile), outFolderName);
        }

        if (!File.Exists (options.InputFile)) {
            Console.WriteLine ("The specified input path does not exist.");
            return 1;
        }
        if (Directory.Exists (options.InputFile)) {
            Console.WriteLine ("The specified input path is a directory.");
            return 1;
        }

        if (Directory.Exists (options.OutputDirectory)) {
            Console.WriteLine ("The specified output path already exists.");
            return 1;
        }
        if (File.Exists (options.OutputDirectory)) {
            Console.WriteLine ("The specified output path exists as a file.");
            return 1;
        }

        if (!ReadInputBit (options.InputFile, out var fileBytes))
            return 1;

        var retVal = 0;
        BitFile.ReadFile (fileBytes).Match (
            bitFile => retVal = ExtractBit_DoExtract (options, bitFile),
            magic => {
                Console.WriteLine ($"Invalid \"magic\": \"{magic.ToString ()}\".");
                retVal = 2;
            },
            () => {
                Console.Write ("Malformed file.");
                retVal = 3;
            },
            compFormat => {
                Console.WriteLine ($"Invalid compression format \"{compFormat}\".");
                retVal = 4;
            }
        );

        return retVal;
    }

    private int ExtractBit_DoExtract (ExtractOptions options, BitFile bitFile) {
        Directory.CreateDirectory (options.OutputDirectory);

        if (options.Raw)
            return ExtractBit_RawExtract (options, bitFile);

        return ExtractBit_ConvertAndExtract (options, bitFile);
    }

    private int ExtractBit_RawExtract (ExtractOptions options, BitFile bitFile) {
        var folders = new Dictionary<BitFileIdent, string> (255);

        foreach (var entry in bitFile.Entries) {
            if (!folders.TryGetValue (entry.FileIdent, out var folderPath)) {
                folderPath = Path.Combine (options.OutputDirectory, entry.FileIdent.ToString ());
                Directory.CreateDirectory (folderPath);
                folders [entry.FileIdent] = folderPath;
            }

            var path = Path.Combine (folderPath, $"{entry.Id:X8}.bin");
            using var fs = new FileStream (path, FileMode.Create, FileAccess.Write);
            fs.Write (entry.Bytes);
        }

        return 0;
    }

    private int ExtractBit_ConvertAndExtract (ExtractOptions options, in BitFile bitFile) {
        ExtractLocalData (bitFile);

        var folders = new ModFolders (options.OutputDirectory, true);
        foreach (var entry in bitFile.Entries) {
            if (ExtractBit_ConvertPalette (ref folders, entry))
                continue;
            else if (ExtractBit_ConvertImage (ref folders, entry))
                continue;

            Console.WriteLine ($"Unrecognized file: Id {entry.Id:X8} | {entry.Hash:X8} | {entry.FileIdent}");

            if (!options.AbortOnUnknownFiles)
                continue;

            return 1;
        }

        return 0;
    }

    private bool ExtractBit_ConvertPalette (ref ModFolders folders, in BitEntry entry) {
        if (entry.FileIdent != SkoutIdents.IdentPalette)
            return false;

        if (entry.Bytes.Length != ST_PalColor.PalLength)
            return false;

        if (!ST_PalColor.TryGetPaletteNumFromId (entry.Id, out _))
            return false;

        using var fs = new FileStream (Path.Combine (folders.PalettesFolder, $"{entry.Id:X8}.rawpal"), FileMode.CreateNew, FileAccess.Write);
        fs.Write (entry.Bytes);

        return true;
    }

    private bool ExtractBit_ConvertImage (ref ModFolders folders, in BitEntry entry) {
        if (!SkoutIdents.IsTexture (entry.FileIdent, out var palNum))
            return false;

        var palette = ReadOnlySpan<ST_PalColor>.Empty;
        if (palNum != -1) {
            palette = GetPalette ((byte) palNum);
            if (palette.IsEmpty) {
                Console.WriteLine ($"Texture id {entry.Id:X8} has an unknown palette.");
                return false;
            }
        }

        try {
            var texFile = SkoutTexFile.ReadTex (entry.Bytes, palette);
            using var fs = new FileStream (Path.Combine (folders.GraphicsFolder, $"{entry.Id:X8}.png"), FileMode.CreateNew, FileAccess.Write);

            var converter = new SkoutUtils.SkiaDecoder ();
            try {
                texFile.DecodeImageData (ref converter, 0);
                converter.Image.Encode (fs, SkiaSharp.SKEncodedImageFormat.Png, 3);
            } finally {
                converter.Dispose ();
            }
        } catch (ArgumentException e) {
            Console.Error.WriteLine ($"Error decoding image with id {entry.Id:X8}: {e.Message}");
        }

        return true;
    }
}
