We don't actually know what the "Ident" value is right now. The first value here is the "File type", which... isn't quite that, but we don't know quite what it is yet. It **is** somehow *related* to the file-type, though. The second two hex values are the "Ident".
Some of these values are guesses, while some we know based on observations.

* `01 00-FF`: Palettes. Ids seem to always start with "FF FF 04", the 4th byte is the palette's index.
* `01 05-FF`: String tables of some sort. Seems like they might be for level loading screens?
* `01 12-FF`: Level data? Contains numbers in plain text and what looks like could be actor names. Starts with a very consistent header, too.
* `04 0C-??`: Graphics. If the 3rd byte is FF, it's a 16 or 32 bit image; Otherwise, it's a palette index. Some files seem to not follow this? Fucking wack.
* `05 0D-FF`: ??? Something with a built-in palette.
* `07 0F-FF`: Unknown, very strange. All of them seem to start with 464 bytes set to "CD" with a hole of 4 zeroes at 0x4C?
* `08 10-??`: Font graphics. Literally the same as normal graphics, just it's a font instead.
* `0C 0F-FF`: Model animations? All seem to have 660 zero bytes at the start?