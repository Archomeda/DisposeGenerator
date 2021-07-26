using System;

namespace DisposeGenerator.Attributes
{
    /// <summary>
    /// An attribute for specifying that a field should not be disposed automatically.
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    internal class ExcludeDisposeAttribute : Attribute { }
}
