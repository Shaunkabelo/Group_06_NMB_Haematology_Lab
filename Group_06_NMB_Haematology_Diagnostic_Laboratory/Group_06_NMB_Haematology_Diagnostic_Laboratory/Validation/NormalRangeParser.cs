using System.Globalization;
using System.Text.RegularExpressions;
namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;
public static class NormalRangeParser
{
    public const string Error = "Enter a number with up to 8 digits before and 4 after one comma or dot. Do not use thousands separators.";
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0; text = text?.Trim();
        return text != null && Regex.IsMatch(text, @"^-?[0-9]{1,8}([.,][0-9]{1,4})?$")
            && decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);
    }
}