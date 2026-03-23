# MTG SQLite `cards` table — developer notes

Reference for how card rows are shaped in the app’s MTG master database (MTGJSON-derived SQLite). Use this when writing or debugging queries in [`Data/MTGSearchHelper.cs`](../Data/MTGSearchHelper.cs) and related code.

**CI:** The weekly release workflow runs [`scripts/normalize_mtg_sqlite_columns.py`](../scripts/normalize_mtg_sqlite_columns.py) on `AllPrintings.sqlite` so `availability`, `finishes`, and `keywords` are stored as JSON arrays before zipping. The app still accepts legacy comma-separated text for older local DBs.

## `availability` column (important)

In shipped databases, **`availability` is often plain comma-separated text**, not a JSON array.

Example value:

```text
mtgo, paper
```

Tokens are lowercase MTGJSON names (`paper`, `mtgo`, `arena`, …), separated by commas; spaces after commas may appear. Search filters use logic that handles:

- Valid JSON arrays (if present),
- Quoted JSON substrings,
- This comma-separated form (see `MtgSearchHelper.WhereAvailabilityAny`).

Do not assume `json_valid(availability) = 1` for every row.

## `finishes` and `keywords`

Exports may store **`finishes`** as plain text (e.g. `nonfoil`) or comma-separated values, not only JSON arrays. **`keywords`** may appear as comma-separated oracle keywords (e.g. `Goad, Treasure, Enchant`). Search uses the same multi-format matching as `availability` for finishes (`WhereFinishesAny`), and JSON-or-plain-LIKE for keyword filters (`WhereKeywordTermsAll`). When mapping cards, `IsFoil` / `IsNonFoil` treat `finishes` as comma-separated tokens so `nonfoil` does not count as foil.

## Column order (header row)

Tab-separated header as provided from exports / tooling:

```text
artist	artistIds	asciiName	attractionLights	availability	boosterTypes	borderColor	cardParts	colorIdentity	colorIndicator	colors	defense	duelDeck	edhrecRank	edhrecSaltiness	faceConvertedManaCost	faceFlavorName	faceManaValue	faceName	facePrintedName	finishes	flavorName	flavorText	frameEffects	frameVersion	hand	hasAlternativeDeckLimit	hasContentWarning	isAlternative	isFullArt	isFunny	isGameChanger	isOnlineOnly	isOversized	isPromo	isRebalanced	isReprint	isReserved	isStorySpotlight	isTextless	isTimeshifted	keywords	language	layout	leadershipSkills	life	loyalty	manaCost	manaValue	name	number	originalPrintings	originalReleaseDate	originalText	otherFaceIds	power	printedName	printedText	printedType	printings	producedMana	promoTypes	rarity	rebalancedPrintings	relatedCards	securityStamp	setCode	side	signature	sourceProducts	subsets	subtypes	supertypes	text	toughness	type	types	uuid	variations	watermark
```

## Example row (abbreviated)

Full rows are wide and include long oracle text. Below is a shortened illustration; **`availability`** is shown explicitly.

| Field          | Example (truncated) |
|----------------|---------------------|
| artist         | Svetlin Velinov     |
| availability   | `mtgo, paper`       |
| colors         | R                   |
| name           | Shiny Impetus       |
| manaCost       | {2}{R}              |
| manaValue      | 3                   |
| type           | Enchantment — Aura  |
| uuid           | ef4abcab-0b15-5023-8820-170115dc0857 |

Other columns (`text`, `printedText`, `relatedCards`, JSON blobs, etc.) follow the header order above.

## Related code

- Card mapping: [`Data/CardMapper.cs`](../Data/CardMapper.cs) (`CleanJsonArray` and friends assume JSON for some fields; `availability` may still be comma-separated at rest in SQLite).
- Search: [`Data/SearchOptionsApplier.cs`](../Data/SearchOptionsApplier.cs), [`Core/SearchOptions.cs`](../Core/SearchOptions.cs).
