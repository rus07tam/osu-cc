# osu!cc: разработка

Заметки для тех, кто копается в этом репозитории.

**Языки:** [English](../en/DEVELOPMENT.md) · Русский

## Структура

```plaintext
osucc.Shared/   общая логика layout/версий/стейджинга (namespace osucc.Common), единый источник
                правды для лаунчера, хука и менеджера обновлений
osucc.Host/     DLL стартап-хука (classlib, net8.0), он же NuGet-пакет osucc.Host
  StartupHook.cs     точка входа, которую вызывает рантайм
  Core/              бутстраппер, рефлексия, логирование
  Client/            публичный API + состояние клиента
  Patches/           Harmony-патчи
  UI/                оверлеи, секция настроек, мод-UI
  Plugin/            менеджер плагинов и host API
osucc/          лаунчер CLI (только run / start / status — ничего не собирает, не пишет в установку)
plugins/        встроенные плагины (ExamplePlugin, FakeSupporter, FriendsLeaderboard, Oii, osuccDebug, OsuCcUpdater, SubdivideNations, UsernameVisuals)
docs/           скриншоты (assets/), доки по языкам (en/, ru/)
```

## Как работает хук

Рантайм вызывает `StartupHook.Initialize()` до того, как выполнится `Main()`.
Хук подписывается на `AppDomain.AssemblyLoad`, и когда появляется `osu.Game`,
`ClientBootstrapper.InstallPatches()` ставит все патчи.

Цели патчей резолвятся по **имени** (assembly/type/method) в рантайме, поэтому
патч переживает обновления osu!. А вот UI/API код компилируется против NuGet-рефа
`ppy.osu.Game`. Продакшн-`osu.Game.dll` обычно новее этого рефа, так что не
ссылайтесь на внутренности osu напрямую. Ищите их по имени. Хелперы рефлексии лежат
в `osucc.Host/Core/Reflection.cs`.

Обнаруженная проблема: `GetField`/`GetMethod` с `FlattenHierarchy` **не видит** private
instance-члены базовых классов. Читайте их через declaring-тип или ходи по
`BaseType`.

## Плагины

Плагин это classlib, реализующий `IOsuCcPlugin` (или наследующий `OsuCcPluginBase`).
Рабочий пример: `plugins/ExamplePlugin`. Метаданные плагина объявляются в **проектном
файле**, а не в коде: сборка превращает свойства `PluginId` / `PluginName` /
`PluginAuthor` / `PluginDescription` / `PluginVersion` / `PluginPriority`, значения
`PluginIcon` (файл-картинка), `PluginIconGlyph` (имя FontAwesome) и `PluginIconResource`
(встроенный ресурс), а также элементы `PluginDependency` в assembly-уровневый атрибут
`[OsuCcPlugin]` (генерируется в `obj/PluginMetadata.g.cs`), который менеджер читает при
обнаружении. Legacy-архивы, где атрибут лежит на классе плагина, по-прежнему
обнаруживаются, но с предупреждением о deprecation. Через `IOsuCcPluginHost` плагин
может:

- добавлять кнопки на тулбар (`AddToolbarButton(factory, placement, layoutPosition)`)
- добавлять подсекции настроек
- регистрировать полноэкранные оверлеи
- отправлять уведомления (`host.Notify`)
- показывать эффекты личного рекорда
- ставить собственные Harmony-патчи через `host.AddPatch(...)` или обёртки
  `PatchHelper.AttachPrefix/AttachPostfix/AttachConstructorPostfix/AttachMethodPostfix`,
  возвращающие одноразовый хендл патча, который хост может отозвать
- сохранять свой конфиг (`GetSettings` / `GetStorage`)

