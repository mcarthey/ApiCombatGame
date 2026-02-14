# CrewTrack UI/UX Guidelines

**Purpose:** Make the mobile app feel like a *real* native app, not a web page in a phone wrapper.

**Audience:** Sara (primary), Mark (review)

**Design System:** Material 3 Expressive (M3E) - Google's latest design language, unveiled at I/O 2025

---

## The Golden Rule

> **If it feels like a website, we failed.**

Field crews will reject a clunky app. It needs to feel natural, fast, and purpose-built for their thumbs.

## Why Material 3 Expressive?

Google's research (46 studies, 18,000+ participants) found:
- Users spot key UI elements **up to 4x faster**
- Older adults (45+) perform **at parity** with younger users (critical for our diverse crew age range)
- 87% of 18-24 year olds prefer expressive design

**See:** [MOBILE_M3_EXPRESSIVE_DESIGN.md](../design/MOBILE_M3_EXPRESSIVE_DESIGN.md) for full design spec.

---

## The Problem with Flat Design (Why M3E Matters)

M3E directly addresses the usability problems that "flat design" created over the past decade.

### Nielsen Norman Group Research Findings

[NNg's 2017 study on flat design](https://www.nngroup.com/articles/flat-design/) found:

| Metric | Weak Signifiers (Flat) | Strong Signifiers |
|--------|------------------------|-------------------|
| Task completion time | **22% longer** | Baseline |
| Page fixations (eye scanning) | **25% more** | Baseline |
| User confidence | Lower | Higher |

> "Users spent 22 percent more time completing tasks on pages with weak signifiers... They also had 25 percent more page fixations on the weak-signifier pages, i.e., their eyes spent more time scanning the page before they found what they needed."

### What Are "Weak Signifiers"?

Flat design often creates **click uncertainty** - users can't tell what's interactive:

```
WEAK SIGNIFIER (Flat):
┌─────────────────────────────────────┐
│  Hardwood Floor Installation        │  ← Is this clickable?
│  WO-2026-0142                       │
│  Johnson Residence                  │
│  ─────────────────── 2/6            │  ← Subtle progress bar
│                          In Progress │  ← Tiny, same-weight text
└─────────────────────────────────────┘

STRONG SIGNIFIER (M3E):
┌─────────────────────────────────────┐
│ ▌ Hardwood Floor Installation       │  ← Left accent = status
│   WO-2026-0142                      │
│                                     │
│   📍 Johnson Residence              │  ← Icon + text
│                                     │
│   ████████░░░░░░░░░░░░░  2/6        │  ← Chunky progress
│                                     │
│              ┌──────────────┐       │
│              │ IN PROGRESS  │       │  ← Large, bold badge
│              └──────────────┘       │
└─────────────────────────────────────┘
  ↑ Tonal card fill (amber) = active
```

### M3E Design Principles to Combat This

1. **Color as Signal** - Active items get tonal background fills, not just badges
2. **Size as Hierarchy** - Important elements are physically larger
3. **Shape as Affordance** - Interactive elements have consistent, distinct shapes
4. **Motion as Feedback** - Animations confirm interactions happened
5. **Containment as Grouping** - Related items share visual containers

### For CrewTrack Specifically

Our crews are often:
- In bright sunlight (need high contrast)
- In a hurry (need instant recognition)
- Wearing gloves (need large touch targets)
- Various ages (need accessibility)

**Every screen should answer instantly:** "What do I need to do next?"

The "In Progress" work order should be **impossible to miss** - not something users hunt for.

---

## Part 1: Mobile App (MAUI) Guidelines

### 1.1 The Thumb Zone

**This is critical.** Most users hold their phone one-handed and use their thumb.

```
┌─────────────────────┐
│                     │  ← HARD TO REACH (avoid putting actions here)
│                     │
│                     │
├─────────────────────┤
│                     │  ← OKAY ZONE (secondary content)
│                     │
├─────────────────────┤
│                     │  ← EASY ZONE (primary actions go here)
│  ┌───┬───┬───┬───┐  │
│  │ 🏠 │ 📋 │ ⏱️ │ ⚙️ │  │  ← BOTTOM NAV (always reachable)
│  └───┴───┴───┴───┘  │
└─────────────────────┘
```

**Rules:**
- Primary actions (Complete Task, Submit, etc.) go in the bottom half
- Bottom navigation for main sections (3-5 items max)
- Floating Action Button (FAB) for the #1 action on a screen
- Never put critical buttons at the top of the screen

### 1.2 Touch Targets (M3 Expressive)

**Minimum tap target: 48x48 dp** - but M3E recommends **larger for primary actions**

| Element | Minimum | M3E Recommended |
|---------|---------|-----------------|
| Standard buttons | 48dp | 48dp |
| **Primary action (Clock In/Out)** | 48dp | **56-64dp height, full-width** |
| Task checkboxes | 24dp | **32-40dp** |
| Bottom nav icons | 24dp | **28-32dp** |

```
BAD:  [Save]           ← Too small, hard to tap
GOOD: [    Save    ]   ← Generous padding
BEST: [══════════════] ← Full-width primary action (M3E)
          SAVE
```

- Primary buttons: full-width, 56-64dp height, bold tonal color
- Task checkboxes: 32-40dp with larger surrounding touch target
- Space between tappable items: minimum 8dp
- Think "work gloves" - crews often can't tap precisely

### 1.3 Navigation Patterns

**Bottom Navigation Bar** (3-5 items):
```
┌────────────────────────────────┐
│  My Work  │  Time  │  More    │
│    🏠     │   ⏱️   │    ☰    │
└────────────────────────────────┘
```

**Rules:**
- Icons + text labels (never icons alone - accessibility!)
- Highlight active tab clearly
- Max 5 items - more than that, use "More" menu
- Don't hide navigation behind hamburger menus on mobile

**Within-screen navigation:**
- Use back arrow (top-left) for drilling into detail
- Swipe gestures for common actions (swipe to complete, etc.)
- Pull-to-refresh for lists

### 1.4 Visual Hierarchy

**Make it scannable.** Crews are in the field, often in bright sunlight, sometimes in a hurry.

```
GOOD HIERARCHY:
┌─────────────────────────────┐
│ ██████████████████████████  │  ← BIG: Work Order Title
│ ████████████                │  ← Medium: Job Site
│ ████████                    │  ← Small: Address
│                             │
│ ┌─────────────────────────┐ │
│ │ ☑ Task 1 - Complete     │ │  ← Clear status indicators
│ │ ☐ Task 2 - Pending      │ │
│ │ ☐ Task 3 - Pending      │ │
│ └─────────────────────────┘ │
│                             │
│     [ Complete Task ]       │  ← Primary action, bottom half
└─────────────────────────────┘
```

**Typography:**
- Title: 20-24sp, bold
- Subtitle: 16-18sp, medium
- Body: 14-16sp, regular
- Caption: 12sp, light (use sparingly)

**Don't:**
- Use more than 3 font sizes per screen
- Make body text smaller than 14sp
- Use light gray text on white (contrast!)

### 1.5 Color Usage (M3 Expressive Tonal System)

**M3E uses tonal surfaces, not flat colors.** Each status gets a container color + foreground color pair.

| Status | Container (background) | On-Container (text/icon) |
|--------|------------------------|--------------------------|
| **Primary** | `#D3E4FF` (blue-50) | `#001C3A` (blue-900) |
| **Success/Complete** | `#C8E6C9` (green-100) | `#1B5E20` (green-900) |
| **Warning/In Progress** | `#FFE0B2` (amber-100) | `#E65100` (amber-900) |
| **Error/Blocked** | `#FFCDD2` (red-100) | `#B71C1C` (red-900) |
| **Neutral/Pending** | `#F5F5F5` (gray-100) | `#424242` (gray-800) |

```css
/* Example: Status Badge */
.badge-in-progress {
  background-color: var(--m3-warning-container);  /* Tonal fill */
  color: var(--m3-on-warning-container);          /* High contrast text */
  padding: 6px 12px;
  border-radius: 8px;
}
```

**Accessibility:**
- Text contrast ratio: minimum 4.5:1 (tonal pairs are pre-validated)
- Don't rely on color alone - use icons + color
- Provide "High Contrast Mode" for outdoor use (darker primary, pure white bg)

### 1.6 Status Indicators

Crews need to see status at a glance.

```
TASK STATUS:
☐  Pending     (outline, gray)
🔄 In Progress (filled, amber)
✓  Complete    (filled, green)
⚠  Issue       (filled, red)

GPS STATUS:
📍 At job site    (green badge)
📍 Away from site (amber badge)
📍 No GPS signal  (gray badge)
```

**Use:**
- Filled shapes for completed/active
- Outline shapes for pending
- Color + icon together (never color alone)

### 1.7 Loading & Feedback

**Never leave users wondering if something worked.**

| Action | Feedback |
|--------|----------|
| Button tap | Ripple effect + brief disable |
| Form submit | Loading spinner + "Saving..." |
| Success | Brief toast/snackbar + checkmark |
| Error | Inline error message (not popup) |
| Sync in progress | Subtle indicator (not blocking) |

**Offline mode:**
- Show clear "Offline" indicator in header
- Queue icon with count: "3 items pending sync"
- Don't show errors for expected offline behavior

### 1.8 Motion & Animation (M3 Expressive)

**M3E introduces "springy" physics-based animations** that feel natural and delightful.

```css
/* Spring easing curves */
--m3-spring-standard: cubic-bezier(0.2, 0, 0, 1);      /* 300ms - most interactions */
--m3-spring-emphasized: cubic-bezier(0.05, 0.7, 0.1, 1); /* 400ms - primary actions */
--m3-spring-quick: cubic-bezier(0.4, 0, 0.2, 1);       /* 150ms - micro-interactions */
```

| Interaction | Animation |
|-------------|-----------|
| Card tap | scale(0.98) → scale(1.0), 150ms spring |
| Task complete | checkbox scale(1.2) → scale(1.0), 300ms + checkmark draw |
| Clock In/Out tap | ripple + scale(0.95) → scale(1.0), 200ms |
| Page enter | slide up 24px + fade in, 300ms |
| Bottom nav switch | icon scale(1.1) + indicator slide, 300ms |

**Important:** Respect `prefers-reduced-motion` for accessibility:
```css
@media (prefers-reduced-motion: reduce) {
  * { animation-duration: 0.01ms !important; }
}
```

### 1.8 Forms & Input

**Mobile forms should be dead simple.**

**Rules:**
- One column only (never side-by-side on mobile)
- Large input fields (48dp height minimum)
- Clear labels above fields (not placeholder-only)
- Show keyboard appropriate to input (number pad for quantities, etc.)
- Auto-advance to next field when possible
- Validation on blur, not on every keystroke

```
GOOD FORM:
┌─────────────────────────────┐
│ Quantity                    │  ← Label above
│ ┌─────────────────────────┐ │
│ │ 12                    ▼ │ │  ← Large input, number keyboard
│ └─────────────────────────┘ │
│                             │
│ Notes (optional)            │
│ ┌─────────────────────────┐ │
│ │                         │ │
│ │                         │ │  ← Multi-line for notes
│ └─────────────────────────┘ │
│                             │
│     [ Save ]                │  ← Single primary action
└─────────────────────────────┘
```

### 1.9 Photos & Camera

**Photo capture is critical for this app.**

- Use full-screen camera view
- Show clear capture button (large, centered bottom)
- Preview before confirming
- Show thumbnail after capture with "Retake" option
- Compress images before upload (crews have limited data)

### 1.10 Spanish Language Considerations

- Spanish text is often 15-30% longer than English
- Design with extra space for text expansion
- Test every screen in Spanish before finalizing
- Icons help reduce text dependency

---

## Part 2: Web Admin Guidelines

The web admin (Owner/Supervisor) is desktop-first. Different rules apply.

### 2.1 Layout

```
┌─────────────────────────────────────────────────────────┐
│  Logo    │  Dashboard  │  Work Orders  │  Crew  │ User │
├──────────┼──────────────────────────────────────────────┤
│          │                                              │
│  Sidebar │              Main Content                    │
│  (nav)   │                                              │
│          │                                              │
│          │                                              │
│          │                                              │
└──────────┴──────────────────────────────────────────────┘
```

- Fixed sidebar navigation (collapsible on smaller screens)
- Breadcrumbs for deep navigation
- Tables for data lists (sortable, filterable)
- Cards for dashboard widgets

### 2.2 Keep Styling Professional

The admin interface uses Tailwind CSS (same as the mobile app for consistency).

**Do:**
- Customize the color palette to match brand
- Use consistent spacing (stick to Tailwind's spacing scale)
- Subtle shadows and borders
- Clean typography (no crazy fonts)

**Don't:**
- Rainbow of colors
- Rounded corners on everything
- Gratuitous animations

### 2.3 Data Tables

Admin users will spend time in tables. Make them good.

- Sortable columns (click header)
- Filterable (search box, dropdowns)
- Pagination (25/50/100 per page)
- Row actions on right side (View, Edit, Delete)
- Bulk select for batch operations
- Export to CSV

### 2.4 Dashboard

The dashboard should answer: "What do I need to know right now?"

**Key widgets:**
- Crew status map (who's where)
- Today's work orders (count + status breakdown)
- Alerts/issues requiring attention
- Recent activity feed

**Don't:**
- Cram too much data on one screen
- Use pie charts for more than 5 segments
- Show data nobody acts on

---

## Part 3: Design Checklist

Before marking any screen "done," verify:

### Mobile (MAUI)
- [ ] Primary actions are in thumb zone (bottom half)
- [ ] Touch targets are 48dp minimum
- [ ] Works in landscape (or gracefully restricts to portrait)
- [ ] Text is readable in bright light (good contrast)
- [ ] All text uses localization (test in Spanish!)
- [ ] Loading states for all async operations
- [ ] Offline behavior is clear to user
- [ ] No horizontal scrolling
- [ ] Back button works correctly

### Web Admin
- [ ] Works at 1920x1080 (primary) and 1366x768 (common laptop)
- [ ] Tables are sortable and filterable
- [ ] Forms have clear validation messages
- [ ] Breadcrumbs for navigation depth > 2
- [ ] All text uses localization
- [ ] Responsive down to tablet (768px) at minimum

### Both Platforms
- [ ] Consistent color usage
- [ ] Status indicators are color + icon (not color alone)
- [ ] Error messages are helpful (not "An error occurred")
- [ ] Success feedback for completed actions
- [ ] Accessible (screen reader friendly, good contrast)

---

## Part 4: Resources

### Material 3 Expressive (Primary Reference)
- [M3 Expressive Research](https://design.google/library/expressive-material-design-google-research) - Research data (46 studies, 18k participants)
- [Building with M3 Expressive](https://m3.material.io/blog/building-with-m3-expressive) - Step-by-step implementation guide
- [Material Design 3](https://m3.material.io/) - Component specs and guidelines
- [M3 Expressive Launch Blog](https://blog.google/products-and-platforms/platforms/android/material-3-expressive-android-wearos-launch/) - Overview
- [Material Theme Builder](https://material-foundation.github.io/material-theme-builder/) - Generate tonal color schemes
we
### Design Resources
- [Material 3 Design Kit (Figma)](https://www.figma.com/community/file/1035203688168086460/material-3-design-kit) - Official Figma components
- [M3E Motion Theming](https://m3.material.io/blog/m3-expressive-motion-theming) - Animation system
- [Expressive Motion Playground](https://expressivemotion.pages.dev/) - Interactive animation demos

### Android Development Reference
- [Compose Material 3 Package](https://developer.android.com/reference/kotlin/androidx/compose/material3/package-summary) - Full component API
- [Material 3 in Compose](https://developer.android.com/develop/ui/compose/designsystems/material3) - Getting started

### Flat Design Research (Why M3E Exists)
- [NNg: Flat Design Study](https://www.nngroup.com/articles/flat-design/) - 22% longer task completion on flat design
- [NNg: Signifiers & Affordances](https://www.nngroup.com/articles/signifiers-affordances-ui-design/) - Why strong signifiers matter
- [M3E: Building on Flat Design Failures](https://uxdesign.cc/material-3-expressive-building-on-the-failures-of-flat-design-d7a9bb627298) - Excellent analysis

### CrewTrack Design Docs
- [MOBILE_M3_EXPRESSIVE_DESIGN.md](../design/MOBILE_M3_EXPRESSIVE_DESIGN.md) - Full design spec with Blueprint theme
- [M3E_VISUAL_COMPARISON.md](../design/M3E_VISUAL_COMPARISON.md) - Before/after mockups

### CrewTrack Logo & Brand Assets
Located in `src/CrewTrack.Maui/wwwroot/img/`:
- `logo.svg` - CT monogram icon (64x64)
- `logo-full.svg` - Full logo with "CrewTrack" text
- `logo-blueprint.svg` - Blueprint-styled variant for splash screens

### MAUI-Specific
- [DevExpress MAUI Material Design 3](https://community.devexpress.com/blogs/mobile/archive/2024/05/22/how-to-implement-material-design-3-in-a-net-maui-application.aspx) - If we need component library
- [MDC-MAUI (open source)](https://github.com/yiszza/mdc-maui) - Material components for MAUI
- [MAUI Color Schemes](https://simplea.com/resources/articles/net-maui-color-schemes) - Converting Material colors to MAUI

### General Mobile UX
- [Mobile App UI Design Best Practices](https://nextnative.dev/blog/mobile-app-ui-design-best-practices) - Good overview
- [Bottom Tab Bar Best Practices](https://uxdworld.com/bottom-tab-bar-navigation-design-best-practices/) - Navigation patterns
- [Mobile UX Ultimate Guide](https://uxcam.com/blog/mobile-ux/) - Comprehensive reference

### Accessibility
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/) - Accessibility standards
- Contrast checker: WebAIM Contrast Checker

---

## Part 5: CrewTrack-Specific Patterns

### Work Order Card (Mobile)

```
┌─────────────────────────────────────┐
│ 📍 At Site                    9:15a │  ← Status badge + time
│                                     │
│ Three Oaks Senior Living            │  ← Title (bold, large)
│ 123 Oak Street, Wales WI            │  ← Address (smaller)
│                                     │
│ ████████████████░░░░  4/6 tasks     │  ← Progress bar
│                                     │
│ ┌─────────────────────────────────┐ │
│ │  View Details              →   │ │  ← Tappable row
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### Task List Item (Mobile)

```
┌─────────────────────────────────────┐
│ ✓  Remove existing flooring         │  ← Checkbox + task
│    Completed 9:32am by Michael      │  ← Metadata (gray, small)
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ ☐  Install underlayment             │  ← Pending task
│    └─ Required: Photo proof         │  ← Requirements (amber)
└─────────────────────────────────────┘
```

### GPS Verification (Mobile)

```
┌─────────────────────────────────────┐
│           ← Back                    │
│                                     │
│        📍                           │
│     Verifying                       │
│     Location...                     │  ← Animated pulse
│                                     │
│   Three Oaks Senior Living          │
│   123 Oak Street, Wales WI          │
│                                     │
│   ━━━━━━━━━━━━━━━━━━                │  ← Progress indicator
│                                     │
│                                     │
│     [ Cancel ]                      │
└─────────────────────────────────────┘

       ↓ Success State ↓

┌─────────────────────────────────────┐
│           ← Back                    │
│                                     │
│        ✓                            │
│     At Job Site                     │  ← Green checkmark
│                                     │
│   Three Oaks Senior Living          │
│   123 Oak Street, Wales WI          │
│   Distance: 15m                     │
│                                     │
│                                     │
│     [ Continue ]                    │  ← Primary action
└─────────────────────────────────────┘
```

---

## When In Doubt

1. **Look at apps you use daily** - Gmail, Maps, your banking app. What feels good?
2. **Test on a real phone** - Emulators lie. Use a physical device.
3. **Ask: would a tired crew member at 4pm understand this?**
4. **Less is more** - When unsure, remove elements, don't add them.

---

*Last Updated: January 27, 2026*

*Sources: [Material 3 Expressive Research](https://design.google/library/expressive-material-design-google-research), [Material Design 3](https://m3.material.io/), [Mobile UX Best Practices](https://nextnative.dev/blog/mobile-app-ui-design-best-practices)*
