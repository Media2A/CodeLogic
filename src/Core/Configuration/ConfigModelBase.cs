using System.ComponentModel.DataAnnotations;

namespace CodeLogic.Core.Configuration;

/// <summary>
/// Base class for all CodeLogic configuration models.
/// Inherit from this and use DataAnnotations attributes for validation.
/// Override Validate() for custom validation logic.
/// </summary>
public abstract class ConfigModelBase
{
    /// <summary>
    /// Validates this configuration model using DataAnnotations and any
    /// custom logic in overriding classes.
    /// </summary>
    public virtual ConfigValidationResult Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(this, context, results, validateAllProperties: true))
            return ConfigValidationResult.Valid();

        var errors = results
            .Select(r => r.ErrorMessage ?? "Unknown validation error")
            .ToList();

        return ConfigValidationResult.Invalid(errors);
    }
}

/// <summary>
/// Marks a configuration model with a section name used in file naming.
/// Example: [ConfigSection("database")] → config.database.json
/// Without this attribute, the file name is config.json.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigSectionAttribute : Attribute
{
    public string SectionName { get; }
    public int Version { get; init; } = 1;

    public ConfigSectionAttribute(string sectionName)
    {
        SectionName = sectionName;
    }
}
