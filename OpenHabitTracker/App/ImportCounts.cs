namespace OpenHabitTracker.App;

public record ImportCounts(int Categories, int Notes, int Tasks, int Habits, int Items, int Completions)
{
    public bool IsEmpty => Categories == 0 && Notes == 0 && Tasks == 0 && Habits == 0 && Items == 0 && Completions == 0;
}
