using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using ModsCommon.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using UnityEngine;

namespace AdvancedStopSelection
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override ulong StableWorkshopId => 2862973068;
        protected override ulong BetaWorkshopId => 0;

        public override string NameRaw => "Advanced Stop Selection";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        public override List<ModVersion> Versions => new List<ModVersion>()
        {
            new ModVersion(new Version(2, 1), new DateTime(2023, 4, 6)),
            new ModVersion(new Version(2, 0), new DateTime(2022, 9, 14)),
        };
        protected override Version RequiredGameVersion => new Version(1, 17, 1, 2);

        protected override string IdRaw => nameof(AdvancedStopSelection);
        protected override List<BaseDependencyInfo> DependencyInfos
        {
            get
            {
                var infos = base.DependencyInfos;

                var oldLocalSearcher = IdSearcher.Invalid & new UserModNameSearcher("Advanced Stop Selection", BaseMatchSearcher.Option.None);
                var oldIdSearcher = new IdSearcher(1394468624u);
                infos.Add(new ConflictDependencyInfo(DependencyState.Unsubscribe, oldLocalSearcher | oldIdSearcher, "Original Advanced Stop Selection"));

                return infos;
            }
        }

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        protected override LocalizeManager LocalizeManager => Localize.LocaleManager;

        #endregion

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = AddTranspiler(typeof(Patcher), nameof(Patcher.TransportToolGetStopPositionTranspiler), typeof(TransportTool), "GetStopPosition");
            return success;
        }

        #endregion
    }

    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> TransportToolGetStopPositionTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var alternateModeLocal = generator.DeclareLocal(typeof(bool));
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetAlternateMode)));
            yield return new CodeInstruction(OpCodes.Stloc, alternateModeLocal);

            bool segmentNotZeroPassed = false;
            Label segmentElseLabel = default;
            bool buildingCheckPatched = false;
            bool transportLine1CheckPatched = false;
            bool transportLine2CheckPatched = false;
            CodeInstruction prevInstruction = null;
            CodeInstruction prevPrevInstruction = null;
            var segmentArg = original.GetLDArg("segment");
            var buildingArg = original.GetLDArg("building");
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if(!segmentNotZeroPassed)
                {
                    if(prevPrevInstruction != null && prevPrevInstruction.opcode == OpCodes.Ret && prevInstruction != null && prevInstruction.opcode == segmentArg.opcode && prevInstruction.operand == segmentArg.operand && instruction.opcode == OpCodes.Brfalse)
                    {
                        segmentNotZeroPassed = true;
                        segmentElseLabel = (Label)instruction.operand;
                    }
                }
                else
                {
                    if (!transportLine1CheckPatched && prevInstruction != null && prevInstruction.opcode == OpCodes.Ldloc_S && prevInstruction.operand is LocalBuilder local1 && local1.LocalIndex == 12 && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine1CheckPatched = true;
                    }

                    if (!transportLine2CheckPatched && prevInstruction != null && prevInstruction.opcode == OpCodes.Ldloc_S && prevInstruction.operand is LocalBuilder local2 && local2.LocalIndex == 13 && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine2CheckPatched = true;
                    }

                    if (!buildingCheckPatched && prevInstruction != null && prevInstruction.labels.Contains(segmentElseLabel) && prevInstruction.opcode == buildingArg.opcode && prevInstruction.operand == buildingArg.operand && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        buildingCheckPatched = true;
                    }
                }

                prevPrevInstruction = prevInstruction;
                prevInstruction = instruction;
            }
        }
        private static bool GetAlternateMode()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
    }

    public class Settings : BaseSettings<Mod>
    {
        protected override void FillSettings()
        {
            base.FillSettings();
            AddNotifications(GeneralTab);
        }
    }
}