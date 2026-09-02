using VocabBotWeb.Api.Models;

namespace VocabBotWeb.Api.Services;

/// <summary>
/// Качество ответа на карточку — как и в боте, 4 варианта вместо классических 6 у SM-2.
/// Значения совпадают с тем, что раньше передавалось как int quality в
/// Db.UpdateAnkiWordAsync, чтобы ветвление ниже читалось один в один с оригиналом.
/// </summary>
public enum ReviewQuality
{
    Again = 2, // 🔴 — было quality < 3 в боте, используем 2 как представителя этой группы
    Hard = 3,  // 🟠
    Good = 4,  // 🟢
    Easy = 5,  // 🔵
}

/// <summary>
/// Перенос Db.UpdateAnkiWordAsync и Db.MarkWordFullyKnownAsync из бота — 1:1 та же
/// логика (см. комментарии в оригинале про то, чем это отличается от классического
/// SM-2), но теперь как чистая функция над EF-сущностью вместо прямых SQL UPDATE.
/// Вызывающий код сам отвечает за SaveChangesAsync — сервис только мутирует
/// переданный UserWord.
/// </summary>
public static class SpacedRepetitionService
{
    private const double EasyBonus = 0.30;
    private const double HardMultiplier = 1.2;

    /// <summary>
    /// Применяет результат ответа к карточке. Если userWord только что создан
    /// (EFactor/Interval/Repetitions ещё не выставлены), убедись, что перед вызовом
    /// у него значения по умолчанию (EFactor = 2.5, Interval = 0, Repetitions = 0) —
    /// как раз то, что даёт стандартный конструктор UserWord.
    /// </summary>
    public static void ApplyReview(UserWord userWord, ReviewQuality quality)
    {
        var now = DateTime.UtcNow;
        var isLearningPhase = userWord.Interval <= 0;
        var q = (int)quality;

        if (q < 3)
        {
            // 🔴 Again — карточка сброшена, уходит обратно в обучение. Короткий шаг
            // (2 минуты), а не 10 — иначе в рамках одной сессии слово физически не
            // может вернуться на повтор.
            userWord.EFactor = Math.Max(1.3, userWord.EFactor - 0.20);
            userWord.Repetitions = 0;
            userWord.Interval = 0;
            userWord.NextReview = now.AddMinutes(2);
        }
        else if (isLearningPhase)
        {
            // Карточка ещё не выпущена в Review (совсем новая или только что
            // сброшенная Again). Repetitions здесь — счётчик пройденных шагов обучения.
            switch (q)
            {
                case 3: // 🟠 Hard — повторяем текущий шаг ещё раз, без продвижения дальше
                    userWord.Interval = 0;
                    userWord.NextReview = now.AddMinutes(5);
                    break;

                case 4: // 🟢 Good — переходим к следующему шагу обучения
                    if (userWord.Repetitions == 0)
                    {
                        // Первый успешный ответ — ещё не выпускаем в Review, откладываем
                        // ненадолго (второй шаг обучения)
                        userWord.Repetitions = 1;
                        userWord.Interval = 0;
                        userWord.NextReview = now.AddMinutes(5);
                    }
                    else
                    {
                        // Шаги обучения пройдены — выпускаем в Review со стандартным интервалом
                        userWord.Interval = 1;
                        userWord.NextReview = now.AddDays(userWord.Interval);
                        userWord.Repetitions += 1;
                    }
                    break;

                default: // 🔵 Easy — сразу в Review, минуя шаги обучения
                    userWord.Interval = 1;
                    userWord.NextReview = now.AddDays(userWord.Interval);
                    userWord.Repetitions += 1;
                    break;
            }
        }
        else
        {
            // Карточка уже в фазе Review — обычные интервалы SM-2/Anki
            switch (q)
            {
                case 3: // 🟠 Hard: interval × 1.2, EF − 15 п.п.
                    userWord.Interval = Math.Max(userWord.Interval + 1,
                        (int)Math.Round(userWord.Interval * HardMultiplier));
                    userWord.EFactor = Math.Max(1.3, userWord.EFactor - 0.15);
                    break;

                case 4: // 🟢 Good: interval × EF, EF не меняется
                    userWord.Interval = (int)Math.Round(userWord.Interval * userWord.EFactor);
                    break;

                default: // 🔵 Easy: interval × (EF + бонус), EF + 15 п.п. после расчёта интервала
                    userWord.Interval = (int)Math.Round(userWord.Interval * (userWord.EFactor + EasyBonus));
                    userWord.EFactor += 0.15;
                    break;
            }
            userWord.Repetitions += 1;
            userWord.NextReview = now.AddDays(userWord.Interval);
        }
    }

    /// <summary>
    /// Перенос Db.MarkWordFullyKnownAsync — слово уходит на 10 лет вперёд ("я это
    /// точно знаю, не показывай больше").
    /// </summary>
    public static void MarkFullyKnown(UserWord userWord)
    {
        userWord.EFactor = 3.0;
        userWord.Interval = 3650;
        userWord.Repetitions = 10;
        userWord.NextReview = DateTime.UtcNow.AddDays(365 * 10);
    }
}
