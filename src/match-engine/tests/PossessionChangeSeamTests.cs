// W8 pre-B: structural guard for the single live possession-identity seam.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using NUnit.Framework;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class PossessionChangeSeamTests
    {
        private static readonly Dictionary<ushort, OpCode> OpCodesByValue = BuildOpCodeMap();
        private int _addressScannerProbe;

        [Test]
        public void PossessionIdentityWriters_MatchExplicitInventory()
        {
            FieldInfo holderField = typeof(MatchEngine).GetField(
                "_possessingAgentId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(holderField, "MatchEngine._possessingAgentId disappeared or changed visibility.");

            var actual = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Type type in typeof(MatchEngine).Assembly.GetTypes())
            {
                foreach (MethodBase method in DeclaredMethodsAndConstructors(type))
                {
                    int addresses = CountFieldOpcode(method, holderField, OpCodes.Ldflda);
                    Assert.Zero(
                        addresses,
                        $"Address-taking _possessingAgentId is forbidden in {type.FullName}.{method.Name}; " +
                        "a by-ref/indirect write would bypass SetPossessingAgent and the writer inventory.");

                    int stores = CountFieldOpcode(method, holderField, OpCodes.Stfld);
                    if (stores == 0)
                    {
                        continue;
                    }

                    string key = type.FullName + "." + method.Name;
                    actual.TryGetValue(key, out int previous);
                    actual[key] = previous + stores;
                }
            }

            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                // Boot initialization + opening-kickoff award: no live keeper hand episode can exist yet.
                ["TacticalDirector.MatchEngine.MatchEngine..ctor"] = 2,

                // Restore reconstructs saved state; it is not a gameplay possession transition.
                ["TacticalDirector.MatchEngine.MatchEngine.DeserializeWorldState"] = 1,

                // Test-only world forcing remains explicit and outside production writer policy.
                ["TacticalDirector.MatchEngine.MatchEngine.TestOnly_ForceBallLoose"] = 1,

                // Sole ordinary mid-match direct store. All six production mutation sites call this seam.
                ["TacticalDirector.MatchEngine.MatchEngine.SetPossessingAgent"] = 1,
            };

            CollectionAssert.AreEquivalent(
                expected,
                actual,
                "Direct _possessingAgentId writer inventory drifted. Classify any new writer explicitly; " +
                "ordinary mid-match possession changes must route through SetPossessingAgent.");
        }

        [Test]
        public void FieldOpcodeScanner_DetectsAddressTaking()
        {
            FieldInfo probeField = typeof(PossessionChangeSeamTests).GetField(
                "_addressScannerProbe",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo probeMethod = typeof(PossessionChangeSeamTests).GetMethod(
                nameof(AddressScannerProbe),
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(probeField);
            Assert.IsNotNull(probeMethod);
            Assert.That(CountFieldOpcode(probeMethod, probeField, OpCodes.Ldflda), Is.EqualTo(1));
            Assert.That(CountFieldOpcode(probeMethod, probeField, OpCodes.Stfld), Is.EqualTo(0));
        }

        private void AddressScannerProbe()
        {
            ConsumeRef(ref _addressScannerProbe);
        }

        private static void ConsumeRef(ref int value)
        {
            // Intentionally empty. Passing the field by ref forces ldflda in AddressScannerProbe.
        }

        private static IEnumerable<MethodBase> DeclaredMethodsAndConstructors(Type type)
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            foreach (ConstructorInfo constructor in type.GetConstructors(Flags))
            {
                yield return constructor;
            }

            foreach (MethodInfo method in type.GetMethods(Flags))
            {
                yield return method;
            }
        }

        private static int CountFieldOpcode(
            MethodBase method,
            FieldInfo target,
            OpCode fieldOpcode)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null)
            {
                return 0;
            }

            byte[] il = body.GetILAsByteArray();
            int offset = 0;
            int matches = 0;

            while (offset < il.Length)
            {
                OpCode opCode = ReadOpCode(il, ref offset);
                if (opCode == fieldOpcode)
                {
                    EnsureAvailable(il, offset, 4, method, opCode);
                    int token = BitConverter.ToInt32(il, offset);
                    if (method.Module == target.Module && token == target.MetadataToken)
                    {
                        matches++;
                    }
                }

                SkipOperand(il, ref offset, opCode, method);
            }

            return matches;
        }

        private static OpCode ReadOpCode(byte[] il, ref int offset)
        {
            byte first = il[offset++];
            ushort value = first;
            if (first == 0xFE)
            {
                if (offset >= il.Length)
                {
                    throw new InvalidOperationException("Truncated two-byte IL opcode.");
                }

                value = (ushort)(0xFE00 | il[offset++]);
            }

            if (!OpCodesByValue.TryGetValue(value, out OpCode opCode))
            {
                throw new InvalidOperationException($"Unknown IL opcode 0x{value:X4}.");
            }

            return opCode;
        }

        private static void SkipOperand(byte[] il, ref int offset, OpCode opCode, MethodBase method)
        {
            int width;
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    width = 0;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    width = 1;
                    break;
                case OperandType.InlineVar:
                    width = 2;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    width = 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    width = 8;
                    break;
                case OperandType.InlineSwitch:
                    EnsureAvailable(il, offset, 4, method, opCode);
                    int count = BitConverter.ToInt32(il, offset);
                    if (count < 0)
                    {
                        throw new InvalidOperationException(
                            $"Negative switch target count in {method.DeclaringType?.FullName}.{method.Name}.");
                    }
                    width = checked(4 + (count * 4));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported IL operand type {opCode.OperandType} in " +
                        $"{method.DeclaringType?.FullName}.{method.Name}.");
            }

            EnsureAvailable(il, offset, width, method, opCode);
            offset += width;
        }

        private static void EnsureAvailable(
            byte[] il,
            int offset,
            int width,
            MethodBase method,
            OpCode opCode)
        {
            if (offset < 0 || width < 0 || offset > il.Length - width)
            {
                throw new InvalidOperationException(
                    $"Truncated IL operand for {opCode.Name} in " +
                    $"{method.DeclaringType?.FullName}.{method.Name}.");
            }
        }

        private static Dictionary<ushort, OpCode> BuildOpCodeMap()
        {
            var map = new Dictionary<ushort, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is OpCode opCode)
                {
                    map[unchecked((ushort)opCode.Value)] = opCode;
                }
            }
            return map;
        }
    }
}
