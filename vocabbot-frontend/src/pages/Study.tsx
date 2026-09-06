import { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import { getNextWord, reviewWord, WordCard, ReviewQuality } from "../api/words";
import { DEFAULT_LEVEL } from "./Home";
import "./Study.css";

/**
 * В дизайне (card-design.html) было два режима с разным набором кнопок на
 * обороте: "Повторение" (4 штампа Again/Hard/Good/Easy) и "Новое слово"
 * (3 штампа Знаю/Почти/Не знаю). Backend же принимает только 4 значения
 * ReviewQuality вне зависимости от того, новое слово или нет — сам решает,
 * в какой фазе (Learning/Review) карточка находится, через isLearningPhase
 * внутри SpacedRepetitionService. Поэтому здесь один и тот же набор из 4
 * кнопок для обоих случаев (card.isNew только меняет подпись сверху) —
 * это сознательное упрощение дизайна под реальный контракт API, а не
 * полный перенос двух отдельных наборов кнопок.
 */

type Lang = "ru" | "uz";

export function Study() {
  const [card, setCard] = useState<WordCard | null>(null);
  const [nextReviewAt, setNextReviewAt] = useState<string | null>(null);
  const [flipped, setFlipped] = useState(false);
  const [lang, setLang] = useState<Lang>("ru");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reviewedCount, setReviewedCount] = useState(0);

  const loadNext = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await getNextWord(DEFAULT_LEVEL);
      setCard(res.card);
      setNextReviewAt(res.nextReviewAt);
      setFlipped(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось загрузить слово");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadNext();
  }, [loadNext]);

  async function handleReview(quality: ReviewQuality) {
    if (!card) return;
    try {
      await reviewWord(card.id, quality);
      setReviewedCount((c) => c + 1);
      await loadNext();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось сохранить ответ");
    }
  }

  const translation = lang === "ru" ? card?.translation : card?.translationUz;
  const example = lang === "ru" ? card?.exampleRu : card?.exampleUz;

  return (
    <div className="study-page">
      <div className="top-note">
        <Link className="back-link" to="/">
          ‹ На главную
        </Link>
        <p className="eyebrow">VocabBot · сессия обучения</p>
        <h1>{card?.isNew ? "Новое слово" : "Повторение"}</h1>
      </div>

      {loading && <div className="hint-text">Загрузка…</div>}
      {error && <div className="hint-text" style={{ color: "var(--again)" }}>{error}</div>}

      {!loading && !error && !card && (
        <div className="empty-state">
          <div className="big">🎉</div>
          <p>
            {nextReviewAt
              ? `Пока всё повторено. Следующее слово будет готово: ${new Date(
                  nextReviewAt
                ).toLocaleString("ru-RU")}`
              : "Слов для этого уровня пока нет в базе. Похоже, словарь ещё не засеян на сервере."}
          </p>
        </div>
      )}

      {!loading && card && (
        <div className="deck-row">
          <div>
            <div className="stage">
              <div
                className={`card3d${flipped ? " flipped" : ""}`}
                onClick={() => !flipped && setFlipped(true)}
              >
                <div className="face front">
                  <span className="tab">{card.topic}</span>
                  <span className="cat-no">{card.isNew ? "новое" : "повтор"}</span>
                  <div className="face-body">
                    <div className="word-row">
                      <span className="headword">{card.word}</span>
                      {card.partOfSpeech && <span className="pos">{card.partOfSpeech}</span>}
                    </div>
                    {card.transcription && (
                      <div className="transcription">[{card.transcription}]</div>
                    )}
                    <hr className="rule-line" />
                    <div className="hint-text">
                      Вспомните перевод, затем переверните карточку
                    </div>
                  </div>
                  <div className="hole" />
                </div>

                <div className="face back">
                  <span className="tab">{card.topic}</span>
                  <div className="face-body">
                    <div className="translation-line">
                      <span className="en">{card.word}</span>
                      <span className="dash">—</span>
                      <span className="native">{translation ?? "—"}</span>
                    </div>
                    <hr className="rule-line" />
                    {example && (
                      <div className="field">
                        <span className="field-label">Пример</span>
                        <div className="field-value">{example}</div>
                      </div>
                    )}
                    {card.synonyms && (
                      <div className="field">
                        <span className="field-label">Синонимы</span>
                        <div className="field-value">{card.synonyms}</div>
                      </div>
                    )}
                  </div>
                  <div className="hole" />
                </div>
              </div>
            </div>

            <div className="controls">
              {!flipped ? (
                <div>
                  <div className="btn-row">
                    <button
                      className="btn"
                      onClick={(e) => {
                        e.stopPropagation();
                        // TODO: TtsService из бота ещё не перенесён на backend —
                        // как появится /api/words/{id}/audio, подключить сюда.
                      }}
                    >
                      🔊 Слушать
                    </button>
                    <button
                      className="btn primary"
                      onClick={(e) => {
                        e.stopPropagation();
                        setFlipped(true);
                      }}
                    >
                      Показать перевод ▸
                    </button>
                  </div>
                </div>
              ) : (
                <div>
                  <div className="stamp-row">
                    <button className="stamp again" onClick={() => handleReview("again")}>
                      Again<span className="sub">&lt;1 мин</span>
                    </button>
                    <button className="stamp hard" onClick={() => handleReview("hard")}>
                      Hard<span className="sub">~6 мин</span>
                    </button>
                    <button className="stamp good" onClick={() => handleReview("good")}>
                      Good<span className="sub">1 дн.</span>
                    </button>
                    <button className="stamp easy" onClick={() => handleReview("easy")}>
                      Easy<span className="sub">4 дн.</span>
                    </button>
                  </div>
                </div>
              )}
              <div className="foot-note">Отвечено в этой сессии: {reviewedCount}</div>
            </div>
          </div>
        </div>
      )}

      <div className="toolbar" style={{ marginTop: 24 }}>
        <div className="seg">
          <button className={lang === "ru" ? "active" : ""} onClick={() => setLang("ru")}>
            RU
          </button>
          <button className={lang === "uz" ? "active" : ""} onClick={() => setLang("uz")}>
            UZ
          </button>
        </div>
      </div>
    </div>
  );
}
