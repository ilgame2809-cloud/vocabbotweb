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

public record ProfileResponse(
    string Email,
    string? FirstName,
    string Level,
    string Direction,
    int WordsPerDay,
    int WordsLearnedToday,
    int CurrentStreak,
    int LongestStreak,
    int WordsInProgress, // Interval <= 0 — перенос Db.GetLearningStatsAsync.inProgress
    int WordsMastered,   // Interval > 0  — перенос Db.GetLearningStatsAsync.mastered
    int DecksCount,
    int? TestAccuracyPercent // null если тест на уровень ещё не проходили (Total == 0)
);
