# 💦 Project Puddle
*A Game Jam Practice Project | Game Jam 热身试验作*

![Unity](https://img.shields.io/badge/Unity-2022%2B-black?style=flat&logo=unity)
![C#](https://img.shields.io/badge/C%23-Scripting-blue?style=flat&logo=csharp)
![Status](https://img.shields.io/badge/Status-Playable_Demo-success)

## 📖 About / 项目简介
**English:**  
Project Cloud is a fun, fast-paced Game Jam practice project developed in Unity. You play as a cloud, raining on pedestrians to create puddles and spread happiness! We've just completed our first playable prototype (V1 Demo) .

**中文：**  
《Project Cloud》是一个基于 Unity 引擎开发的小游戏 Game Jam 练手项目。在游戏中，你将扮演一朵软绵绵的云，通过降雨在地上制造水坑，让路过的行人们因为尽情踩水而变得快乐！我们的第一版核心可玩 DEMO 已搭建完毕。

---

## 🎮 Controls / 操作指南
The game fully supports both Keyboard and Gamepad inputs!  
本作代码已原生支持键盘与主流手柄输入：

| Action / 操作                 | Keyboard / 键盘          | Gamepad / 手柄            |
| :---------------------------- | :----------------------- | :------------------------ |
| **Move / 移动飞行**           | `W` `A` `S` `D` / Arrows | Left Stick / D-Pad        |
| **Rain / 降雨**               | `Space` (空格)           | `A` Button / South Button |
| **Pause & Quit / 暂停与退出** | `ESC`                    | `B` / `Start` Button      |

*(Movement includes an elastic inertia/momentum system for a smooth drifting feel. / 移动配置了丝滑的物理惯性与漂移手感。)*

---

## ✅ Current Progress / 现有进度
**What we've built so far:**
- **Core Loop:** Built the underlying Game State Machine (Menu > Playing > Paused > Game Over).
- **Core Mechanics:** Dynamic procedural spawning engine for NPCs and Power-ups bounded globally.
- **Scoring & Systems:** Highscore persisting (`PlayerPrefs`), scalable events, UI routing, and camera smooth tracking.

**已完成的底层基建：**
- **核心游戏循环**：稳健的全局状态机防穿游戏流（主菜单/游玩/暂停/结算）。
- **实体系统**：基于时间增量的动态难度刷怪器机制、随机道具机制。
- **系统闭环**：基于 PlayerPrefs 的最高分榜单持久化、响应式的 UI 弹窗与事件总线、带边界限制的平滑跟随摄像机。

---

## 🤝 Further Plan / 团队需求
We are actively looking for collaborative minds in the following areas:  
代码的地基已经打好，我们现在非常迫切地需要以下方向的协助来为其注入灵魂：

🔥 **[High Priority / 核心需求]**
- 🧮 **Game Balance (数值策划)**  
  Tuning the rain depletion rate, NPC spawn curves, and scoring weights. / 调优降雨水量的消耗率、各阶段刷怪曲线与得分池的收益比。
- 💡 **Mechanics Design (机制设计)**  
  Designing cool new Power-Ups and unique NPC behaviors (e.g., umbrella guys, fast runners). / 设计更多脑洞大开的强化道具效果（增/减益）以及产生变数的新 NPC 种类（如打伞的人、快冲刺鸭）。
- 🎵 **Audio & SFX (音效设计)**  
  UI sounds, rain loops, satisfying splashing effects, and BGM. / 界面交互的清脆反馈音、降雨白噪音、治愈的踩水音效以及氛围 BGM。
- 🎨 **Art & Assets (美工资产)**  
  VFX (particles), UI polishing, character/cloud sprites, and scene environment art. / 强化道具等特效粒子、精美的主题 UI 贴图、角色帧动画以及地图原画搭建。
- ✨ **UX Polish (体验打磨)**  
  Enhancing game feel through screen shakes, subtle animations, and feedback. / 通过各种微动效、震屏、顿帧特效等大幅提升整体感官反馈手感。

🧊 **[Low Priority Backlog / 延后排期的低优先度任务]**
- Advanced Settings Menu (Full Video/Audio configuration) / 独立的详细设置配置面板。
- Custom Keybinding system / 游戏内自定义按键修改功能。
- Procedural Random Events System / 中后期的深度随机天气事件系统。

---
*If you are interested in making people smile with a bouncy raincloud, dive into the project and let's get building!*  
*如果你也想用这朵软绵绵的云给大家带来欢乐，欢迎随时开工，尽情发挥你的灵感！*
