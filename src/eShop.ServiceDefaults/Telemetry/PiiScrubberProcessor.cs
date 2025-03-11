using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace eShop.ServiceDefaults.Telemetry;

public class PiiScrubberProcessor : BaseProcessor<Activity>
{
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "email", "card", "password", "creditcard", "credit", "phone", "address", "ssn", "username", "cvv", "db.user", "db.connection_string"
    };

    private string MaskEmail(string value)
    {
        return EmailPattern.Replace(value, match =>
        {
            var email = match.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***@***";

            return email[0] + "***" + email.Substring(atIndex);
        });
    }

    private string MaskCreditCard(string value)
    {
        return CreditCardPattern.Replace(value, match => "****-****-****-" + match.Value.Substring(match.Value.Length - 4));
    }

    private string ScrubValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        value = MaskEmail(value);
        value = MaskCreditCard(value);

        return value;
    }

    private bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        return SensitiveKeys.Any(k => key.ToLowerInvariant().Contains(k));
    }

    public override void OnEnd(Activity activity)
    {
        if (activity == null) return;

        // Process activity tags
        foreach (var tag in activity.Tags.ToList())
        {
            if (IsSensitiveKey(tag.Key) || EmailPattern.IsMatch(tag.Key) || CreditCardPattern.IsMatch(tag.Key))
            {
                int length = tag.Value?.Length ?? 0;
                activity.SetTag(tag.Key, new string('*', length));
            }
        }

        base.OnEnd(activity);
    }
}
