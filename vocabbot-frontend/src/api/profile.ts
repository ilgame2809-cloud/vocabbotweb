import { apiFetch } from "./client";

export interface Profile {
  email: string;
  firstName: string | null;
  level: string;
  direction: string;
  wordsPerDay: number;
  wordsLearnedToday: number;
  currentStreak: number;
  longestStreak: number;
  wordsInProgress: number;
  wordsMastered: number;
  decksCount: number;
  testAccuracyPercent: number | null;
}

export function getProfile() {
  return apiFetch<Profile>("/api/profile");
}
