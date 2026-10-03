using System;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using Polyglot.Localization;
using S1Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Polyglot.Text;

/// <summary>
/// The item tooltip of a product lists its effects in two columns of 80 px. Translated effect names
/// are often wider ("Антигравитационный", "Вызывающий припадки") and ran into the other column.
/// When they do not fit, the list becomes one column over the tooltip's full width, and the tooltip
/// grows by the extra rows (the panel sizes itself from the content's Height after Initialize).
/// </summary>
internal static class ProductTooltipFit
{
    private static bool _failed;

    [HarmonyPatch(typeof(S1.UI.Items.ProductItemInfoContent), nameof(S1.UI.Items.ProductItemInfoContent.Initialize),
        typeof(S1.ItemFramework.ItemDefinition))]
    private static class InitializePatch
    {
        private static void Postfix(S1.UI.Items.ProductItemInfoContent __instance)
        {
            if (_failed || !Translator.Active)
                return;
            try
            {
                Fit(__instance);
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Warning($"Product tooltips could not be fitted to translated effects: {ex}");
            }
        }
    }

    private static void Fit(S1.UI.Items.ProductItemInfoContent content)
    {
        var labels = UnityQuery.ToManaged(content.PropertyLabels).Where(l => l != null && l.enabled).ToList();
        if (labels.Count < 2)
            return;
        var grid = UiKit.Get<GridLayoutGroup>(labels[0].transform.parent);
        if (grid == null)
            return;
        var columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? grid.constraintCount : 2;
        if (columns < 2)
            return;
        var cell = grid.cellSize;
        var widest = labels.Max(l => l.GetPreferredValues(Translator.Translate(l.text), float.PositiveInfinity, float.PositiveInfinity).x);
        // A little shrinking (the game's own labels auto-size) is fine; more is not readable.
        if (widest <= cell.x * 1.1f)
            return;

        var rowsBefore = (labels.Count + columns - 1) / columns;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.cellSize = new Vector2(cell.x * columns + grid.spacing.x * (columns - 1), cell.y);
        content.Height += (labels.Count - rowsBefore) * (cell.y + grid.spacing.y);
    }
}
