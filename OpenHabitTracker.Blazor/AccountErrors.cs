using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace OpenHabitTracker.Blazor;

// The account API answers a refused request with a list of messages, a validation problem, or a plain text.
public static class AccountErrors
{
    public static IReadOnlyList<string> From(int status, string? body, IStringLocalizer loc)
    {
        if (status is 400 or 403 && !string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(body);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                    return document.RootElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList();

                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
                    return errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList();
            }
            catch (JsonException)
            {
                return [body];
            }
        }

        return [loc["Connection error"]];
    }
}
