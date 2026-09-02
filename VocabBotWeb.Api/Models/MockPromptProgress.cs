namespace VocabBotWeb.Api.Models;

/// <summary>
/// Соответствует таблице `mock_prompt_progress`. Composite key (UserId, Exam, Section,
/// PromptId) — какие именно задания (по стабильному Id из MockTestSeed) пользователь
/// уже проходил, чтобы не повторять одно и то же, пока не пройден весь банк.
/// </summary>
public class MockPromptProgress
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Exam { get; set; } = "";

    /// <summary>"Writing" | "Speaking" | "Reading" | "Listening".</summary>
    public string Section { get; set; } = "";

    /// <summary>Стабильный Id задания из MockTestSeed.cs (не Id этой строки).</summary>
    public int PromptId { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
