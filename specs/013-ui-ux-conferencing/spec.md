# Feature Specification: UI/UX Conferencing System

**Feature Branch**: `013-ui-ux-conferencing`
**Created**: 2026-01-21
**Status**: Planning
**Priority**: P0 (Required for user-facing product)

---

## Overview

Design and implement a minimal, futuristic, user-centric UI/UX system for hologram conferencing that works across all target platforms: iOS, Android, Web (mobile/desktop), Quest, and visionOS.

**Design Philosophy**: Inspired by visionOS spatial UI, Zoom, and Google Meet - prioritizing clarity, simplicity, and accessibility.

---

## Research: Platform UI/UX Patterns

### visionOS Design Principles

Based on [Apple HIG for visionOS](https://developer.apple.com/design/human-interface-guidelines/designing-for-visionos/):

| Principle | Description | Our Implementation |
|-----------|-------------|-------------------|
| **Transparency** | Use glass/blur over solid colors | Glassmorphism materials |
| **Spatial Anchoring** | Anchor UI in world, not to head | World-space UI panels |
| **Eye + Hand Input** | Gaze + pinch interaction | HoloKit hand tracking + touch fallback |
| **Dynamic Scale** | UI scales with distance | Auto-sizing based on viewer proximity |
| **Ergonomic Placement** | Center content in FOV | UI at comfortable viewing angles |

### Zoom/Meet Conferencing Patterns

| Pattern | Zoom | Meet | Our Approach |
|---------|------|------|--------------|
| **Participant Grid** | Gallery view | Tiled layout | Hologram spatial arrangement |
| **Active Speaker** | Spotlight | Pin | Seat-based highlighting |
| **Controls Bar** | Bottom floating | Bottom floating | Floating HUD, minimizable |
| **Reactions** | Emoji reactions | Hand raise | Gesture-based reactions |
| **Chat** | Side panel | Right panel | Voice + text overlay |

---

## Architecture Decision: React-Unity vs Unity-Only

### Option Comparison

| Criteria | Unity UI Toolkit | React-Unity WebGL | Recommendation |
|----------|-----------------|-------------------|----------------|
| **iOS/Android Native** | ✅ Best | ⚠️ WebView wrapper | Unity |
| **Web Browser** | ⚠️ WebGL only | ✅ Native React | React-Unity |
| **Quest** | ✅ Native | ❌ No support | Unity |
| **visionOS** | ✅ PolySpatial | ❌ No support | Unity |
| **Auth Integration** | ⚠️ Firebase SDK | ✅ Firebase JS | Depends |
| **Hot Reload** | ❌ Slow | ✅ Fast | React |
| **Figma→Code** | ⚠️ Manual | ✅ Plugins exist | React |
| **Long-term Maintainability** | ✅ Single codebase | ⚠️ Two codebases | Unity |

### Recommendation: **Hybrid Approach**

1. **Primary**: Unity UI Toolkit for native platforms (iOS, Android, Quest, visionOS)
2. **Secondary**: React wrapper for web deployment using react-unity-webgl
3. **Auth**: Firebase Unity SDK for native, Firebase JS for web

```
                    ┌─────────────────────────────────────────┐
                    │           Shared UI Design              │
                    │        (Figma → UI Toolkit USS)         │
                    └───────────────┬─────────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            ▼                       ▼                       ▼
    ┌───────────────┐      ┌───────────────┐      ┌───────────────┐
    │  Unity Native │      │  Unity WebGL  │      │  React Shell  │
    │  iOS/Android  │      │   + React UI  │      │   (Web Only)  │
    │  Quest/Vision │      │               │      │               │
    └───────────────┘      └───────────────┘      └───────────────┘
            │                       │                       │
            ▼                       ▼                       ▼
    Firebase Unity SDK      Firebase Unity SDK      Firebase JS SDK
```

---

## Authentication System

### Supported Providers

| Provider | Priority | SDK | Notes |
|----------|----------|-----|-------|
| **Google** | P0 | Firebase Auth | ⚠️ Deprecated Feb 2025, use Play Games or Unity Player Accounts |
| **Apple** | P0 | Sign In With Apple | Required for iOS App Store |
| **Email/Password** | P1 | Firebase Auth | With email verification |
| **Phone/SMS** | P2 | Firebase Auth | OTP verification |
| **GitHub** | P3 | Firebase Auth (OAuth) | Developer-focused |

### Auth Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        Auth Manager                              │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │   Google    │  │    Apple    │  │   Email     │              │
│  │  Sign-In    │  │  Sign-In    │  │  Password   │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                      │
│         └────────────────┼────────────────┘                      │
│                          ▼                                       │
│              ┌───────────────────────┐                          │
│              │   Firebase Auth       │                          │
│              │   (Credential)        │                          │
│              └───────────┬───────────┘                          │
│                          ▼                                       │
│              ┌───────────────────────┐                          │
│              │   Firestore User      │                          │
│              │   Profile Document    │                          │
│              └───────────────────────┘                          │
└─────────────────────────────────────────────────────────────────┘
```

### User Profile Schema (Firestore)

```typescript
interface User {
  id: string;                // Firebase UID
  username: string;          // Unique handle
  displayName: string;       // Display name
  email: string;             // Email address
  avatar: string;            // Avatar URL
  bio: string;               // User bio
  createdAt: Timestamp;      // Account creation

  // Social
  followers: number;
  following: number;

  // Conferencing
  roomHistory: string[];     // Recent room IDs
  preferredQuality: 'low' | 'medium' | 'high' | 'ultra';

  // Preferences
  audioEnabled: boolean;
  videoEnabled: boolean;
  theme: 'auto' | 'light' | 'dark';
}
```

---

## UI Components

### Core Screens

| Screen | Purpose | Key Elements |
|--------|---------|--------------|
| **Auth** | Login/Signup | Provider buttons, email form |
| **Lobby** | Pre-conference | Room code, user list, settings |
| **Conference** | Active call | Holograms, controls, chat |
| **Settings** | User preferences | Profile, audio/video, quality |

### HUD-UI-K (Heads-Up Display)

Adapt existing `VFXToggleUI` pattern with:

```
┌─────────────────────────────────────────────────────────────┐
│                     Conference HUD                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────┐                                          ┌─────┐   │
│  │Timer│                                          │Users│   │
│  │00:45│                                          │ 3/8 │   │
│  └─────┘                                          └─────┘   │
│                                                              │
│                    [Active Speaker: @alice]                  │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  🎤 Mute  │  📹 Camera  │  🖐 Raise  │  💬 Chat  │ ⚙️ │   │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌────────────────────────────────────┐                     │
│  │ Voice Activity: ████████░░░░░ 70%  │                     │
│  └────────────────────────────────────┘                     │
└─────────────────────────────────────────────────────────────┘
```

### Control Bar Actions

| Action | Icon | Gesture | Keyboard |
|--------|------|---------|----------|
| Mute/Unmute | 🎤 | Pinch mic icon | M |
| Camera On/Off | 📹 | Pinch camera icon | V |
| Raise Hand | 🖐 | Raise real hand | H |
| Reactions | 😊 | Gesture patterns | R |
| Chat Toggle | 💬 | Look at chat | C |
| Settings | ⚙️ | Look at gear | S |
| Leave | 🚪 | Pinch + hold | Esc |

---

## Visual Design System

### Colors (Glassmorphism)

```css
/* Base Colors */
--primary: rgba(0, 122, 255, 0.8);     /* iOS Blue */
--secondary: rgba(88, 86, 214, 0.8);   /* Purple accent */
--background: rgba(30, 30, 30, 0.7);   /* Dark glass */
--surface: rgba(50, 50, 50, 0.6);      /* Elevated glass */
--text-primary: rgba(255, 255, 255, 0.95);
--text-secondary: rgba(255, 255, 255, 0.7);

/* Semantic Colors */
--success: rgba(52, 199, 89, 0.8);     /* Green */
--warning: rgba(255, 149, 0, 0.8);     /* Orange */
--error: rgba(255, 59, 48, 0.8);       /* Red */
--muted: rgba(142, 142, 147, 0.6);     /* Gray */

/* Glass Effect */
--blur-amount: 20px;
--border-radius: 16px;
--border-color: rgba(255, 255, 255, 0.1);
```

### Typography (SF Pro / Inter)

```css
--font-display: 'SF Pro Display', system-ui;
--font-body: 'SF Pro Text', system-ui;

--text-xs: 11px;   /* Captions */
--text-sm: 13px;   /* Labels */
--text-base: 15px; /* Body */
--text-lg: 17px;   /* Titles */
--text-xl: 22px;   /* Headers */
--text-2xl: 28px;  /* Large headers */
```

### Spacing

```css
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;
--space-xl: 32px;
--space-2xl: 48px;
```

---

## Implementation Plan

### Phase 1: Foundation (Sprint 1)

- [ ] Set up UI Toolkit project structure
- [ ] Create USS (Unity Style Sheets) with glassmorphism
- [ ] Implement Auth screen (email/password only)
- [ ] Firebase Unity SDK integration

### Phase 2: Core UI (Sprint 2)

- [ ] Lobby screen with room code entry
- [ ] Conference HUD with basic controls
- [ ] Settings panel
- [ ] Profile editing

### Phase 3: Auth Providers (Sprint 3)

- [ ] Apple Sign In integration
- [ ] Google Sign In (or Unity Player Accounts)
- [ ] Phone/SMS verification
- [ ] GitHub OAuth

### Phase 4: Polish (Sprint 4)

- [ ] Animations and transitions
- [ ] Accessibility (VoiceOver, TalkBack)
- [ ] Localization framework
- [ ] Figma design sync workflow

### Phase 5: Web Deployment (Sprint 5)

- [ ] React-Unity WebGL integration
- [ ] Responsive web layout
- [ ] Firebase JS auth for web
- [ ] PWA configuration

---

## File Structure

```
Assets/
├── UI/
│   ├── Styles/
│   │   ├── Common.uss           # Shared styles
│   │   ├── Glassmorphism.uss    # Glass effects
│   │   ├── Typography.uss       # Text styles
│   │   └── Colors.uss           # Color variables
│   ├── Views/
│   │   ├── AuthView.uxml        # Login/Signup
│   │   ├── LobbyView.uxml       # Pre-conference
│   │   ├── ConferenceHUD.uxml   # In-call HUD
│   │   └── SettingsView.uxml    # Settings panel
│   ├── Components/
│   │   ├── Button.uxml          # Reusable button
│   │   ├── Card.uxml            # Glass card
│   │   ├── Avatar.uxml          # User avatar
│   │   └── ControlBar.uxml      # Action bar
│   └── Controllers/
│       ├── AuthController.cs
│       ├── LobbyController.cs
│       ├── ConferenceHUDController.cs
│       └── SettingsController.cs
├── Scripts/
│   └── Auth/
│       ├── IAuthProvider.cs     # Auth provider interface
│       ├── FirebaseAuthManager.cs
│       ├── AppleSignInProvider.cs
│       ├── GoogleSignInProvider.cs
│       └── UserProfileManager.cs
```

---

## Dependencies

### Unity Packages

- `com.unity.ui` (UI Toolkit)
- `com.google.firebase.auth` (Firebase Auth)
- `com.google.firebase.firestore` (Firestore)
- `com.unity.sign-in-with-apple` (Apple Sign In)

### External SDKs

- Firebase Unity SDK
- Sign In With Apple Unity Plugin

---

## Success Criteria

- [ ] SC-001: User can sign up with email/password
- [ ] SC-002: User can sign in with Google
- [ ] SC-003: User can sign in with Apple
- [ ] SC-004: Conference HUD appears during call
- [ ] SC-005: Controls work on iOS, Android, Quest
- [ ] SC-006: UI renders correctly in WebGL
- [ ] SC-007: Dark/light theme support
- [ ] SC-008: Accessibility labels on all controls

---

## References

- [Apple visionOS Design](https://developer.apple.com/design/human-interface-guidelines/designing-for-visionos/)
- [Apple Design Resources for Figma](https://www.figma.com/community/file/1253443272911187215/apple-design-resources-visionos)
- [Firebase Unity Auth](https://firebase.google.com/docs/auth/unity/start)
- [React-Unity WebGL](https://react-unity-webgl.dev/)
- portals_main: `src/services/auth.ts` - Reference implementation

---

*Created: 2026-01-21*
*Author: Claude Code*
