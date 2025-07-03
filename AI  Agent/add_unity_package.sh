#!/bin/bash
# Скрипт для додавання Unity package у manifest.json
# Використання: ./add_unity_package.sh <package_name> <package_version>

PACKAGE_NAME=${1:-com.unity.textmeshpro}
PACKAGE_VERSION=${2:-3.0.6}
MANIFEST_PATH="Packages/manifest.json"

if [ ! -f "$MANIFEST_PATH" ]; then
  echo "manifest.json не знайдено! Створіть Unity-проєкт у цій папці."
  exit 1
fi

if ! command -v jq &> /dev/null; then
  echo "Встановіть jq: sudo apt install jq"
  exit 1
fi

jq --arg name "$PACKAGE_NAME" --arg ver "$PACKAGE_VERSION" \
  '.dependencies[$name]=$ver' "$MANIFEST_PATH" > tmp_manifest.json && mv tmp_manifest.json "$MANIFEST_PATH"

echo "Пакет $PACKAGE_NAME@$PACKAGE_VERSION додано до manifest.json"

# Скрипт для структурування Unity Package

mkdir -p Runtime
mkdir -p Editor

mv -f AIAgentSettings.cs Runtime/AIAgentSettings.cs
mv -f AIAgentUnity.cs Editor/AIAgentUnity.cs

echo "Package structure prepared."
