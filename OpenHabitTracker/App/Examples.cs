using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

public class Examples(MarkdownToHtml markdownToHtml)
{
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;

    public UserImportExportData GetWelcomeNote(UserModel user, SettingsModel settings)
    {
        DateTime now = DateTime.Now;

        string markdown =
            """
            **A few tips:**
            - click on the title of this note to enter edit mode where you can also change the priority (`⊘`,`︾`,`﹀`,`—`,`︿`,`︽`)
            - to exit edit mode click on `Close`
            - in `Settings` you can customize the look and feel of OpenHT and set which screen opens every time you start OpenHT
            - load examples in the `Data` → `Load examples` submenu
            - use `Search, Filter, Sort` to display only what you want to focus on

            *Feedback is welcome on [Reddit](https://www.reddit.com/r/OpenHabitTracker) and [GitHub](https://github.com/Jinjinov/OpenHabitTracker/discussions)*
            """;

        UserImportExportData userData = new()
        {
            Settings = settings,
            Categories =
            [
                new()
                {
                    UserId = user.Id,
                    Title = "Welcome",
                    Notes =
                    [
                        new()
                        {
                            Title = "Welcome!",
                            Priority = Priority.None,
                            Content = markdown,
                            ContentMarkdown = _markdownToHtml.GetMarkdown(markdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                }
            ]
        };
        return userData;
    }

    public UserImportExportData GetExamples(UserModel user, SettingsModel settings)
    {
        DateTime now = DateTime.Now;

        string markdown =
            """
            # Markdown
            ## Heading
                indented
                code
                block
            **bold**
            *italic*
            ***bold and italic***
            `code`
            [OpenHabitTracker](https://openhabittracker.net)
            - one item
            - another item
            ---
            1. first item
            2. second item

            > blockquote
            """;

        string extendedMarkdown =
            """
            here is a footnote[^1]
            [^1]: this is the footnote

            ```
            fenced
            code
            block
            ```
            ~~strikethrough~~
            ==highlight==

            | table | with   | headers |
            | :---  | :---:  |    ---: |
            | left  | middle | right   |

            subscript: H~2~O
            superscript: X^2^
            """;

        UserImportExportData userData = new()
        {
            Settings = settings,
            Categories =
            [
                new()
                {
                    UserId = user.Id,
                    Title = "Examples",
                    Notes =
                    [
                        new()
                        {
                            Title = "Markdown example",
                            Priority = Priority.Medium,
                            Content = markdown,
                            ContentMarkdown = _markdownToHtml.GetMarkdown(markdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        },
                        new()
                        {
                            Title = "Extended Markdown example",
                            Priority = Priority.Medium,
                            Content = extendedMarkdown,
                            ContentMarkdown = _markdownToHtml.GetMarkdown(extendedMarkdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = user.Id,
                    Title = "Work",
                    Notes =
                    [
                        new()
                        {
                            Title = "Meeting Notes",
                            Priority = Priority.Medium,
                            Content = "Discuss project milestones\nAssign tasks to team members\nReview budget allocation",
                            ContentMarkdown = _markdownToHtml.GetMarkdown("Discuss project milestones\nAssign tasks to team members\nReview budget allocation"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Prepare Project Report",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Collect data from team" },
                                new() { Title = "Draft the report" },
                                new() { Title = "Review with manager" }
                            ],
                            PlannedAt = now.AddDays(1),
                            Duration = new TimeOnly(2,0),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Daily Code Review",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Review pull requests" },
                                new() { Title = "Comment on code quality" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(0,45),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-1), CompletedAt = now.AddDays(-1) }
                            ],
                            LastTimeDoneAt = now.AddDays(-1),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = user.Id,
                    Title = "Personal Development",
                    Notes =
                    [
                        new()
                        {
                            Title = "Book Summary: Atomic Habits",
                            Priority = Priority.Low,
                            Content = "Key concepts: Habit stacking, 1% improvement, Cue-Routine-Reward loop",
                            ContentMarkdown = _markdownToHtml.GetMarkdown("Key concepts: Habit stacking, 1% improvement, Cue-Routine-Reward loop"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Complete Online Course",
                            Priority = Priority.Medium,
                            Items =
                            [
                                new() { Title = "Watch module 1" },
                                new() { Title = "Complete module 1 quiz" }
                            ],
                            PlannedAt = now.AddDays(3),
                            Duration = new TimeOnly(1,0),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Morning Meditation",
                            Priority = Priority.Medium,
                            Items =
                            [
                                new() { Title = "Find a quiet place" },
                                new() { Title = "Focus on breathing" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(0,20),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-2), CompletedAt = now.AddDays(-2) },
                                new() { StartedAt = now.AddMinutes(-20), CompletedAt = now }
                            ],
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = user.Id,
                    Title = "Health & Fitness",
                    Notes =
                    [
                        new()
                        {
                            Title = "Diet Plan",
                            Priority = Priority.Low,
                            Content = "Breakfast: Oatmeal with fruits\nLunch: Grilled chicken with salad\nDinner: Steamed veggies with quinoa",
                            ContentMarkdown = _markdownToHtml.GetMarkdown("Breakfast: Oatmeal with fruits\nLunch: Grilled chicken with salad\nDinner: Steamed veggies with quinoa"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Grocery Shopping",
                            Priority = Priority.Low,
                            Items =
                            [
                                new() { Title = "Buy fruits and vegetables" },
                                new() { Title = "Get whole grains" },
                                new() { Title = "Restock on lean proteins" }
                            ],
                            PlannedAt = now.AddDays(2),
                            Duration = new TimeOnly(1,30),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Daily Exercise Routine",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Warm-up" },
                                new() { Title = "Strength training" },
                                new() { Title = "Cool down" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(1,0),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-1), CompletedAt = now.AddDays(-1) },
                                new() { StartedAt = now.AddHours(-1), CompletedAt = now }
                            ],
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                }
            ]
        };
        return userData;
    }
}
