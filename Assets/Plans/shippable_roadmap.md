# Project Overview
- Game Title: Celestial Solitaire
- High-Level Concept: A celestial-themed Solitaire game with a "Category" based stacking rule, featuring ability power-ups and an "Eternal Atlas" collection system.
- Players: Single player
- Inspiration / Reference Games: Classic Solitaire with modern casual puzzle game progression.
- Tone / Art Direction: Celestial, ethereal, polished VFX.
- Target Platform: iOS (iPhone + iPad)
- Screen Orientation / Resolution: Landscape (Optimized for iPad and iPhone Safe Areas)
- Render Pipeline: URP (Universal Render Pipeline)

# Game Mechanics
## Core Gameplay Loop
1. **Selection**: Level Select (Grand Orrery) or Daily Challenge.
2. **Play**: Solitaire with category-based stacking and Abilities.
3. **Discovery**: Reveal "Facts" for the Eternal Atlas (Journal).
4. **Win/Progress**: Unlock new levels and earn stars.

## Controls and Input Methods
- Touch: Tap and drag cards, tap buttons for abilities. (New Input System with touch support).

# UI
- **Main Menu**: Play, Daily Challenge, Journal, Settings.
- **Grand Orrery**: Map-based level selector.
- **Game Board**: Mobile-optimized layout (Stock, Waste, Foundations, Tableaus).
- **Victory Panel**: Post-game feedback.
- **Eternal Atlas**: Journal viewer.
- **Settings**: Volume, Battery Saver mode (Target Frame Rate), Credits.

# Key Asset & Context
- `SolitaireManager.cs`: Gameplay logic.
- `AbilitySystem.cs`: Ability logic.
- `ProgressionManager.cs`: Player progress (PlayerPrefs).
- `CollectionManager.cs`: Fact collection.

# Implementation Steps

## Phase 1: Core Gameplay & Mobile Logic
1. **Fix Placeholders**: Complete logic in `SolitaireManager.cs` for `CheckTableauCompletion` and `CheckWinCondition`.
   - Assigned role: developer
   - Dependencies: None
2. **Implement Failure/Stuck State**: Add "No More Moves" check and "Game Over" UI panel.
   - Assigned role: developer
   - Dependencies: Step 1
3. **Touch Input Optimization**: Ensure card dragging feels responsive on mobile screens. Implement double-tap to auto-move to foundations.
   - Assigned role: developer
   - Dependencies: None

## Phase 2: Mobile UI/UX & Systems
1. **iOS Safe Area Integration**: Update the Canvas/UI layout to respect safe areas (notches/home indicators) on iPhone and iPad.
   - Assigned role: developer (UI)
   - Dependencies: None
2. **Settings Menu (Mobile Optimized)**:
   - Volume sliders.
   - Battery Saver toggle (sets `Application.targetFrameRate` to 30 vs 60).
   - Credits section.
   - Assigned role: developer (UI)
   - Dependencies: Phase 1
3. **Pause Menu**: Add pause functionality with "Restart" and "Quit to Menu".
   - Assigned role: developer
   - Dependencies: None

## Phase 3: Visuals & Performance
1. **URP Mobile Optimization**: Audit Post-processing (Volume) to ensure it runs smoothly on older iOS devices. Disable heavy effects if necessary.
   - Assigned role: developer (Tech Artist)
   - Dependencies: None
2. **VFX Polish**: Ensure `CelestialVFXManager` and particle systems are triggered correctly for mobile feedback.
   - Assigned role: developer
   - Dependencies: Phase 1

## Phase 4: Production Readiness (iOS)
1. **Player Settings (iOS)**:
   - Set Bundle Identifier (e.g., com.company.celestialsolitaire).
   - Configure Icons for all required iPhone/iPad sizes.
   - Set up Launch Screen (Storyboard or Image).
   - Set Version and Build numbers.
   - Assigned role: developer
   - Dependencies: None
2. **Provisioning & Testing**: Ensure signing is configured and perform a build via Xcode to verify on a physical device.
   - Assigned role: developer
   - Dependencies: All prior phases

# Verification & Testing
- **Safe Area Check**: Verify UI on different aspect ratios (iPad 4:3 vs iPhone 19.5:9).
- **Touch Responsiveness**: Test drag-and-drop and double-tap speed.
- **Performance**: Check frame rate stability on an iOS device.
- **Persistence**: Verify data saves across app restarts on mobile.
- **Build**: Successful build and deployment to an iOS device.