Плагины также могут выставлять **публичный API** друг для друга. Система плагинов
даёт только транспорт — типы контракта живут там, где их разместит экспортирующий
плагин. Чтобы экспортировать, вызовите `host.ExportApi(api)` в `Load`; потребители
получают его по id плагина через `host.GetApi<T>(pluginId)` (возвращает `null`,
если плагина нет или экспортировано нечто, не приводимое к `T`). Поскольку хендлер
`AssemblyLoadContext.Default.Resolving` в хосте биндит любую ссылку на `osucc` к
задеплоенному хуку, типы контракта из `osucc.Host` унифицируются между плагинами;
контракт же внутри сборки экспортирующего плагина требует, чтобы потребитель
ссылался на эту сборку (в монорепо — `ProjectReference`). Для приведения к типу
между сборками контракт должен быть общим, поэтому интерфейсы экспортируемого API
стоит размещать в `osucc.Host` или общем пакете. Пример: встроенный плагин
`UsernameVisuals` экспортирует `IUsernameVisualsApi` (палитры своего/чужих имён,
оверрайды отображения и точечные оверрайды для конкретных игроков как
приоритетные кондишены) под id `username-visuals`. Собственные настройки
отображения своего имени (скрытие, замена) всегда побеждают правила других
плагинов; fallback-палитра чужих имён использует минимальный приоритет, так что
плагины всё ещё могут красить других игроков. Встроенный `ExamplePlugin`
потребляет его в `AttachToGame` через
`host.GetApi<IUsernameVisualsApi>("username-visuals")` (см.
`ExampleUsernameVisualsApiConsumer`); поскольку контракт живёт в сборке
`UsernameVisuals`, он добавляет `ProjectReference` на неё. Отсутствующий или
отключённый плагин-экспортёр даёт `null` из `GetApi` (потребитель обязан это
обрабатывать); компиляционная ссылка резолвится только когда код потребителя
реально трогает тип контракта.

Экспортируйте в `Load`, чтобы другие плагины видели API в своём `Load` (порядок —
через `Priority`) или в `AttachToGame`; `GetApi` безопасно вызывать из `AttachToGame`.

Зависимости объявляются в проектном файле через
`<PluginDependency Include="plugin-id" />`. Разрешитель зависимостей гарантирует, что
зависимый плагин грузится (и подключается) после своих зависимостей; когда ни
одна зависимость не требует иного порядка, порядок `Priority` сохраняется как
есть, так что система приоритетов продолжает работать (порядок отображения в
оверлее остаётся чисто приоритетным, стрелки переупорядочивания не затронуты).
Зависимости **мягкие**: отсутствующий или отключённый плагин-зависимость лишь
логирует предупреждение, а плагин грузится как обычно — `GetApi` вернёт `null`,
и потребитель должен это обрабатывать как и раньше. `ExamplePlugin` объявляет
зависимость на `username-visuals` как эталонный пример (см. его csproj).

Плагины поставляются как zip-архивы. Лаунчер кладёт их в папку данных osu-cc
(`plugins/`), где менеджер распаковывает каждый в папку, названную по `Id`
плагина. Отключённые плагины остаются в списке оверлея, но не грузятся.

Плагин **Менеджер обновлений** (`plugins/OsuCcUpdater`) особенный только тем, что
делает, а не как устроен: это обычный плагин с подсекцией настроек и кнопкой на
тулбаре. Он держит хук и встроенные плагины актуальными: получает рантайм-бандл
— из GitHub releases или собирая его локально из официального репозитория —
стейджит его в `<данные>/osu-cc/staging/` рядом с маркером `update.json`
(`UpdateMarker` из `osucc.Shared`), а лаунчер применяет его при следующем
запуске (запущенная игра держит файлы `hook/` в Windows, поэтому обновление
можно подменить только на следующем старте).

### Lifecycle-хуки и миграции данных

Поверх `Load` / `AttachToGame` плагин может реализовать опциональные интерфейсы,
чтобы реагировать на установку/удаление/обновление и версионировать свои данные:

- `IPluginLifecycle`: `OnInstall(host)` (первый запуск после установки, после
  `AttachToGame`), `OnUninstall(host)` (вызывается in-place в момент подтверждения
  удаления, до сноса payload-папки на следующем запуске), `OnUpdate(host,
  previousVersion)` (загруженная `Version` отличается от записанной ранее). Все
  хуки выполняются на update-thread, после миграций данных и после `AttachToGame`.
- `IPluginMigrations`: `SchemaVersion` плюс упорядоченные шаги `IPluginMigration`
  (`ToVersion`, `Apply(host)`). Когда сохранённая схема плагина ниже
  `SchemaVersion`, менеджер применяет шаги в порядке `ToVersion`, сохраняя результат
  каждого шага до выполнения следующего. Fresh-установки миграции пропускают.
  Внутри шага используйте `PluginSettings.ReadPersisted` / `Remove` / `ContainsKey`,
  чтобы прочитать legacy-значения и переименовать ключи.
