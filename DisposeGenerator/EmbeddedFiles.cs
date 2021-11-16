namespace DisposeGenerator
{
    internal static class EmbeddedFiles
    {
        public const string NAMESPACE = nameof(DisposeGenerator);

        public const string DISPOSE_ALL_ATTRIBUTE_NAME = "DisposeAllAttribute";
        public const string EXCLUDE_DISPOSE_ATTRIBUTE_NAME = "ExcludeDisposeAttribute";
        public const string INCLUDE_DISPOSE_ATTRIBUTE_NAME = "IncludeDisposeAttribute";
        public const string DISPOSER_ATTRIBUTE_NAME = "DisposerAttribute";
        public const string FINALIZER_ATTRIBUTE_NAME = "FinalizerAttribute";
        public const string ASYNC_DISPOSER_ATTRIBUTE_NAME = "AsyncDisposerAttribute";


        public static string DisposeAllAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, DISPOSE_ALL_ATTRIBUTE_NAME);

        public static string ExcludeDisposeAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, EXCLUDE_DISPOSE_ATTRIBUTE_NAME);

        public static string IncludeDisposeAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, INCLUDE_DISPOSE_ATTRIBUTE_NAME);

        public static string DisposerAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, DISPOSER_ATTRIBUTE_NAME);

        public static string FinalizerAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, FINALIZER_ATTRIBUTE_NAME);

        public static string AsyncDisposerAttributeFQN =>
            GeneratorUtils.GetEmbeddedName(NAMESPACE, ASYNC_DISPOSER_ATTRIBUTE_NAME);
    }
}
