# Daniel in the Lions' Den - Hackathon Game Plan

## Overview

A 2.5D action-adventure game where the player observes and subtly influences events from the story of Daniel in the lions' den. The player is not Daniel but an entity (observer/guardian) moving through the environment while the story unfolds dynamically.

**Key Features:**

* Player interacts with obstacles, traps, and puzzles while story unfolds in the background.
* AI Angel provides dynamic commentary based on player actions.
* Abstract/low-poly visuals for lions and Daniel.
* Continuous gameplay with story triggers, no full cutscenes.

---

## Level Blueprint / Zones

### Zone 1 — Entrance / Tutorial

* **Player Objectives:**

  * Learn movement: left/right, up/down, jump, slide.
  * Navigate low stone wall and narrow corridor.
* **Story Overlay:**

  * Angel dialogue: "A new path begins. Stay alert."
* **Trigger:** Trigger 1 at corridor exit.

### Zone 2 — Trap Corridor / First Challenge

* **Player Objectives:**

  * Avoid floor spikes, rolling logs, low-hanging stones.
  * Optional mini-puzzle: lever to unlock next corridor.
* **Story Overlay:**

  * Angel commentary: "Danger lurks even in shadows."
* **Trigger:** Trigger 2 after corridor.

### Zone 3 — Rest / Story Observation

* **Player Objectives:**

  * Step sequence puzzle to open small gate.
* **Story Overlay:**

  * Daniel praying (silhouette visible).
  * Angel commentary: "Faith shines in the darkest places."
* **Trigger:** Trigger 3 at puzzle completion.

### Zone 4 — Guards Enter / Multiple Paths

* **Player Objectives:**

  * Choose split paths: high, low, or side.
  * Navigate gaps, crawl under beams, jump crates.
* **Story Overlay:**

  * Player sees glimpses of Daniel being led by guards.
  * Angel commentary: "The faithful are sometimes moved, but do not lose hope."
* **Trigger:** Trigger 4 at end of split paths.

### Zone 5 — Lions’ Den / Guardian Role

* **Player Objectives:**

  * Avoid shadows of lions (abstract silhouettes).
  * Pull levers, move crates, light torches to help Daniel.
  * Timed sequences for shadow avoidance.
* **Story Overlay:**

  * Angel commentary reacts to player performance.
  * End cinematic: Daniel safe, light floods den.
* **Trigger:** Trigger 5 for final reflection.

---

## Gameplay Mechanics

* **Movement:** 2.5D, mainly left-right; vertical sections for jumps/slides.
* **Obstacles:** Jump, slide, timed movement.
* **Puzzles:** Step sequences, environmental interactions.
* **AI Angel:**

  * Gives dynamic dialogue based on player actions and memory.
  * Tracks performance, failed attempts, and puzzle completions.

## Visual & Audio Design

* **Daniel / Story:** Separate layer or semi-transparent silhouette.
* **Lions:** Abstract shadows along paths.
* **Lighting:** Dim, with pools guiding player movement.
* **Audio:** Ambient wind, torch flickers, faint lion growls, soft Angel voice lines.

## Implementation Notes

1. Use empty GameObjects as triggers for story events and AI dialogue.
2. Build zones sequentially in Unity: Zone 1 → Zone 3 → Zone 4 → Zone 5.
3. Add obstacles and puzzles after basic layout is functional.
4. Hardcode Angel dialogue first; integrate AI (Neocortex / Mistral) later.
5. Test each zone individually before connecting full level.

## Level Map Image

![Level Map](./A_2D_top-down_level_map_titled_"Daniel_in_the_Lion.png")

---

## 72-Hour Hackathon Plan

### Day 1 — Core Game Foundation

* 5 PM – 8 PM: Project setup, sketch level layout, placeholder assets.
* 8 PM – 11 PM: Player movement, collision, basic obstacle interactions.
* 11 PM – Midnight: Prototype playtest and mark triggers.

### Day 2 — Assets + AI Integration

* 9 AM – 12 PM: Generate/import assets (Hyper3D / Nimble Fox).
* 12 PM – 3 PM: Level layout, place obstacles and triggers, playtest.
* 3 PM – 6 PM: Angel system scaffolding, hardcoded dialogue.
* 6 PM – 9 PM: AI integration (temporary API keys in-app), dynamic dialogue testing.
* 9 PM – 11 PM: Full zone playthrough, adjust timings and dialogue.

### Day 3 — Polish & Submission

* 9 AM – 12 PM: Level and UI polish, lighting, particle effects.
* 12 PM – 3 PM: Full playtest, check AI triggers and memory.
* 3 PM – 6 PM: Demo video recording.
* 6 PM – 9 PM: Optional cleanup (Netlify backend, remove keys), package submission.

---

## Notes

* Focus on SAFE & STRONG first: playable level with AI Angel.
* Den section allows the player to feel like the "guardian angel".
* Shadows instead of real lions = easy to implement in 3 days.
* Dynamic AI commentary ties story and gameplay together.
* Polishing visuals, lighting, and particle effects optional if time-limited.
