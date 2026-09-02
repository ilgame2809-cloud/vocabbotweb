namespace VocabBotWeb.Api.Models;

/// <summary>
/// Соответствует таблице `words`. Хранит и системные слова (Level = CEFR-уровень или
/// код экзамена IELTS/SAT/TOEFL, OwnerUserId = null), и карточки из "своих колод"
/// пользователя (Level = "Custom", OwnerUserId = владелец, Topic = имя колоды —
/// как и в боте, отдельной таблицы для содержимого своих колод не заводим).
/// </summary>
public class Word
{
    public int Id { get; set; }

    /// <summary>CEFR-уровень ("Beginner / A1-A2" и т.д.), код экзамена ("IELTS"/"SAT"/"TOEFL")
    /// или "Custom" для карточек из пользовательских колод.</summary>
    public string Level { get; set; } = "";

    public string WordText { get; set; } = "";
    public string? Transcription { get; set; }
    public string? PartOfSpeech { get; set; }

    public string? Translation { get; set; }
    public string? TranslationUz { get; set; }

    public string? ExampleEn { get; set; }
    public string? ExampleRu { get; set; }
    public string? ExampleUz { get; set; }

    public string? Synonyms { get; set; }

    /// <summary>Английское определение — используется на обратной стороне карточки для
    /// направления EN_EN (self-study) и в тесте на уровень для этого направления.</summary>
    public string DefinitionEn { get; set; } = "";

    /// <summary>Тема ("Еда", "Путешествия", ... ) для системных слов; имя колоды — для
    /// карточек из своих колод пользователя.</summary>
    public string Topic { get; set; } = "general";

    /// <summary>Null для системных слов. Заполнено — для карточек из "своих колод".</summary>
    public Guid? OwnerUserId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public ICollection<UserWord> UserWords { get; set; } = new List<UserWord>();
}
