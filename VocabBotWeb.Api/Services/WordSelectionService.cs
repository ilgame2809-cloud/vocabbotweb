using Microsoft.EntityFrameworkCore;
using VocabBotWeb.Api.Data;
using VocabBotWeb.Api.Models;

namespace VocabBotWeb.Api.Services;

/// <summary>
/// Перенос трёх функций подбора слов из Db.cs (Db.GetNextAnkiWordAsync,
/// Db.GetNextNewWordAsync, Db.HasAnyStartedWordsAsync) — тот же SQL, но теперь как
/// LINQ-запросы к EF Core. Логика "сквозного повтора по всем словам сразу"
/// (GetNextAnkiWordAnyAsync) сюда пока не перенесена — добавим, когда будем делать
/// экран "Повторить всё".
/// </summary>
public class WordSelectionService
{
    private readonly AppDbContext _db;

    public WordSelectionService(AppDbContext db) => _db = db;

    /// <summary>
    /// Карточка, которую пора повторить прямо сейчас: уже начатая (есть в user_words),
    /// её либо ещё не выпустили из Learning (Interval &lt;= 0), либо подошло время
    /// (NextReview &lt;= сейчас). Соответствует Db.GetNextAnkiWordAsync.
    /// </summary>
    public async Task<Word?> GetNextDueWordAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var now = DateTime.UtcNow;

        var query = _db.UserWords
            .Where(uw => uw.UserId == userId)
            .Where(uw => uw.Interval <= 0 || uw.NextReview <= now)
            .Join(_db.Words, uw => uw.WordId, w => w.Id, (uw, w) => new { uw, w })
            .Where(x => x.w.Level == level);

        if (topic is not null) query = query.Where(x => x.w.Topic == topic);
        if (ownerUserId is not null) query = query.Where(x => x.w.OwnerUserId == ownerUserId);

        return await query
            .OrderBy(x => x.uw.NextReview)
            .Select(x => x.w)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Ещё не начатая карточка этого уровня/темы — самая ранняя по Id (детерминированный
    /// порядок, как и в боте, чтобы всем пользователям слова приходили в одном порядке).
    /// Соответствует Db.GetNextNewWordAsync.
    /// </summary>
    public async Task<Word?> GetNextNewWordAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var startedWordIds = _db.UserWords
            .Where(uw => uw.UserId == userId)
            .Select(uw => uw.WordId);

        var query = _db.Words
            .Where(w => w.Level == level)
            .Where(w => !startedWordIds.Contains(w.Id));

        if (topic is not null) query = query.Where(w => w.Topic == topic);
        if (ownerUserId is not null) query = query.Where(w => w.OwnerUserId == ownerUserId);

        return await query.OrderBy(w => w.Id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Отличает "вы ещё ничего не учили здесь" от "всё повторили, следующие слова
    /// созреют позже" — разные сообщения на экране. Соответствует Db.HasAnyStartedWordsAsync.
    /// </summary>
    public async Task<bool> HasAnyStartedWordsAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var query = _db.UserWords
            .Where(uw => uw.UserId == userId)
            .Join(_db.Words, uw => uw.WordId, w => w.Id, (uw, w) => w)
            .Where(w => w.Level == level);

        if (topic is not null) query = query.Where(w => w.Topic == topic);
        if (ownerUserId is not null) query = query.Where(w => w.OwnerUserId == ownerUserId);

        return await query.AnyAsync();
    }

    /// <summary>
    /// Ближайшее время, когда что-то на этом уровне/теме снова станет доступно
    /// к повтору (используется, чтобы показать "следующее слово через N минут",
    /// когда GetNextDueWordAsync и GetNextNewWordAsync оба вернули null).
    /// Соответствует Db.GetEarliestUpcomingReviewAsync.
    /// </summary>
    public async Task<DateTime?> GetEarliestUpcomingReviewAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var query = _db.UserWords
            .Where(uw => uw.UserId == userId && uw.Interval > 0)
            .Join(_db.Words, uw => uw.WordId, w => w.Id, (uw, w) => new { uw, w })
            .Where(x => x.w.Level == level);

        if (topic is not null) query = query.Where(x => x.w.Topic == topic);
        if (ownerUserId is not null) query = query.Where(x => x.w.OwnerUserId == ownerUserId);

        return await query.OrderBy(x => x.uw.NextReview).Select(x => x.uw.NextReview).FirstOrDefaultAsync();
    }
}
