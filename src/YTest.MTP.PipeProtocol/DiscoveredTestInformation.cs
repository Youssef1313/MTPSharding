namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Represents the information about a discovered test.
/// This type is returned by <see cref="MTPPipeDiscoverer.DiscoverTestsAsync"/>
/// </summary>
public sealed class DiscoveredTestInformation
{
    internal DiscoveredTestInformation(string uid, string displayName, string? filePath, int? lineNumber, string? @namespace, string? typeName, string? methodName, TestTrait[]? traits)
    {
        Uid = uid;
        DisplayName = displayName;
        FilePath = filePath;
        LineNumber = lineNumber;
        Namespace = @namespace;
        TypeName = typeName;
        MethodName = methodName;
        Traits = traits;
    }

    /// <summary>
    /// The unique identifier of the test. In many cases it's a Guid, but that's not a guarantee!
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The source code file path that declares the test.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// The line number in the source code file that declares the test.
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// The namespace of the class containing the test.
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    /// The name of the class containing the test.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// The name of the method containing the test.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// The traits associated with the test.
    /// </summary>
    public TestTrait[]? Traits { get; }
}

/// <summary>
/// Represents a metadata property of a test.
/// </summary>
public sealed class TestTrait
{
    internal TestTrait(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// The name of the trait.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The value of the trait.
    /// </summary>
    public string? Value { get; }
}
