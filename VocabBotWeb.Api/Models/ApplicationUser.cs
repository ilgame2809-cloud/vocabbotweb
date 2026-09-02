using Microsoft.AspNetCore.Identity;

namespace VocabBotWeb.Api.Models;

/// <summary>
/// Заменяет таблицу `users` из Telegram-бота. Identity уже даёт нам Id (Guid), Email,
/// PasswordHash, EmailConfirmed и т.д. — сюда переносим только то, чего у Identity
/// изначально нет: профиль изучения языка, стрики, лимиты, направление обучения.
///
/// Поля, которые в боте были нужны из-за специфики Telegram, сюда НЕ перенесены:
/// - onboarding_complete / daily_limit_set — на сайте это просто "заполнил ли профиль
///   при регистрации", ведём через обычный флаг в форме, отдельное поле не требуется.
/// - referred_by / referral_reward_granted / invite_count / bonus_invites — реферальная
///   механика завязана на персональную ссылку-в-Telegram; если понадобится на сайте —
///   вернём отдельной моделью Referral, не мешая в User.
/// - activity_hours / last_day_notif_date / last_night_notif_date / reminder_count_* —
///   это всё троттлинг фоновых Telegram-уведомлений, для сайта нужна другая механика
///   (email/push), поэтому не переносим вслепую.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // --- Профиль обучения ---

    /// <summary>Язык интерфейса сайта: "ru" | "uz" | "en".</summary>
    public string InterfaceLanguage { get; set; } = "ru";

    /// <summary>Уровень CEFR: "Beginner / A1-A2" | "Intermediate / B1-B2" | "Advanced / C1-C2".</summary>
    public string Level { get; set; } = "Beginner / A1-A2";

    /// <summary>Языковая пара обучения: "EN_RU" | "UZ_RU" | "EN_UZ" | "EN_EN" (self-study).</summary>
    public string Direction { get; set; } = "EN_RU";

    /// <summary>Какой из двух языков пары изучается: "en" | "ru" | "uz".</summary>
    public string DirectionTarget { get; set; } = "en";

    public string? FirstName { get; set; }

    // --- Тест на уровень ---

    public int TestTotalQuestions { get; set; }
    public int TestCorrectAnswers { get; set; }

    /// <summary>Индекс наивысшего уровня, который пользователь реально сдал тестом
    /// (позволяет вручную переключаться между УЖЕ пройденными уровнями).</summary>
    public int HighestLevelPassedIndex { get; set; } = -1;

    // --- Дневная норма и лимиты ---

    public int WordsPerDay { get; set; } = 20;
    public int WordsLearnedToday { get; set; }
    public DateOnly? LastLearningDate { get; set; }
    public int CardsAddedToday { get; set; }

    // --- Стрик ---

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastStreakDate { get; set; }

    /// <summary>Список кодов достижений через запятую (как и было в боте) — простая
    /// схема, менять на отдельную таблицу Achievements не нужно, пока их немного.</summary>
    public string UnlockedAchievements { get; set; } = "";

    // --- Уведомления (для сайта переосмыслим как email-напоминания) ---

    /// <summary>"09:00" | "14:00" | "20:00" | null (не выбрано явно).</summary>
    public string? NotificationTime { get; set; }
    public bool IsNotificationEnabled { get; set; } = true;

    // --- Навигационные свойства ---

    public ICollection<UserWord> UserWords { get; set; } = new List<UserWord>();
    public ICollection<UserDeck> UserDecks { get; set; } = new List<UserDeck>();
    public ICollection<GrammarStat> GrammarStats { get; set; } = new List<GrammarStat>();
    public StudyPlan? StudyPlan { get; set; }
    public ICollection<MockTestResult> MockTestResults { get; set; } = new List<MockTestResult>();
    public ICollection<MockPromptProgress> MockPromptProgress { get; set; } = new List<MockPromptProgress>();

    /// <summary>Свои слова, добавленные этим пользователем в свои колоды (Word.OwnerUserId).</summary>
    public ICollection<Word> OwnedWords { get; set; } = new List<Word>();
}
