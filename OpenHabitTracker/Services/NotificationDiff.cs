namespace OpenHabitTracker.Services;

// What changes between the notifications a platform holds and the ones it should hold.
// Every platform call costs a call into another process, so a rebuild hands the platform only these.
public static class NotificationDiff
{
    public sealed record Changes<TKey>(List<TKey> Removed, List<TKey> Added, List<TKey> Fired);

    // Removed: held, and not wanted any more or wanted with another moment, title, body or route.
    // Added: wanted, and not held or held with another moment, title, body or route.
    // Fired: held with a moment that has passed, which the platform has shown already; the caller forgets them without a call.
    public static Changes<TKey> Compare<TKey>(IReadOnlyDictionary<TKey, NotificationRequest> held, IReadOnlyDictionary<TKey, NotificationRequest> wanted, DateTime now)
        where TKey : notnull
    {
        List<TKey> removed = new();
        List<TKey> fired = new();

        foreach ((TKey key, NotificationRequest request) in held)
        {
            if (request.NotifyAt <= now)
                fired.Add(key);
            else if (!wanted.TryGetValue(key, out NotificationRequest? wantedRequest) || !IsSame(request, wantedRequest))
                removed.Add(key);
        }

        List<TKey> added = new();

        foreach ((TKey key, NotificationRequest request) in wanted)
        {
            if (!held.TryGetValue(key, out NotificationRequest? heldRequest) || heldRequest.NotifyAt <= now || !IsSame(heldRequest, request))
                added.Add(key);
        }

        return new Changes<TKey>(removed, added, fired);
    }

    private static bool IsSame(NotificationRequest a, NotificationRequest b)
    {
        return a.NotifyAt == b.NotifyAt
            && string.Equals(a.Title, b.Title, StringComparison.Ordinal)
            && string.Equals(a.Body, b.Body, StringComparison.Ordinal)
            && string.Equals(a.Route, b.Route, StringComparison.Ordinal);
    }

    // Android and Apple identify a notification by a number. A number derived from the request id stays the same
    // from one rebuild to the next, so an unchanged request is recognized, also after the app starts again.
    // Two ids with the same hash: the one first in ordinal order keeps it, and the other takes the next free number.
    public static Dictionary<int, NotificationRequest> Numbered(IEnumerable<NotificationRequest> requests)
    {
        Dictionary<int, NotificationRequest> numbered = new();

        foreach (NotificationRequest request in requests.OrderBy(request => request.Id, StringComparer.Ordinal))
        {
            int number = StableNumber(request.Id);

            while (numbered.ContainsKey(number))
                number = number == int.MaxValue ? 1 : number + 1;

            numbered[number] = request;
        }

        return numbered;
    }

    // FNV-1a over the UTF-16 code units, which unlike string.GetHashCode is the same in every process.
    // Positive and never 0.
    public static int StableNumber(string id)
    {
        uint hash = 2166136261;

        foreach (char c in id)
        {
            hash ^= c;
            hash *= 16777619;
        }

        int number = (int)(hash & 0x7FFFFFFF);

        return number == 0 ? 1 : number;
    }
}
