# 🚀 PROJECT PUDDLE: ITERATION REPORT & TASK LIST (20260221_Night)

## 📋 ITERATION REPORT (Current Status)
- **Art Integration:** Successfully integrated existing art assets into the game (NPC Sprites, Puddle mechanics).
- **Experience Optimization:** 
  - Overhauled Power-up system (bouncing animations, temp speed scaling, glowing feedback).
  - Improved rain mechanics (rain volume scales dynamically with cloud size).
  - Refined puddle collision logic (raindrops accurately enlarge existing puddles instead of overlapping).
  - Upgraded NPC animation engine (built-in 2-frame sprite swapping, facing directional updates).
  - Built a robust centralized `AudioManager` with pitch randomization and AudioMixer volume controls.

---

## 🎨 ART & VISUALS REQUIREMENTS

**🔴 High Priority:**
- **Game Title Logo:** Visually appealing logo for the main menu.
- **Game Fonts:** Custom or hand-drawn fonts for UI, scoring, and text.
- **UI Button Assets:** Sprites for Start, Settings, Guide, Resume, Quit.
- **Emotion Bar (Unity Slider UI):** To build a custom Unity Slider, we need:
  - *Background Sprite:* The empty container of the bar.
  - *Fill Sprite:* The color/texture strip when the bar is filled.
  - *Handle Sprite (Optional):* An icon or knob indicating the current value position.

**🟡 Medium Priority:**
- **Game Icon:** For the desktop/executable icon.
- **Title Screen Splash Art:** Background illustration for the main menu.
- **Environmental Decorations:** Non-interactive park objects to make the world feel alive (e.g., bushes, benches, park slide, swings).
- **Water Splash Animations:** Frame-by-frame animation for when raindrops hit the ground or NPCs jump in puddles.

**🟢 Low Priority:**
- **Puddle Variants:** Additional puddle shapes for the `Puddle.cs` randomizer to pick from to avoid repetitive ground textures.

---

## 🎵 AUDIO REQUIREMENTS (Interface Summary)
*The programming for these hooks is fully implemented. The audio team just needs to provide the audio files to drag and drop into the Inspector.*

**Global / UI (`AudioManager.cs`):**
- `Title BGM` (Main Menu Music)
- `Gameplay BGM` (In-Game Ambient/Music)
- `Game Over BGM` (Defeat/Score Screen Music)
- `UI Button Click SFX`
- `Game Paused SFX`

**Player / Cloud (`CloudController.cs`):**
- `Rain Loop SFX` (Continuous white noise while holding the rain button)
- `Power-up Collected SFX`

**Environment (`Raindrop.cs`):**
- `Raindrop Hit SFX` (Played when a drop hits the ground/puddle)

**NPCs (`NPCBase.cs`):**
- `Puddle Splash SFX` (Played when an NPC jumps into water)
- `Bubble Found Puddle SFX` (Alert/Exclamation mark sound)
- `Bubble Happy SFX` (Joy/Laugh sound)
- `Bubble Sad SFX` (Shock/Cry sound)

---

## 💻 PROGRAMMING & DESIGN REQUIREMENTS (`Assets/Scripts`)

**🔴 High Priority (Design & Planning):**
- **Number Balancing:** Fine-tune movement speed, rain decay rate, NPC spawn rates, and Power-up effectiveness.
- **Game Polish:** General bug fixing, juice, and ensuring a smooth "Game Feel" before playtesting.

**🟡 Medium Priority (Programming):**
- **Settings Page:** Implement the UI panel for volume sliders (BGM/SFX) and link them to the AudioManager.
- **Tutorial / Guide Page:** Create an in-game tutorial screen explaining controls and mechanics.

**🟢 Low Priority (Programming):**
- **Gamepad / Controller Optimization:** Fix deep UI navigation bugs for controllers. *(Optimization: If not completed in time, simply do not provide controllers for the playtest and rely on keyboard/mouse).*
- **Custom Keybindings:** Allow players to remap their controls.
