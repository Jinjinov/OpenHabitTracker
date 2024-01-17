using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class PriorityService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public Dictionary<long, PriorityModel>? Priorities { get; set; }

    public PriorityModel? SelectedPriority { get; set; }

    public PriorityModel? NewPriority { get; set; }

    public async Task Initialize()
    {
        if (Priorities is null)
        {
            IReadOnlyList<PriorityEntity> priorities = await _dataAccess.GetPriorities();
            Priorities = priorities.Select(c => new PriorityModel
            {
                Id = c.Id,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }

        NewPriority ??= new();
    }

    public void SetSelectedPriority(long? id)
    {
        if (Priorities is null)
            return;

        SelectedPriority = id.HasValue && Priorities.TryGetValue(id.Value, out PriorityModel? priority) ? priority : null;
    }

    public async Task AddPriority()
    {
        if (Priorities is null || NewPriority is null)
            return;

        PriorityEntity priority = new()
        {
            Title = NewPriority.Title
        };

        await _dataAccess.AddPriority(priority);

        NewPriority.Id = priority.Id;

        Priorities.Add(NewPriority.Id, NewPriority);

        NewPriority = new();
    }

    public async Task UpdatePriority(string title)
    {
        if (Priorities is null || SelectedPriority is null)
            return;

        SelectedPriority.Title = title;

        if (await _dataAccess.GetPriority(SelectedPriority.Id) is PriorityEntity priority)
        {
            priority.Title = SelectedPriority.Title;

            await _dataAccess.UpdatePriority(priority);
        }

        SelectedPriority = null;
    }

    public async Task DeletePriority(PriorityModel priority)
    {
        if (Priorities is null)
            return;

        Priorities.Remove(priority.Id);

        await _dataAccess.RemovePriority(priority.Id);
    }
}
