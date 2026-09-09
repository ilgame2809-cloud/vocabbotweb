# VocabBotWeb — перенос бота на сайт

## Статус

- ✅ Схема БД (EF Core модели + Fluent-конфигурация под Postgres)
- ✅ SM-2 логика (`Services/SpacedRepetitionService.cs` — перенос `Db.UpdateAnkiWordAsync`)
- ✅ Подбор слов (`Services/WordSelectionService.cs` — перенос `Db.GetNextAnkiWordAsync` / `GetNextNewWordAsync`)
- ✅ Auth: регистрация/логин с JWT (`Controllers/AuthController.cs`)
- ✅ Первый вертикальный срез: `GET /api/words/next`, `POST /api/words/review`
- ✅ Фикс бага с повторами слов: `WordSelectionService.GetNextDueWordAsync` игнорировал
  таймер `NextReview` для карточек в фазе Learning (условие `Interval <= 0 || ...`
  всегда считало их "просроченными") — из-за этого "Again"/"Hard" мгновенно
  возвращали ту же карточку следующей вместо честного ожидания 2-5 минут.
- ✅ Словарь (1250 слов из `Data/Seed/words.json` — перенос `WordSeed.cs` бота, загружается идемпотентно при старте, см. `Services/WordSeedService.cs`)
- ✅ React-фронт (Login/Register/Home/Study — папка `vocabbot-frontend`, отдельный README там)
- ✅ Дневная норма / стрик (`Services/DailyProgressService.cs` — перенос `CheckAndResetDailyLimitAsync` + `IncrementTodayLearnedWordsAsync` + `RecordActivityAndGetStreakAsync`)
- ✅ `GET /api/profile` — статистика для главного экрана (стрик, дневная норма, "на изучении"/"освоено надолго", точность теста на уровень)
- ⬜ Свои колоды (`user_decks`), грамматический тренажёр, мок-тесты, учебный план

## Деплой на Render

Важно: `db.Database.Migrate()` в `Program.cs` применяет миграции автоматически
при старте контейнера — но саму папку `Migrations/` (с файлами, которые
генерирует `dotnet ef migrations add`) нужно закоммитить в git. Без неё
Migrate() применять нечего. То есть порядок такой:

**1. Локально, один раз, перед первым деплоем:**

```bash
cd VocabBotWeb.Api
dotnet ef migrations add InitialCreate
git add Migrations/
git commit -m "Add initial migration"
git push
```

**2. На Render — создай Postgres:**

- Dashboard → New → PostgreSQL
- Возьми **Internal Database URL** после создания (Render покажет в формате
  `postgres://user:pass@host/db` — для Npgsql нужно переформатировать в
  `Host=host;Port=5432;Database=db;Username=user;Password=pass`, либо просто
  вставить как есть — Npgsql с версии 8 умеет парсить `postgres://` URL напрямую).

**3. На Render — создай Web Service:**

- Dashboard → New → Web Service → подключи свой git-репозиторий
- Runtime: **Docker** (Render сам найдёт `Dockerfile` в корне)
- Environment Variables (Settings → Environment):
  - `ConnectionStrings__Default` = строка подключения из шага 2 (двойное
    подчёркивание — так ASP.NET читает вложенные ключи из env-переменных
    вместо `appsettings.json`)
  - `Jwt__Key` = длинная случайная строка (32+ символов) — **не тот же плейсхолдер,
    что в appsettings.json**, сгенерируй новый, например `openssl rand -base64 32`
  - `Jwt__Issuer` = `VocabBotWeb`
  - `Jwt__Audience` = `VocabBotWeb`
  - `FrontendOrigins__0` = адрес твоего фронта на Vercel/Netlify (когда появится;
    пока можно `http://localhost:5173` для теста)
  - `ASPNETCORE_ENVIRONMENT` = `Production`
- Render сам передаст `PORT` — его подхватывает `ENTRYPOINT` в `Dockerfile`,
  ничего вручную выставлять не нужно.

**4. Деплой** — Render соберёт образ по `Dockerfile` и запустит. В логах
(Dashboard → твой сервис → Logs) будет видно, применились ли миграции —
если что-то не так со строкой подключения, ошибка будет прямо там при старте.

**5. Проверка** — Render даст публичный URL вида
`https://vocabbotweb-api.onrender.com`. Swagger в проде выключен (см.
`if (app.Environment.IsDevelopment())` в `Program.cs`) — это осознанно,
не выставлять API-документацию наружу. Проверять эндпоинты после деплоя —
через Postman/curl, либо временно поменять это условие на true, если очень
нужно потыкать в проде руками.

