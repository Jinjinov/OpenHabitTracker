using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class PriorityService(ClientState clientState) : IPriorityService
{
    private readonly ClientState _clientState = clientState;

    public IReadOnlyCollection<PriorityModel>? Priorities => _clientState.Priorities?.Values;

    public PriorityModel? SelectedPriority { get; set; }

    public PriorityModel? NewPriority { get; set; }

    public string GetPriorityTitle(Priority priority)
    {
        return priority switch
        {
            Priority.None => "⊘",
            Priority.VeryLow => "︾",
            Priority.Low => "﹀",
            Priority.Medium => "—",
            Priority.High => "︿",
            Priority.VeryHigh => "︽",
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };

        // after we delete and re-load all data, "(long)priority" does not work anymore, because database Id are not 0,1,2,3,4,5 anymore

        /*
        if (priority == Priority.None)
            return "⊘";

        return _clientState.Priorities?.GetValueOrDefault((long)priority)?.Title ?? priority.ToString();
        */
    }

    public async Task Initialize()
    {
        await _clientState.LoadPriorities();

        NewPriority ??= new();
    }

    public void SetSelectedPriority(long? id)
    {
        if (_clientState.Priorities is null)
            return;

        SelectedPriority = id.HasValue && _clientState.Priorities.TryGetValue(id.Value, out PriorityModel? priority) ? priority : null;
    }
    /*
    public async Task AddPriority()
    {
        if (_clientState.Priorities is null || NewPriority is null)
            return;

        PriorityEntity priority = NewPriority.ToEntity();

        await _clientState.DataAccess.AddPriority(priority);

        NewPriority.Id = priority.Id;

        _clientState.Priorities.Add(NewPriority.Id, NewPriority);

        NewPriority = new();
    }
    */
    public async Task UpdatePriority(string title)
    {
        if (Priorities is null || SelectedPriority is null)
            return;

        SelectedPriority.Title = title;

        if (await _clientState.DataAccess.GetPriority(SelectedPriority.Id) is PriorityEntity priority)
        {
            priority.Title = SelectedPriority.Title;

            await _clientState.DataAccess.UpdatePriority(priority);
        }

        SelectedPriority = null;
    }
    /*
    public async Task DeletePriority(PriorityModel priority)
    {
        if (_clientState.Priorities is null)
            return;

        _clientState.Priorities.Remove(priority.Id);

        await _clientState.DataAccess.RemovePriority(priority.Id);
    }
    */
}
