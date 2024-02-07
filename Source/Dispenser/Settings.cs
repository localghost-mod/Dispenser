using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dispenser
{
    public class Settings : ModSettings
    {
        public IEnumerable<ThingDef> Allmeals =>
            DefDatabase<ThingDef>.AllDefsListForReading.Where(
                def => def.GetCompProperties<CompProperties_Ingredients>() != null
            );
        private List<string> _forbiddenMeals;
        public IEnumerable<ThingDef> ForbiddenMeals
        {
            get
            {
                if (_forbiddenMeals == null)
                    _forbiddenMeals = new List<string>();
                return _forbiddenMeals.Select(
                    name => DefDatabase<ThingDef>.GetNamedSilentFail(name)
                );
            }
        }
        public IEnumerable<ThingDef> AvaiableMeals => Allmeals.Except(ForbiddenMeals);

        private Dictionary<string, string> _defaultMeal = new Dictionary<string, string>();
        public IEnumerable<ThingDef> dispenserDefs =
            DefDatabase<ThingDef>.AllDefsListForReading.Where(
                def => def.HasComp(typeof(CompSetMeal))
            );

        public ThingDef DefaultMeal(ThingDef def)
        {
            var meal = _defaultMeal.GetWithFallback(def.defName);
            if (meal == null || DefDatabase<ThingDef>.GetNamedSilentFail(meal) == null)
                _defaultMeal[def.defName] = ThingDefOf.MealNutrientPaste.defName;
            return DefDatabase<ThingDef>.GetNamed(_defaultMeal[def.defName]);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref _forbiddenMeals, "forbiddenMeals", LookMode.Value);
            Scribe_Collections.Look(
                ref _defaultMeal,
                "defaultMeal",
                LookMode.Value,
                LookMode.Value
            );
        }

        private Vector2 scrollPosition = Vector2.zero;

        public void DoWindowContents(Rect inRect)
        {
            var curY = 40f;
            var width = inRect.width / 2;
            var meals = Allmeals.Except(ForbiddenMeals).Concat(ForbiddenMeals);
            var height = 32f;
            var margin = (height - Text.LineHeight) / 2;
            var scrollviewHeight = meals.Count() * height;
            var toggledmeals = new List<ThingDef>();
            Widgets.Label(
                new Rect(0f, curY, width - 20f, height),
                "Dispenser.MealRestriction".Translate()
            );
            TooltipHandler.TipRegion(
                new Rect(0f, curY, width - 20f, height),
                "Dispenser.MealRestrictionTooltip".Translate()
            );
            curY += height;
            Widgets.BeginScrollView(
                new Rect(0f, curY, width - 20f, inRect.height * 0.618f),
                ref scrollPosition,
                new Rect(0f, curY, width - 40f, scrollviewHeight)
            );
            foreach (var meal in meals)
                if (meal != null)
                {
                    var label = meal.label;
                    var icon = meal.uiIcon;
                    var forbidden = ForbiddenMeals.Contains(meal);

                    var color = forbidden ? Color.gray : Color.white;
                    var iconRect = new Rect(0f, curY, height - 4f, height - 4f);
                    var labelRect = new Rect(40f, curY + margin, width - 60f, height - margin);
                    var buttonRect = new Rect(0f, curY, width - 20f, height);
                    if (Mouse.IsOver(buttonRect))
                        GUI.DrawTexture(buttonRect, TexUI.HighlightTex);
                    GUI.DrawTexture(iconRect, icon);
                    Widgets.Label(labelRect, label.Colorize(color));
                    curY += height;
                    if (meal == ThingDefOf.MealNutrientPaste)
                        continue;
                    if (Widgets.ButtonInvisible(buttonRect))
                    {
                        if (toggledmeals.Contains(meal))
                            toggledmeals.Remove(meal);
                        else
                            toggledmeals.Add(meal);
                    }
                }
            Widgets.EndScrollView();
            var resetbuttonRect = new Rect(0f, inRect.height * 0.618f + 80f, width - 20f, height);
            if (Widgets.ButtonText(resetbuttonRect, "ResetButton".Translate()))
                _forbiddenMeals = null;
            curY = 40f;
            Widgets.Label(
                new Rect(width, curY, width - 20f, height),
                "Dispenser.DefaultMeal".Translate()
            );
            TooltipHandler.TipRegion(
                new Rect(width, curY, width - 20f, height),
                "Dispenser.DefaultMealTooltip".Translate()
            );
            curY += height;
            foreach (var def in dispenserDefs)
            {
                var icon = def.uiIcon;
                var label = def.label;
                var meal = DefaultMeal(def);
                var iconRect = new Rect(width, curY, height - 4f, height - 4f);
                var labelRect = new Rect(width + 40f, curY + margin, width - 40f, height - margin);
                var buttonRect = new Rect(width * 2 - 160f, curY, 160f, height);
                GUI.DrawTexture(iconRect, icon);
                Widgets.Label(labelRect, label);
                if (Widgets.ButtonText(buttonRect, meal.label))
                    Find.WindowStack.Add(
                        new FloatMenu(
                            Allmeals
                                .Except(ForbiddenMeals)
                                .Select(
                                    _meal =>
                                        new FloatMenuOption(
                                            _meal.label,
                                            () => _defaultMeal[def.defName] = _meal.defName
                                        )
                                )
                                .ToList()
                        )
                    );
                curY += height;
            }

            foreach (var meal in toggledmeals)
                if (_forbiddenMeals.Contains(meal.defName))
                    _forbiddenMeals.Remove(meal.defName);
                else
                    _forbiddenMeals.Add(meal.defName);
            foreach (var def in dispenserDefs)
                if (ForbiddenMeals.Contains(DefaultMeal(def)))
                    _defaultMeal.Remove(def.defName);
        }
    }

    public class DispenserMod : Mod
    {
        public static Settings settings;

        public DispenserMod(ModContentPack content)
            : base(content) => settings = GetSettings<Settings>();

        public override void DoSettingsWindowContents(Rect inRect) =>
            settings.DoWindowContents(inRect);

        public override string SettingsCategory() => "Dispenser.DispenserMod".Translate();
    }
}
