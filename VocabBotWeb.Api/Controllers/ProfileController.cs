using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VocabBotWeb.Api.Data;
using VocabBotWeb.Api.Dtos;
using VocabBotWeb.Api.Services;

namespace VocabBotWeb.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db) => _db = db;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>
    /// Даёт всё, что нужно главному экрану сайта (карточка "Прогресс",
    /// стрик, дневная норма) — раньше это были прочерки во фронте, потому
    /// что этого эндпоинта не было. Считает "на изучении"/"освоено надолго"
    /// как Db.GetLearningStatsAsync (Interval &lt;= 0 / &gt; 0), точность —
    /// как результат теста на уровень (не точность по карточкам — так же
    /// было устроено и в боте, см. Handlers.cs строка ~2939).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var inProgress = await _db.UserWords.CountAsync(uw => uw.UserId == userId && uw.Interval <= 0);
        var mastered = await _db.UserWords.CountAsync(uw => uw.UserId == userId && uw.Interval > 0);
        var decksCount = await _db.UserDecks.CountAsync(d => d.UserId == userId);

        int? accuracy = user.TestTotalQuestions > 0
            ? (int)Math.Round(100.0 * user.TestCorrectAnswers / user.TestTotalQuestions)
            : null;

        return Ok(new ProfileResponse(
            user.Email!,
            user.FirstName,
            user.Level,
            user.Direction,
            user.WordsPerDay,
            DailyProgressService.PeekWordsLearnedToday(user),
            DailyProgressService.PeekCurrentStreak(user),
            user.LongestStreak,
            inProgress,
            mastered,
            decksCount,
            accuracy
        ));
    }
}
