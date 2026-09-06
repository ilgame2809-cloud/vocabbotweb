import { apiFetch } from "./client";

export interface WordCard {
  id: number;
  word: string;
  transcription: string | null;
  partOfSpeech: string | null;
  translation: string | null;
  translationUz: string | null;
  exampleEn: string | null;
  exampleRu: string | null;
  exampleUz: string | null;
  synonyms: string | null;
  topic: string;
  definitionEn: string;
  isNew: boolean;
}

export type ReviewQuality = "again" | "hard" | "good" | "easy";

const API_URL = import.meta.env.VITE_API_URL;

/**
 * GET /api/words/next может вернуть 200 с карточкой, либо 204 (нечего
 * показывать прямо сейчас). apiFetch превращает 204 в undefined — этот
 * враппер делает это явным через null, плюс достаёт заголовок
 * X-Next-Review-At, который бэкенд шлёт только при 204.
 */
export async function getNextWord(
  level: string,
  topic?: string
): Promise<{ card: WordCard | null; nextReviewAt: string | null }> {
  const params = new URLSearchParams({ level });
  if (topic) params.set("topic", topic);

  const token = localStorage.getItem("vocabbot_token");
  const res = await fetch(`${API_URL}/api/words/next?${params}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (res.status === 204) {
    return { card: null, nextReviewAt: res.headers.get("X-Next-Review-At") };
  }
  if (!res.ok) throw new Error(`Ошибка загрузки слова (${res.status})`);

  const card = (await res.json()) as WordCard;
  return { card, nextReviewAt: null };
}

export function reviewWord(wordId: number, quality: ReviewQuality) {
  return apiFetch<void>("/api/words/review", {
    method: "POST",
    body: JSON.stringify({ wordId, quality }),
  });
}
