using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
public class QueryController(IDataAccess dataAccess, MarkdownToHtml markdownToHtml) : ControllerBase
{
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;

    [HttpPost("habits")]
    [EndpointName("Habits")]
    public async Task<ActionResult<IReadOnlyList<HabitModel>>> Habits([FromBody] QueryParameters queryParameters)
    {
        ClientData clientData = new(_dataAccess, _markdownToHtml);

        IEnumerable<HabitModel> result = await clientData.GetHabits(queryParameters);

        return Ok(result.ToList());
    }

    [HttpPost("notes")]
    [EndpointName("Notes")]
    public async Task<ActionResult<IReadOnlyList<NoteModel>>> Notes([FromBody] QueryParameters queryParameters)
    {
        ClientData clientData = new(_dataAccess, _markdownToHtml);

        IEnumerable<NoteModel> result = await clientData.GetNotes(queryParameters);

        return Ok(result.ToList());
    }

    [HttpPost("tasks")]
    [EndpointName("Tasks")]
    public async Task<ActionResult<IReadOnlyList<TaskModel>>> Tasks([FromBody] QueryParameters queryParameters)
    {
        ClientData clientData = new(_dataAccess, _markdownToHtml);

        IEnumerable<TaskModel> result = await clientData.GetTasks(queryParameters);

        return Ok(result.ToList());
    }
}
