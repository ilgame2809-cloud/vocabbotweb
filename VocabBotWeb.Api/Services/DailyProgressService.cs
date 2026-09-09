using VocabBotWeb.Api.Models;

namespace VocabBotWeb.Api.Services;

/// <summary>
/// Перенос трёх функций бота, которые всегда вызывались вместе на каждую
/// оценённую карточку (неважно, новую или повторную) — Db.CheckAndResetDailyLimitAsync,
/// Db.IncrementTodayLearnedWordsAsync и Db.RecordActivityAndGetStreakAsync.
/// Здесь объединены в один вызов, потому что в боте они всегда шли подряд
/// одним блоком (см. Handlers.ProcessLearningScoreAsync) — разделять их
/// смысла не было.
/// </summary>
public static class DailyProgressService
{
    /// <summary>
    /// Мутирует переданного пользователя (дневной счётчик слов + стрик).
    /// Вызывающий код сам отвечает за SaveChangesAsync — сервис намеренно
    /// не делает это сам, чтобы вызывающий мог сохранить это одним
    /// SaveChanges вместе с изменениями UserWord (как в WordsController.Review).
    /// </summary>
    public static void ApplyLearningActivity(ApplicationUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // --- Сброс дневного счётчика слов, если наступил новый день ---
        // (Db.CheckAndResetDailyLimitAsync)
        if (user.LastLearningDate != today)
        {
            user.WordsLearnedToday = 0;
            user.CardsAddedToday = 0;
            user.LastLearningDate = today;
        }

        // --- Инкремент дневного счётчика --- (Db.IncrementTodayLearnedWordsAsync)
        user.WordsLearnedToday += 1;

        // --- Стрик --- (Db.RecordActivityAndGetStreakAsync)
        // Идемпотентно в рамках одного дня: если уже засчитано сегодня — не трогаем.
        if (user.LastStreakDate != today)
        {
            var yesterday = today.AddDays(-1);
            user.CurrentStreak = user.LastStreakDate == yesterday ? user.CurrentStreak + 1 : 1;
            user.LongestStreak = Math.Max(user.LongestStreak, user.CurrentStreak);
            user.LastStreakDate = today;
        }
    }

    /// <summary>
    /// Не мутирует пользователя — используется в GET /api/profile, чтобы
    /// показать "выучено сегодня: 0", если человек открыл главный экран в
    /// новый день, ещё не ответив ни на одну карточку (иначе показал бы
    /// вчерашнее число до первого ответа сегодня — реальный сброс происходит
    /// лениво, только внутри ApplyLearningActivity при следующем ответе).
    /// </summary>
    public static int PeekWordsLearnedToday(ApplicationUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return user.LastLearningDate == today ? user.WordsLearnedToday : 0;
    }

    /// <summary>
    /// Аналогично PeekWordsLearnedToday, но для стрика: если последняя
    /// засчитанная активность была раньше вчерашнего дня, серия формально
    /// уже прервана — хотя запись в БД ещё не обновлена (обновление ленивое,
    /// произойдёт только на следующий реальный ответ по карточке). Без этой
    /// проверки профиль показывал бы устаревший "живой" стрик несколько дней
    /// после того, как человек на самом деле его прервал.
    /// </summary>
    public static int PeekCurrentStreak(ApplicationUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        if (user.LastStreakDate == today || user.LastStreakDate == yesterday)
            return user.CurrentStreak;
        return 0;
    }
}
