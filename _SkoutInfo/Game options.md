Skout accepts the following options in the options.txt file. Debug-only options require debug.dll to work. (Can be acquired from the beta build)
* Controls:
    * mouse_sensitivity
        * Mouse sensitivity. Not clamped.
    * mouse_inverse
    * switchable_run
        * 0: Hold to run
        * 1: Toggle run
* Debug/demo options
    * show_menu
        * When 0, skips the menu and loads directly into the level specified by `load_level`. Crashes if `load_level` is not present.
    * load_level
        * When `show_menu` is 1, determines which level to load into when the game is started.
        * If present while `show_menu` is 0, gets automatically set to the last level played.
    * profiler
        * Enables profiler. Measurements listed at the bottom of the log file.
        * Related to stat GLOPTIONS flag?
    * debugger
        * Enables debug features.
        * TODO: What debug features?
    * committed_mem
        * Shows the amount of committed memory on screen.
        * Requires `debugger` to be enabled in beta?
    * cheat_keys
        * ?
    * testVersion (Beta?)
        * Hides the info screen on start.
    * non_index_version
        * Disables blood.
        * 0: Blood enabled
        * 1: Blood disabled
* Sound:
    * menu_cd_track
        * ?
    * no_cd_music (Retail)/dont_like_cd (Beta)
        * Disables CD audio.
Gameplay:
    * skill
        * Difficulty level
        * 0: Normal
        * 1: Hard
        * 2: Easy
* Multiplayer:
    * net_handycap
    * net_person
        * Selected skin.
        * 0: Skout
        * 1: Skout Girl
        * 2: Hunter
    * net_teamplay
    * net_team
    * net_arena (Retail)
        * Multiplayer server map
    * net_connection
        * Connection type?
    * net_clientname
        * Player name in multiplayer
    * net_servername
        * Server name in multiplayer
    * color1.r
        * ?
    * color1.g
        * ?
    * color1.b
        * ?
    * color2.r
        * ?
    * color2.g
        * ?
    * color2.b
        * ?
    * color3.r
        * ?
    * color3.g
        * ?
    * color3.b
        * ?
    * cSlider1
        * ?
    * cSlider2
        * ?
    * cSlider3
        * ?
* Graphics options:
    * gfx_texture_size
        * Texture resolution.
        * 0: Low
        * 1: Medium
        * 2: High
        * 3: X-High (Retail only; Doesn't work, forced back down to Low)
    * gfx_color_depth
        * Color depth
        * 0: 8-bit
        * 1: 16-bit
        * 2: 32-bit
    * gfx_shadowmap
        * Shadowmaps? Lightmaps? Lighting looks ugly and broken with this disabled.
        * 0: Disabled.
        * 1: Enabled.
* Gamma:
    * nBrightness
        * Screen brightness.
    * nGamma
        * Gamma correction.
    * nFilter
        * Texture filtering. 0 disables filtering.
        * Doesn't get set properly, only works after setting from the menu and gets reenabled when alttabbing (though the setting stays at the same value)
* Misc:
    * language (Retail)
        * Game language.
    * slanguage (Retail)
        * ?
* Quickstart unlocks:
    * ep_solved (Retail)
        * Bitfield. Which episodes are unlocked for quickstarting in the singleplayer menu.
        * 1 << 0: Episode 1
        * 1 << 1: Episode 2
        * 1 << 2: Episode 3
        * 1 << 3: Episode 4
    * lev_solved (Retail)
        * Bitfield. Which levels are unlocked for quickstarting in the singleplayer menu.
        * Episode 1:
            * 1 << 0: E1L1
            * 1 << 1: E1L2
            * 1 << 2: E1L3
            * 1 << 3: E1L4
        * Episode 2:
            * 1 << 4: E2L1
            * 1 << 5: E2L2
            * 1 << 6: E2L3
            * 1 << 7: E2L4
        * Episode 3:
            * 1 <<  8: E3L1
            * 1 <<  9: E3L2
            * 1 << 10: E3L3
            * 1 << 11: E3L4
        * Episode 4:
            * 1 << 12: E4L1
            * 1 << 13: E4L2
            * 1 << 14: E4L3
            * 1 << 15: E4L4
            * 1 << 16: E4L5
* Unknown:
    * not_firstrun (Retail)
        * ?
    * switch_pan
        * ?
    * weaponOn (Beta)
        * ?
    * xstatus
        * ?
    * hausversion (Retail)
        * ?