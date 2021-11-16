using System;
using System.IO;

namespace DisposeGenerator
{
    internal static class GeneratorUtils
    {
        private const string EMBEDDED_PATH = nameof(DisposeGenerator) + ".Embedded";

        public static string GetEmbeddedFileName(string context) =>
            $"{EMBEDDED_PATH}.{context}.cs";

        public static string GetEmbeddedName(string @namespace, string context) =>
            $"{@namespace}.{context}";


        public static void AddEmbeddedSourceCopy(Action<string, string> addSourceAction, string file)
        {
            string fileName = GetEmbeddedFileName(file);

            var type = typeof(GeneratorUtils).Assembly;
            using var stream = type.GetManifestResourceStream(fileName) ?? throw new InvalidOperationException($"{fileName} does not exist as embedded resource");

            using var reader = new StreamReader(stream);
            string text = reader.ReadToEnd();
            addSourceAction(fileName, text);
        }
    }
}
