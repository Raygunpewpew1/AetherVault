# Collection/Decks black-screen debugging

Logs are written to:
- **Android**: `LocalApplicationData/<AppRootFolder>/mtgfetch.log` (e.g. `/data/data/com.mtgfetch.mobile/files/...` or app-specific path; use `adb pull` or Device File Explorer).

Search for **`[CollectionUI]`** to see the flow:

- `ApplyFilterAndSort: empty branch` = collection has 0 items, setting empty state
- `ApplyFilterAndSort: set IsCollectionEmpty=true` = UI updated on main thread
- `ApplyFilterAndSort: hasData branch, setting IsCollectionEmpty=false` = collection has items, showing grid
- `LoadCollectionAsync: loaded _allItems.Count=N` = items loaded from DB
- `LoadCollectionAsync: after ApplyFilterAndSort, IsCollectionEmpty=X, willInvokeCollectionLoaded=Y` = if Y is true when collection is empty, that’s the bug (grid gets shown)
- `UpdateContentHostContent: IsCollectionEmpty=X ... setting Content to EmptyState|Grid` = what the page is actually displaying
- `CollectionLoaded fired` = scroll-to-top ran (should only happen when we have items)

Ensure **Debug** level is written: `Logger.LogStuff(..., LogLevel.Debug)`.
