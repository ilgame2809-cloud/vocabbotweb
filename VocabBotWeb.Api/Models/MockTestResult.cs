namespace VocabBotWeb.Api.Models;

/// <summary>Соответствует таблице `mock_test_results`.</summary>
public class MockTestResult
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>"IELTS" | "SAT" | "TOEFL".</summary>
    public string Exam { get; set; } = "";

    /// <summary>"Reading" | "Listening" | "Writing" | "Speaking" | "Grammar" и т.д.</summary>
    public string TestType { get; set; } = "";

    public int ScoreRaw { get; set; }
    public int ScoreTotal { get; set; }

    /// <summary>Строка, т.к. у разных экзаменов разный формат шкалы (band 6.5, 1200 и т.д.).</summary>
    public string? ScaledScore { get; set; }

    public int SecondsTaken { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
}
