The file-types in .BIT seem to be primarily based on the "file ident". Some types, such as textures, can be further subtyped internally.
Some of these values are guesses, while some we know based on observations.

* `01-00-FF`: Palettes. Ids seem to always start with "FF FF 04", the 4th byte is the palette's index. Crash on start if not present.
* `01-05-FF`: String tables for the level load screens? Doesn't seem to be used for anything else.
    * Loaded by GLtextGet and GLtextGetUnsafe
* `01-06-FF`: Crash on start if not present. Works fine if present but empty?
* `01-0B-FF`: No idea, but the game crashes when starting a new game if these files are gone. Works fine if present but empty?
* `01-12-FF`: Level data? Contains numbers in plain text and what looks like could be actor names. Starts with a very consistent header, too. Removing these from `skout.bit` causes an instant crash on starting a new game. Removing them from `network.bit` makes the game say `Cutscene 0x?? not found [cutscene ok, but level 0x?? not found!]`
* `02-09-FF`: Audio files.
* `04-0C-??`: Graphics. If the 3rd ident byte is FF, it's a 16 or 32 bit image; Otherwise, it's a palette index. The kind of image is further subtyped by "Data1" and "Data2" in the header, which determines what kind of image data it contains (e.g., Indexed, RGBA1555, RGBA8888)
* `05-0D-FF`: ??? Something with a built-in palette. Unsure. Didn't notice any changes when not present.
* `07-0F-FF`: Unknown, very strange. All of them seem to start with 464 bytes set to "CD" with a hole of 4 zeroes at 0x4C? Always starts with `010C`? Delayed crash on start if not present. Cutscenes black out, main menu shows nothing then crashes.
    * Loaded by GLspr3DoesCheckExist, GLspr3CountShots, GLspr3DrawSprBound and more
* `08-10-??`: Font graphics. Mostly the same as normal graphics, has some kind of extra data, and `Data1` is 257. Game crashes if not present, but if they're present but 0 bytes, it just doesn't render text.
* `09-11-FF`: Crash on launch if not present.
    * Loaded by GLspr3CDlineObjectMaterial, CDspr3CDlineObject, GLspr3DrawObj, GLspr3DrawObjScaleUV and more
* `0A-12-FF`: "Single player" option doesn't work if these files are removed from `skout.bit`. Seems to be levels? Removing them from `network.bit` makes the game say `Cutscene 0x?? not found [cutscene ok, but level 0x?? not found!]`
* `0B-13-FF`: Removing these causes error messages saying `Cutscene 0x?? not found`
* `0C-0F-FF`: Model animations? All seem to have 660 zero bytes at the start? Crash on new game start if not present.