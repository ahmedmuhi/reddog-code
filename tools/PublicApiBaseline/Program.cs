using System.Reflection;
using System.Runtime.Loader;
using PublicApiGenerator;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <assemblyPath> <outputPath>");
    return 1;
}

var assemblyPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var resolver = new AssemblyDependencyResolver(assemblyPath);
Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName name)
{
    var resolvedPath = resolver.ResolveAssemblyToPath(name);
    if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
    {
        return context.LoadFromAssemblyPath(resolvedPath);
    }

    var localCandidate = Path.Combine(Path.GetDirectoryName(assemblyPath)!, $"{name.Name}.dll");
    if (File.Exists(localCandidate))
    {
        return context.LoadFromAssemblyPath(localCandidate);
    }

    return null;
}

Assembly? handler(AssemblyLoadContext context, AssemblyName name) => ResolveAssembly(context, name);
AssemblyLoadContext.Default.Resolving += handler;

try
{
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    var options = new ApiGeneratorOptions();
    var publicApi = assembly.GeneratePublicApi(options);
    var normalized = NormalizeLineEndings(publicApi) + Environment.NewLine;
    File.WriteAllText(outputPath, normalized);
    Console.WriteLine($"Generated API baseline for {assembly.GetName().Name} -> {outputPath}");
    return 0;
}
finally
{
    AssemblyLoadContext.Default.Resolving -= handler;
}

static string NormalizeLineEndings(string input) =>
    input.Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace("\r", "\n", StringComparison.Ordinal);
