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

public class SkoutUtils {
    public static int DecodeARGB1555 (ushort pixel) {
        static int Conv5To8 (int val) => (int) (uint) ((float) val / 0x1F * 0xFF);
        return (
            ((pixel & 0x8000) != 0 ? 0xFF << 24 : 0) |
            Conv5To8 ((pixel & 0x7C00) >> 10) << 16 |
            Conv5To8 ((pixel & 0x03E0) >>  5) <<  8 |
            Conv5To8 ( pixel & 0x001F       )
        );
    }

    public static int DecodeARGB4444 (ushort pixel) {
        static int Conv4To8 (int val) => (int) (uint) ((float) val / 0x0F * 0xFF);
        return (
            Conv4To8 ((pixel & 0xF000) >> 12) << 24 |
            Conv4To8 ((pixel & 0x0F00) >>  8) << 16 |
            Conv4To8 ((pixel & 0x00F0) >>  4) << 8  |
            Conv4To8 ( pixel & 0x000F       )
        );
    }
}
