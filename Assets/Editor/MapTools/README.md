# Map Pin Editor

The **Map Pin Editor** window (`Tools ▸ Map Tools ▸ Map Pin Editor`) keeps the world map and each zone mini map in sync.

1. Assign your world map texture at the top of the window, create the pins that should appear, and click the world preview to store their normalized (0–1) positions.
2. Add every zone mini map mask you maintain, select one, then click its preview to record the localized offsets for the currently selected pin.
3. Use the JSON or CSV buttons to export the shared data set. Designers can bulk-edit the exported files and re-import them so both the world map and all zone mini maps reference the same pin definitions.

> Tip: CSV rows include the pin identifier, world coordinates, and one zone entry per row. This makes it easy to copy/paste between spreadsheets while keeping the world/zone data coupled.
