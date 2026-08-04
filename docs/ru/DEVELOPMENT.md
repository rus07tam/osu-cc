# osu!cc: разработка

Заметки для тех, кто копается в этом репозитории.

**Языки:** [English](../en/DEVELOPMENT.md) · Русский

## Структура

```plaintext
osucc.Host/     DLL стартап-хука (classlib, net8.0), он же NuGet-пакет osucc.Host
  StartupHook.cs     точка входа, которую вызывает рантайм
  Core/              бутстраппер, рефлексия, логирование
  Client/            публичный API + состояние клиента
  Patches/           Harmony-патчи
  UI/                оверлеи, секция настроек, мод-UI
  Plugin/            менеджер плагинов и host API
osucc/          лаунчер CLI (build / deploy / run / start / clean / status)
plugins/        встроенные плагины (ExamplePlugin, FriendsLeaderboard, Oii, osuccDebug, SubdivideNations, UsernameVisuals)
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

Плагин это classlib с типом, помеченным `[OsuCcPlugin]`. Рабочий пример:
`plugins/ExamplePlugin`. Через `IOsuCcPluginHost` плагин может:

- добавлять кнопки на тулбар (`AddToolbarButton(factory, placement, layoutPosition)`)
- добавлять подсекции настроек
- регистрировать полноэкранные оверлеи
- отправлять уведомления (`host.Notify`)
- показывать целебрации
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

Зависимости объявляются в `[OsuCcPlugin]` через
`DependsOn = new[] { "plugin-id" }`. Разрешитель зависимостей гарантирует, что
зависимый плагин грузится (и подключается) после своих зависимостей; когда ни
одна зависимость не требует иного порядка, порядок `Priority` сохраняется как
есть, так что система приоритетов продолжает работать (порядок отображения в
оверлее остаётся чисто приоритетным, стрелки переупорядочивания не затронуты).
Зависимости **мягкие**: отсутствующий или отключённый плагин-зависимость лишь
логирует предупреждение, а плагин грузится как обычно — `GetApi` вернёт `null`,
и потребитель должен это обрабатывать как и раньше. `ExamplePlugin` объявляет
зависимость на `username-visuals` как эталонный пример (см. его атрибут).

Плагины поставляются как zip-архивы. Лаунчер кладёт их в папку данных osu-cc
(`plugins/`), где менеджер распаковывает каждый в папку, названную по `Id`
плагина. Отключённые плагины остаются в списке оверлея, но не грузятся.

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
сравнивает атрибут `Version` из `[OsuCcPlugin]`, поэтому бампайте его при каждом релизе,
меняющем поведение или данные, иначе `OnUpdate` не сработает.

## Сборка и запуск

```shell
dotnet build osucc/osucc.csproj -c Debug
dotnet osucc/bin/Debug/net8.0/osucc.dll start       # build + deploy + run
```

`osucc build` делегирует в единую MSBuild-точку входа `osucc.build.proj`: он пакует
`osucc.Host` / `osucc.Build` / `osucc` (dotnet tool) / `osucc.Templates` в репозиторий-локальный
фид (`artifacts/nuget`), чистит их устаревшие копии в глобальном NuGet-кэше, затем собирает хук и
все `plugins/*/*.csproj` в одном параллельном MSBuild-процессе. Все четыре
дистрибутивных пакета разделяют одну версию (`OsuCcVersion`), централизованную в
`Directory.Packages.props` (CPM), поэтому бамп версии это одна правка.

`osucc deploy` копирует `osucc.dll`, `0Harmony.dll` и `SharpCompress.dll` в
`<данные osu-cc>/hook/` и архивы плагинов в `plugins/`. NuGet-копии `osu.*` из
`bin` сознательно **не** деплоятся, так как они перезапишут продакшн-сборки.

Сборка требует локального чекаута: лаунчер находит репозиторий, поднимаясь от своего
расположения до `osucc.sln` (`--repo` переопределяет). Команды, которые не компилируют
(`run`, `status`, `clean`), работают без чекаута; `osucc run` запускает уже
задеплоенный хук и вообще не трогает репозиторий.

### Обновление без сборки

`osucc update` держит хук и плагины актуальными без чекаута, забирая готовые
артефакты из публичных фидов:

- хук: последний пакет `osucc.Host` с nuget.org — flat-container API возвращает
  новейшую стабильную версию, затем nupkg распаковывается в
  `<данные osu-cc>/hook/` вместе с его рантайм-зависимостями (`Lib.Harmony` →
  `0Harmony.dll`, `SharpCompress`), версии которых читаются из собственного nuspec пакета;
- плагины: zip-ассеты последнего GitHub release (их прикладывает CI)
  скачиваются в `<данные osu-cc>/plugins/`, где встроенный `PluginPackageStore`
  распакует их при следующем запуске;
- лаунчер (`--launcher`): глобальный dotnet tool выполняет `dotnet tool update`,
  standalone-бинарник заменяется на release-сборку той же ОС (Windows откладывает
  замену в отдельный скрипт, потому что работающий exe заблокирован).

`osucc update` пропускает хук, если задеплоенный `osucc.dll` уже несёт последнюю
версию, и всегда качает архивы плагинов из последнего GitHub release (они малы и
идемпотентны). Если в последнем релизе нет ни одного архива плагинов, команда
возвращает ошибку — сломанный релиз не остаётся молчаливым no-op.

### Standalone-исполняемые файлы

Лаунчер можно опубликовать как self-contained single file для Linux и Windows (без .NET runtime
на целевой машине; кросс-сборка работает с любой ОС, так как приложение полностью managed):

```shell
dotnet publish osucc/osucc.csproj -p:PublishProfile=linux-x64   # artifacts/publish/linux-x64/osucc
dotnet publish osucc/osucc.csproj -p:PublishProfile=win-x64     # artifacts/publish/win-x64/osucc.exe
```

`PublishTrimmed` выключен (резолвер путей опирается на `AppContext.BaseDirectory`, который пуст
при unsafed-для-тримминга reflection). Standalone-бинарник без чекаута может `osucc run`
уже задеплоенного хука.

### Публикация на NuGet

Всё, что нужно для дистрибуции, `osucc build` кладёт в `artifacts/nuget`:

- `osucc.Host` — API плагинов (и сама сборка хука);
- `osucc.Build` — общие MSBuild props/targets для плагинов;
- `osucc` — лаунчер как [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
  (`osucc` ставит `PackAsTool`), поэтому `osucc status` / `run` работают без чекаута
  и сборки (`start`/`deploy` всё ещё требуют репозиторий);
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

- **build** и **build-windows** собирают хук, плагины и четыре NuGet-пакета (`osucc build`
  в Release), гейт на `dotnet format --verify-no-changes` и публикуют standalone-лаунчеры.
  Всё прикрепляется как CI-artifacts: файлы `.nupkg`, архивы плагинов `.zip` и бинарники
  `linux-x64` / `win-x64`.
- **publish** работает только на тегах `v*`: пушит четыре пакета на nuget.org через
  **trusted publishing** (OIDC — джоба получает `id-token: write` и обменивает GitHub-токен
  на короткоживущий API-ключ через `NuGet/login@v1`, никаких секретов в репозитории)
  и создаёт GitHub Release со всеми ассетами. Политика доверия настраивается один раз на
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
