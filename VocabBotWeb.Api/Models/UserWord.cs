namespace VocabBotWeb.Api.Models;

/// <summary>
/// Соответствует таблице `user_words` — состояние SM-2 конкретного слова у конкретного
/// пользователя. Составной первичный ключ (UserId, WordId), как и в боте.
/// </summary>
public class UserWord
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int WordId { get; set; }
    public Word Word { get; set; } = null!;

    public double EFactor { get; set; } = 2.5;

    /// <summary>0 = карточка ещё в фазе Learning (не "закончила обучение"); &gt;0 = дни
    /// интервала в фазе Review. См. Db.UpdateAnkiWordAsync в исходном боте.</summary>
    public int Interval { get; set; }

    public int Repetitions { get; set; }

    public DateTime? NextReview { get; set; }
}
