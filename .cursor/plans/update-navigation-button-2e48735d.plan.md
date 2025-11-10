<!-- 2e48735d-4aa5-4006-9695-cfdb29a1a088 bb7b6b43-a2ee-4bf4-8c6f-eb0fd8559845 -->
# Darken Hover and Inactive Colors

## Changes Required

Update `10xCards/Components/Layout/NavMenu.razor.css`:

### 1. Inactive/Default state text color (line 88)

- Change `color: #0d6efd` to darker blue like `#0b5ed7` or `#0a58ca`
- Update icon colors to match

### 2. Hover state background (line 118)

- Change `background-color: #0d6efd` to darker blue like `#0a58ca`
- Update `border-color` to match

### 3. Update default icon colors (lines 59, 63)

- Change blue icons from `%230d6efd` to match new darker text color

This will create better contrast and visual hierarchy with darker colors for both inactive text and hover state.