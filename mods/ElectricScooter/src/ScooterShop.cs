using System;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace ElectricScooter;

/// <summary>Adds the scooter to the skate shop: one more option in the seller's dialogue.</summary>
internal static class ScooterShop
{
    private const float CheckInterval = 3f;

    private static float _nextCheck;
    private static bool _failed;

    public static void Tick()
    {
        if (_failed || Time.unscaledTime < _nextCheck)
            return;
        _nextCheck = Time.unscaledTime + CheckInterval;
        var definition = ScooterItem.Definition;
        if (definition == null || S1.PlayerScripts.Player.Local == null)
            return;
        try
        {
            foreach (var seller in UnityQuery.FindInScenes<S1.Dialogue.DialogueController_SkateboardSeller>())
                Stock(seller, definition);
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
}
