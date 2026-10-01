using System;
using System.IO;
using System.Reflection;
using MelonLoader;
using S1Shared;
using UnityEngine;

namespace ElectricScooter;

/// <summary>
/// The "Electric Scooter" item: a copy of the Golden Skateboard's definition with its own id, name,
/// icon and price. It shares the golden board's prefabs; the ride itself is re-tuned and re-dressed
/// when the player mounts it (see <see cref="ScooterRide"/>).
/// </summary>
internal static class ScooterItem
{
    /// <summary>Stored in saves — never change.</summary>
    public const string Id = "electricscooter";
    public const string DisplayName = "Electric Scooter";
    public const string Description =
        "Electric kick scooter with big wheels. Rolls over curbs, never wears you out and outruns any skateboard.";

    private const string SourceId = "goldenskateboard";

    private static S1.ItemFramework.StorableItemDefinition? _definition;
    private static bool _failed;

    public static S1.ItemFramework.StorableItemDefinition? Definition => _definition;

    public static bool Is(S1.ItemFramework.ItemInstance? item) => item != null && item.ID == Id;

    /// <summary>
    /// Keeps the item in the game's registry. Called every frame: the registry drops items added at
    /// runtime whenever a scene changes, and saves are loaded right after.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_failed)
            return;
        var registry = S1.Registry.Instance;
        if (registry == null)
            return;
        try
        {
            if (_definition == null && !Create())
                return;
            if (!S1.Registry.ItemExists(Id))
                registry.AddToRegistry(_definition);
        }
        catch (Exception ex)
        {
            _failed = true;
            MelonLogger.Error($"Electric Scooter item could not be registered: {ex}");
        }
    }

    private static bool Create()
    {
        if (!UnityQuery.TryCastTo<S1.ItemFramework.StorableItemDefinition>(S1.Registry.Instance._GetItem(SourceId, false), out var golden))
            return false;
        var definition = UnityEngine.Object.Instantiate(golden);
        definition.name = "ElectricScooter";
        definition.hideFlags = HideFlags.DontUnloadUnusedAsset;
        definition.ID = Id;
        definition.Name = DisplayName;
        definition.Description = Description;
        definition.BasePurchasePrice = Config.Price;
        var icon = LoadIcon();
        if (icon != null)
            definition.Icon = icon;
        _definition = definition;
        MelonLogger.Msg("Electric Scooter item created");
        return true;
    }

    private static Sprite? LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ElectricScooter.item-icon.png");
        if (stream == null)
            return null;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = "ElectricScooter Icon",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset,
        };
        if (!ImageConversion.LoadImage(texture, memory.ToArray(), true))
            return null;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }
}
