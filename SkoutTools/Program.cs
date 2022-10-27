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
using System.Runtime.InteropServices;
using CommandLine;
using SkoutLib;

namespace SkoutTools;

class Program {
    [Verb ("bit-extract", HelpText = "Extracts a bit file")]
    internal class ExtractOptions {
        [Option (Required = true)]
        public string InputFile { get; set; }

        [Option (Default = null)]
        public string OutputDirectory { get; set; }
    }

    [Verb ("bit-list", HelpText = "Lists the contents of a bit file")]
    internal class ListOptions {
        [Option (Required = true)]
        public string InputFile { get; set; }
    }

    static int Main (string [] args) {
        return Parser.Default.ParseArguments<ExtractOptions, ListOptions> (args).MapResult (
            (ExtractOptions options) => ExtractBit (options),
            (ListOptions options) => ListBit (options),
            errors => 1
        );
    }

    static bool ReadInputBit (string inputFile, out byte [] fileBytes) {
        fileBytes = null;
        try {
            fileBytes = File.ReadAllBytes (inputFile);
            return true;
        } catch (PathTooLongException) {
            Console.WriteLine ("The specified input path is too long.");
            return false;
        } catch (DirectoryNotFoundException) {
            Console.WriteLine ("The specified input path is invalid.");
            return false;
        } catch (FileNotFoundException) {
            Console.WriteLine ("The specified input path was not found.");
            return false;
        } catch (IOException) {
            Console.WriteLine ("An IO error ocurred while opening the .bit file.");
            return false;
        } catch (UnauthorizedAccessException) {
            Console.WriteLine ("The specified input path cannot be accessed.");
            return false;
        } catch (NotSupportedException) {
            Console.WriteLine ("The specified input path is in an invalid format.");
            return false;
        } catch (System.Security.SecurityException) {
            Console.WriteLine ("You do not have the required permissions to access the input file.");
            return false;
        }
    }

    static int ListBit (ListOptions options) {
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

    static int ListBit_DoList (ListOptions options, BitFile bitFile) {
        Console.WriteLine ($"Revision: {bitFile.Revision}\nEntries count: {bitFile.Entries.Count}");
        foreach (var entry in bitFile.Entries)
            Console.WriteLine ($"Id {entry.Id:X8} | Hash {entry.Hash:X8} | File type {entry.FileType:X2}");

        return 0;
    }

    static int ExtractBit (ExtractOptions options) {
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

    static int ExtractBit_DoExtract (ExtractOptions options, BitFile bitFile) {
        Directory.CreateDirectory (options.OutputDirectory);

        foreach (var entry in bitFile.Entries) {
            var path = Path.Combine (options.OutputDirectory, $"{entry.Id:X8}.bin");
            using var fs = new FileStream (path, FileMode.Create, FileAccess.Write);
            fs.Write (entry.Bytes);
        }

        return 0;
    }
}
