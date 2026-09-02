namespace VocabBotWeb.Api.Models;

/// <summary>Соответствует таблице `grammar_stats`. Composite key (UserId, Topic).</summary>
public class GrammarStat
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Одна из 5 тем: времена, условные, артикли, предлоги, словообразование.</summary>
    public string Topic { get; set; } = "";

    public int Correct { get; set; }
    public int Total { get; set; }
}
