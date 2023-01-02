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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine;
using SkoutLib;

namespace SkoutTools;

internal partial class BitUtils {
    [Verb ("bit-repack-raw", HelpText = "Lists the data and contents of a bit file")]
    internal class RepackRawOptions {
        [Value (0, MetaName = "input folder", Required = true, HelpText = "The input folder to repack.")]
        public string InputDir { get; set; }

        [Value (1, MetaName = "output file", Required = false, HelpText = "The output .BIT file.")]
        public string OutputFile { get; set; }

        [Option (Default = (ushort) 258, HelpText = "The revision number to use for the .BIT file.")]
        public ushort Revision { get; set; }

        [Option (Default = CompressionKind.RLE, HelpText = "The compression method to use when repacking the archive.")]
        public CompressionKind CompressionMethod { get; set; }
    }

    internal int RepackRawBit (RepackRawOptions options) {
        options.InputDir = Path.GetFullPath (options.InputDir);
        if (options.OutputFile is not null)
            options.OutputFile = Path.GetFullPath (options.OutputFile);
        else {
            var outFolderName = Path.ChangeExtension (options.InputDir, ".bit");
            options.OutputFile = Path.Combine (Path.GetDirectoryName (options.InputDir), outFolderName);
        }

        if (!Directory.Exists (options.InputDir)) {
            Console.WriteLine ("The specified input path does not exist.");
            return 1;
        }
        if (File.Exists (options.InputDir)) {
            Console.WriteLine ("The specified input path is a file.");
            return 1;
        }
        if (File.Exists (options.OutputFile) || Directory.Exists (options.OutputFile)) {
            Console.WriteLine ("The specified output path already exists");
            return 1;
        }

        var outFile = new BitFile (options.Revision);
        foreach (var dirPath in Directory.EnumerateDirectories (options.InputDir, "*-*-*", SearchOption.TopDirectoryOnly)) {
            var dirName = Path.GetFileNameWithoutExtension (dirPath);
            if (!Regex.IsMatch (dirName, "^[0-9A-F]{2}-[0-9A-F]{2}-[0-9A-F]{2}$", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase))
                continue;

            var identStr = dirName.Split ('-');
            Debug.Assert (identStr.Length == 3);

            var ident = new BitFileIdent (
                byte.Parse (identStr [0], NumberStyles.AllowHexSpecifier),
                byte.Parse (identStr [1], NumberStyles.AllowHexSpecifier),
                byte.Parse (identStr [2], NumberStyles.AllowHexSpecifier)
            );

            foreach (var filePath in Directory.EnumerateFiles (dirPath, "*.bin", SearchOption.TopDirectoryOnly)) {
                var fileName = Path.GetFileName (filePath);
                if (!Regex.IsMatch (fileName, "^[0-9A-F]{8}\\.bin$", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase))
                    continue;

                outFile.Entries.Add (new () {
                    Id = uint.Parse (Path.GetFileNameWithoutExtension (fileName), NumberStyles.AllowHexSpecifier),
                    Hash = 0,
                    FileIdent = ident,

                    CompressionMode = options.CompressionMethod,

                    UncompressedBytes = 0,
                    Bytes = File.ReadAllBytes (filePath)
                });
            }
        }

        try {
            using var outStream = File.Open (options.OutputFile, FileMode.CreateNew);
            BitFile.WriteFile (outStream, outFile, false);
        } catch (IOException ex) {
            Console.Write ($"IO Exception: {ex.Message}");
            return 2;
        }

        return 0;
    }
}
