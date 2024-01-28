using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class PriorityService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<PriorityModel>? Priorities => _appData.Priorities?.Values;

    public PriorityModel? SelectedPriority { get; set; }

    public PriorityModel? NewPriority { get; set; }

    public PriorityModel? GetPriority(Priority priority)
    {
        return _appData.Priorities?.GetValueOrDefault((long)priority);
    }

    public string GetPriorityTitle(Priority priority)
    {
        return _appData.Priorities?.GetValueOrDefault((long)priority)?.Title ?? priority.ToString();
    }

    public async Task Initialize()
    {
        await _appData.InitializePriorities();

        NewPriority ??= new();
    }

    public void SetSelectedPriority(long? id)
    {
        if (_appData.Priorities is null)
            return;

        SelectedPriority = id.HasValue && _appData.Priorities.TryGetValue(id.Value, out PriorityModel? priority) ? priority : null;
    }

    public async Task AddPriority()
    {
        if (_appData.Priorities is null || NewPriority is null)
            return;

        PriorityEntity priority = new()
        {
            Title = NewPriority.Title
        };

        await _dataAccess.AddPriority(priority);

        NewPriority.Id = priority.Id;

        _appData.Priorities.Add(NewPriority.Id, NewPriority);

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
        if (_appData.Priorities is null)
            return;

        _appData.Priorities.Remove(priority.Id);

        await _dataAccess.RemovePriority(priority.Id);
    }
}
