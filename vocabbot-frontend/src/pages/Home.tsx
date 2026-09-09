import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getProfile, Profile } from "../api/profile";
import "./Home.css";

export function Home() {
  const { email } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getProfile()
      .then(setProfile)
      .catch(() => setProfile(null)) // экран остаётся рабочим и без профиля — просто с прочерками
      .finally(() => setLoading(false));
  }, []);

  const displayName = profile?.firstName || email?.split("@")[0] || "друг";
  const goalDone = profile ? Math.min(profile.wordsLearnedToday, profile.wordsPerDay) : 0;
  const goalTotal = profile?.wordsPerDay ?? 0;

  return (
    <div className="phone">
      <div className="top-bar">
        <div>
          <p className="greeting-eyebrow">Доброе утро</p>
          <h1 className="greeting">{displayName} 👋</h1>
          <div className="level-chip">{profile?.level ?? "—"} · {profile?.direction ?? "—"}</div>
        </div>
        <div className="streak-badge">
          <span className="flame">🔥</span>
          <span className="num">{loading ? "…" : profile?.currentStreak ?? "—"}</span>
          <span className="lbl">дней</span>
        </div>
      </div>

      <div className="stats-card">
        <span className="stats-tab">Прогресс</span>
        <div className="stats-grid">
          <div className="stat">
            <div className="value">
              {loading ? "…" : goalDone} <small>/ {loading ? "—" : goalTotal}</small>
            </div>
            <div className="label">выучено сегодня</div>
          </div>
          <div className="stat">
            <div className="value">{loading ? "…" : profile?.wordsInProgress ?? "—"}</div>
            <div className="label">на изучении</div>
          </div>
          <div className="stat">
            <div className="value">{loading ? "…" : profile?.wordsMastered ?? "—"}</div>
            <div className="label">освоено надолго</div>
          </div>
          <div className="stat">
            <div className="value">{loading ? "…" : profile?.decksCount ?? "—"}</div>
            <div className="label">своих колод</div>
          </div>
        </div>
        <hr className="stats-rule" />
        <div className="accuracy-row">
          <span className="label">Точность теста</span>
          <div className="accuracy-bar">
            <div
              className="accuracy-fill"
              style={{ width: `${profile?.testAccuracyPercent ?? 0}%` }}
            />
          </div>
          <span className="accuracy-pct">
            {profile?.testAccuracyPercent != null ? `${profile.testAccuracyPercent}%` : "—"}
          </span>
        </div>
      </div>

      <button className="cta" onClick={() => navigate("/study")}>
        ▶ Продолжить обучение
      </button>
      <div className="cta-sub">Слова к повторению — по расписанию SM-2</div>

      <p className="section-label">Меню</p>
      <div className="menu-list">
        <button
          className="menu-item"
          style={{ ["--item-accent" as string]: "#3E6259" }}
          onClick={() => navigate("/study")}
        >
          <span className="menu-icon">📚</span>
          <span className="menu-body">
            <div className="menu-title">Учить слова</div>
            <div className="menu-sub">по темам · повторить · свои колоды</div>
          </span>
          <span className="menu-arrow">›</span>
        </button>

        {/* Остальные пункты меню — экраны ещё не перенесены с backend-стороны
            (grammar_stats, study_plans, mock_test_results и т.д. — модели
            есть, эндпоинтов пока нет), поэтому оставлены неактивными. */}
        <button className="menu-item" disabled>
          <span className="menu-icon">🎓</span>
          <span className="menu-body">
            <div className="menu-title">Подготовка к экзаменам</div>
            <div className="menu-sub">скоро</div>
          </span>
          <span className="menu-arrow">›</span>
        </button>
        <button className="menu-item" disabled>
          <span className="menu-icon">🏆</span>
          <span className="menu-body">
            <div className="menu-title">Друзья</div>
            <div className="menu-sub">скоро</div>
          </span>
          <span className="menu-arrow">›</span>
        </button>
        {/* Раньше здесь была кнопка "Выйти" — без экрана логина она вела в
            тупик (обратно зайти было бы некуда), поэтому убрана вместе с
            /login и /register. См. AuthContext.tsx. */}
      </div>

      <p className="foot">VocabBot · веб-версия</p>
    </div>
  );
}

// Уровень для подбора слов на экране Study — используется, пока там нет
// собственного запроса профиля (см. TODO в Study.tsx).
export const DEFAULT_LEVEL = "Beginner / A1-A2";
