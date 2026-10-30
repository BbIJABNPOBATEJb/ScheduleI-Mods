using System;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace ElectricScooter;

/// <summary>
/// Adds the scooter to the skate shop: one more option in the seller's dialogue.
///
/// Finding the seller means going through every loaded object, which takes a noticeable moment on a
/// big save, so it is done once per scene: the seller found is kept until its scene unloads. Where no
/// seller turns up (a scene without the shop), the searches get rarer.
/// </summary>
internal static class ScooterShop
{
    private const float CheckInterval = 3f;
    private const float MaxSearchInterval = 60f;

    private static S1.Dialogue.DialogueController_SkateboardSeller? _seller;
    private static float _nextCheck;
    private static float _nextSearch;
    private static float _searchInterval = CheckInterval;
    private static bool _failed;

    public static void Tick()
    {
        var now = Time.unscaledTime;
        if (_failed || now < _nextCheck)
            return;
        _nextCheck = now + CheckInterval;
        if (_seller != null)
            return;
        if ((object?)_seller != null)
        {
            // Destroyed with its scene: the next scene has its own seller.
            _seller = null;
            _nextSearch = 0f;
            _searchInterval = CheckInterval;
        }
        var definition = ScooterItem.Definition;
        if (definition == null || S1.PlayerScripts.Player.Local == null || now < _nextSearch)
            return;
        _nextSearch = now + _searchInterval;
        _searchInterval = Mathf.Min(_searchInterval * 2f, MaxSearchInterval);
#if DEV
        Searches++;
#endif
        try
        {
            foreach (var seller in UnityQuery.FindInScenes<S1.Dialogue.DialogueController_SkateboardSeller>())
            {
                Stock(seller, definition);
                _seller = seller;
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Error($"Electric Scooter could not be added to the skate shop: {ex}");
        }
    }

    private static void Stock(S1.Dialogue.DialogueController_SkateboardSeller seller, S1.ItemFramework.StorableItemDefinition definition)
    {
        var options = seller.Options;
        for (var i = 0; i < options.Count; i++)
        {
            var existing = options[i];
            if (existing.Item == null || existing.Item.ID != ScooterItem.Id)
                continue;
            existing.Price = Config.Price;
            return;
        }
        options.Add(new S1.Dialogue.DialogueController_SkateboardSeller.Option
        {
            Name = ScooterItem.DisplayName,
            Price = Config.Price,
            IsAvailable = true,
            NotAvailableReason = "",
            Item = definition,
        });
        MelonLogger.Msg($"Electric Scooter is now sold at the skate shop for ${Config.Price:0}");
    }

#if DEV
    /// <summary>How many times the shop searched the loaded objects for the seller (should stay at 1 per scene).</summary>
    public static int Searches;
#endif
}
