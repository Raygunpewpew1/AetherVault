using System.Text.Json;
using AetherVault.Core;
using AetherVault.Models;

namespace AetherVault.Data;

/// <summary>Maps <c>atomic_cards</c> rows to <see cref="Card"/> for lite catalog mode.</summary>
public static class AtomicCatalogMapper
{
    public sealed class AtomicRow
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int face_index { get; set; }
        public string? ascii_name { get; set; }
        public string? face_name { get; set; }
        public string? mana_cost { get; set; }
        public double mana_value { get; set; }
        public string? type_line { get; set; }
        public string? oracle_text { get; set; }
        public string? power { get; set; }
        public string? toughness { get; set; }
        public string? loyalty { get; set; }
        public string? defense { get; set; }
        public string? layout { get; set; }
        public string? colors { get; set; }
        public string? color_identity { get; set; }
        public string? keywords { get; set; }
        public string? scryfall_id { get; set; }
        public string? scryfall_oracle_id { get; set; }
        public string? first_printing { get; set; }
        public string? printings_json { get; set; }
        public string? legalities_json { get; set; }
        public string? rulings_json { get; set; }
        public string? related_json { get; set; }
        public string? leadership_json { get; set; }
        public int is_reserved { get; set; }
        public int is_funny { get; set; }
    }

    public static Card ToCard(AtomicRow r)
    {
        var sid = (r.scryfall_id ?? "").Trim();
        var oracle = (r.scryfall_oracle_id ?? "").Trim();
        var stableId = !string.IsNullOrEmpty(sid)
            ? sid
            : !string.IsNullOrEmpty(oracle)
                ? oracle
                : $"atomic:{r.id}";

        var card = new Card
        {
            Uuid = stableId,
            ScryfallId = stableId,
            Name = r.name ?? "",
            PrintedName = r.face_name ?? "",
            ManaCost = r.mana_cost ?? "",
            Cmc = r.mana_value,
            CardType = r.type_line ?? "",
            Text = r.oracle_text ?? "",
            OriginalText = r.oracle_text ?? "",
            Power = r.power ?? "",
            Toughness = r.toughness ?? "",
            Loyalty = r.loyalty ?? "",
            Defense = r.defense ?? "",
            Layout = EnumExtensions.ParseCardLayout(r.layout),
            Colors = r.colors ?? "",
            Keywords = r.keywords ?? "",
            SetCode = r.first_printing ?? "",
            SetName = "",
            Number = "",
            KeyruneCode = r.first_printing ?? "",
            Side = r.face_index == 0 ? 'a' : 'b',
            IsReserved = r.is_reserved != 0,
            IsFunny = r.is_funny != 0,
            Legalities = ParseLegalities(r.legalities_json),
            ImageUrl = string.IsNullOrEmpty(stableId)
                ? ""
                : ScryfallCdn.GetImageUrl(stableId, ScryfallSize.Small, ScryfallFace.Front)
        };

        return card;
    }

    public static CardLegalities ParseLegalities(string? json)
    {
        var leg = new CardLegalities();
        if (string.IsNullOrWhiteSpace(json))
            return leg;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var fmt = EnumExtensions.ParseDeckFormat(prop.Name);
                if (prop.Value.ValueKind == JsonValueKind.String)
                    leg[fmt] = EnumExtensions.ParseLegalityStatus(prop.Value.GetString());
            }
        }
        catch
        {
            // leave defaults
        }

        return leg;
    }

    public static List<CardRuling> ParseRulings(string? json)
    {
        var list = new List<CardRuling>();
        if (string.IsNullOrWhiteSpace(json))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dateStr = el.TryGetProperty("date", out var d) ? d.GetString() : null;
                var text = el.TryGetProperty("text", out var t) ? t.GetString() : null;
                if (string.IsNullOrEmpty(text))
                    continue;
                DateTime.TryParse(dateStr, out var date);
                list.Add(new CardRuling(date, text));
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }
}
