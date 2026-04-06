namespace CodeLogic.Core.Utilities;

/// <summary>
/// Represents a semantic version in the form major.minor.patch.
/// </summary>
public sealed class SemanticVersion : IComparable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public SemanticVersion(int major, int minor, int patch)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Parses a semantic version string in the form "major.minor.patch".
    /// Throws <see cref="FormatException"/> on invalid input.
    /// </summary>
    public static SemanticVersion Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Version string cannot be null or empty.");

        var parts = value.Trim().Split('.');
        if (parts.Length != 3)
            throw new FormatException($"Invalid semantic version format: '{value}'. Expected 'major.minor.patch'.");

        if (!int.TryParse(parts[0], out int major) || major < 0)
            throw new FormatException($"Invalid major version component: '{parts[0]}'.");

        if (!int.TryParse(parts[1], out int minor) || minor < 0)
            throw new FormatException($"Invalid minor version component: '{parts[1]}'.");

        if (!int.TryParse(parts[2], out int patch) || patch < 0)
            throw new FormatException($"Invalid patch version component: '{parts[2]}'.");

        return new SemanticVersion(major, minor, patch);
    }

    /// <summary>
    /// Attempts to parse a semantic version string. Returns false on invalid input.
    /// </summary>
    public static bool TryParse(string? value, out SemanticVersion? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            result = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;

        int cmp = Major.CompareTo(other.Major);
        if (cmp != 0) return cmp;

        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0) return cmp;

        return Patch.CompareTo(other.Patch);
    }

    public override bool Equals(object? obj) =>
        obj is SemanticVersion other && CompareTo(other) == 0;

    public override int GetHashCode() =>
        HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.CompareTo(right) == 0;

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) =>
        !(left == right);

    public static bool operator <(SemanticVersion? left, SemanticVersion? right)
    {
        if (left is null) return right is not null;
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(SemanticVersion? left, SemanticVersion? right)
    {
        if (left is null) return false;
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) =>
        !(left > right);

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) =>
        !(left < right);
}
