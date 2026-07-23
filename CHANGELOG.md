# Changelog

Todos los cambios relevantes de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), y el
versionado sigue [SemVer](https://semver.org/), calculado automáticamente con
[GitVersion](https://gitversion.net/) en el workflow `create-release.yml`.

## [1.1.1](https://github.com/raestradab/AzurefunctionDemo/compare/v1.1.0...v1.1.1) (2026-07-23)


### Bug Fixes

* use a PAT for Release Please so it can trigger downstream workflows ([d7b29ec](https://github.com/raestradab/AzurefunctionDemo/commit/d7b29ece15fc314d0ebeaa2589dfed70fc26b9cf))

## [1.1.0](https://github.com/raestradab/AzurefunctionDemo/compare/v1.0.0...v1.1.0) (2026-07-23)


### Features

* add deploy-dev workflow ([67762f3](https://github.com/raestradab/AzurefunctionDemo/commit/67762f3670f2e4e27ef238681e9818342266a452))
* add deploy-prod workflow with manual approval ([48b7207](https://github.com/raestradab/AzurefunctionDemo/commit/48b7207f8a658b1d11622a7635d9bf1d2f29f737))
* add deploy-qa workflow with manual approval ([303bf1b](https://github.com/raestradab/AzurefunctionDemo/commit/303bf1b4e0bca00e6080eec979d9388b46ffb5bd))
* add package-release workflow ([2533ff2](https://github.com/raestradab/AzurefunctionDemo/commit/2533ff21e80d4b5c77af047fb84af94fc9a5a8e4))
* add rollback workflow ([1d4e9d9](https://github.com/raestradab/AzurefunctionDemo/commit/1d4e9d922e066f166f9ad085c15272728fb17986))


### Bug Fixes

* rename release-please config files to required dotfile names ([474346d](https://github.com/raestradab/AzurefunctionDemo/commit/474346dee09d22a14e67d33537b189eb1dcce171))

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
