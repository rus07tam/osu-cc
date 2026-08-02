using HarmonyLib;
using osucc.Core;
using System.Reflection;
using System.Reflection.Emit;

namespace osucc.Patches
{
    /// <summary>
    /// Removes the hard-coded <c>Math.Clamp(value, 0, 3)</c> inside
    /// <c>SupporterIcon.set_SupportLevel</c> so the fake supporter can render more than three
    /// hearts. The transpiler nops the clamp call and its two bound constants, leaving the raw
    /// level on the stack for the field store.
    /// </summary>
    public static class SupporterIconSupportLevelPatch
    {
        public static bool Install()
        {
            var setter = Reflection.GetGameType("osu.Game.Overlays.Profile.Header.Components.SupporterIcon")?.GetProperty("SupportLevel")?.GetSetMethod(true);

            if (setter == null)
            {
                TimingLog.Error("SupporterIconSupportLevelPatch: SupporterIcon.SupportLevel setter not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.supporter.icon").Patch(setter, transpiler: Reflection.HarmonyMethod(typeof(SupporterIconSupportLevelPatch), nameof(Transpiler)));
            TimingLog.Info("SupporterIcon.set_SupportLevel patched (transpiler)");
            return true;
        }

        private static readonly MethodInfo clampMethod = typeof(Math).GetMethod(nameof(Math.Clamp), new[] { typeof(int), typeof(int), typeof(int) })!;

        private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || !Equals(codes[i].operand, clampMethod))
                    continue;

                // `ldc.i4 0; ldc.i4 3; call Clamp` — drop the two bounds and the call itself,
                // leaving the already-pushed `value` for the following local/field store.
                if (i >= 2 && isIntegerConstant(codes[i - 2]) && isIntegerConstant(codes[i - 1]))
                {
                    codes[i - 2] = new CodeInstruction(OpCodes.Nop);
                    codes[i - 1] = new CodeInstruction(OpCodes.Nop);
                    codes[i] = new CodeInstruction(OpCodes.Nop);
                    TimingLog.Info("SupporterIconSupportLevelPatch: clamp removed");
                    break;
                }
            }

            return codes;
        }

        private static bool isIntegerConstant(CodeInstruction instruction)
            => instruction.opcode == OpCodes.Ldc_I4_M1
               || instruction.opcode == OpCodes.Ldc_I4_0
               || instruction.opcode == OpCodes.Ldc_I4_1
               || instruction.opcode == OpCodes.Ldc_I4_2
               || instruction.opcode == OpCodes.Ldc_I4_3
               || instruction.opcode == OpCodes.Ldc_I4_4
               || instruction.opcode == OpCodes.Ldc_I4_5
               || instruction.opcode == OpCodes.Ldc_I4_6
               || instruction.opcode == OpCodes.Ldc_I4_7
               || instruction.opcode == OpCodes.Ldc_I4_8
               || (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte)
               || (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int);
    }
}
