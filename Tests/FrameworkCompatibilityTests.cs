using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

internal static class FrameworkCompatibilityTests
{
    private static IEnumerable<MethodBase> Calls(MethodBase method, Dictionary<short, OpCode> codes)
    {
        MethodBody body = method.GetMethodBody();
        if (body == null) yield break;
        byte[] il = body.GetILAsByteArray();
        int offset = 0;
        while (offset < il.Length)
        {
            byte first = il[offset++];
            short key = first == 0xfe ? (short)(0xfe00 | il[offset++]) : (short)first;
            OpCode code = codes[key];
            if (code.OperandType == OperandType.InlineMethod)
            {
                MethodInfo generic = method as MethodInfo;
                yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, offset),
                    method.DeclaringType.GetGenericArguments(),
                    generic != null && generic.IsGenericMethod ? generic.GetGenericArguments() : null);
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
    }

    private static void CheckCaptureBoundary(MethodBase method, Dictionary<short, OpCode> codes)
    {
        DllImportAttribute import = (DllImportAttribute)Attribute.GetCustomAttribute(method, typeof(DllImportAttribute));
        if (import != null && Array.IndexOf(new[] { "BitBlt", "StretchBlt", "PrintWindow", "GetDC", "GetWindowDC" }, import.EntryPoint) >= 0)
            throw new Exception("Screen-capture native import in public executable: " + import.EntryPoint);
        foreach (MethodBase target in Calls(method, codes))
            if (target.DeclaringType.FullName == "System.Drawing.Graphics" && target.Name == "CopyFromScreen")
                throw new Exception("Screen-capture API call in public executable: " + method.DeclaringType.FullName + "." + method.Name);
    }

    private static int Main(string[] args)
    {
        try
        {
            Assembly app = Assembly.LoadFrom(Path.GetFullPath(args.Length == 0 ? "BudgetDesk.exe" : args[0]));
            MethodInfo run = app.GetType("BudgetDesk.Program", true).GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic);
            Dictionary<short, OpCode> codes = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType == typeof(OpCode)) { OpCode code = (OpCode)field.GetValue(null); codes[code.Value] = code; }
            int trims = 0;
            foreach (MethodBase target in Calls(run, codes))
            {
                if (target.DeclaringType == typeof(string) && target.Name == "TrimEnd")
                {
                    ParameterInfo[] parameters = target.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(char[]))
                        throw new Exception("Incompatible Windows call in executable: " + target + ". Expected TrimEnd(Char[]).");
                    trims++;
                }
            }
            if (trims != 1) throw new Exception("The startup TrimEnd call was not verified exactly once.");
            Console.WriteLine("PASS executable startup calls the Framework-compatible String.TrimEnd(Char[]) signature");
            object[] targets = app.GetCustomAttributes(typeof(TargetFrameworkAttribute), false);
            if (targets.Length != 1 || ((TargetFrameworkAttribute)targets[0]).FrameworkName != ".NETFramework,Version=v4.5")
                throw new Exception("The executable must declare .NET Framework 4.5 as its target.");
            Console.WriteLine("PASS executable declares the Windows .NET Framework 4.5 target");
            const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in app.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(declared)) CheckCaptureBoundary(method, codes);
                foreach (ConstructorInfo constructor in type.GetConstructors(declared)) CheckCaptureBoundary(constructor, codes);
            }
            Console.WriteLine("PASS public executable contains no screen-capture API calls or native imports");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex.Message); return 1; }
    }
}
