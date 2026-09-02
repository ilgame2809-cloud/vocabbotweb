namespace VocabBotWeb.Api.Dtos;

public record RegisterRequest(string Email, string Password, string? FirstName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, Guid UserId, string Email);

public record WordCardResponse(
    int Id,
    string Word,
    string? Transcription,
    string? PartOfSpeech,
    string? Translation,
    string? TranslationUz,
    string? ExampleEn,
    string? ExampleRu,
    string? ExampleUz,
    string? Synonyms,
    string Topic,
    string DefinitionEn,
    bool IsNew // true = ещё не в user_words (первое предъявление), false = карточка на повтор
);

public record ReviewRequest(int WordId, string Quality); // "again" | "hard" | "good" | "easy"
