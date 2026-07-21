using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace FitTracker.TagHelpers;

/// <summary>
/// Projects the DataAnnotations already declared on the page models onto the
/// equivalent native HTML5 constraint-validation attributes, so the browser
/// enforces the same rules the server does without those rules being restated
/// in the markup. The models stay the single source of truth.
/// </summary>
/// <remarks>
/// <see cref="CompareAttribute"/> (password confirmation) has no HTML5
/// equivalent — cross-field comparison is not expressible — so it remains
/// server-side only.
/// </remarks>
[HtmlTargetElement("input", Attributes = ForAttributeName)]
[HtmlTargetElement("textarea", Attributes = ForAttributeName)]
[HtmlTargetElement("select", Attributes = ForAttributeName)]
public class Html5ValidationTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    /// <summary>Input types that accept minlength/maxlength.</summary>
    private static readonly string[] TextLikeTypes =
        ["text", "email", "password", "search", "tel", "url"];

    /// <summary>
    /// Types that either cannot express these constraints, or that `required`
    /// would make impossible to satisfy — a [Required] bool must still accept
    /// false, so marking its checkbox required would force it checked.
    /// </summary>
    private static readonly string[] SkippedTypes =
        ["checkbox", "radio", "hidden", "file", "submit", "button", "image", "reset"];

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; } = default!;

    /// <summary>
    /// Runs after the built-in InputTagHelper (order 0) so the input type it
    /// resolved is visible here.
    /// </summary>
    public override int Order => 100;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // <textarea>/<select> have no type attribute; treat that as text-like.
        var inputType = output.Attributes["type"]?.Value?.ToString()?.ToLowerInvariant();

        if (inputType is not null && SkippedTypes.Contains(inputType))
        {
            return;
        }

        var modelType = Nullable.GetUnderlyingType(For.Metadata.ModelType) ?? For.Metadata.ModelType;
        if (modelType == typeof(bool))
        {
            return;
        }

        var isTextLike = inputType is null || TextLikeTypes.Contains(inputType);
        var isNumeric = inputType is "number" or "range";

        foreach (var validator in For.Metadata.ValidatorMetadata)
        {
            switch (validator)
            {
                case RequiredAttribute:
                    SetIfAbsent(output, "required", "required");
                    break;

                case StringLengthAttribute stringLength when isTextLike:
                    if (stringLength.MaximumLength > 0)
                    {
                        SetIfAbsent(output, "maxlength", Format(stringLength.MaximumLength));
                    }
                    if (stringLength.MinimumLength > 0)
                    {
                        SetIfAbsent(output, "minlength", Format(stringLength.MinimumLength));
                    }
                    break;

                case MinLengthAttribute minLength when isTextLike:
                    SetIfAbsent(output, "minlength", Format(minLength.Length));
                    break;

                case MaxLengthAttribute maxLength when isTextLike:
                    SetIfAbsent(output, "maxlength", Format(maxLength.Length));
                    break;

                // Range on a date/time field would need ISO formatting rather than
                // the raw operand, so only numeric inputs are mapped.
                case RangeAttribute range when isNumeric:
                    if (range.Minimum is not null)
                    {
                        SetIfAbsent(output, "min", Format(range.Minimum));
                    }
                    if (range.Maximum is not null)
                    {
                        SetIfAbsent(output, "max", Format(range.Maximum));
                    }
                    break;

                case RegularExpressionAttribute regex when isTextLike:
                    SetIfAbsent(output, "pattern", regex.Pattern);
                    break;
            }
        }
    }

    /// <summary>Never overwrite an attribute the view set explicitly.</summary>
    private static void SetIfAbsent(TagHelperOutput output, string name, string value)
    {
        if (!output.Attributes.ContainsName(name))
        {
            output.Attributes.SetAttribute(name, value);
        }
    }

    private static string Format(object value) =>
        Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}
