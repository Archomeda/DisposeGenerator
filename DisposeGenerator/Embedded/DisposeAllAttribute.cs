using System;

namespace DisposeGenerator
{
    /// <summary>
    /// An attribute for specifying that a class should implement <see cref="IDisposable"/> automatically
    /// for all fields that implement <see cref="IDisposable"/> and/or <see cref="IAsyncDisposable"/>.
    /// Fields can be excluded with <see cref="ExcludeDisposeAttribute"/>.
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    internal class DisposeAllAttribute : Attribute { }
}