⚠️ **Бесплатный план Render** усыпляет сервис после ~15 минут без запросов —
первый запрос после простоя будет отвечать заметно дольше (холодный старт +
поднятие контейнера). Для реальных пользователей на постоянку стоит закладывать
это в ожидания или переходить на платный план, когда дойдёт до продакшена.



После `dotnet ef database update` (см. ниже):

```bash
dotnet run
# → открой https://localhost:5001/swagger
```

1. `POST /api/auth/register` — `{"email": "test@test.com", "password": "Passw0rd!", "firstName": "Азиз"}` → получишь JWT.
2. Нажми "Authorize" в Swagger, вставь `Bearer <токен>`.
3. `GET /api/words/next?level=Beginner%20%2F%20A1-A2` — вернёт карточку (если в БД есть слова этого уровня — нужно сначала засеять `words`, сидер из `WordSeed.cs` бота пока не перенесён, это следующий шаг).
4. `POST /api/words/review` — `{"wordId": 1, "quality": "good"}`.

# Фундамент: EF Core модели под Postgres

## Что здесь

Перенос схемы БД из `Db.cs` (SQLite, ручные CREATE TABLE) в EF Core модели
для Postgres. Бизнес-смысл полей сохранён 1:1, изменилось только то, что
диктует переход с Telegram на сайт:

| Было в боте | Стало | Почему |
|---|---|---|
| `users.user_id` (Telegram ID, PK) | `ApplicationUser.Id` (Guid, из Identity) | Регистрация теперь по email/паролю, не по Telegram-аккаунту |
| `users.language` | `ApplicationUser.InterfaceLanguage` | Переименовано для ясности (не путать с `Direction`) |
| `sessions` (FSM-состояние диалога) | — не переносится | На сайте это не пошаговый диалог, состояние живёт в React |
| `reminder_state`, `activity_hours`, `last_day_notif_date`, `last_night_notif_date`, `reminder_count_*` | — не переносится | Троттлинг именно Telegram-уведомлений; для email/push понадобится другая механика — сделаем отдельно, когда дойдём до уведомлений на сайте |
| `referred_by`, `referral_reward_granted`, `invite_count`, `bonus_invites` | — не переносится (пока) | Реферальная механика была завязана на персональную ссылку в Telegram; если нужна на сайте — вынесем в отдельную модель `Referral`, не смешивая с User |
| `onboarding_complete`, `daily_limit_set` | — не переносится | На сайте это будет просто состояние формы регистрации, отдельного флага в БД не нужно |
| Остальные таблицы (`words`, `user_words`, `user_decks`, `grammar_stats`, `study_plans`, `mock_test_results`, `mock_prompt_progress`) | перенесены как есть | Чистая бизнес-логика, не завязанная на Telegram |

## Как применить у себя

Мне здесь недоступен интернет, поэтому `dotnet restore`/`dotnet ef` я
прогнать не смог — сделай это на своей машине:

```bash
cd VocabBotWeb.Api

# 1. Поставь пакет для CLI-инструментов EF Core (один раз глобально)
dotnet tool install --global dotnet-ef

# 2. Восстанови зависимости
dotnet restore

# 3. Подними Postgres локально (если ещё нет), например через Docker:
docker run -d --name vocabbot-pg -e POSTGRES_PASSWORD=CHANGE_ME \
  -e POSTGRES_DB=vocabbot -p 5432:5432 postgres:16

# 4. Поправь appsettings.json (или лучше — appsettings.Development.json,
#    чтобы пароль не улетел в git) под свои данные подключения.

# 5. Сгенерируй первую миграцию
dotnet ef migrations add InitialCreate

# 6. Примени её к БД
dotnet ef database update
```

Если на шаге 5 компилятор ругнётся на несовпадение версий пакетов —
это нормально, `dotnet ef` в ошибке всегда укажет точную строку и
какую версию поставить взамен (как и в README самого бота).

## Что дальше

Следующий логичный шаг — либо:
- **auth-эндпоинты** (`/api/auth/register`, `/api/auth/login` с JWT), либо
- **вертикальный срез "изучение слов"**: `GET /api/words/next` → SM-2 логика
  из `Db.UpdateAnkiWordAsync` (её тоже надо перенести как `SpacedRepetitionService`)
  → `POST /api/words/{id}/review`.

Handlers.cs бота — хорошая шпаргалка для списка нужных эндпоинтов (каждый
`@dp.message`-хендлер = потенциальный REST-маршрут), но копировать его
логику один в один не получится: там реплики завязаны на Telegram-клавиатуры,
на сайте это будет JSON-контракт + React-компонент.
