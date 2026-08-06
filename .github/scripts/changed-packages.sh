#!/usr/bin/env bash

set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
base_ref="${1:?usage: changed-packages.sh <base-ref>}"

cd "$repo"

# package-id -> path prefix and the version property in Directory.Packages.props
declare -A prefix=(
  [osucc.Host]=osucc.Host/
  [osucc.Build]=build/osucc.Build/
  [osucc.Shared]=osucc.Shared/
  [osucc]=osucc/
  [osucc.Templates]=templates/
)
declare -A version_prop=(
  [osucc.Host]=OsuCcHostVersion
  [osucc.Build]=OsuCcBuildVersion
  [osucc.Shared]=OsuCcSharedVersion
  [osucc]=OsuCcLauncherVersion
  [osucc.Templates]=OsuCcTemplatesVersion
)
declare -A depends=(
  [osucc.Shared]="osucc.Host osucc"
)

mapfile -t files < <(git diff --name-only "$base_ref")

changed=""
all=false

for f in "${files[@]}"; do
  case "$f" in
    docs/*|.github/*|*.md)
      continue ;;
    Directory.Packages.props|Directory.Build.props|NuGet.config|.editorconfig|osucc.build.proj|*.sln)
      all=true ;;
  esac

  [ "$all" = true ] && break

  matched=false
  for id in "${!prefix[@]}"; do
    if [[ "$f" == "${prefix[$id]}"* ]]; then
      changed="${changed} ${id}"
      matched=true
    fi
  done
  [ "$matched" = false ] && all=true
done

if [ "$all" = true ]; then
  ids=(osucc.Host osucc.Build osucc.Shared osucc osucc.Templates)
else
  queue=($changed)
  for id in "${queue[@]}"; do
    for dep in ${depends[$id]:-}; do
      case " ${queue[*]} " in
        *" $dep "*) ;;
        *) queue+=("$dep") ;;
      esac
    done
  done
  ids=($(printf '%s\n' "${queue[@]}" | sort -u))
fi

for id in "${ids[@]}"; do
  ver="$(grep -oP "<${version_prop[$id]}>\K[^<]+" Directory.Packages.props)"
  echo "$id=$ver"
done
