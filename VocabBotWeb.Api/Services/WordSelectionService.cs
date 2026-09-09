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
    /// Карточка, которую пора повторить прямо сейчас: уже начатая (есть в user_words)
    /// и её NextReview уже наступил. Соответствует Db.GetNextAnkiWordAsync.
    ///
    /// ВАЖНО (фикс бага): раньше условие было `Interval &lt;= 0 || NextReview &lt;= now`.
    /// Это ломало таймер ожидания для карточек ещё в фазе Learning (Interval &lt;= 0
    /// у только что добавленных и у тех, кому только что нажали Again/Hard) — такая
    /// карточка считалась "к повтору" ВСЕГДА, независимо от NextReview. В итоге,
    /// например, "Again" должен откладывать слово на 2 минуты, но условие тут же
    /// снова находило его "просроченным" и подсовывало следующей же карточкой —
    /// отсюда залипание на одних и тех же словах. ApplyReview (SpacedRepetitionService)
    /// всегда корректно проставляет NextReview в обеих фазах, так что единственная
    /// нужная проверка — NextReview &lt;= now, без особого случая для Interval.
    /// </summary>
    public async Task<Word?> GetNextDueWordAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var now = DateTime.UtcNow;

        var query = _db.UserWords
            .Where(uw => uw.UserId == userId)
            .Where(uw => uw.NextReview <= now)
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
    ///
    /// Раньше здесь был фильтр `Interval &gt; 0`, который специально исключал
    /// карточки в фазе Learning — из-за старого бага в GetNextDueWordAsync они
    /// и так считались "уже готовыми", так что смысла показывать их таймер не было.
    /// После фикса NextReview честно отражает время ожидания в обеих фазах, поэтому
    /// фильтр убран — иначе, если единственная ожидающая карточка была в фазе
    /// Learning, метод вернул бы null вместо реального времени ожидания.
    /// </summary>
    public async Task<DateTime?> GetEarliestUpcomingReviewAsync(Guid userId, string level, string? topic = null, Guid? ownerUserId = null)
    {
        var query = _db.UserWords
            .Where(uw => uw.UserId == userId)
            .Join(_db.Words, uw => uw.WordId, w => w.Id, (uw, w) => new { uw, w })
            .Where(x => x.w.Level == level);

        if (topic is not null) query = query.Where(x => x.w.Topic == topic);
        if (ownerUserId is not null) query = query.Where(x => x.w.OwnerUserId == ownerUserId);

        return await query.OrderBy(x => x.uw.NextReview).Select(x => x.uw.NextReview).FirstOrDefaultAsync();
    }
}
