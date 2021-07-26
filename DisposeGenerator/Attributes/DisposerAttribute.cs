using System;

namespace DisposeGenerator.Attributes
{
    /// <summary>
    /// An attribute for specifying that a method is used for additional disposing managed resources.
    /// This method will be called when the object is being disposed of, but not when the finalizer is called.
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Method)]
    internal class DisposerAttribute : Attribute { }
}
