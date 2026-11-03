using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;

internal static class FrameworkCompatibilityTests
{
    private static int Main(string[] args)
    {
        try
        {
            Assembly app = Assembly.LoadFrom(Path.GetFullPath(args.Length == 0 ? "BudgetDesk.exe" : args[0]));
            MethodInfo run = app.GetType("BudgetDesk.Program", true).GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic);
            byte[] il = run.GetMethodBody().GetILAsByteArray();
            Dictionary<short, OpCode> codes = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType == typeof(OpCode)) { OpCode code = (OpCode)field.GetValue(null); codes[code.Value] = code; }
            int offset = 0, trims = 0;
            while (offset < il.Length)
            {
                byte first = il[offset++];
                short key = first == 0xfe ? (short)(0xfe00 | il[offset++]) : (short)first;
                OpCode code = codes[key];
                if (code.OperandType == OperandType.InlineMethod)
                {
                    MethodBase target = run.Module.ResolveMethod(BitConverter.ToInt32(il, offset));
                    if (target.DeclaringType == typeof(string) && target.Name == "TrimEnd")
                    {
                        ParameterInfo[] parameters = target.GetParameters();
                        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(char[]))
                            throw new Exception("Incompatible Windows call in executable: " + target + ". Expected TrimEnd(Char[]).");
                        trims++;
                    }
                }
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: offset += 1; break;
                    case OperandType.InlineVar: offset += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: offset += 8; break;
                    case OperandType.InlineSwitch: offset += 4 + 4 * BitConverter.ToInt32(il, offset); break;
                    default: offset += 4; break;
                }
            }
            if (trims != 1) throw new Exception("The startup TrimEnd call was not verified exactly once.");
            Console.WriteLine("PASS executable startup calls the Framework-compatible String.TrimEnd(Char[]) signature");
            object[] targets = app.GetCustomAttributes(typeof(TargetFrameworkAttribute), false);
            if (targets.Length != 1 || ((TargetFrameworkAttribute)targets[0]).FrameworkName != ".NETFramework,Version=v4.5")
                throw new Exception("The executable must declare .NET Framework 4.5 as its target.");
            Console.WriteLine("PASS executable declares the Windows .NET Framework 4.5 target");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex.Message); return 1; }
    }
}
