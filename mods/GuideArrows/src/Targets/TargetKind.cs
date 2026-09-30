using UnityEngine;

namespace GuideArrows.Targets;

/// <summary>What an arrow can point at, in display order.</summary>
internal enum TargetKind
{
    Deal = 0,
    Stash = 1,
    Quest = 2,
    Customer = 3,
    Home = 4,
}

internal static class TargetKinds
{
    public static readonly TargetKind[] All =
        { TargetKind.Deal, TargetKind.Stash, TargetKind.Quest, TargetKind.Customer, TargetKind.Home };

    public static string Name(TargetKind kind) => kind switch
    {
        TargetKind.Deal => "Deals",
        TargetKind.Stash => "Stashes",
        TargetKind.Quest => "Quests",
        TargetKind.Customer => "Potential customers",
        TargetKind.Home => "Home base",
        _ => kind.ToString(),
    };

    public static string Hint(TargetKind kind) => kind switch
    {
        TargetKind.Deal => "Customers waiting for a delivery you accepted.",
        TargetKind.Stash => "Dead drops with items in them (e.g. an order a supplier delivered).",
        TargetKind.Quest => "The current objectives of active quests.",
        TargetKind.Customer => "People you can win over with a free sample (friends of your customers).",
        TargetKind.Home => "Your main property: where you last slept, or the one with the most valuable equipment.",
        _ => "",
    };

    public static int DefaultColor(TargetKind kind) => kind switch
    {
        TargetKind.Deal => Palette.Green,
        TargetKind.Stash => Palette.Red,
        TargetKind.Quest => Palette.Orange,
        TargetKind.Customer => Palette.Purple,
        TargetKind.Home => Palette.Blue,
        _ => Palette.White,
    };
}

internal static class Palette
{
    public const int Green = 0, Red = 1, Orange = 2, Yellow = 3, Purple = 4, Blue = 5, Cyan = 6, Pink = 7, White = 8;

    public static readonly string[] Names = { "Green", "Red", "Orange", "Yellow", "Purple", "Blue", "Cyan", "Pink", "White" };

    private static readonly Color[] Colors =
    {
        new(0.24f, 0.86f, 0.36f),
        new(0.95f, 0.22f, 0.2f),
        new(1f, 0.6f, 0.1f),
        new(1f, 0.87f, 0.15f),
        new(0.66f, 0.33f, 0.97f),
        new(0.2f, 0.55f, 1f),
        new(0.2f, 0.88f, 0.95f),
        new(1f, 0.4f, 0.72f),
        new(0.93f, 0.93f, 0.93f),
    };

    public static Color Get(int index) => Colors[Mathf.Clamp(index, 0, Colors.Length - 1)];
}

/// <summary>A place an arrow can point at. Moving targets (people) follow their transform.</summary>
internal sealed class Target
{
    public TargetKind Kind;
    public string Key = "";
    public string Label = "";
    public Transform? Anchor;
    public Vector3 Fixed;

    public Vector3 Position => Anchor != null ? Anchor.position : Fixed;
}
