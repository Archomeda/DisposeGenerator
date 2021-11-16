using System;

namespace DisposeGenerator
{
    /// <summary>
    /// An attribute for specifying that a method is used for additional disposing managed resources asynchronously.
    /// This method will be called when the object is being disposed of asynchronously, but not when the finalizer is called.
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Method)]
    internal class AsyncDisposerAttribute : Attribute { }
}
