using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocabBotWeb.Api.Data;
using VocabBotWeb.Api.Dtos;
using VocabBotWeb.Api.Models;
using VocabBotWeb.Api.Services;

namespace VocabBotWeb.Api.Controllers;

[ApiController]
[Route("api/words")]
[Authorize]
public class WordsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WordSelectionService _selection;

    public WordsController(AppDbContext db, WordSelectionService selection)
    {
        _db = db;
        _selection = selection;
    }

    /// <summary>Guid текущего пользователя из JWT (claim "sub"). См. Program.cs —
    /// DefaultMapInboundClaims = false, чтобы claim не переименовывался на входе.</summary>
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>
    /// Следующая карточка к показу: сперва то, что уже начато и пора повторить
    /// (Db.GetNextAnkiWordAsync), если таких нет — новое слово (Db.GetNextNewWordAsync).
    /// Если нет ни того ни другого — 204 с временем ближайшего повтора в заголовке.
    /// </summary>
    [HttpGet("next")]
    public async Task<ActionResult<WordCardResponse>> GetNext([FromQuery] string level, [FromQuery] string? topic)
    {
        var userId = CurrentUserId;

        var due = await _selection.GetNextDueWordAsync(userId, level, topic);
        if (due is not null) return Ok(ToCard(due, isNew: false));

        var fresh = await _selection.GetNextNewWordAsync(userId, level, topic);
        if (fresh is not null) return Ok(ToCard(fresh, isNew: true));

        var nextReview = await _selection.GetEarliestUpcomingReviewAsync(userId, level, topic);
        if (nextReview is not null)
            Response.Headers.Append("X-Next-Review-At", nextReview.Value.ToString("O"));

        return NoContent();
    }

    /// <summary>
    /// Принимает ответ по карточке, применяет SM-2 (SpacedRepetitionService — перенос
    /// Db.UpdateAnkiWordAsync) и сохраняет. Первый ответ по слову создаёт запись
    /// в UserWords (аналог "SQLite INSERT ... ON CONFLICT DO UPDATE" из бота).
    /// </summary>
    [HttpPost("review")]
    public async Task<IActionResult> Review(ReviewRequest req)
    {
        var quality = req.Quality.ToLowerInvariant() switch
        {
            "again" => ReviewQuality.Again,
            "hard" => ReviewQuality.Hard,
            "good" => ReviewQuality.Good,
            "easy" => ReviewQuality.Easy,
            _ => (ReviewQuality?)null,
        };
        if (quality is null) return BadRequest("quality должен быть one of: again, hard, good, easy");

        var userId = CurrentUserId;

        var userWord = await _db.UserWords.FindAsync(userId, req.WordId);
        if (userWord is null)
        {
            userWord = new UserWord { UserId = userId, WordId = req.WordId };
            _db.UserWords.Add(userWord);
        }

        SpacedRepetitionService.ApplyReview(userWord, quality.Value);

        // TODO (следующий срез): дневная норма/стрик — Db.cs считал words_learned_today
        // и current_streak прямо здесь же при первом успешном ответе по новому слову.
        // Осознанно не переносим сейчас, чтобы этот эндпоинт остался маленьким и
        // проверяемым — вынесем в отдельный StreakService, когда будем делать
        // экран "Дневная норма" на сайте.

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WordCardResponse ToCard(Word w, bool isNew) => new(
        w.Id, w.WordText, w.Transcription, w.PartOfSpeech, w.Translation, w.TranslationUz,
        w.ExampleEn, w.ExampleRu, w.ExampleUz, w.Synonyms, w.Topic, w.DefinitionEn, isNew);
}
