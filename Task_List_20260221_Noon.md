# 🚀 PROJECT PUDDLE: TASK LIST (20260221_Noon)

### 🎨 DESIGN & MECHANICS (TODO) 

**🔴 High Priority - Core Tuning:**
- Polish overall game mechanics, game feel, and difficulty.
- **Emotion System:** Define the starting value, maximum cap, and the speed of natural decay. 
  - *Optimization:* Scale the decay rate linearly based on survival time to naturally increase difficulty.
- **Cloud/Player:** Tune movement speed, inertia, rain volume, and the collision for raining on kids.
- **Puddles:** Set standard sizes and evaporation speeds. Decide if random sizes are needed.
- **Power-ups:** Determine values and how to visually show the player they got buffed. 
  - *Optimization:* Drop the idea of negative side-effect items to keep the gameplay loop pure and prevent player frustration.

**🟡 Medium Priority - Content:**
- **NPC Variants:** Design variants. 
  - *Optimization:* Keep it to 2-3 types (e.g., a normal kid, a fast runner, and a "Raincoat Kid" who is immune to rain penalties).
- **Game Board:** Finalize level size and the generation mechanics for NPCs and power-ups.
- **UI/UX Logic:** Determine how to display remaining water and power-up buffs. 
  - *Optimization:* Do not build a separate water UI bar; directly use the cloud's physical size to represent remaining water.

**🟢 Low Priority:**
- **Random Events:** Wind blowing, school ending, etc. 
  - *Optimization:* Skip for now to ensure core features are delivered on time.

---

### 🖌️ ART & VISUALS (TODO) 

**Sprites Needed:**
- **Cloud**
- **NPCs:** Need walking, happy running away, and sad running away states. Discuss variants like the different kinds. 
  - *Optimization:* Instead of drawing full body animations, use floating Emotion Bubbles (!, Happy, Sad) over their heads to save significant art time.
- **Power-ups:** *(Need further design and communication)*
- **Puddles & Raindrops**
- **Ground / Decorations**
- **Water Splash Animations:** 
  - *Optimization:* Focus the most art polish here, as splashes provide the core satisfying feedback of the game.

**UI / UX Assets:**
- Find suitable fonts or hand-drawn fonts.
- Title text and Background image for the Title Screen.
- Button assets: Start, Quit, Resume, Restart, Menu.
- **In-Game HUD:** Emotion bar and Scoreboard. Determine how to show power-up buffs.
- *Low Priority:* Pause and Game Over screens might only need text without complex backgrounds.

---

### 🎵 AUDIO (TODO)

**In-Game Sounds:**
- Raining and puddle creation.
- Stepping in puddles.
- Picking up power-ups.
  - *Optimization:* Add pitch randomization to the rain and splash sounds in the code so they don't sound repetitive.
- **Music:** Game BGM or ambient background noise.
- *Low Priority:* Title BGM, Game Over BGM. The system sounds like button confirmation, pause, and quit.

---

### 💻 PROGRAMMING & TECH (TODO) 

**🔴 High Priority:**
- Implement the NPC Emotion system. 
  - *Optimization:* Use emotion bubbles instead of full animations if art time is short.
- General feature implementation, game feel optimization, and bug fixing.
- TA: implement art and music.

**🟡 Medium Priority:**
- **Game Records / Leaderboard:** 
  - *Optimization:* Keep this strictly to local saves.
- **Button tutorial and guidance:** 
  - *Optimization:* Embed a static image of the controls (WASD/Space) directly on the Title Screen background.

**🟢 Low Priority:**
- **Gamepad UI Bug:** Gamepad cannot select UI buttons, but the mouse works fine. 
  - *Optimization:* If time is tight, ignore this bug and just use the mouse for menus during the playtest.
- **Settings Page:** Button configurations. 
  - *Optimization:* Only implement a basic volume/mute toggle if time allows.
