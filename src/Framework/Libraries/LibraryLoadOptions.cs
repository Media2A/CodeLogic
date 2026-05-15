namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Per-library options threaded through <see cref="LibraryManager.LoadLibraryAsync{T}(LibraryLoadOptions)"/>
/// (and the static <see cref="global::CodeLogic.Libraries.LoadAsync{T}(LibraryLoadOptions)"/>
/// overload) to customise how the library participates in the boot sequence.
/// </summary>
public sealed record LibraryLoadOptions
{
    /// <summary>
    /// When <see langword="true"/>, a failure during Configure / Initialize /
    /// Start phases is logged + the library is marked
    /// <see cref="LibraryState.Failed"/>, but the application boots without it.
    /// Consumers retrieving the library via <see cref="global::CodeLogic.Libraries.Get{T}()"/>
    /// will get the loaded instance back — but since its <c>Context</c> is
    /// never set and downstream services typically check
    /// <c>IsAvailable</c>-style flags, calls into the disabled library are
    /// expected to no-op gracefully.
    /// <para>
    /// Default: <see langword="false"/> — a failure aborts the boot. Use
    /// this opt-in for "nice to have" dependencies (mail, social connect,
    /// GeoIP, S3 storage) so a single bad config doesn't bring the whole
    /// app down. NEVER set this for the database or auth library; failure
    /// there should fail fast.
    /// </para>
    /// </summary>
    public bool OptionalAtBoot { get; init; }
}
