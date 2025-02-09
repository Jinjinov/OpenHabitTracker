using Microsoft.AspNetCore.Mvc;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataAccessController(IDataAccess dataAccess) : ControllerBase
{
    private readonly IDataAccess _dataAccess = dataAccess;

    [HttpPost("user")]
    public async Task<IActionResult> AddUser([FromBody] UserEntity user)
    {
        await _dataAccess.AddUser(user);
        return Ok();
    }
    [HttpPost("habit")]
    public async Task<IActionResult> AddHabit([FromBody] HabitEntity habit)
    {
        await _dataAccess.AddHabit(habit);
        return Ok();
    }
    [HttpPost("note")]
    public async Task<IActionResult> AddNote([FromBody] NoteEntity note)
    {
        await _dataAccess.AddNote(note);
        return Ok();
    }
    [HttpPost("task")]
    public async Task<IActionResult> AddTask([FromBody] TaskEntity task)
    {
        await _dataAccess.AddTask(task);
        return Ok();
    }
    [HttpPost("time")]
    public async Task<IActionResult> AddTime([FromBody] TimeEntity time)
    {
        await _dataAccess.AddTime(time);
        return Ok();
    }
    [HttpPost("item")]
    public async Task<IActionResult> AddItem([FromBody] ItemEntity item)
    {
        await _dataAccess.AddItem(item);
        return Ok();
    }
    [HttpPost("category")]
    public async Task<IActionResult> AddCategory([FromBody] CategoryEntity category)
    {
        await _dataAccess.AddCategory(category);
        return Ok();
    }
    [HttpPost("priority")]
    public async Task<IActionResult> AddPriority([FromBody] PriorityEntity priority)
    {
        await _dataAccess.AddPriority(priority);
        return Ok();
    }
    [HttpPost("setting")]
    public async Task<IActionResult> AddSettings([FromBody] SettingsEntity settings)
    {
        await _dataAccess.AddSettings(settings);
        return Ok();
    }

    [HttpPost("users")]
    public async Task<IActionResult> AddUsers([FromBody] IReadOnlyCollection<UserEntity> users)
    {
        await _dataAccess.AddUsers(users);
        return Ok();
    }
    [HttpPost("habits")]
    public async Task<IActionResult> AddHabits([FromBody] IReadOnlyCollection<HabitEntity> habits)
    {
        await _dataAccess.AddHabits(habits);
        return Ok();
    }
    [HttpPost("notes")]
    public async Task<IActionResult> AddNotes([FromBody] IReadOnlyCollection<NoteEntity> notes)
    {
        await _dataAccess.AddNotes(notes);
        return Ok();
    }
    [HttpPost("tasks")]
    public async Task<IActionResult> AddTasks([FromBody] IReadOnlyCollection<TaskEntity> tasks)
    {
        await _dataAccess.AddTasks(tasks);
        return Ok();
    }
    [HttpPost("times")]
    public async Task<IActionResult> AddTimes([FromBody] IReadOnlyCollection<TimeEntity> times)
    {
        await _dataAccess.AddTimes(times);
        return Ok();
    }
    [HttpPost("items")]
    public async Task<IActionResult> AddItems([FromBody] IReadOnlyCollection<ItemEntity> items)
    {
        await _dataAccess.AddItems(items);
        return Ok();
    }
    [HttpPost("categories")]
    public async Task<IActionResult> AddCategories([FromBody] IReadOnlyCollection<CategoryEntity> categories)
    {
        await _dataAccess.AddCategories(categories);
        return Ok();
    }
    [HttpPost("priorities")]
    public async Task<IActionResult> AddPriorities([FromBody] IReadOnlyCollection<PriorityEntity> priorities)
    {
        await _dataAccess.AddPriorities(priorities);
        return Ok();
    }
    [HttpPost("settings")]
    public async Task<IActionResult> AddSettingsBatch([FromBody] IReadOnlyCollection<SettingsEntity> settings)
    {
        await _dataAccess.AddSettings(settings);
        return Ok();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _dataAccess.GetUsers();
        return Ok(result);
    }
    [HttpGet("habits")]
    public async Task<IActionResult> GetHabits()
    {
        var result = await _dataAccess.GetHabits();
        return Ok(result);
    }
    [HttpGet("notes")]
    public async Task<IActionResult> GetNotes()
    {
        var result = await _dataAccess.GetNotes();
        return Ok(result);
    }
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks()
    {
        var result = await _dataAccess.GetTasks();
        return Ok(result);
    }
    [HttpGet("times")]
    public async Task<IActionResult> GetTimes(long? habitId = null)
    {
        var result = await _dataAccess.GetTimes(habitId);
        return Ok(result);
    }
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(long? parentId = null)
    {
        var result = await _dataAccess.GetItems(parentId);
        return Ok(result);
    }
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _dataAccess.GetCategories();
        return Ok(result);
    }
    [HttpGet("priorities")]
    public async Task<IActionResult> GetPriorities()
    {
        var result = await _dataAccess.GetPriorities();
        return Ok(result);
    }
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var result = await _dataAccess.GetSettings();
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(long id)
    {
        var result = await _dataAccess.GetUser(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("habits/{id}")]
    public async Task<IActionResult> GetHabit(long id)
    {
        var result = await _dataAccess.GetHabit(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("notes/{id}")]
    public async Task<IActionResult> GetNote(long id)
    {
        var result = await _dataAccess.GetNote(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("tasks/{id}")]
    public async Task<IActionResult> GetTask(long id)
    {
        var result = await _dataAccess.GetTask(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("times/{id}")]
    public async Task<IActionResult> GetTime(long id)
    {
        var result = await _dataAccess.GetTime(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetItem(long id)
    {
        var result = await _dataAccess.GetItem(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategory(long id)
    {
        var result = await _dataAccess.GetCategory(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("priorities/{id}")]
    public async Task<IActionResult> GetPriority(long id)
    {
        var result = await _dataAccess.GetPriority(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("settings/{id}")]
    public async Task<IActionResult> GetSettings(long id)
    {
        var result = await _dataAccess.GetSettings(id);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPut("users")]
    public async Task<IActionResult> UpdateUser([FromBody] UserEntity user)
    {
        await _dataAccess.UpdateUser(user);
        return Ok();
    }
    [HttpPut("habits")]
    public async Task<IActionResult> UpdateHabit([FromBody] HabitEntity habit)
    {
        await _dataAccess.UpdateHabit(habit);
        return Ok();
    }
    [HttpPut("notes")]
    public async Task<IActionResult> UpdateNote([FromBody] NoteEntity note)
    {
        await _dataAccess.UpdateNote(note);
        return Ok();
    }
    [HttpPut("tasks")]
    public async Task<IActionResult> UpdateTask([FromBody] TaskEntity task)
    {
        await _dataAccess.UpdateTask(task);
        return Ok();
    }
    [HttpPut("times")]
    public async Task<IActionResult> UpdateTime([FromBody] TimeEntity time)
    {
        await _dataAccess.UpdateTime(time);
        return Ok();
    }
    [HttpPut("items")]
    public async Task<IActionResult> UpdateItem([FromBody] ItemEntity item)
    {
        await _dataAccess.UpdateItem(item);
        return Ok();
    }
    [HttpPut("categories")]
    public async Task<IActionResult> UpdateCategory([FromBody] CategoryEntity category)
    {
        await _dataAccess.UpdateCategory(category);
        return Ok();
    }
    [HttpPut("priorities")]
    public async Task<IActionResult> UpdatePriority([FromBody] PriorityEntity priority)
    {
        await _dataAccess.UpdatePriority(priority);
        return Ok();
    }
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsEntity settings)
    {
        await _dataAccess.UpdateSettings(settings);
        return Ok();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> RemoveUser(long id)
    {
        await _dataAccess.RemoveUser(id);
        return Ok();
    }
    [HttpDelete("habits/{id}")]
    public async Task<IActionResult> RemoveHabit(long id)
    {
        await _dataAccess.RemoveHabit(id);
        return Ok();
    }
    [HttpDelete("notes/{id}")]
    public async Task<IActionResult> RemoveNote(long id)
    {
        await _dataAccess.RemoveNote(id);
        return Ok();
    }
    [HttpDelete("tasks/{id}")]
    public async Task<IActionResult> RemoveTask(long id)
    {
        await _dataAccess.RemoveTask(id);
        return Ok();
    }
    [HttpDelete("times/{id}")]
    public async Task<IActionResult> RemoveTime(long id)
    {
        await _dataAccess.RemoveTime(id);
        return Ok();
    }
    [HttpDelete("items/{id}")]
    public async Task<IActionResult> RemoveItem(long id)
    {
        await _dataAccess.RemoveItem(id);
        return Ok();
    }
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> RemoveCategory(long id)
    {
        await _dataAccess.RemoveCategory(id);
        return Ok();
    }
    [HttpDelete("priorities/{id}")]
    public async Task<IActionResult> RemovePriority(long id)
    {
        await _dataAccess.RemovePriority(id);
        return Ok();
    }
    [HttpDelete("settings/{id}")]
    public async Task<IActionResult> RemoveSettings(long id)
    {
        await _dataAccess.RemoveSettings(id);
        return Ok();
    }

    [HttpDelete("users")]
    public async Task<IActionResult> RemoveUsers()
    {
        await _dataAccess.RemoveUsers();
        return Ok();
    }
    [HttpDelete("habits")]
    public async Task<IActionResult> RemoveHabits()
    {
        await _dataAccess.RemoveHabits();
        return Ok();
    }
    [HttpDelete("notes")]
    public async Task<IActionResult> RemoveNotes()
    {
        await _dataAccess.RemoveNotes();
        return Ok();
    }
    [HttpDelete("tasks")]
    public async Task<IActionResult> RemoveTasks()
    {
        await _dataAccess.RemoveTasks();
        return Ok();
    }
    [HttpDelete("times")]
    public async Task<IActionResult> RemoveTimes()
    {
        await _dataAccess.RemoveTimes();
        return Ok();
    }
    [HttpDelete("items")]
    public async Task<IActionResult> RemoveItems()
    {
        await _dataAccess.RemoveItems();
        return Ok();
    }
    [HttpDelete("categories")]
    public async Task<IActionResult> RemoveCategories()
    {
        await _dataAccess.RemoveCategories();
        return Ok();
    }
    [HttpDelete("priorities")]
    public async Task<IActionResult> RemovePriorities()
    {
        await _dataAccess.RemovePriorities();
        return Ok();
    }
    [HttpDelete("settings")]
    public async Task<IActionResult> RemoveSettings()
    {
        await _dataAccess.RemoveSettings();
        return Ok();
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllTables()
    {
        await _dataAccess.ClearAllTables();
        return Ok();
    }
}
