## PLEASE DO NOT SUBMIT BUG REPORTS WHILE USING THIS MOD AS THERE ARE SIGNIFICANT CHANGES IF YOU ABSOLUTELY MUST SUBMIT A BUG REPORT PLEASE NOTIFY OWLCAT THAT IT IS IN USE

# Disable FastTravel if you're using this, otherwise they will fight over trying to set the speed.

This mod does not aim to introduce any changes to the game balance or mecahnics.

It is purely quality of life features.

The mod should aim to fit into the base game without looking like a mod.

**How to install**

1. Download and install [Unity Mod Manager](https://github.com/newman55/unity-mod-manager), make sure it is at least version 0.23.0
2. Run Unity Mod Manger and set it up to find Wrath of the Righteous
3. Download the Bubble Tweaks mod
4. Install the mod by dragging the zip file from step 5 into the Unity Mod Manager window under the Mods tab

**FEATURES:**

	Implementation of the wonderful FastTravel mod
		also adds support for a speed scale for tactical "homm-style" combat
		settings are adjusted using the native game settings window (ugly unity mod manager go away!)

	Army Disband button
		You can't disband armies that have generals
	
	Jump to Siege button
		Only jumps to the first village under siege, I will try to make it cycle through them

	GlobalMap Info Panel
		In GlobalMap mode, adds an expandable menu on the right hand side (under the mini-map), clicking on any entry pans the map to the location:
			Shows all your villages
			Shows all invading armies
			Shows all locations with quests

	Loot on the Local Map is coloured:
		Red - dropped by a dead unit
		Green - lootable object


	Cursor Enhancements
		Scalable cursor for combat/non-combat
		Full-attack text color can be semi-customised

	Character Statistics (beta)
		Each character will have a new tab 'statistics' in their character sheet
		It can only track stats while it's installed so sadly it won't retroactively find them

***CIRCLES***
Edit the `aoe_indicators.json` file in the mod folder.

Most spells should already have an entry, they look like:

```
    //GreaseArea
    {
      "ability": "d46313be45054b248a1f1656ddb38614",
      "type": "bad"
    }
```

For type you can enter:
 * `good` - uses colors from `__default_good__` at the very bottom of the file
 * `bad` - uses colors from `__default_bad__` at the very bottom of the file
 * `skip` - no circle

You can also supply either (or both) of the normal/hi colors directly as rgb:
```
    {
      //EXAMPLE_DOES_NOT_EXIST: use "hi" and "normal" to set custom RGB for the highlighted/normal circles
      "ability": "EXAMPLE",
      "hi": [ 1, 1, 1 ],
      "normal": [ 0.4, 0.3, 0.2 ]
    },
```

`normal` - normal color of circle
`hi` - color of circle when you path over it in TBM

Acknowledgments:  

-   Pathfinder Wrath of The Righteous Discord channel members
-   @Balkoth, @Narria, @edoipi, @SpaceHamster and the rest of our great Discord modding community - help.
-	@Vek17 extra special thanks because this mod is pretty much a copy paste of his to get it off the ground :pray:
-	@newman55 for the original FastTravel mod
-   PS: Spacehamster's [Modding Wiki](https://github.com/spacehamster/OwlcatModdingWiki/wiki/Beginner-Guide) is an excellent place to start if you want to start modding on your own.
-   Join our [Discord](https://discord.gg/bQVwsP7cky)



ASSETS:

DisbandArmy Icon made by Freepik (https://www.freepik.com) from https://www.flaticon.com/

