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

namespace SkoutLib;

public static class SkoutIdents {
    public static BitFileIdent IdentPalette => (0x01, 0x00, 0xFF);

    /// <summary>Checks if an ident is a texture.</summary>
    /// <param name="ident">The ident to check.</param>
    /// <param name="palette">The texture's palette. -1 if not paletted.</param>
    /// <returns>A value indicating whether the ident is a texture.</returns>
    public static bool IsTexture (BitFileIdent ident, out int palette) {
        if (ident.A == 0x04 && ident.B == 0x0C) {
            palette = ident.C == 0xFF ? -1 : ident.C;
            return true;
        }

        palette = 0;
        return false;
    }
}
