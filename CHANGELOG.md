# Changelog

Todos los cambios relevantes de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), y el
versionado sigue [SemVer](https://semver.org/), calculado automáticamente con
[GitVersion](https://gitversion.net/) en el workflow `create-release.yml`.

## [1.0.1-1] - 2026-07-23

### Changed

- Cambio menor de prueba en `HttpTriggerFunction.cs` para validar el pipeline
  de release de extremo a extremo.

## [1.0.0] - 2026-07-23

### Added

- Pipeline de CI (`ci.yml`): build, pruebas unitarias con cobertura y análisis
  con SonarCloud en cada push/PR a `master`.
- Workflow de release automático (`create-release.yml`): se dispara al
  completarse la CI en `master`, publica el artefacto ZIP de la Function App,
  crea el tag `vX.Y.Z` y una GitHub Release con notas generadas automáticamente.
- Versionado semántico automático con GitVersion (workflow `GitHubFlow/v1`).

### Fixed

- Ruta de proyecto incorrecta en `create-release.yml` que impedía publicar el
  artefacto.
- Condición de disparo del release: ahora se limita a ejecuciones de CI
  exitosas sobre `master`, evitando releases sobre ramas o PRs.
- Error de validación de configuración de GitVersion (`Branch configuration
  'master' is missing required configuration 'regex'`) causado por una entrada
  `branches.master` redundante que no heredaba el `regex` por defecto del
  branch `main` en el preset `GitHubFlow/v1`.
