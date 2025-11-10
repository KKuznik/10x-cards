<!-- d8a7d408-3948-4a85-b47e-1c519694c82e c04f3336-28db-4b97-a28b-2d750e467d59 -->
# Fix Icon Visibility on Active/Hover States

## Problem

Navigation icons are blue (#0d6efd), but when links are active or hovered they have blue background (#0d6efd), making the icons invisible.

## Solution

Create white versions of icons for active and hover states:

- **Default state:** Blue icons (#0d6efd) on white/transparent background ✓
- **Active state:** White icons (#ffffff) on blue background (#0d6efd)
- **Hover state:** White icons (#ffffff) on blue background (#0d6efd)

## Implementation

**File:** `10xCards/Components/Layout/NavMenu.razor.css`

### 1. Create White Icon Variants

Add new CSS rules for white icons:

- `.nav-item ::deep a.active .bi-house-door-fill-nav-menu`
- `.nav-item ::deep a.active .bi-lightbulb-fill-nav-menu`
- `.nav-item ::deep .nav-link:hover .bi-house-door-fill-nav-menu`
- `.nav-item ::deep .nav-link:hover .bi-lightbulb-fill-nav-menu`

Each should use the same SVG but with `fill='%23ffffff'` (white) instead of `fill='%230d6efd'` (blue).

## Result

Icons will be clearly visible in all states:

- Blue icons on white background (default)
- White icons on blue background (active/hover)

### To-dos

- [ ] Add white icon styles for active state in NavMenu.razor.css
- [ ] Add white icon styles for hover state in NavMenu.razor.css