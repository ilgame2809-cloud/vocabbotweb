namespace VocabBotWeb.Api.Models;

/// <summary>
/// Соответствует таблице `study_plans`. PK = UserId в исходнике (один план на
/// пользователя одновременно) — сохраняем это же ограничение через 1:1 связь с User.
/// </summary>
public class StudyPlan
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>"IELTS" | "SAT" | "TOEFL".</summary>
    public string Exam { get; set; } = "";

    public DateOnly ExamDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Рекомендованная дневная норма слов, посчитанная при создании плана.</summary>
    public int TargetDailyWords { get; set; }

    /// <summary>Чтобы не слать напоминание об отставании от плана чаще раза в день.</summary>
    public DateOnly? LastPaceReminderDate { get; set; }
}
