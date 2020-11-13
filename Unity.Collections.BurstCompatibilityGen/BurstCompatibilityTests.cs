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
            public Type InstanceType;
            public Type[] MethodGenericTypeArguments;
            public Type[] InstanceTypeGenericTypeArguments;
            public Dictionary<string, Type> MethodGenericArgumentLookup;
            public Dictionary<string, Type> InstanceTypeGenericArgumentLookup;
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
                    GetFullTypeName(lp[i].ParameterType, lb, this);
                    GetFullTypeName(rp[i].ParameterType, rb, other);

                    tc = lb.ToString().CompareTo(rb.ToString());
                    if (tc != 0)
                        return tc;

                    lb.Clear();
                    rb.Clear();
                }

                return 0;
            }

            public Type ReplaceGeneric(Type genericType)
            {
                if (genericType.IsByRef)
                {
                    genericType = genericType.GetElementType();
                }

                if (MethodGenericArgumentLookup == null & InstanceTypeGenericArgumentLookup == null)
                {
                    throw new InvalidOperationException("For '{InstanceType.Name}.{Info.Name}', generic argument lookups are null! Did you forget to specify GenericTypeArguments in the [BurstCompatible] attribute?");
                }

                bool hasMethodReplacement = MethodGenericArgumentLookup.ContainsKey(genericType.Name);
                bool hasInstanceTypeReplacement = InstanceTypeGenericArgumentLookup.ContainsKey(genericType.Name);

                if (hasMethodReplacement)
                {
                    return MethodGenericArgumentLookup[genericType.Name];
                }
                else if (hasInstanceTypeReplacement)
                {
                    return InstanceTypeGenericArgumentLookup[genericType.Name];
                }
                else
                {
                    throw new ArgumentException($"'{genericType.Name}' in '{InstanceType.Name}.{Info.Name}' has no generic type replacement in the generic argument lookups! Did you forget to specify GenericTypeArguments in the [BurstCompatible] attribute?");
                }
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
            buf.AppendLine("using Unity.Collections.LowLevel.Unsafe;");

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
                var isIndexer = method.Name.Equals("get_Item") || method.Name.Equals("set_Item");
                var sourceName = isProperty ? method.Name.Substring(4) : method.Name;

                var safeName = GetSafeName(methodData);
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
                    var pt = param.ParameterType;

                    if (pt.IsGenericParameter ||
                       (pt.IsByRef && pt.GetElementType().IsGenericParameter))
                    {
                        pt = methodData.ReplaceGeneric(pt);
                    }

                    if (pt.IsGenericTypeDefinition)
                    {
                        pt = pt.MakeGenericType(methodData.InstanceTypeGenericTypeArguments);
                    }

                    if (pt.IsPointer)
                    {
                        TypeToString(pt, buf, methodData);
                        buf.Append($" v{i} = (");
                        TypeToString(pt, buf, methodData);
                        buf.AppendLine($") ((byte*)p + {i * 1024});");
                    }
                    else
                    {
                        buf.Append($"var v{i} = default(");
                        TypeToString(pt, buf, methodData);
                        buf.AppendLine(");");
                    }
                }


                if (method.IsStatic)
                {
                    if (isGetter)
                        buf.Append("var __result = ");
                    TypeToString(methodData.InstanceType, buf, methodData);
                    buf.Append($".{sourceName}");
                }
                else
                {
                    buf.Append($"        ref var instance = ref UnsafeUtility.AsRef<");
                    TypeToString(methodData.InstanceType, buf, methodData);
                    buf.AppendLine(">((void*)p);");

                    if (isIndexer)
                    {
                        if (isGetter)
                            buf.Append("        var __result = instance");
                        else if (isSetter)
                            buf.Append("        instance");
                    }
                    else
                    {
                        if (isGetter)
                            buf.Append("var __result = ");
                        buf.Append($"        instance.{sourceName}");
                    }
                }

                if (method.IsGenericMethod)
                {
                    buf.Append("<");
                    var args = method.GetGenericArguments();
                    for (int i = 0; i < args.Length; ++i)
                    {
                        if (i > 0)
                            buf.Append(", ");
                        TypeToString(args[i], buf, methodData);
                    }
                    buf.Append(">");
                }

                // Make dummy arguments.
                if (isIndexer)
                {
                    buf.Append("[");
                }
                else
                {
                    if (isGetter)
                    {
                    }
                    else if (isSetter)
                        buf.Append("=");
                    else
                        buf.Append("(");
                }

                for (int i = 0; i < parameters.Length; ++i)
                {
                    // Close the indexer brace and assign the value if we're handling an indexer and setter.
                    if (isIndexer && isSetter && i + 1 == parameters.Length)
                    {
                        buf.Append($"] = v{i}");
                        break;
                    }

                    if (i > 0)
                        buf.Append(" ,");

                    // Close the indexer brace. This is separate from the setter logic above because the
                    // comma separating arguments is required for the getter case but not for the setter.
                    if (isIndexer && isGetter && i + 1 == parameters.Length)
                    {
                        buf.Append($"v{i}]");
                        break;
                    }

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

        private static void TypeToString(Type t, StringBuilder buf, in MethodData methodData)
        {
            if (t.IsPrimitive || t == typeof(void))
            {
                buf.Append(PrimitiveTypeToString(t));
                return;
            }

            if (t.IsByRef)
            {
                TypeToString(t.GetElementType(), buf, methodData);
                return;
            }

            // This should come after the IsByRef check above to avoid adding an extra asterisk.
            // You could have a T*& (ref to a pointer) which causes t.IsByRef and t.IsPointer to both be true and if
            // you check t.IsPointer first then descend down the types with t.GetElementType() you end up with
            // T* which causes you to descend a second time then print an asterisk twice as you come back up the
            // recursion.
            if (t.IsPointer)
            {
                TypeToString(t.GetElementType(), buf, methodData);
                buf.Append("*");
                return;
            }

            GetFullTypeName(t, buf, methodData);
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
            if (type == typeof(IntPtr))
                return "IntPtr";
            if (type == typeof(UIntPtr))
                return "UIntPtr";

            throw new InvalidOperationException($"{type} is not a primitive type");
        }

        private static void GetFullTypeName(Type type, StringBuilder buf, in MethodData methodData)
        {
            // If we encounter a generic parameter (typically T) then we should replace it with a real one
            // specified by [BurstCompatible(GenericTypeArguments = new [] { typeof(...) })].
            if (type.IsGenericParameter)
            {
                GetFullTypeName(methodData.ReplaceGeneric(type), buf, methodData);
                return;
            }

            if (type.DeclaringType != null)
            {
                GetFullTypeName(type.DeclaringType, buf, methodData);
                buf.Append(".");
            }
            else
            {
                // These appends for the namespace used to be protected by an if check for Unity.Collections or
                // Unity.Collections.LowLevel.Unsafe, but HashSetExtensions lives in both so just fully disambiguate
                // by always appending the namespace.
                buf.Append(type.Namespace);
                buf.Append(".");
            }

            var name = type.Name;

            var idx = name.IndexOf('`');
            if (-1 != idx)
            {
                name = name.Remove(idx);
            }

            buf.Append(name);

            if (type.IsConstructedGenericType || type.IsGenericTypeDefinition)
            {
                var gt = type.GetGenericArguments();

                // Avoid printing out the generic arguments for cases like UnsafeHashMap<TKey, TValue>.ParallelWriter.
                // ParallelWriter is considered to be a generic type and will have two generic parameters inherited
                // from UnsafeHashMap. Because of this, if we don't do this check, we could code gen this:
                //
                // UnsafeHashMap<int, int>.ParallelWriter<int, int>
                //
                // But we want:
                //
                // UnsafeHashMap<int, int>.ParallelWriter
                //
                // ParallelWriter doesn't actually have generic arguments you can give it directly so it's not correct
                // to give it generic arguments. If the nested type has the same number of generic arguments as its
                // declaring type, then there should be no new generic arguments and therefore nothing to print.
                if (type.IsNested && gt.Length == type.DeclaringType.GetGenericArguments().Length)
                {
                    return;
                }

                buf.Append("<");

                for (int i = 0; i < gt.Length; ++i)
                {
                    if (i > 0)
                    {
                        buf.Append(", ");
                    }

                    TypeToString(gt[i], buf, methodData);
                }

                buf.Append(">");
            }
        }

        private static readonly Type[] EmptyGenericTypeArguments = { };

        MethodData[] GetTestMethods()
        {
            var seenMethods = new HashSet<MethodInfo>();
            var result = new List<MethodData>();

            void MaybeAddMethod(MethodInfo m, Type[] methodGenericTypeArguments, Type[] declaredTypeGenericTypeArguments, string requiredDefine)
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

                var methodGenericArgumentLookup = new Dictionary<string, Type>();

                if (m.IsGenericMethodDefinition)
                {
                    if (methodGenericTypeArguments == null)
                    {
                        Debug.LogError($"Method `{m.DeclaringType}.{m}` is generic but doesn't have a type array in its BurstCompatible attribute");
                        return;
                    }

                    var genericArguments = m.GetGenericArguments();

                    if (genericArguments.Length != methodGenericTypeArguments.Length)
                    {
                        Debug.LogError($"Method `{m.DeclaringType}.{m}` is generic with {genericArguments.Length} generic parameters but BurstCompatible attribute has {methodGenericTypeArguments.Length} types, they must be the same length!");
                        return;
                    }

                    try
                    {
                        m = m.MakeGenericMethod(methodGenericTypeArguments);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Could not instantiate method `{m.DeclaringType}.{m}` with type arguments `{methodGenericTypeArguments}`.");
                        Debug.LogException(e);
                        return;
                    }

                    // Build up the generic name to type lookup for this method.
                    for (int i = 0; i < genericArguments.Length; ++i)
                    {
                        var name = genericArguments[i].Name;
                        var type = methodGenericTypeArguments[i];

                        try
                        {
                            methodGenericArgumentLookup.Add(name, type);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"For method `{m.DeclaringType}.{m}`, could not add ({name}, {type}).");
                            Debug.LogException(e);
                            return;
                        }
                    }
                }

                var instanceType = m.DeclaringType;
                var instanceTypeGenericLookup = new Dictionary<string, Type>();

                if (instanceType.IsGenericTypeDefinition)
                {
                    var instanceGenericArguments = instanceType.GetGenericArguments();

                    if (instanceGenericArguments.Length != declaredTypeGenericTypeArguments.Length)
                    {
                        Debug.LogError($"Type `{instanceType}` is generic with {instanceGenericArguments.Length} generic parameters but BurstCompatible attribute has {declaredTypeGenericTypeArguments.Length} types, they must be the same length!");
                        return;
                    }

                    if (declaredTypeGenericTypeArguments == null)
                    {
                        Debug.LogError($"Type `{m.DeclaringType}` is generic but doesn't have a type array in its BurstCompatible attribute");
                        return;
                    }

                    try
                    {
                        instanceType = instanceType.MakeGenericType(declaredTypeGenericTypeArguments);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Could not instantiate type `{instanceType}` with type arguments `{declaredTypeGenericTypeArguments}`.");
                        Debug.LogException(e);
                        return;
                    }

                    // Build up the generic name to type lookup for this method.
                    for (int i = 0; i < instanceGenericArguments.Length; ++i)
                    {
                        var name = instanceGenericArguments[i].Name;
                        var type = declaredTypeGenericTypeArguments[i];

                        try
                        {
                            instanceTypeGenericLookup.Add(name, type);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"For type `{instanceType}`, could not add ({name}, {type}).");
                            Debug.LogException(e);
                            return;
                        }
                    }
                }

                //if (m.GetParameters().Any((p) => !p.ParameterType.IsValueType && !p.ParameterType.IsPointer))
                //   return;

                // These are crazy nested function names. They'll be covered anyway as the parent function is burst compatible.
                if (m.Name.Contains('<'))
                    return;

                if (seenMethods.Contains(m))
                    return;

                seenMethods.Add(m);
                result.Add(new MethodData
                {
                    Info = m, InstanceType = instanceType, MethodGenericTypeArguments = methodGenericTypeArguments,
                    InstanceTypeGenericTypeArguments = declaredTypeGenericTypeArguments, RequiredDefine = requiredDefine,
                    MethodGenericArgumentLookup = methodGenericArgumentLookup, InstanceTypeGenericArgumentLookup = instanceTypeGenericLookup
                });
            }

            var declaredTypeGenericArguments = new Dictionary<Type, Type[]>();

            // Go through types tagged with [BurstCompatible] and their methods before performing the direct method
            // search (below this loop) to ensure that we get the declared type generic arguments.
            //
            // If we were to run the direct method search first, it's possible we would add the method to the seen list
            // and then by the time we execute this loop we might skip it because we think we have seen the method
            // already but we haven't grabbed the declared type generic arguments yet.
            foreach (var t in TypeCache.GetTypesWithAttribute<BurstCompatibleAttribute>())
            {
                if (t.Assembly.GetName().Name != m_AssemblyName)
                    continue;

                foreach (var typeAttr in t.GetCustomAttributes<BurstCompatibleAttribute>())
                {
                    // As we go through all the types with [BurstCompatible] on them, remember their GenericTypeArguments
                    // in case we encounter the type again when we do the direct method search later. When we do the
                    // direct method search, we don't have as easy access to the [BurstCompatible] attribute on the
                    // type so just remember this now to make life easier.
                    declaredTypeGenericArguments[t] = typeAttr.GenericTypeArguments;

                    foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var methodAttrs = m.GetCustomAttributes<BurstCompatibleAttribute>().ToArray();

                        if (methodAttrs.Length == 0)
                        {
                            MaybeAddMethod(m, EmptyGenericTypeArguments, typeAttr.GenericTypeArguments, typeAttr.RequiredUnityDefine);
                        }
                        else
                        {
                            foreach (var methodAttr in methodAttrs)
                            {
                                MaybeAddMethod(m, methodAttr.GenericTypeArguments, typeAttr.GenericTypeArguments, methodAttr.RequiredUnityDefine ?? typeAttr.RequiredUnityDefine);
                            }
                        }

                    }
                }
            }

            // Direct method search.
            foreach (var m in TypeCache.GetMethodsWithAttribute<BurstCompatibleAttribute>())
            {
                if (m.DeclaringType.Assembly.GetName().Name != m_AssemblyName)
                    continue;

                // Look up the GenericTypeArguments on the declaring type that we probably got earlier. If the key
                // doesn't exist then it means [BurstCompatible] was only on the method and not the type, which is fine
                // but if [BurstCompatible] was on the type we should go ahead and use whatever GenericTypeArguments
                // it may have had.
                var typeGenericArguments = declaredTypeGenericArguments.ContainsKey(m.DeclaringType) ? declaredTypeGenericArguments[m.DeclaringType] : EmptyGenericTypeArguments;

                foreach (var attr in m.GetCustomAttributes<BurstCompatibleAttribute>())
                {
                    MaybeAddMethod(m, attr.GenericTypeArguments, typeGenericArguments, attr.RequiredUnityDefine);
                }
            }

            var array = result.ToArray();
            Array.Sort(array);
            return array;
        }

        private static string GetSafeName(in MethodData methodData)
        {
            var method = methodData.Info;
            return GetSafeName(method.DeclaringType, methodData) + "_" + r.Replace(method.Name, "__");
        }

        public static readonly Regex r = new Regex(@"[^A-Za-z_0-9]+");

        private static string GetSafeName(Type t, in MethodData methodData)
        {
            var b = new StringBuilder();
            GetFullTypeName(t, b, methodData);
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
