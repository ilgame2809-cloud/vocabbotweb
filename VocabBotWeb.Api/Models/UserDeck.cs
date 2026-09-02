namespace VocabBotWeb.Api.Models;

/// <summary>
/// Соответствует таблице `user_decks`. В боте ключом была пара (user_id, name) —
/// на сайте даём суррогатный Id (удобнее для REST: /api/decks/{id}), но сохраняем
/// уникальность имени в рамках пользователя через индекс в DbContext.
/// Содержимое колоды — это просто Word-строки с Word.OwnerUserId = этот пользователь
/// и Word.Topic = имя колоды (см. Word.cs) — так было устроено и в боте.
/// </summary>
public class UserDeck
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
