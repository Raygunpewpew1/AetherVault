using AetherVault.Core;
using AetherVault.Data;
using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

public enum ValidationLevel
{
    Success,
    Warning,
    Error
}

public class ValidationResult
{
    public ValidationLevel Level { get; set; }
    public string Message { get; set; } = "";

    /// <summary>Separate lines for UI (e.g. validation detail sheet). Empty when not populated.</summary>
    public IReadOnlyList<string> DetailLines { get; set; } = [];

    public bool IsSuccess => Level == ValidationLevel.Success;
    public bool IsError => Level == ValidationLevel.Error;
    public bool IsWarning => Level == ValidationLevel.Warning;

    private static IReadOnlyList<string> NormalizeDetailLines(string? message) =>
        string.IsNullOrWhiteSpace(message) ? [] : [message.Trim()];

    public static ValidationResult Success() => new() { Level = ValidationLevel.Success };

    public static ValidationResult Error(string message) => new()
    {
        Level = ValidationLevel.Error,
        Message = message ?? "",
        DetailLines = NormalizeDetailLines(message)
    };

    public static ValidationResult Warning(string message) => new()
    {
        Level = ValidationLevel.Warning,
        Message = message ?? "",
        DetailLines = NormalizeDetailLines(message)
    };

    /// <summary>
    /// Merges multiple validation results into one message (joined) and preserves each message as its own detail line.
    /// Prefer over ad hoc <c>new ValidationResult { … }</c> so <see cref="DetailLines"/> stays consistent.
    /// </summary>
    public static ValidationResult Combined(params ValidationResult[] results) => Combined((IReadOnlyList<ValidationResult>)results);

    /// <inheritdoc cref="Combined(ValidationResult[])"/>
    public static ValidationResult Combined(IReadOnlyList<ValidationResult> results)
    {
        var level = ValidationLevel.Success;
        var messages = new List<string>();
        foreach (var r in results)
        {
            if (r.Level == ValidationLevel.Success || string.IsNullOrWhiteSpace(r.Message))
                continue;

            if (r.Level == ValidationLevel.Error)
                level = ValidationLevel.Error;
            else if (r.Level == ValidationLevel.Warning && level != ValidationLevel.Error)
                level = ValidationLevel.Warning;

            messages.Add(r.Message);
        }

        if (level == ValidationLevel.Success)
            return Success();

        var message = string.Join(" ", messages);
        return new ValidationResult
        {
            Level = level,
            Message = message,
            DetailLines = messages
        };
    }
}

public class DeckValidator
{
    private readonly ICardRepository _cardRepository;

