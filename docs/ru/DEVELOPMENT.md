# osu!cc: разработка

Заметки для тех, кто копается в этом репозитории.

**Языки:** [English](../en/DEVELOPMENT.md) · Русский

Документация по разработке плагинов находится в [PLUGINS.md](PLUGINS.md).

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
osucc/          лаунчер CLI (только status; голый osucc запускает - ничего не собирает, не пишет в установку)
plugins/        встроенные плагины (CustomUserGroups, ExamplePlugin, FakeSupporter, FriendsLeaderboard, Oii, osuccDebug, OsuCcUpdater, SubdivideNations, UsernameVisuals)
docs/           скриншоты (assets/), доки по языкам (en/, ru/)
```

## Как работает хук

Рантайм вызывает `StartupHook.Initialize()` до того, как выполнится `Main()`. Хук подписывается на `AppDomain.AssemblyLoad`, и когда появляется `osu.Game`, `ClientBootstrapper.InstallPatches()` ставит все патчи.

Цели патчей резолвятся по **имени** (assembly/type/method) в рантайме, поэтому патч переживает обновления osu!. А вот UI/API код компилируется против NuGet-рефа `ppy.osu.Game`. Продакшн-`osu.Game.dll` обычно новее этого рефа, так что не ссылайтесь на внутренности osu напрямую. Ищите их по имени. Хелперы рефлексии лежат в `osucc.Host/Core/Reflection.cs`.

Обнаруженная проблема: `GetField`/`GetMethod` с `FlattenHierarchy` **не видит** private instance-члены базовых классов. Читайте их через declaring-тип или ходите по `BaseType`.

## Сборка и запуск

```shell
dotnet build osucc.build.proj -c Debug        # хук + плагины (+ локальный NuGet-фид)
dotnet build osucc.build.proj -t:PackBootstrapBundle -c Release   # artifacts/runtime/osucc-runtime-<версия>.zip
dotnet osucc/bin/Debug/net8.0/osucc.dll status # где все находится и куда будет ставиться
dotnet osucc/bin/Debug/net8.0/osucc.dll        # запуск osu! с установленным хуком (действие по умолчанию)
```

`osucc.build.proj` - это единая точка входа MSBuild в репозитории: собирает пакеты `osucc.Host` / `osucc.Build` / `osucc` (как dotnet tool) / `osucc.Shared` / `osucc.Templates` в локальный фид (`artifacts/nuget`), очищает их кэш, затем собирает хук и все плагины `plugins/*/*.csproj` параллельно. Каждый пакет версионируется независимо (`OsuCcHostVersion`/`OsuCcBuildVersion`/`OsuCcSharedVersion`/`OsuCcLauncherVersion`/`OsuCcTemplatesVersion`), централизованно в `Directory.Packages.props`, так что обновление версий затрагивает только изменившиеся части.

`PackBootstrapBundle` собирает рантайм-бандл в один zip: `osucc.dll`, `0Harmony.dll`, `SharpCompress.dll` и `osucc.Shared.dll` в `hook/`, плюс архивы плагинов в `plugins/`. Восстановленные через NuGet копии `osu.*` в `bin` намеренно **не** включаются, так как они перезаписали бы продакшн-сборки. Деплой - это распаковка бандла в папку данных (`hook/` + `plugins/`), что делает менеджер обновлений при применении стейджа или при чистой установке.

Лаунчер (`osucc` / `osucc status`) ничего из этого не делает: он находит установку osu! и папку данных, накатывает подготовленное обновление (если оно есть) и запускает игру. Если хук отсутствует, он падает с сообщением и ссылкой на рантайм-бандл - он никогда не собирает и не пишет в установку, так что работает без чекаута кода и ничего не портит. Логика путей живет в `osucc/OsuCcPaths.cs` и общем `OsuCcDataRootResolver`; имена каталогов - в `osucc.Shared/OsuCcLayout.cs`.

### Обновление из игры

Обновление происходит внутри игры через плагин **Менеджер обновлений**, а не через лаунчер:

- **GitHub бандл (по умолчанию):** плагин запрашивает последний релиз в GitHub репозитории, скачивает `osucc-runtime-<версия>.zip` во временный файл.
- **Локальная сборка:** клонирует (или стягивает) официальный репозиторий в `<данные>/osu-cc/src/osu-cc`, чекаутит тег свежей версии и запускает `dotnet build osucc.build.proj -t:PackBootstrapBundle -c Release`, собирая такой же бандл. На машине должны быть .NET SDK и git.

В обоих случаях бандл распаковывается в `<данные>/osu-cc/staging/` (только папки `hook/` и `plugins/` с защитой от zip-slip) и создается маркер `update.json` с версией, источником и временем. При **следующем** запуске osu! лаунчер перенесет файлы поверх `hook/` и `plugins/` и удалит `staging/` - запущенная игра держит файлы хука в Windows, поэтому замена на лету невозможна. `osucc status` показывает отложенное обновление, а настройки плагина обновлений выводят текущую/свежую/подготовленную версии. Автопроверка запускается раз в 6 часов на старте; она только уведомляет и ничего не стейджит без ведома пользователя.

### Автономные исполняемые файлы

Лаунчер можно опубликовать как self-contained single file под Linux и Windows (без требований к .NET рантайму на целевой машине; кросс-публикация работает с любой ОС):

```shell
dotnet publish osucc/osucc.csproj -p:PublishProfile=linux-x64   # artifacts/publish/linux-x64/osucc
dotnet publish osucc/osucc.csproj -p:PublishProfile=win-x64     # artifacts/publish/win-x64/osucc.exe
```

Свойство `PublishTrimmed` отключено (разрешитель путей полагается на `AppContext.BaseDirectory`, который пуст при тримминге из-за рефлексии). Автономный бинарник без чекаута репозитория может запускать уже развернутый хук (просто `osucc`) или выводить `osucc status`.

### Публикация в NuGet

Все дистрибутивы собираются через `dotnet build osucc.build.proj` в каталог `artifacts/nuget`:

- `osucc.Host` - API плагинов (и сама сборка хука);
- `osucc.Build` - общие MSBuild props/targets для плагинов;
- `osucc` - лаунчер как [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) (`osucc` задает `PackAsTool`), чтобы `osucc` и `osucc status` работали глобально без сборки;
- `osucc.Shared` - общая логика layout/версий, подтягивается плагинами с NuGet (код лежит в `osucc.Shared` project, namespace `osucc.Common`);
- `osucc.Templates` - `dotnet new osucc-plugin`, создающий standalone-репозиторий плагина, идентичный монорепе.

Тест локальной установки из фида:

```shell
dotnet tool install osucc --tool-path /tmp/osucc-tool --add-source artifacts/nuget
dotnet new install artifacts/nuget/osucc.Templates.1.0.0.nupkg
dotnet new osucc-plugin -n MyPlugin -o /tmp/MyPlugin && dotnet build /tmp/MyPlugin
```

Созданный проект плагина ссылается на `osucc.Host` с NuGet и собирается без исходников osucc; бросьте `my-plugin.zip` в папку игры `osu-cc/plugins`.

### CI и релизы

`.github/workflows/ci.yml` запускается на каждый пуш, PR и тег `v*`:

- **build** и **build-windows** собирают хук, плагины и NuGet-пакеты (`dotnet build osucc.build.proj` в Release), проверяют форматирование через `dotnet format --verify-no-changes`, публикуют автономные лаунчеры и упаковывают рантайм-бандл. Все файлы прикрепляются как артефакты сборки: `.nupkg`, архивы плагинов `.zip`, бинарники `linux-x64` / `win-x64` и бандл `osucc-runtime-*.zip`. Стейджинг для NuGet публикует только изменившиеся пакеты относительно base-ref (`.github/scripts/changed-packages.sh`: для PR - базовой ветки, для тега `v*` - предыдущего тега, иначе - предыдущего коммита; изменение `osucc.Shared` также перепубликует `osucc.Host` и `osucc`).
- **publish** запускается только по тегам `v*`: отправляет изменившиеся пакеты на nuget.org через доверенную публикацию (OIDC - джоба получает `id-token: write` и обменивает токен GitHub на короткоживущий ключ API через `NuGet/login@v1`, без использования секретов репо) и создает релиз на GitHub со всеми артефактами. Описание релиза генерируется из списка версий пакетов. Доверенная публикация настраивается на nuget.org один раз (`account/trustedpublishing`, владелец `rus07tam`, репозиторий `osu-cc`).

Для релиза: поднимите версии изменившихся компонентов в `Directory.Packages.props` (и дефолты шаблонов в `templates/osucc.Templates/.../template.json`), затем поставьте тег `vX.Y.Z`. CI опубликует ровно эти пакеты, а менеджер обновлений заберет бандл по версии хоста. Исполняемые файлы прикрепляются к релизу вручную или скриптом, а не через NuGet.

Форматирование проверяется через `dotnet format osucc.sln` и `dotnet format osucc.sln --verify-no-changes`. Правила лежат в `.editorconfig` и корневом `Directory.Build.props`. Используются стандартные анализаторы .NET в режиме warnings.

## Отладка

- Лог хука: `<данные>/osu-cc/logs/<unix timestamp>.osu-cc.log` - один файл на сессию с информацией по каждому патчу и времени загрузки.
- Если лог хука пуст, но игра падает или некорректно работает, смотрите собственный лог игры: `<данные>/logs/<timestamp>.runtime.log`.

`<данные>` - это папка данных osu!: `%APPDATA%\osu` на Windows, `~/.local/share/osu` на Linux.
