import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./Home.css";

/**
 * Backend пока не отдаёт профиль/статистику (ApplicationUser.Level,
 * WordsPerDay, CurrentStreak и т.д. — поля есть в модели, но GET-эндпоинта
 * для них ещё нет, см. README_MIGRATION.md → "Что дальше"). Поэтому здесь
 * два вида данных:
 *   - то, что реально приходит с сервера (email из AuthContext),
 *   - и явно помеченные плейсхолдеры для стрика/статистики — они провисят
 *     до тех пор, пока не появится GET /api/profile.
 * Уровень для подбора слов пока захардкожен — как только появится профиль,
 * замени DEFAULT_LEVEL на user.level из ответа API.
 */
const DEFAULT_LEVEL = "Beginner / A1-A2";

export function Home() {
  const { email, logout } = useAuth();
  const navigate = useNavigate();

  const displayName = email?.split("@")[0] ?? "друг";

  return (
    <div className="phone">
      <div className="top-bar">
        <div>
          <p className="greeting-eyebrow">Доброе утро</p>
          <h1 className="greeting">{displayName} 👋</h1>
          <div className="level-chip">{DEFAULT_LEVEL} · EN → RU</div>
        </div>
        {/* TODO: стрик считается в WordsController.Review (см. TODO там же) —
            как только появится, заменить на реальное значение из профиля. */}
        <div className="streak-badge" title="Скоро — стрик ещё не считается на бэкенде">
          <span className="flame">🔥</span>
          <span className="num">—</span>
          <span className="lbl">дней</span>
        </div>
      </div>

      {/* TODO: карточка статистики — нужен GET /api/profile или /api/stats.
          Сейчас показываем структуру без реальных цифр, чтобы не врать
          пользователю числами "из воздуха". */}
      <div className="stats-card">
        <span className="stats-tab">Прогресс</span>
        <div className="stats-grid">
          <div className="stat">
            <div className="value">— <small>/ —</small></div>
            <div className="label">выучено сегодня</div>
          </div>
          <div className="stat">
            <div className="value">—</div>
            <div className="label">на изучении</div>
          </div>
          <div className="stat">
            <div className="value">—</div>
            <div className="label">освоено надолго</div>
          </div>
          <div className="stat">
            <div className="value">—</div>
            <div className="label">своих колод</div>
          </div>
        </div>
        <hr className="stats-rule" />
        <div className="accuracy-row">
          <span className="label">Точность</span>
          <div className="accuracy-bar">
            <div className="accuracy-fill" style={{ width: "0%" }} />
          </div>
          <span className="accuracy-pct">—%</span>
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
        <button
          className="menu-item"
          onClick={logout}
          style={{ ["--item-accent" as string]: "var(--rule-strong)" }}
        >
          <span className="menu-icon">⚙️</span>
          <span className="menu-body">
            <div className="menu-title">Выйти</div>
            <div className="menu-sub">{email}</div>
          </span>
          <span className="menu-arrow">›</span>
        </button>
      </div>

      <p className="foot">VocabBot · веб-версия</p>
    </div>
  );
}

export { DEFAULT_LEVEL };
