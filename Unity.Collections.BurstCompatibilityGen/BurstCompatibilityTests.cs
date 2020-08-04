#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Collections.Tests
{
    /// <summary>
    /// Base class for semu-automated burst compatibility testing.
    /// </summary>
    public abstract class BurstCompatibilityTests
    {
        private string m_AssetPath;
        private string m_AssemblyName;
        private static readonly string s_GeneratedClassName = "_generated_burst_compat_tests";

        protected BurstCompatibilityTests(string asset, string assemblyName)
        {
            m_AssetPath = asset;
            m_AssemblyName = assemblyName;
        }

        struct MethodData : IComparable<MethodData>
        {
            public MethodInfo Info;
            public string RequiredDefine;

            public int CompareTo(MethodData other)
            {
                var lhs = Info;
                var rhs = other.Info;

                var ltn = Info.DeclaringType.FullName;
                var rtn = other.Info.DeclaringType.FullName;

                int tc = ltn.CompareTo(rtn);
                if (tc != 0) return tc;

                tc = lhs.Name.CompareTo(rhs.Name);
                if (tc != 0) return tc;

                var lp = lhs.GetParameters();
                var rp = rhs.GetParameters();
                if (lp.Length < rp.Length)
                    return -1;
                if (lp.Length > rp.Length)
                    return 1;

                var lb = new StringBuilder();
                var rb = new StringBuilder();
                for (int i = 0; i < lp.Length; ++i)
                {
                    GetFullTypeName(lp[i].ParameterType, lb);
                    GetFullTypeName(rp[i].ParameterType, rb);

                    tc = lb.ToString().CompareTo(rb.ToString());
                    if (tc != 0)
                        return tc;

                    lb.Clear();
                    rb.Clear();
                }

                return 0;
            }
        }

        private void UpdateGeneratedFile(string path)
        {
            var buf = new StringBuilder();

            var methods = GetTestMethods();

            buf.AppendLine("// auto-generated");
            buf.AppendLine("#if !NET_DOTS");
            buf.AppendLine("using System;");
            buf.AppendLine("using NUnit.Framework;");
            buf.AppendLine("using Unity.Burst;");
            buf.AppendLine("using Unity.Collections;");

            buf.AppendLine("[BurstCompile]");
            buf.AppendLine($"public unsafe class {s_GeneratedClassName}");
            buf.AppendLine("{");
            buf.AppendLine("    private delegate void TestFunc(IntPtr p);");

            var overloadHandling = new Dictionary<string, int>();

            foreach (var methodData in methods)
            {
                var method = methodData.Info;

                var isGetter = method.Name.StartsWith("get_");
                var isSetter = method.Name.StartsWith("set_");
                var isProperty = isGetter | isSetter;
                var sourceName = isProperty ? method.Name.Substring(4) : method.Name;

                var safeName = GetSafeName(method);
                if (overloadHandling.ContainsKey(safeName))
                {
                    int num = overloadHandling[safeName]++;
                    safeName += $"_overload{num}";
                }
                else
                {
                    overloadHandling.Add(safeName, 0);
                }

                if (methodData.RequiredDefine != null)
                {
                    buf.AppendLine($"#if {methodData.RequiredDefine}");
                }

                buf.AppendLine("    [BurstCompile(CompileSynchronously = true)]");
                buf.AppendLine($"    public static void Burst_{safeName}(IntPtr p)");
                buf.AppendLine("    {");

                // Generate targets for out/ref parameters
                var parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; ++i)
                {
                    var param = parameters[i];

                    if (param.ParameterType.IsPointer)
                    {
                        TypeToString(param.ParameterType, buf);
                        buf.Append($"* v{i} = (");
                        TypeToString(param.ParameterType, buf);
                        buf.AppendLine($"*) ((byte*)p + {i * 1024});");
                    }
                    else
                    {
                        buf.Append($"var v{i} = default(");
                        TypeToString(param.ParameterType, buf);
                        buf.AppendLine(");");
                    }
                }


                if (method.IsStatic)
                {
                    if (isGetter)
                        buf.Append("var __result = ");
                    TypeToString(method.DeclaringType, buf);
                    buf.Append($".{sourceName}");
                }
                else
                {
                    buf.Append($"        var instance = (");
                    TypeToString(method.DeclaringType, buf);
                    buf.AppendLine("*)p;");
                    if (isGetter)
                        buf.Append("var __result = ");
                    buf.Append($"        instance->{sourceName}");
                }

                if (method.IsGenericMethod)
                {
                    buf.Append("<");
                    var args = method.GetGenericArguments();
                    for (int i = 0; i < args.Length; ++i)
                    {
                        if (i > 0)
                            buf.Append(", ");
                        TypeToString(args[i], buf);
                    }
                    buf.Append(">");
                }

                // Make dummy arguments.
                if (isGetter) { }
                else if (isSetter)
                    buf.Append("=");
                else
                    buf.Append("(");

                for (int i = 0; i < parameters.Length; ++i)
                {
                    if (i > 0)
                        buf.Append(" ,");

                    var param = parameters[i];

                    if (param.IsOut)
                    {
                        buf.Append("out ");
                    }
                    else if (param.IsIn)
                    {
                        buf.Append("in ");
                    }
                    else if (param.ParameterType.IsByRef)
                    {
                        buf.Append("ref ");
                    }

                    buf.Append($"v{i}");
                }

                if (!isProperty)
                    buf.Append(")");

                buf.AppendLine(";");

                buf.AppendLine("    }");

                buf.AppendLine($"    public static void BurstCompile_{safeName}()");
                buf.AppendLine("    {");
                buf.AppendLine($"        BurstCompiler.CompileFunctionPointer<TestFunc>(Burst_{safeName});");
                buf.AppendLine("    }");

                if (methodData.RequiredDefine != null)
                {
                    buf.AppendLine($"#endif");
                }
            }

            buf.AppendLine("}");
            buf.AppendLine("#endif");

            File.WriteAllText(path, buf.ToString());
        }

        private static void TypeToString(Type t, StringBuilder buf)
        {
            if (t.IsPrimitive || t == typeof(void))
            {
                buf.Append(PrimitiveTypeToString(t));
                return;
            }

            if (t.IsByRef || t.IsPointer)
            {
                TypeToString(t.GetElementType(), buf);
                return;
            }

            if (t.Namespace != "Unity.Collections")
            {
                buf.Append(t.Namespace);
                buf.Append(".");
            }
            GetFullTypeName(t, buf);

            if (t.IsConstructedGenericType)
            {
                buf.Append("<");
                var gt = t.GenericTypeArguments;

                for (int i = 0; i < gt.Length; ++i)
                {
                    if (i > 0)
                    {
                        buf.Append(", ");
                    }

                    TypeToString(gt[i], buf);
                }

                buf.Append(">");
            }
        }

        private static string PrimitiveTypeToString(Type type)
        {
            if (type == typeof(void))
                return "void";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(sbyte))
                return "sbyte";
            if (type == typeof(short))
                return "short";
            if (type == typeof(ushort))
                return "ushort";
            if (type == typeof(int))
                return "int";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(long))
                return "long";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(char))
                return "char";
            if (type == typeof(double))
                return "double";
            if (type == typeof(float))
                return "float";

            throw new InvalidOperationException($"{type} is not a primitive type");
        }

        private static void GetFullTypeName(Type type, StringBuilder buf)
        {
            if (type.DeclaringType != null)
            {
                GetFullTypeName(type.DeclaringType, buf);
                buf.Append(".");
            }

            var name = type.Name;

            if (type.IsConstructedGenericType)
            {
                name = name.Remove(name.IndexOf('`'));
            }

            buf.Append(name);
        }

        MethodData[] GetTestMethods()
        {
            var seenMethods = new HashSet<MethodInfo>();
            var result = new List<MethodData>();

            void MaybeAddMethod(MethodInfo m, Type[] genericTypeArguments, string requiredDefine)
            {
                if (m.IsPrivate)
                    return;

                PropertyInfo property = m.Name.StartsWith("get_") || m.Name.StartsWith("set_") ? m.DeclaringType.GetProperty(m.Name.Substring(4), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.SetProperty) : null;

                if (m.GetCustomAttribute<ObsoleteAttribute>() != null)
                    return;

                if (m.GetCustomAttribute<NotBurstCompatibleAttribute>() != null)
                    return;

                if (property != null)
                {
                    if (property.GetCustomAttribute<ObsoleteAttribute>() != null)
                        return;
                    if (property.GetCustomAttribute<NotBurstCompatibleAttribute>() != null)
                        return;
                }
                else
                {
                    if (m.IsSpecialName)
                        return;
                }

                if (m.IsGenericMethodDefinition)
                {
                    if (genericTypeArguments == null)
                    {
                        Debug.LogError($"Method {m.DeclaringType}.{m} is generic but doesn't have a type array in its BurstCompatible attribute");
                        return;
                    }

                    try
                    {
                        m = m.MakeGenericMethod(genericTypeArguments);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Could not instantiate method {m.DeclaringType}.{m} with type arguments {genericTypeArguments}");
                        Debug.LogException(e);
                        return;
                    }
                }

                if (m.DeclaringType.IsGenericTypeDefinition)
                {
                    Debug.LogError($"Method {m.DeclaringType}.{m} is declared on a generic type, this is not currently supported by the interop generator");
                    return;
                }

                //if (m.GetParameters().Any((p) => !p.ParameterType.IsValueType && !p.ParameterType.IsPointer))
                //   return;

                // These are crazy nested function names. They'll be covered anyway as the parent function is burst compatible.
                if (m.Name.Contains('<'))
                    return;

                if (seenMethods.Contains(m))
                    return;

                seenMethods.Add(m);
                result.Add(new MethodData { Info = m, RequiredDefine = requiredDefine });
            }

            foreach (var m in TypeCache.GetMethodsWithAttribute<BurstCompatibleAttribute>())
            {
                if (m.DeclaringType.Assembly.GetName().Name != m_AssemblyName)
                    continue;

                foreach (var attr in m.GetCustomAttributes<BurstCompatibleAttribute>())
                {
                    MaybeAddMethod(m, attr.GenericTypeArguments, attr.RequiredUnityDefine);
                }
            }

            foreach (var t in TypeCache.GetTypesWithAttribute<BurstCompatibleAttribute>())
            {
                if (t.Assembly.GetName().Name != m_AssemblyName)
                    continue;

                foreach (var attr in t.GetCustomAttributes<BurstCompatibleAttribute>())
                {
                    foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var methodAttrs = m.GetCustomAttributes<BurstCompatibleAttribute>().ToArray();

                        if (methodAttrs.Length == 0)
                        {
                            MaybeAddMethod(m, attr.GenericTypeArguments, attr.RequiredUnityDefine);
                        }
                        else
                        {
                            foreach (var methodAttr in methodAttrs)
                            {
                                var typeArguments = attr.GenericTypeArguments;
                                var requiredDefine = attr.RequiredUnityDefine;

                                if (methodAttr.GenericTypeArguments != null)
                                {
                                    typeArguments = methodAttr.GenericTypeArguments;
                                }

                                if (methodAttr.RequiredUnityDefine != null)
                                {
                                    requiredDefine = methodAttr.RequiredUnityDefine;
                                }

                                MaybeAddMethod(m, typeArguments, requiredDefine);
                            }
                        }

                    }
                }
            }

            var array = result.ToArray();
            Array.Sort(array);
            return array;
        }

        private static string GetSafeName(MethodInfo method)
        {
            return GetSafeName(method.DeclaringType) + "_" + r.Replace(method.Name, "__");
        }

        public static readonly Regex r = new Regex(@"[^A-Za-z_0-9]+");

        private static string GetSafeName(Type t)
        {
            var b = new StringBuilder();
            GetFullTypeName(t, b);
            return r.Replace(b.ToString(), "__");
        }

        [UnityTest]
        public IEnumerator CompatibilityTests()
        {
            int runCount = 0;
            int successCount = 0;

            try
            {
                UpdateGeneratedFile(m_AssetPath);

                yield return new RecompileScripts();

                var t = GetType().Assembly.GetType(s_GeneratedClassName);
                if (t == null)
                {
                    throw new ApplicationException($"could not find generated type {s_GeneratedClassName} in assembly {GetType().Assembly.FullName}");
                }

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name.StartsWith("BurstCompile_"))
                    {
                        ++runCount;
                        try
                        {
                            m.Invoke(null, null);
                            ++successCount;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }
            finally
            {
                if (runCount != successCount)
                {
                    Debug.LogError($"Burst compatibility tests failed; ran {runCount} tests, {successCount} OK, {runCount - successCount} FAILED.");
                }
                else
                {
                    Debug.Log($"Ran {runCount} tests, all OK");
                }

                AssetDatabase.DeleteAsset(m_AssetPath);
            }

            yield return new RecompileScripts();
        }
    }
}
#endif
