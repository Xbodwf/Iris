# Iridium

An optimized mod for A Dance of Fire and Ice, focusing on performance, visual customization, and compatibility.

[简体中文](#简体中文) | [English](#english-description)

---

## 简体中文

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理和现代化的视觉调整来提升您的《冰与火之舞》体验。

### 核心功能

#### 性能优化
- **纹理动态压缩**：在关卡加载时智能调整并压缩装饰物纹理，极大地缓解显存 (VRAM) 压力。
- **智能缩放适配**：自动同步调整碰撞箱与渲染缩放，确保在享受优化的同时，判定逻辑依然严丝合缝。
- **显存节省统计**：每次加载完成后，系统将通过通知告知您节省的具体显存容量。

#### 兼容性与修复
- **旧版暂停逻辑 (2.9.3)**：还原了 2.9.4 版本之前的 U 型转弯暂停行为，让老关卡重现其原本的节奏设计。
- **不死模式智能判定**：在不死模式下，致死装饰物碰撞将自动转换为“太快了 (FailOverload)”判定，帮助您更有效地练习。
- **强制难度 UI**：在所有 CLS 关卡中启用完整的难度选择界面。

#### 视觉自定义
- **拖尾深度定制**：支持手动调节行星拖尾的长度、发射密度，并提供**音高跟随模式**。
- **圆弧化转角**：还原并增强了极具动感的地砖转角圆弧视觉效果。
- **界面净化**：支持隐藏选关界面的官方新闻容器，回归极简视觉。

#### 现代化 UI
- **Material 3 设计**：基于 M3 规范的设置面板，支持流畅的交互和直观的卡片布局。

---

## English Description

> [!IMPORTANT]
> Iridium is designed to elevate your ADOFAI experience through better memory management and modern visual enhancements.

### Key Features

#### Performance Optimizer
- **Dynamic Texture Compression**: Resizes and compresses decoration textures during level load to significantly reduce VRAM usage.
- **Smart Scaling**: Automatically synchronizes colliders and render scales, ensuring gameplay precision is never compromised by optimization.
- **VRAM Tracker**: Receive instant notifications showing the exact amount of memory saved after each load.

#### Compatibility & Fixes
- **Legacy Pause (2.9.3)**: Restores the pre-2.9.4 U-turn pause behavior, allowing classic levels to play as originally intended.
- **No-Fail Judgment Conversion**: Lethal decoration hits are converted into "FailOverload" (Too Early) during No-Fail mode for better feedback.
- **Force Difficulty UI**: Forces the full difficulty selection UI to appear in all CLS levels.

#### Visual Customization
- **Advanced Tail Tweaks**: Manually adjust tail length and emission, or enable the dynamic **Pitch-Follow Mode**.
- **Circle Arc Corners**: Restores and enhances the smooth circular arc visuals for floor corners.
- **UI Decutter**: Option to hide the official news container in the level select screen for a cleaner look.

#### Modern UI
- **Material 3 Design**: A settings menu built on M3 principles, featuring smooth animations and intuitive card layouts.

---

## Project Structure
- `Main.cs`: Entry point
- `Settings.cs`: UI & configuration
- `Patches/`: Feature-specific Harmony patches
- `Resources/`: Assets & Localization



