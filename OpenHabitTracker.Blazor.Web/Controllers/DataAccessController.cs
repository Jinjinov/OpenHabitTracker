using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DataAccessController(IDataAccess dataAccess) : ControllerBase
{
    private readonly IDataAccess _dataAccess = dataAccess;

    [HttpPost("user")]
    [EndpointName("AddUser")]
    public async Task<IActionResult> AddUser([FromBody] UserEntity user)
    {
        await _dataAccess.AddUser(user);
        return Ok();
    }
    [HttpPost("habit")]
    [EndpointName("AddHabit")]
    public async Task<IActionResult> AddHabit([FromBody] HabitEntity habit)
    {
        await _dataAccess.AddHabit(habit);
        return Ok();
    }
    [HttpPost("note")]
    [EndpointName("AddNote")]
    public async Task<IActionResult> AddNote([FromBody] NoteEntity note)
    {
        await _dataAccess.AddNote(note);
        return Ok();
    }
    [HttpPost("task")]
    [EndpointName("AddTask")]
    public async Task<IActionResult> AddTask([FromBody] TaskEntity task)
    {
        await _dataAccess.AddTask(task);
        return Ok();
    }
    [HttpPost("time")]
    [EndpointName("AddTime")]
    public async Task<IActionResult> AddTime([FromBody] TimeEntity time)
    {
        await _dataAccess.AddTime(time);
        return Ok();
    }
    [HttpPost("item")]
    [EndpointName("AddItem")]
    public async Task<IActionResult> AddItem([FromBody] ItemEntity item)
    {
        await _dataAccess.AddItem(item);
        return Ok();
    }
    [HttpPost("category")]
    [EndpointName("AddCategory")]
    public async Task<IActionResult> AddCategory([FromBody] CategoryEntity category)
    {
        await _dataAccess.AddCategory(category);
        return Ok();
    }
    [HttpPost("priority")]
    [EndpointName("AddPriority")]
    public async Task<IActionResult> AddPriority([FromBody] PriorityEntity priority)
    {
        await _dataAccess.AddPriority(priority);
        return Ok();
    }
    [HttpPost("setting")]
    [EndpointName("AddSetting")]
    public async Task<IActionResult> AddSettings([FromBody] SettingsEntity settings)
    {
        await _dataAccess.AddSettings(settings);
        return Ok();
    }

    [HttpPost("users")]
    [EndpointName("AddUsers")]
    public async Task<IActionResult> AddUsers([FromBody] IReadOnlyCollection<UserEntity> users)
    {
        await _dataAccess.AddUsers(users);
        return Ok();
    }
    [HttpPost("habits")]
    [EndpointName("AddHabits")]
    public async Task<IActionResult> AddHabits([FromBody] IReadOnlyCollection<HabitEntity> habits)
    {
        await _dataAccess.AddHabits(habits);
        return Ok();
    }
    [HttpPost("notes")]
    [EndpointName("AddNotes")]
    public async Task<IActionResult> AddNotes([FromBody] IReadOnlyCollection<NoteEntity> notes)
    {
        await _dataAccess.AddNotes(notes);
        return Ok();
    }
    [HttpPost("tasks")]
    [EndpointName("AddTasks")]
    public async Task<IActionResult> AddTasks([FromBody] IReadOnlyCollection<TaskEntity> tasks)
    {
        await _dataAccess.AddTasks(tasks);
        return Ok();
    }
    [HttpPost("times")]
    [EndpointName("AddTimes")]
    public async Task<IActionResult> AddTimes([FromBody] IReadOnlyCollection<TimeEntity> times)
    {
        await _dataAccess.AddTimes(times);
        return Ok();
    }
    [HttpPost("items")]
    [EndpointName("AddItems")]
    public async Task<IActionResult> AddItems([FromBody] IReadOnlyCollection<ItemEntity> items)
    {
        await _dataAccess.AddItems(items);
        return Ok();
    }
    [HttpPost("categories")]
    [EndpointName("AddCategories")]
    public async Task<IActionResult> AddCategories([FromBody] IReadOnlyCollection<CategoryEntity> categories)
    {
        await _dataAccess.AddCategories(categories);
        return Ok();
    }
    [HttpPost("priorities")]
    [EndpointName("AddPriorities")]
    public async Task<IActionResult> AddPriorities([FromBody] IReadOnlyCollection<PriorityEntity> priorities)
    {
        await _dataAccess.AddPriorities(priorities);
        return Ok();
    }
    [HttpPost("settings")]
    [EndpointName("AddSettings")]
    public async Task<IActionResult> AddSettings([FromBody] IReadOnlyCollection<SettingsEntity> settings)
    {
        await _dataAccess.AddSettings(settings);
        return Ok();
    }

    [HttpGet("users")]
    [EndpointName("GetUsers")]
    public async Task<ActionResult<IReadOnlyList<UserEntity>>> GetUsers()
    {
        var result = await _dataAccess.GetUsers();
        return Ok(result);
    }
    [HttpGet("habits")]
    [EndpointName("GetHabits")]
    public async Task<ActionResult<IReadOnlyList<HabitEntity>>> GetHabits()
    {
        var result = await _dataAccess.GetHabits();
        return Ok(result);
    }
    [HttpGet("notes")]
    [EndpointName("GetNotes")]
    public async Task<ActionResult<IReadOnlyList<NoteEntity>>> GetNotes()
    {
        var result = await _dataAccess.GetNotes();
        return Ok(result);
    }
    [HttpGet("tasks")]
    [EndpointName("GetTasks")]
    public async Task<ActionResult<IReadOnlyList<TaskEntity>>> GetTasks()
    {
        var result = await _dataAccess.GetTasks();
        return Ok(result);
    }
    [HttpGet("times")]
    [EndpointName("GetTimes")]
    public async Task<ActionResult<IReadOnlyList<TimeEntity>>> GetTimes(long? habitId = null)
    {
        var result = await _dataAccess.GetTimes(habitId);
        return Ok(result);
    }
    [HttpGet("items")]
    [EndpointName("GetItems")]
    public async Task<ActionResult<IReadOnlyList<ItemEntity>>> GetItems(long? parentId = null)
    {
        var result = await _dataAccess.GetItems(parentId);
        return Ok(result);
    }
    [HttpGet("categories")]
    [EndpointName("GetCategories")]
    public async Task<ActionResult<IReadOnlyList<CategoryEntity>>> GetCategories()
    {
        var result = await _dataAccess.GetCategories();
        return Ok(result);
    }
    [HttpGet("priorities")]
    [EndpointName("GetPriorities")]
    public async Task<ActionResult<IReadOnlyList<PriorityEntity>>> GetPriorities()
    {
        var result = await _dataAccess.GetPriorities();
        return Ok(result);
    }
    [HttpGet("settings")]
    [EndpointName("GetSettings")]
    public async Task<ActionResult<IReadOnlyList<SettingsEntity>>> GetSettings()
    {
        var result = await _dataAccess.GetSettings();
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    [EndpointName("GetUser")]
    public async Task<ActionResult<UserEntity?>> GetUser(long id)
    {
        var result = await _dataAccess.GetUser(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("habits/{id}")]
    [EndpointName("GetHabit")]
    public async Task<ActionResult<HabitEntity?>> GetHabit(long id)
    {
        var result = await _dataAccess.GetHabit(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("notes/{id}")]
    [EndpointName("GetNote")]
    public async Task<ActionResult<NoteEntity?>> GetNote(long id)
    {
        var result = await _dataAccess.GetNote(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("tasks/{id}")]
    [EndpointName("GetTask")]
    public async Task<ActionResult<TaskEntity?>> GetTask(long id)
    {
        var result = await _dataAccess.GetTask(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("times/{id}")]
    [EndpointName("GetTime")]
    public async Task<ActionResult<TimeEntity?>> GetTime(long id)
    {
        var result = await _dataAccess.GetTime(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("items/{id}")]
    [EndpointName("GetItem")]
    public async Task<ActionResult<ItemEntity?>> GetItem(long id)
    {
        var result = await _dataAccess.GetItem(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("categories/{id}")]
    [EndpointName("GetCategory")]
    public async Task<ActionResult<CategoryEntity?>> GetCategory(long id)
    {
        var result = await _dataAccess.GetCategory(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("priorities/{id}")]
    [EndpointName("GetPriority")]
    public async Task<ActionResult<PriorityEntity?>> GetPriority(long id)
    {
        var result = await _dataAccess.GetPriority(id);
        return result != null ? Ok(result) : NotFound();
    }
    [HttpGet("settings/{id}")]
    [EndpointName("GetSetting")]
    public async Task<ActionResult<SettingsEntity?>> GetSettings(long id)
    {
        var result = await _dataAccess.GetSettings(id);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPut("users")]
    [EndpointName("UpdateUser")]
    public async Task<IActionResult> UpdateUser([FromBody] UserEntity user)
    {
        await _dataAccess.UpdateUser(user);
        return Ok();
    }
    [HttpPut("habits")]
    [EndpointName("UpdateHabit")]
    public async Task<IActionResult> UpdateHabit([FromBody] HabitEntity habit)
    {
        await _dataAccess.UpdateHabit(habit);
        return Ok();
    }
    [HttpPut("notes")]
    [EndpointName("UpdateNote")]
    public async Task<IActionResult> UpdateNote([FromBody] NoteEntity note)
    {
        await _dataAccess.UpdateNote(note);
        return Ok();
    }
    [HttpPut("tasks")]
    [EndpointName("UpdateTask")]
    public async Task<IActionResult> UpdateTask([FromBody] TaskEntity task)
    {
        await _dataAccess.UpdateTask(task);
        return Ok();
    }
    [HttpPut("times")]
    [EndpointName("UpdateTime")]
    public async Task<IActionResult> UpdateTime([FromBody] TimeEntity time)
    {
        await _dataAccess.UpdateTime(time);
        return Ok();
    }
    [HttpPut("items")]
    [EndpointName("UpdateItem")]
    public async Task<IActionResult> UpdateItem([FromBody] ItemEntity item)
    {
        await _dataAccess.UpdateItem(item);
        return Ok();
    }
    [HttpPut("categories")]
    [EndpointName("UpdateCategory")]
    public async Task<IActionResult> UpdateCategory([FromBody] CategoryEntity category)
    {
        await _dataAccess.UpdateCategory(category);
        return Ok();
    }
    [HttpPut("priorities")]
    [EndpointName("UpdatePriority")]
    public async Task<IActionResult> UpdatePriority([FromBody] PriorityEntity priority)
    {
        await _dataAccess.UpdatePriority(priority);
        return Ok();
    }
    [HttpPut("settings")]
    [EndpointName("UpdateSettings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsEntity settings)
    {
        await _dataAccess.UpdateSettings(settings);
        return Ok();
    }

    [HttpDelete("users/{id}")]
    [EndpointName("RemoveUser")]
    public async Task<IActionResult> RemoveUser(long id)
    {
        await _dataAccess.RemoveUser(id);
        return Ok();
    }
    [HttpDelete("habits/{id}")]
    [EndpointName("RemoveHabit")]
    public async Task<IActionResult> RemoveHabit(long id)
    {
        await _dataAccess.RemoveHabit(id);
        return Ok();
    }
    [HttpDelete("notes/{id}")]
    [EndpointName("RemoveNote")]
    public async Task<IActionResult> RemoveNote(long id)
    {
        await _dataAccess.RemoveNote(id);
        return Ok();
    }
    [HttpDelete("tasks/{id}")]
    [EndpointName("RemoveTask")]
    public async Task<IActionResult> RemoveTask(long id)
    {
        await _dataAccess.RemoveTask(id);
        return Ok();
    }
    [HttpDelete("times/{id}")]
    [EndpointName("RemoveTime")]
    public async Task<IActionResult> RemoveTime(long id)
    {
        await _dataAccess.RemoveTime(id);
        return Ok();
    }
    [HttpDelete("items/{id}")]
    [EndpointName("RemoveItem")]
    public async Task<IActionResult> RemoveItem(long id)
    {
        await _dataAccess.RemoveItem(id);
        return Ok();
    }
    [HttpDelete("categories/{id}")]
    [EndpointName("RemoveCategory")]
    public async Task<IActionResult> RemoveCategory(long id)
    {
        await _dataAccess.RemoveCategory(id);
        return Ok();
    }
    [HttpDelete("priorities/{id}")]
    [EndpointName("RemovePriority")]
    public async Task<IActionResult> RemovePriority(long id)
    {
        await _dataAccess.RemovePriority(id);
        return Ok();
    }
    [HttpDelete("settings/{id}")]
    [EndpointName("RemoveSetting")]
    public async Task<IActionResult> RemoveSettings(long id)
    {
        await _dataAccess.RemoveSettings(id);
        return Ok();
    }

    [HttpDelete("users")]
    [EndpointName("RemoveUsers")]
    public async Task<IActionResult> RemoveUsers()
    {
        await _dataAccess.RemoveUsers();
        return Ok();
    }
    [HttpDelete("habits")]
    [EndpointName("RemoveHabits")]
    public async Task<IActionResult> RemoveHabits()
    {
        await _dataAccess.RemoveHabits();
        return Ok();
    }
    [HttpDelete("notes")]
    [EndpointName("RemoveNotes")]
    public async Task<IActionResult> RemoveNotes()
    {
        await _dataAccess.RemoveNotes();
        return Ok();
    }
    [HttpDelete("tasks")]
    [EndpointName("RemoveTasks")]
    public async Task<IActionResult> RemoveTasks()
    {
        await _dataAccess.RemoveTasks();
        return Ok();
    }
    [HttpDelete("times")]
    [EndpointName("RemoveTimes")]
    public async Task<IActionResult> RemoveTimes()
    {
        await _dataAccess.RemoveTimes();
        return Ok();
    }
    [HttpDelete("items")]
    [EndpointName("RemoveItems")]
    public async Task<IActionResult> RemoveItems()
    {
        await _dataAccess.RemoveItems();
        return Ok();
    }
    [HttpDelete("categories")]
    [EndpointName("RemoveCategories")]
    public async Task<IActionResult> RemoveCategories()
    {
        await _dataAccess.RemoveCategories();
        return Ok();
    }
    [HttpDelete("priorities")]
    [EndpointName("RemovePriorities")]
    public async Task<IActionResult> RemovePriorities()
    {
        await _dataAccess.RemovePriorities();
        return Ok();
    }
    [HttpDelete("settings")]
    [EndpointName("RemoveSettings")]
    public async Task<IActionResult> RemoveSettings()
    {
        await _dataAccess.RemoveSettings();
        return Ok();
    }

    [HttpDelete("clear-all")]
    [EndpointName("ClearAllTables")]
    public async Task<IActionResult> ClearAllTables()
    {
        await _dataAccess.ClearAllTables();
        return Ok();
    }
}
