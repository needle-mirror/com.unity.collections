using System;

namespace Unity.Collections
{
    /// <summary>
    /// Documents and enforces (via generated tests) that the tagged method or property has to stay burst compatible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class BurstCompatibleAttribute : Attribute
    {
        public Type[] GenericTypeArguments { get; set; }

        public string RequiredUnityDefine = null;
    }
    /// <summary>
    /// Internal attribute to state that a method is not burst compatible even though the containing type is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class NotBurstCompatibleAttribute : Attribute
    {
    }
}
