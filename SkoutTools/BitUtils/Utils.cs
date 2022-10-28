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
using SkoutLib;

namespace SkoutTools;

internal partial class BitUtils {
    readonly Dictionary<byte, ST_PalColor []> localPalettes = new ();

    private ReadOnlySpan<ST_PalColor> GetPalette (byte id) {
        if (localPalettes.TryGetValue (id, out var pal))
            return pal;
        else if (ST_ProgramData.GlobalPalettes.TryGetValue (id, out pal))
            return pal;

        return null;
    }

    private bool ReadInputBit (string inputFile, out byte [] fileBytes) {
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

    private void ExtractLocalData (in BitFile bitFile) {
        foreach (var entry in bitFile.Entries) {
            if (entry.FileIdent == SkoutIdents.IdentPalette) {
                if (entry.Bytes.Length != ST_PalColor.PalLength)
                    continue;

                if (!ST_PalColor.TryGetPaletteNumFromId (entry.Id, out var palNum))
                    continue;

                var pal = new ST_PalColor [ST_PalColor.ColCount];
                ST_PalColor.ReadPalette (entry.Bytes, pal);
                localPalettes [palNum] = pal;
            }
        }
    }

    [NonCopyable]
    private ref struct ModFolders {
        public string BasePath { get; private init; }
        public bool CreateFolders { get; private init; }

        private string palettesFolder;
        public string PalettesFolder => GetFolder ("palettes", ref palettesFolder);

        private string graphicsFolder;
        public string GraphicsFolder => GetFolder ("textures", ref graphicsFolder);

        public ModFolders (string basePath, bool createFolders) : this () {
            BasePath = basePath;
            CreateFolders = createFolders;
        }

        private string GetFolder (string name, ref string folderPath) {
            if (!string.IsNullOrEmpty (folderPath))
                return folderPath;

            if (CreateFolders && folderPath is not null)
                return null;

            Directory.CreateDirectory (folderPath = Path.Combine (BasePath, name));
            return folderPath;
        }
    }
}
