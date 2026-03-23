using AetherVault.Core;
using Dapper;

namespace AetherVault.Data;

/// <summary>
/// Builds parameterized SQL against <c>atomic_cards</c> for lite catalog mode.
/// Mirrors a subset of <see cref="SearchOptionsApplier"/> / <see cref="MtgSearchHelper"/> behavior.
/// </summary>
public static class AtomicCatalogQuery
{
    public const string SelectList =
        """
        a.id, a.name, a.face_index, a.ascii_name, a.face_name, a.mana_cost, a.mana_value,
        a.type_line, a.oracle_text, a.power, a.toughness, a.loyalty, a.defense, a.layout,
        a.colors, a.color_identity, a.keywords, a.scryfall_id, a.scryfall_oracle_id,
        a.first_printing, a.printings_json, a.legalities_json, a.rulings_json, a.related_json,
        a.leadership_json, a.is_reserved, a.is_funny
        """;

    public static (string sql, DynamicParameters parameters) BuildSearch(
        SearchOptions options,
        int limit,
        int offset)
    {
        var (whereSql, p) = BuildWhere(options);
        var sql =
            $"""
            SELECT {SelectList}
            FROM atomic_cards a
            WHERE {whereSql}
            ORDER BY a.name COLLATE NOCASE, a.face_index
            LIMIT {limit} OFFSET {offset}
            """;
        return (sql, p);
    }

    public static (string sql, DynamicParameters parameters) BuildCount(SearchOptions options)
    {
        var (whereSql, p) = BuildWhere(options);
        var sql = $"SELECT COUNT(*) FROM atomic_cards a WHERE {whereSql}";
        return (sql, p);
    }

    private static (string whereSql, DynamicParameters parameters) BuildWhere(SearchOptions options)
    {
        var wh = new List<string> { "1=1" };
        var p = new DynamicParameters();
        var n = 0;

        if (!options.IncludeTokens)
            wh.Add("(a.type_line IS NULL OR a.type_line NOT LIKE '%Token%')");

        if (options.PrimarySideOnly && !options.IncludeAllFaces)
            wh.Add("a.face_index = 0");

        if (!string.IsNullOrWhiteSpace(options.NameFilter))
        {
            var key = Next(ref n);
            wh.Add($"(a.name LIKE @{key} OR a.face_name LIKE @{key} OR a.ascii_name LIKE @{key})");
            p.Add(key, "%" + options.NameFilter.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(options.TextFilter))
        {
            var key = Next(ref n);
            wh.Add($"a.oracle_text LIKE @{key}");
            p.Add(key, "%" + options.TextFilter.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(options.KeywordsFilter))
        {
            foreach (var term in options.KeywordsFilter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var key = Next(ref n);
                wh.Add($"LOWER(a.keywords) LIKE @{key}");
                p.Add(key, "%" + term.Trim().ToLowerInvariant() + "%");
            }
        }

        if (!string.IsNullOrEmpty(options.TypeFilter) &&
            !options.TypeFilter.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            var key = Next(ref n);
            wh.Add($"a.type_line LIKE @{key}");
            p.Add(key, "%" + options.TypeFilter.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(options.SubtypeFilter))
        {
            var key = Next(ref n);
            wh.Add($"a.type_line LIKE @{key}");
            p.Add(key, "%" + options.SubtypeFilter.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(options.SupertypeFilter))
        {
            var key = Next(ref n);
            wh.Add($"a.type_line LIKE @{key}");
            p.Add(key, "%" + options.SupertypeFilter.Trim() + "%");
        }

        AppendColorFilters(options, wh, p, ref n);

        if (options.UseCmcExact)
        {
            var key = Next(ref n);
            wh.Add($"a.mana_value = @{key}");
            p.Add(key, options.CmcExact);
        }
        else if (options.UseCmcRange)
        {
            var k0 = Next(ref n);
            var k1 = Next(ref n);
            wh.Add($"a.mana_value >= @{k0} AND a.mana_value <= @{k1}");
            p.Add(k0, options.CmcMin);
            p.Add(k1, options.CmcMax);
        }

        if (!string.IsNullOrWhiteSpace(options.PowerFilter))
        {
            var key = Next(ref n);
            wh.Add($"a.power = @{key}");
            p.Add(key, options.PowerFilter.Trim());
        }

        if (!string.IsNullOrWhiteSpace(options.ToughnessFilter))
        {
            var key = Next(ref n);
            wh.Add($"a.toughness = @{key}");
            p.Add(key, options.ToughnessFilter.Trim());
        }

        if (options.UseLegalFormat)
        {
            var fmt = options.LegalFormat.ToDbField();
            if (fmt.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                var key = Next(ref n);
                wh.Add(
                    $"""
                    a.legalities_json IS NOT NULL
                    AND LOWER(json_extract(a.legalities_json, '$.{fmt}')) = @{key}
                    """);
                p.Add(key, LegalityStatus.Legal.ToDbString().ToLowerInvariant());
            }
        }

        if (!string.IsNullOrWhiteSpace(options.SetFilter))
        {
            var key = Next(ref n);
            wh.Add(
                $"""
                a.printings_json IS NOT NULL
                AND EXISTS (
                  SELECT 1 FROM json_each(a.printings_json)
                  WHERE LOWER(json_each.value) = LOWER(@{key})
                )
                """);
            p.Add(key, options.SetFilter.Trim());
        }

        if (options.LayoutFilter.Count > 0)
        {
            var parts = new List<string>();
            foreach (var layout in options.LayoutFilter)
            {
                var key = Next(ref n);
                parts.Add($"a.layout = @{key}");
                p.Add(key, layout.ToDbString());
            }
            wh.Add("(" + string.Join(" OR ", parts) + ")");
        }

        if (options.CommanderOnly)
        {
            wh.Add(
                """
                (
                  (
                    a.leadership_json IS NOT NULL
                    AND TRIM(a.leadership_json) != ''
                    AND json_extract(a.leadership_json, '$.commander') = 1
                  )
                  OR
                  (
                    (a.type_line LIKE '%Legendary%' AND a.type_line LIKE '%Creature%')
                    OR (a.oracle_text LIKE '%can be your commander%')
                  )
                )
                """);
        }

        return (string.Join(" AND ", wh), p);
    }

    private static void AppendColorFilters(SearchOptions options, List<string> wh, DynamicParameters p, ref int n)
    {
        if (!string.IsNullOrWhiteSpace(options.ColorFilter))
        {
            var parts = new List<string>();
            foreach (var color in options.ColorFilter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = color.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (string.Equals(trimmed, "C", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add("(a.colors IS NULL OR TRIM(a.colors) = '')");
                    continue;
                }
                var key = Next(ref n);
                parts.Add($"a.colors LIKE @{key}");
                p.Add(key, "%" + trimmed + "%");
            }
            if (parts.Count > 0)
                wh.Add("(" + string.Join(" OR ", parts) + ")");
        }

        if (!string.IsNullOrWhiteSpace(options.ColorIdentityFilter))
        {
            var parts = new List<string>();
            foreach (var color in options.ColorIdentityFilter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = color.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (string.Equals(trimmed, "C", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add("(a.color_identity IS NULL OR TRIM(a.color_identity) = '')");
                    continue;
                }
                var key = Next(ref n);
                parts.Add($"a.color_identity LIKE @{key}");
                p.Add(key, "%" + trimmed + "%");
            }
            if (parts.Count > 0)
                wh.Add("(" + string.Join(" OR ", parts) + ")");
        }
    }

    private static string Next(ref int n) => "p" + n++;
}
