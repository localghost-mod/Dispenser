using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Dispenser
{
    public class CompProperties_SetMeal : CompProperties
    {
        public CompProperties_SetMeal() => compClass = typeof(CompSetMeal);
    }

    public class CompSetMeal : ThingComp
    {
        private ThingDef _meal;
        public ThingDef Meal
        {
            get
            {
                if (_meal == null)
                {
                    _meal = AvailableMeals.Contains(DispenserMod.settings.DefaultMeal(parent.def))
                        ? DispenserMod.settings.DefaultMeal(parent.def)
                        : ThingDefOf.MealNutrientPaste;

                    if (parent is Building_Storage storage)
                    {
                        storage.settings.filter.SetDisallowAll();
                        storage.settings.filter.SetAllow(_meal, true);
                    }
                }
                return _meal;
            }
            set
            {
                _meal = value;
                if (parent is Building_Storage storage)
                {
                    storage.settings.filter.SetDisallowAll();
                    storage.settings.filter.SetAllow(_meal, true);
                }
            }
        }
        public int StackCount =>
            (int)Math.Round(0.6f / Meal.GetStatValueAbstract(StatDefOf.Nutrition));
        public static IEnumerable<ThingDef> AvailableMeals =>
            DispenserMod
                .settings.Allmeals.Except(DispenserMod.settings.ForbiddenMeals)
                .Where(
                    def =>
                        MakeableByPlayer(def)
                        && (
                            DefDatabase<ResearchProjectDef>.GetNamed("FoodSynthesis").IsFinished
                            || def.ingestible.preferability <= FoodPreferability.MealSimple
                        )
                );

        public static bool MakeableByPlayer(ThingDef def) =>
            def == ThingDefOf.MealNutrientPaste
            || DefDatabase<RecipeDef>.AllDefsListForReading.Any(
                recipe =>
                    recipe.products.Any(defcount => defcount.thingDef == def)
                    && recipe.AvailableNow
                    && recipe.AllRecipeUsers.Any(
                        user =>
                            user.researchPrerequisites?.All(research => research.IsFinished) ?? true
                    )
            );

        public override void PostExposeData() => Scribe_Defs.Look(ref _meal, "meal");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = Meal.label,
                defaultDesc = "Dispenser.GizmoDesc".Translate(),
                icon = Meal.uiIcon,
                action = () =>
                    Find.WindowStack.Add(
                        new FloatMenu(
                            AvailableMeals
                                .Select(meal => new FloatMenuOption(meal.label, () => Meal = meal))
                                .ToList()
                        )
                    )
            };
        }
    }
}
