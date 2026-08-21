using HarmonyLib;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace FakeSupporter
{
    /// <summary>
    /// Removes the hard-coded <c>Math.Clamp(value, 0, 3)</c> inside
    /// <c>SupporterIcon.set_SupportLevel</c> so the fake supporter can render more than three hearts.
    /// </summary>
    public sealed class SupporterIconSupportLevelPatch : PluginPatch<FakeSupporterPlugin>
    {
        private static readonly MethodInfo clampMethod = typeof(Math).GetMethod(nameof(Math.Clamp), new[] { typeof(int), typeof(int), typeof(int) })!;

        public SupporterIconSupportLevelPatch(FakeSupporterPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Profile.Header.Components.SupporterIcon", "set_SupportLevel", osucc.Core.MethodType.Transpiler)
        {
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
                    TimingLog.Info("clamp removed");
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