- `OsuCcPluginBase`: удобный базовый класс: кеширует host в `Host`, даёт no-op
  lifecycle-хуки и пустые миграции, так что плагин переопределяет только нужное
  (`protected abstract void OnLoad()` вместо `Load`).

Последняя виденная версия (`version.<id>`) и схема (`schema.<id>`) персистятся в
`plugin-states.ini` рядом с папкой плагинов; обе записи стираются при удалении
плагина, поэтому повторная установка снова вызовет `OnInstall`. Дифф версий
сравнивает `PluginVersion`, объявленный в проектном файле, поэтому бампайте его при каждом
релизе, меняющем поведение или данные, иначе `OnUpdate` не сработает.

## Сборка и запуск

```shell
dotnet build osucc.build.proj -c Debug        # хук + плагины (+ локальный NuGet-фид)
dotnet build osucc.build.proj -t:PackRuntimeBundle -c Release   # artifacts/runtime/osucc-runtime-<версия>.zip
dotnet osucc/bin/Debug/net8.0/osucc.dll status # где что лежит / куда пойдёт
dotnet osucc/bin/Debug/net8.0/osucc.dll run    # запуск osu! с развёрнутым хуком
```

`osucc.build.proj` это единая MSBuild-точка входа: он пакует
`osucc.Host` / `osucc.Build` / `osucc` (dotnet tool) / `osucc.Shared` /
`osucc.Templates` в репозиторий-локальный фид (`artifacts/nuget`), чистит их
устаревшие копии в глобальном NuGet-кэше, затем собирает хук и все
`plugins/*/*.csproj` в одном параллельном MSBuild-процессе. Все дистрибутивные
пакеты разделяют одну версию (`OsuCcVersion`), централизованную в
`Directory.Packages.props` (CPM), поэтому бамп версии это одна правка.

`PackRuntimeBundle` собирает деплоимый вывод в один zip: `osucc.dll`,
`0Harmony.dll`, `SharpCompress.dll` и `osucc.Shared.dll` в `hook/`, плюс каждый
архив плагина в `plugins/`. NuGet-копии `osu.*` из `bin` сознательно **не**
включаются, так как они перезапишут продакшн-сборки. Деплой — это распаковка
этого бандла в папку данных (`hook/` + `plugins/`), что ровно и делает плагин
менеджера обновлений при стейджинге и что делают вручную при свежей установке.

Лаунчер (`osucc run` / `osucc start` / `osucc status`) ничего из этого не делает:
он находит установку osu! и корень данных, применяет отложенное обновление,
если оно ждёт, и запускает игру. Если хука нет, он завершается с ошибкой и
указывает на рантайм-бандл — он никогда не собирает и не пишет в установку, так
что работает без чекаута и не может ничего испортить. Резолв путей живёт в
`osucc/OsuCcPaths.cs` и общем `OsuCcDataRootResolver`; канонические имена
layout — в `osucc.Shared/OsuCcLayout.cs`.

### Обновление из игры

Обновление происходит в игре через плагин **Менеджер обновлений**, а не через лаунчер:

- **GitHub-бандл (по умолчанию):** плагин спрашивает у репозитория последний
  GitHub release, ищет в нём ассет `osucc-runtime-<версия>.zip` и скачивает его
  во временный файл.
- **Локальная сборка:** плагин клонирует (или фетчит) официальный репозиторий в
  `<данные>/osu-cc/src/osu-cc`, чекаутит новейший версионный тег и выполняет
  `dotnet build osucc.build.proj -t:PackRuntimeBundle -c Release`, получая тот
  же бандл. Нужны .NET SDK и git на машине.

В любом случае бандл распаковывается в `<данные>/osu-cc/staging/` (только
верхнеуровневые `hook/` и `plugins/`, с защитой от zip-slip) и пишется маркер
`update.json` с версией, источником и временем. При **следующем** запуске osu!
стейдженные файлы накладываются поверх `hook/` и `plugins/`, а `staging/`
удаляется — запущенная игра держит файлы хука в Windows, так что замена на
лету невозможна. `osucc status` показывает ждущее отложенное обновление, а
подсекция настроек и кнопка менеджера обновлений показывают текущую / последнюю /
стейдженную версии. Автопроверка запускается при старте и ограничена одним
разом в шесть часов; она уведомляет, но никогда не стейджит сама.

