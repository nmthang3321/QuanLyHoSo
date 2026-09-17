# Feature - Area selector

Use for area grouping, search, and selection tasks. Also read `engineering/pages/RECORD_INPUT_FORM.md` and `engineering/pages/RECORD_LIST_FILTERS.md`.

Main files: `Models\AreaSelectionModels.cs`; record input, processing, and list XAML/code-behind/ViewModels; and `Infrastructure\Data\AppDataService.cs`.

UI rules:
- Do not use `Popup` or `ContextMenu` for search fields because Vietnamese IME composition can appear at the top-left of the screen.
- Use root overlay canvases: `AreaOverlayCanvas` for intake, `TransferAreaOverlayCanvas` for processing transfer, and `AreaFilterOverlayCanvas` for list filters.
- Position panels with `TransformToVisual(canvas)`, not `TransformToAncestor` when the canvas is a sibling.
- Panels must not stretch page layout or be clipped by cards. Group headers use `StrongTextBrush`; chevrons/counts use `MutedTextBrush`.

Data rules:
- `AreaSelectionOptions.Build(...)` creates groups; `Filter/Flatten` powers search.
- Intake and transfer-agency selectors allow child items only.
- The list filter supports `Tất cả`, groups, and child items. Clicking a group applies it and toggles expansion.
- `AddOptionalAreaFilter()` implements SQL group filtering.
