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
using SkiaSharp;
using SkoutLib;

namespace SkoutTools;

internal class SkoutUtils {
    public unsafe struct SkiaDecoder : SkoutTexFile.TexFileDecoder, IDisposable {
        private int* pixels;

        public SKBitmap Image { get; private set; }

        public bool OnDemand => true;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public void StartDecode (uint width, uint height, SkoutTexFile.Kind kind) {
            Width = (int) width;
            Height = (int) height;
            Image = new SKBitmap (Width, Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            pixels = (int*) Image.GetPixels ();
        }

        public void EndDecode () => pixels = null;

        public void SetPixel (int x, int y, int color) {
            if (pixels == null)
                return;

            pixels [y * Width + x] = color;
        }

        public void Dispose () {
            Image?.Dispose ();

            pixels = null;
            Image = null;
        }
    }
}
