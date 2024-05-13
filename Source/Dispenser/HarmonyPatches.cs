using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace Dispenser
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("localghost.dispenser");
            var transpiler = new HarmonyMethod(Method("Dispenser.HarmonyPatches:Transpiler"));
            var foodUtilityTranspiler = new HarmonyMethod(
                Method("Dispenser.HarmonyPatches:FoodUtilityTranspiler")
            );
            var feederTranspiler = new HarmonyMethod(
                Method("Dispenser.HarmonyPatches:FeederTranspiler")
            );
            var customfoodTranspiler = new HarmonyMethod(
                Method("Dispenser.HarmonyPatches:CustomFoodTranspiler")
            );
            var prefix = new HarmonyMethod(Method("Dispenser.HarmonyPatches:JobDriverPrefix"));
            var postfix = new HarmonyMethod(Method("Dispenser.HarmonyPatches:Postfix"));
            new List<MethodInfo>
            {
                Method("RimWorld.JobDriver_FoodDeliver:GetReport"),
                Method("RimWorld.JobDriver_FoodFeedPatient:GetReport"),
                Method("RimWorld.JobDriver_Ingest:GetReport"),
            }.ForEach(info => harmony.Patch(info, prefix: prefix));
            harmony.Patch(
                Method("RimWorld.Building_NutrientPasteDispenser:TryDispenseFood"),
                postfix: postfix
            );
            harmony.Patch(
                Method(
#if v1_4
                    "RimWorld.FoodUtility+<>c__DisplayClass19_0:<BestFoodSourceOnMap_NewTemp>b__0"
#else
                    "RimWorld.FoodUtility+<>c__DisplayClass14_0:<BestFoodSourceOnMap>b__0"
#endif
                ),
                transpiler: foodUtilityTranspiler
            );
            var dispenserMethods = new List<MethodInfo>
            {
                PropertyGetter("RimWorld.Building_NutrientPasteDispenser:DispensableDef"),
                Method("RimWorld.Building_NutrientPasteDispenser:TryDispenseFood")
            };
            if (Contains("vanillaexpanded.vnutriente"))
                dispenserMethods = dispenserMethods
                    .Concat(
                        new List<MethodInfo>()
                        {
                            Method("VNPE.Building_Dripper:TickRare"),
                            Method("VNPE.Building_NutrientPasteDispenser_GetGizmos:TryDropFood"),
                            Method("VNPE.Building_NutrientPasteDispenser_TryDispenseFood:Prefix"),
                            Method("VNPE.Building_NutrientPasteTap:TryDispenseFoodOverride"),
                            Method("VNPE.Building_NutrientPasteTap:TryDropFood")
                        }
                    )
                    .ToList();
            dispenserMethods.ForEach(method => harmony.Patch(method, transpiler: transpiler));
            if (Contains("vanillaexpanded.vnutriente"))
                harmony.Patch(
                    Method("VNPE.CompConvertToThing_OutputResource:Prefix"),
                    transpiler: feederTranspiler
                );
            if (Contains("mlie.npdtiers"))
                harmony.Patch(
                    Method("NutrientPasteTiers.NPDHarmony:TryDispenseCustomFood"),
                    transpiler: customfoodTranspiler
                );
        }

        static bool Contains(string packageId) =>
            ModLister.GetActiveModWithIdentifier(packageId, true) != null;
    }

    public class HarmonyPatches
    {
        public static IEnumerable<CodeInstruction> FoodUtilityTranspiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            foreach (var instruction in instructions)
            {
                if (
                    instruction.opcode == OpCodes.Ldsfld
                    && (FieldInfo)instruction.operand
                        == Field("RimWorld.ThingDefOf:MealNutrientPaste")
                )
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        Method(
                            "Verse.ThingWithComps:GetComp",
                            generics: new[] { typeof(CompSetMeal) }
                        )
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        PropertyGetter("Dispenser.CompSetMeal:Meal")
                    );
                }
                else
                    yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            foreach (var instruction in instructions)
            {
                if (
                    instruction.opcode == OpCodes.Ldsfld
                    && (FieldInfo)instruction.operand
                        == Field("RimWorld.ThingDefOf:MealNutrientPaste")
                )
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        Method(
                            "Verse.ThingWithComps:GetComp",
                            generics: new[] { typeof(CompSetMeal) }
                        )
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        PropertyGetter("Dispenser.CompSetMeal:Meal")
                    );
                }
                else
                    yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> FeederTranspiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            foreach (var instruction in instructions)
            {
                if (
                    instruction.opcode == OpCodes.Ldsfld
                    && (FieldInfo)instruction.operand
                        == Field("RimWorld.ThingDefOf:MealNutrientPaste")
                )
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        Method(
                            "Verse.ThingWithComps:GetComp",
                            generics: new[] { typeof(CompSetMeal) }
                        )
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        PropertyGetter("Dispenser.CompSetMeal:Meal")
                    );
                }
                else
                {
                    if (
                        instruction.opcode == OpCodes.Stfld
                        && (FieldInfo)instruction.operand == Field("Verse.Thing:stackCount")
                    )
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(
                            OpCodes.Call,
                            Method(
                                "Verse.ThingWithComps:GetComp",
                                generics: new[] { typeof(CompSetMeal) }
                            )
                        );
                        yield return new CodeInstruction(
                            OpCodes.Call,
                            PropertyGetter("Dispenser.CompSetMeal:StackCount")
                        );
                        yield return new CodeInstruction(OpCodes.Mul);
                    }
                    yield return instruction;
                }
            }
        }

        public static IEnumerable<CodeInstruction> CustomFoodTranspiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var list = instructions.ToList();
            for (var i = 0; i < list.Count(); ++i)
            {
                var instruction = list[i];
                if (
                    i + 4 < list.Count()
                    && list[i + 4].operand is MethodInfo method
                    && method == Method("Verse.ThingMaker:MakeThing")
                )
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        Method(
                            "Verse.ThingWithComps:GetComp",
                            generics: new[] { typeof(CompSetMeal) }
                        )
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        PropertyGetter("Dispenser.CompSetMeal:Meal")
                    );
                    i += 2;
                }
                else
                    yield return instruction;
                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        Method(
                            "Verse.ThingWithComps:GetComp",
                            generics: new[] { typeof(CompSetMeal) }
                        )
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        PropertyGetter("Dispenser.CompSetMeal:StackCount")
                    );
                    yield return new CodeInstruction(
                        OpCodes.Stfld,
                        Field("Verse.Thing:stackCount")
                    );
                }
            }
        }

        public static void Postfix(Building __instance, ref Thing __result) =>
            __result.stackCount = __instance.GetComp<CompSetMeal>().StackCount;

        public static bool JobDriverPrefix(ref JobDriver __instance, ref string __result)
        {
            var job = __instance.job;
            var thing = job.targetA.Thing;
            var meal = thing is Building_NutrientPasteDispenser dispenser
                ? dispenser.GetComp<CompSetMeal>().Meal
                : thing.def;
            if (meal?.ingestible != null)
            {
                if (job.targetB.Thing is Pawn deliveree)
                    __result = JobUtility.GetResolvedJobReportRaw(
                        job.def.reportString,
                        meal.label,
                        meal,
                        deliveree.LabelShort,
                        deliveree,
                        "",
                        ""
                    );
                else
                    __result = JobUtility.GetResolvedJobReportRaw(
                        job.def.reportString,
                        meal.label,
                        meal,
                        "",
                        "",
                        "",
                        ""
                    );
            }
            return __result == null;
        }
    }
}
