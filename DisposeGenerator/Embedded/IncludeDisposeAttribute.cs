using System;

namespace DisposeGenerator
{
    /// <summary>
    /// An attribute for specifying that a field should be disposed automatically.
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class IncludeDisposeAttribute : Attribute { }
}