    public DeckValidator(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    /// <param name="preResolvedCommanderDeckIdentity">When set, skips resolving commander/partner/commander rows again (batch validation / perf).</param>
    /// <param name="targetSection">Destination section for this add (e.g. <see cref="DeckCardSections.Commander"/>); used with <paramref name="skipLegalityCheck"/>.</param>
    public async Task<ValidationResult> ValidateCardAdditionAsync(
        DeckEntity deck,
        Card card,
        int quantityToAdd,
        List<DeckCardEntity> currentCards,
        bool skipLegalityCheck = false,
        ColorIdentity? preResolvedCommanderDeckIdentity = null,
        string? targetSection = null)
    {
        var format = EnumExtensions.ParseDeckFormat(deck.Format);

        // 1. Format legality (Vintage Restricted is handled first — clearer than burying it under "not legal").
        if (!skipLegalityCheck)
        {
            bool handledVintageRestricted = false;
            if (format == DeckFormat.Vintage && card.Legalities[DeckFormat.Vintage] == LegalityStatus.Restricted)
            {
                int currentQty = GetTotalQuantity(card.Uuid, currentCards);
                if (currentQty + quantityToAdd > 1)
                {
                    return ValidationResult.Error($"Card '{card.Name}' is Restricted in Vintage (Max 1).");
                }

                handledVintageRestricted = true;
            }

            if (!handledVintageRestricted && !card.Legalities.IsLegalInFormat(format))
            {
                return ValidationResult.Error($"Card '{card.Name}' is not legal in {format.ToDisplayName()}.");
            }
        }

        // 2. Quantity limits
        int existingQuantity = GetTotalQuantity(card.Uuid, currentCards);
        int totalQuantity = existingQuantity + quantityToAdd;

        bool isBasicLand = card.IsBasicLand;
        bool isRelentless = card.Text.Contains(DeckValidationConstants.RelentlessOracleTextFragment, StringComparison.OrdinalIgnoreCase);

        int maxCopies = DeckFormatRules.MaxNonBasicCopies(format);

        if (!isBasicLand && !isRelentless && totalQuantity > maxCopies)
        {
            return ValidationResult.Error($"Cannot have more than {maxCopies} copies of '{card.Name}' in {format.ToDisplayName()}.");
        }

        // 3. Commander color identity (imports may add extra Commander-zone cards with skipLegalityCheck before identity is widened)
        bool skipColorForTrustedCommanderZoneAdd = skipLegalityCheck &&
            string.Equals(targetSection, DeckCardSections.Commander, StringComparison.OrdinalIgnoreCase);

        if (!skipColorForTrustedCommanderZoneAdd && DeckFormatRules.IsCommanderLike(format) && !string.IsNullOrEmpty(deck.CommanderId))
        {
            ColorIdentity commanderIdentity;
            if (preResolvedCommanderDeckIdentity.HasValue)
            {
                commanderIdentity = preResolvedCommanderDeckIdentity.Value;
            }
            else
            {
                var (identity, had, warning) = await DeckColorIdentityResolver
                    .TryResolveCommanderDeckColorIdentityAsync(_cardRepository, deck, currentCards, cardsByUuid: null)
                    .ConfigureAwait(false);
                if (warning != null)
                    return warning;
                if (!had)
                    return ValidationResult.Success();

                commanderIdentity = identity;
            }

            var cardIdentity = card.GetColorIdentity();
            if (!commanderIdentity.Contains(cardIdentity))
            {
                return ValidationResult.Error($"Card '{card.Name}' ({cardIdentity.AsString()}) is outside commander's color identity ({commanderIdentity.AsString()}).");
            }
        }

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateCommanderAsync(Card card, DeckFormat format)
    {
        if (!DeckFormatRules.IsCommanderLike(format))
        {
            return ValidationResult.Error("Commanders are only valid in Commander-like formats.");
        }

        bool isLegendaryCreature = card.IsCreature && card.IsLegendary;
        bool canBeCommander = card.Text.Contains(DeckValidationConstants.CanBeYourCommanderOracleTextFragment, StringComparison.OrdinalIgnoreCase);

        if (!isLegendaryCreature && !canBeCommander)
        {
            if ((format == DeckFormat.Brawl || format == DeckFormat.StandardBrawl) && card.IsPlaneswalker)
            {
                // Brawl commanders can be Planeswalkers
            }
            else
            {
                return ValidationResult.Error($"'{card.Name}' cannot be a commander (must be Legendary Creature).");
            }
        }

        return ValidationResult.Success();
    }

    private int GetTotalQuantity(string cardId, List<DeckCardEntity> cards)
    {
        return cards.Where(c => c.CardId == cardId).Sum(c => c.Quantity);
    }

    /// <summary>
    /// Validates overall deck size (main / sideboard / commander slots) for the given format.
    /// </summary>
    /// <remarks>
    /// <para>Returns <see cref="ValidationLevel.Warning"/> only (never <see cref="ValidationLevel.Error"/>): deck size
    /// violations are real rule issues in tournament Magic, but this app treats them as non-blocking guidance so users
    /// can edit freely while still seeing what is wrong. Do not use <see cref="ValidationResult.Level"/> here as a
    /// proxy for strict legality.</para>
    /// </remarks>
    public ValidationResult ValidateDeckSize(DeckEntity deck, List<DeckCardEntity> currentCards)
    {
        var format = EnumExtensions.ParseDeckFormat(deck.Format);

        int mainCount = currentCards
            .Where(c => string.Equals(c.Section, DeckCardSections.Main, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Quantity);

        int sideboardCount = currentCards
            .Where(c => string.Equals(c.Section, DeckCardSections.Sideboard, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Quantity);

        int commanderCount = currentCards
            .Where(c => string.Equals(c.Section, DeckCardSections.Commander, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Quantity);

        var issues = new List<string>();
        string formatName = format.ToDisplayName();

        if (DeckFormatRules.IsCommanderLike(format))
        {
            int total = mainCount + commanderCount;
            int target = DeckFormatRules.CommanderLikeDeckTargetTotal;

            if (total != target)
            {
                int diff = total - target;
                if (diff < 0)
                {
                    issues.Add($"{formatName} deck is {-diff} card(s) short of {target} (currently {total}/{target}).");
                }
                else
                {
                    issues.Add($"{formatName} deck has {diff} extra card(s) over {target} (currently {total}/{target}).");
                }
            }
        }
        else
        {
            int minMain = DeckFormatRules.MinMainDeckCardsForConstructedWarning(format);

            if (minMain > 0 && mainCount < minMain)
            {
                issues.Add($"{formatName} deck has only {mainCount} main-deck cards (needs at least {minMain}).");
            }

            if (DeckFormatRules.UsesConstructedSideboardCap(format) &&
                sideboardCount > DeckFormatRules.MaxConstructedSideboardCards)
            {
                issues.Add($"Sideboard has {sideboardCount} cards (maximum is {DeckFormatRules.MaxConstructedSideboardCards}).");
            }
        }

        if (issues.Count == 0)
            return ValidationResult.Success();

        return ValidationResult.Warning(string.Join(" ", issues));
    }

    /// <summary>
    /// Validates that all cards currently in a commander-style deck obey the commander's color identity.
    /// Returns a soft warning listing offending cards; does not block changes.
    /// When <paramref name="cardsByUuid"/> is provided (e.g. from <see cref="ICardRepository.GetCardsAsync"/>),
    /// avoids one DB fetch per deck row.
    /// </summary>
    /// <param name="preResolvedCommanderDeckIdentity">When set, skips commander/partner/commander-row resolution.</param>
    public async Task<ValidationResult> ValidateDeckColorIdentityAsync(
        DeckEntity deck,
        List<DeckCardEntity> currentCards,
        IReadOnlyDictionary<string, Card>? cardsByUuid = null,
        ColorIdentity? preResolvedCommanderDeckIdentity = null)
    {
        var format = EnumExtensions.ParseDeckFormat(deck.Format);
        if (!DeckFormatRules.IsCommanderLike(format))
            return ValidationResult.Success();

        if (string.IsNullOrEmpty(deck.CommanderId))
            return ValidationResult.Success();

        ColorIdentity commanderIdentity;
        if (preResolvedCommanderDeckIdentity.HasValue)
        {
            commanderIdentity = preResolvedCommanderDeckIdentity.Value;
        }
        else
        {
            var (identity, had, warning) = await DeckColorIdentityResolver
                .TryResolveCommanderDeckColorIdentityAsync(_cardRepository, deck, currentCards, cardsByUuid)
                .ConfigureAwait(false);
            if (warning != null)
                return warning;
            if (!had)
                return ValidationResult.Success();

            commanderIdentity = identity;
        }

        var offendingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in currentCards)
        {
            if (string.Equals(entity.Section, DeckCardSections.Commander, StringComparison.OrdinalIgnoreCase))
                continue;

            if (entity.Quantity <= 0)
                continue;

            Card? card = null;
            if (cardsByUuid != null && cardsByUuid.TryGetValue(entity.CardId, out var fromMap))
                card = fromMap;
            else
                card = await _cardRepository.GetCardDetailsAsync(entity.CardId).ConfigureAwait(false);
            if (card == null || string.IsNullOrEmpty(card.Uuid))
                continue;

            var cardIdentity = card.GetColorIdentity();
            if (!commanderIdentity.Contains(cardIdentity))
            {
                offendingNames.Add(card.Name);
            }
        }

        if (offendingNames.Count == 0)
            return ValidationResult.Success();

        string commanderColors = commanderIdentity.AsString();
        int maxNames = DeckValidationConstants.MaxOffendingNamesInMessage;
        string list = string.Join(", ", offendingNames.Take(maxNames));
        if (offendingNames.Count > maxNames)
            list += ", ...";

        string msg = $"Deck has {offendingNames.Count} card(s) outside the commander's color identity ({commanderColors}): {list}.";
        return ValidationResult.Warning(msg);
    }
}