### Standalone-исполняемые файлы

Лаунчер можно опубликовать как self-contained single file для Linux и Windows (без .NET runtime
на целевой машине; кросс-сборка работает с любой ОС, так как приложение полностью managed):

```shell
dotnet publish osucc/osucc.csproj -p:PublishProfile=linux-x64   # artifacts/publish/linux-x64/osucc
dotnet publish osucc/osucc.csproj -p:PublishProfile=win-x64     # artifacts/publish/win-x64/osucc.exe
```

`PublishTrimmed` выключен (резолвер путей опирается на `AppContext.BaseDirectory`, который пуст
при unsafed-для-тримминга reflection). Standalone-бинарник без чекаута может `osucc run` уже
задеплоенного хука или `osucc status`.

### Публикация на NuGet

Всё, что нужно для дистрибуции, `dotnet build osucc.build.proj` кладёт в `artifacts/nuget`:

- `osucc.Host` — API плагинов (и сама сборка хука);
- `osucc.Build` — общие MSBuild props/targets для плагинов;
- `osucc` — лаунчер как [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
  (`osucc` ставит `PackAsTool`), поэтому `osucc status` / `run` / `start` работают без чекаута
  и сборки;
- `osucc.Shared` — общая логика layout/версий, подтягивается плагинами с NuGet (код лежит в
  проекте `osucc.Shared`, namespace `osucc.Common`);
- `osucc.Templates` — `dotnet new osucc-plugin`, создающий standalone-репо плагина, идентичное
  плагинам монорепа.

Локальная проверка из фида:

```shell
dotnet tool install osucc --tool-path /tmp/osucc-tool --add-source artifacts/nuget
dotnet new install artifacts/nuget/osucc.Templates.1.0.0.nupkg
dotnet new osucc-plugin -n MyPlugin -o /tmp/MyPlugin && dotnet build /tmp/MyPlugin
```

Сгенерированный проект ссылается на `osucc.Host` с NuGet и собирается без исходников osucc;
результирующий `MyPlugin.zip` дропается в папку игры `osu-cc/plugins`.

### CI и релизы

`.github/workflows/ci.yml` запускается на каждый push и PR, а также на теги `v*`:

- **build** и **build-windows** собирают хук, плагины и NuGet-пакеты
  (`dotnet build osucc.build.proj` в Release), гейт на `dotnet format --verify-no-changes`,
  публикуют standalone-лаунчеры и выполняют `PackRuntimeBundle`, получая единый
  рантайм-zip. Всё прикрепляется как CI-artifacts: файлы `.nupkg`, архивы плагинов
  `.zip`, бинарники `linux-x64` / `win-x64` и бандл `osucc-runtime-*.zip`.
- **publish** работает только на тегах `v*`: пушит пакеты на nuget.org через
  **trusted publishing** (OIDC — джоба получает `id-token: write` и обменивает GitHub-токен
  на короткоживущий API-ключ через `NuGet/login@v1`, никаких секретов в репозитории)
  и создаёт GitHub Release со всеми ассетами, включая рантайм-бандл, который тянет плагин
  менеджера обновлений. Политика доверия настраивается один раз на
  nuget.org (`account/trustedpublishing`, owner `rus07tam`, repo `osu-cc`).

Релиз: бампните `OsuCcVersion` в `Directory.Packages.props` (все пакеты её разделяют)
и дефолты шаблона в `templates/osucc.Templates/.../template.json`, затем затегайте `vX.Y.Z`.
Форма лаунчера на NuGet это dotnet tool `osucc`; standalone-бинарники это ассеты релиза,
а не пакеты.

Форматирование: `dotnet format osucc.sln` и
`dotnet format osucc.sln --verify-no-changes`. Правила стиля задаются в `.editorconfig`
и корневом `Directory.Build.props`. Используются только встроенные .NET-анализаторы,
и они предупреждают, но никогда не дают ошибок.

## Отладка

- Лог хука: `<data>/osu-cc/logs/<unix timestamp>.osu-cc.log`: по файлу на
  сессию, по строке на патч и его тайминг.
- Если лог хука чистый, но что-то не рендерится или падает, смотрите лог самой
  игры: `<data>/logs/<timestamp>.runtime.log`.

`<data>` это папка данных osu!: `%APPDATA%\osu` на Windows,
`~/.local/share/osu` на Linux.
