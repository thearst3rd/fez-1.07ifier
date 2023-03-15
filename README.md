# FEZ 1.07ifier

A [HAT](https://github.com/Krzyhau/HAT) mod for [FEZ](https://fezgame.com) 1.12 which reintroduces some bugs from version 1.07 that have since been patched.

## Features

This mod reintroduces bugs from FEZ 1.07. These bugs make speedrunning faster, and while of course this mod is not legal for speedrunning, it is fun to be able to perform the bugs in the updated/more stable FEZ 1.12!

The bugs currently un-patched are:

* "Warping"
	* When jumping directly before entering a door, Gomez will do an incorrect animation. If the room transition has a "faraway place" effect, then this glitch skips it, making these room transitions much faster than they would be.
* "Long Jump"
	* If Gomez does a turnaround animation shortly before falling off a ledge, he will have much more speed than intended while falling. If you time a jump well, you can use this extra speed to cross gaps that are unintended.

After installing the mod, both of these bugs will be un-patched. Enjoy!

## Installation

* Follow the instructions to install [HAT](https://github.com/Krzyhau/FEZUG#installation-014), the FEZ mod loader
* Download `FEZ107ifier.zip` from the [releases](https://codeberg.org/thearst3rd/fez-1.07ifier/releases/latest) page and copy it into your HAT `Mods` folder
* Run modded FEZ and enjoy!

## Build

* Clone the repository
* Update `UserProperies.xml` to point to your FEZ game directory, MonoMod directory (can be the same as the game directory), and the mod output directory.
	* **Optional but recommended** - To prevent git from tracking your personal changes to this file, run the command `git update-index --skip-worktree UserProperties.xml`
* Open `Fez107ifier.sln` in Visual Studio
* Build the solution
	* This step will automatically copy the files into the mod output directory if you defined it in `UserProperties.xml` and that directory exists. If not, you will need to manually copy over the files.
