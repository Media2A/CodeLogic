using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// Static accessor for loaded libraries.
/// </summary>
public static class Libraries
{
    public static T? Get<T>() where T : class, ILibrary
    {
        var mgr = CodeLogic.GetLibraryManager()
            ?? throw new InvalidOperationException("No libraries loaded. Call ConfigureAsync() first.");
        return mgr.GetLibrary<T>();
    }

    public static async Task<bool> LoadAsync<T>() where T : class, ILibrary, new()
    {
        var mgr = CodeLogic.GetLibraryManager()
            ?? throw new InvalidOperationException("Library manager not available. Call ConfigureAsync() first.");
        return await mgr.LoadLibraryAsync<T>();
    }

    public static ILibrary? Get(string libraryId)
    {
        var mgr = CodeLogic.GetLibraryManager()
            ?? throw new InvalidOperationException("No libraries loaded. Call ConfigureAsync() first.");
        return mgr.GetLibrary(libraryId);
    }

    public static IEnumerable<ILibrary> GetAll()
    {
        var mgr = CodeLogic.GetLibraryManager();
        return mgr?.GetAllLibraries() ?? Enumerable.Empty<ILibrary>();
    }
}
