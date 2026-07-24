# CI/CD Pipeline Playbook

A working, environment-gated release pipeline — CI → auto-versioned release → build-once
artifact → promote to dev/QA/prod without rebuilding — extracted from the real implementation in
this repo. Ten phases, in order, plus the five platform gotchas that cost the most time to find.

Stack used here: GitHub Actions, [Release Please](https://github.com/googleapis/release-please),
Conventional Commits, GitHub Environments, .NET / Azure Functions (with the deploy step already
swapped to Azure App Service in the snippets below — see the note on phase 5).

An interactive version of this doc (same content, nicer to read) is published at
<https://claude.ai/code/artifact/12472941-28db-4c2c-a85d-96bb4ecdd1e5>.

## Before you start

These are load-bearing. Missing any of them doesn't fail loudly — it fails three phases later in
a confusing way.

- [ ] **Repo is public, or you're on GitHub Pro/Team.** Classic branch protection rules and
  Environment protection rules (required reviewers) both 403 on a private repo on the Free plan
  with "Upgrade to GitHub Pro or make this repository public" — not a permissions bug, a plan
  limit.
- [ ] **GitHub Environments created** for every deploy target (e.g. `dev`, `QA`, `PROD`), each
  with its own deploy credential secret(s) — same secret *name*, different value per environment,
  so one workflow file works for all three.
- [ ] **Settings → Actions → General → "Allow GitHub Actions to create and approve pull
  requests" is checked.** Without it, nothing that opens a PR via `GITHUB_TOKEN` works — fails
  with "GitHub Actions is not permitted to create or approve pull requests."
- [ ] **A fine-grained PAT** (Contents: write, Pull requests: write), saved as a repo secret, for
  whichever action opens the release PR. See Gotcha #1 below — this isn't optional.
- [ ] **Conventional Commits** as the PR-title/commit-message convention going forward — the
  versioning step parses these to decide the next SemVer bump.

## Five things that cost real debugging time

Found empirically, not in the docs. Read these before you build, not after you hit them.

### 1. `GITHUB_TOKEN` can't trigger other workflows

A PR or Release created using the default `GITHUB_TOKEN` does not fire other workflows'
`pull_request` or `release` triggers. This is a deliberate anti-recursion guard, not a bug — but
it silently breaks the chain: your release PR never runs CI, and a published release never
triggers the packaging job. No error, just zero workflow runs.

**Fix:** pass a fine-grained PAT via `token:` to whichever action creates the PR/release, so it
acts with your identity instead.

### 2. `workflow_call` breaks environment secrets

Calling a reusable workflow (`uses: ./.github/workflows/deploy-x.yml`) whose job declares
`environment: dev` does *not* reliably resolve that environment's secrets — even though running
the same workflow directly (`workflow_dispatch`) works fine. Symptom: `app-name: should not be
empty` even though the secret exists.

**Fix:** don't chain environment-secret-dependent jobs through `uses:`. Inline the download+deploy
steps directly in the calling job instead.

### 3. Deploy branch policy checks the trigger ref, not the version

An Environment's `deployment_branch_policy: protected_branches` rejects a run if the *ref the
workflow was dispatched from* is a tag — even when the tag/version to deploy is a separate `tag`
input field, untouched. `Tag "v1.2.0" is not allowed to deploy ... due to environment protection
rules.`

**Fix:** always dispatch with `--ref main` (or your default branch); put the version being
deployed in the workflow's `tag` input, never in the ref/branch selector.

### 4. Hidden changelog types don't count as releasable

Release Please's default changelog sections mark `chore`, `docs`, `style`, `refactor`, `test`,
`build`, `ci` as `hidden: true` — and hidden types are also excluded from "is there anything to
release" detection. A push containing only e.g. `chore:` commits logs `No user facing commits
found — skipping` and opens nothing.

**Fix:** set `hidden: false` on every type you want to count in `changelog-sections`, if you want
every commit type to be releasable.

### 5. `gh api` booleans need `-F`, not `-f`

Scripting Environment/branch-protection setup with `gh api`: `-f enabled=true` sends the string
`"true"`, which the API rejects for boolean fields (`"true" is not a boolean"`).

**Fix:** use `-F` (capital) for booleans/numbers, lowercase `-f` only for actual strings.

## Architecture

```text
feature branch
      │  PR opened
      ▼
ci.yml            build · test · coverage · (SonarCloud)
      │  1 review + green check required to merge
      ▼
main    ───────────────────────────────────────────────────
      │  push
      ▼
release-please.yml   opens/updates "release PR" (version + CHANGELOG)
      │  ← the ONE human checkpoint: you merge this PR
      ▼
release-please.yml   (2nd run, same file) cuts tag + GitHub Release
      │  release: published
      ▼
package-release.yml  build ONCE · zip · attach to the Release
      │
      ├──▶ auto-deploy → dev            (inlined job, no approval)
      │
      │        same ZIP, no rebuild, from here down
      ▼
deploy-qa.yml        waits for required-reviewer approval → QA
      ▼
deploy-prod.yml      waits for required-reviewer approval → Production

rollback.yml         pick any past release → redeploy it, no rebuild
```

## Implementation, phase by phase

### Phase 1 — Audit first *(read-only)*

Before changing anything: list existing workflows, branch protection, environments, secrets, and
permissions via `gh api`. Write down what's already there and what's missing. Don't modify yet —
this becomes your punch list for phases 2–10.

**Done when:** you have a clear map of current workflows, and know which of the checklist items
above are already satisfied.

### Phase 2 — CI: build, test, coverage *(on every PR)*

`.github/workflows/ci.yml`

The only workflow that runs on `pull_request`. Restore → build → test with coverage → (optional)
static analysis. Never deploys, never tags, never releases — declare `permissions: contents:
read` explicitly so it structurally can't.

```yaml
name: CI

on:
  push:
    branches: ["main"]
  pull_request:
    branches: ["main"]

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build-test:
    runs-on: ubuntu-latest
    permissions:
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "9.0.x" }
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
      - run: dotnet restore {{SOLUTION}}.sln
      - run: dotnet build {{SOLUTION}}.sln -c Release --no-restore
      - run: dotnet test {{TEST_PROJECT}}.csproj -c Release --no-build
```

**Done when:** opening any PR shows this check running and required for merge (wire that up
properly in phase 9).

### Phase 3 — Versioning: Release Please *(on push to main)*

`.github/workflows/release-please.yml` + `.release-please-config.json` + `.release-please-manifest.json`

Single source of truth for SemVer, CHANGELOG, tags, and GitHub Releases, driven by Conventional
Commits. File names need the leading dot — `.release-please-config.json`, not
`release-please-config.json`.

```json
{
  "$schema": "https://raw.githubusercontent.com/googleapis/release-please/main/schemas/config.json",
  "packages": {
    ".": {
      "release-type": "simple",
      "changelog-sections": [
        { "type": "feat", "section": "Features" },
        { "type": "fix", "section": "Bug Fixes" },
        { "type": "chore", "section": "Miscellaneous Chores" }
      ]
    }
  }
}
```

```yaml
name: Release Please

on:
  push:
    branches: ["main"]

permissions:
  contents: write
  pull-requests: write

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: googleapis/release-please-action@v4
        with:
          token: ${{ secrets.RELEASE_PLEASE_TOKEN }}   # PAT — see Gotcha #1
          config-file: .release-please-config.json
          manifest-file: .release-please-manifest.json
```

**Done when:** a `feat:`/`fix:` push opens a "chore(main): release X.Y.Z" PR, and merging it
produces a real tag + GitHub Release.

### Phase 4 — Package once per release *(on `release: published`)*

`.github/workflows/package-release.yml`

Checks out the exact tag, builds, publishes, zips, and attaches the ZIP as a Release asset. This
is the only place the deployable artifact is produced — every environment below downloads this
same ZIP instead of rebuilding.

```yaml
on:
  release:
    types: [published]
  workflow_dispatch:            # manual recovery path, e.g. to backfill an old release
    inputs:
      tag: { required: true, type: string }

jobs:
  package:
    runs-on: ubuntu-latest
    outputs:
      tag: ${{ steps.release.outputs.tag }}
    steps:
      - id: release
        run: echo "tag=${{ github.event.release.tag_name || inputs.tag }}" >> "$GITHUB_OUTPUT"
      - uses: actions/checkout@v4
        with: { ref: "${{ steps.release.outputs.tag }}" }
      - uses: actions/setup-dotnet@v4          # or actions/setup-node@v4 for a Node app
        with: { dotnet-version: "9.0.x" }
      - run: dotnet publish {{PROJECT}}.csproj -c Release -o publish   # or: npm ci && npm run build
      - run: cd publish && zip -r "../artifact-${{ steps.release.outputs.tag }}.zip" .
      - env: { GH_TOKEN: "${{ secrets.GITHUB_TOKEN }}" }
        run: gh release upload "${{ steps.release.outputs.tag }}" "artifact-*.zip" --clobber
```

**Done when:** every published Release has exactly one ZIP asset attached, automatically.

### Phase 5 — Deploy to dev *(auto, no approval)*

Inline job in `package-release.yml` (see Gotcha #2 — not a separate reusable workflow call).

Downloads the ZIP from the Release and deploys it. Never compiles. Chain it with `needs: package`
in the same workflow so it can't run before the ZIP exists, and give the job its own
`environment: dev` directly.

```yaml
  deploy-dev:
    needs: package
    if: github.event_name == 'release'
    runs-on: ubuntu-latest
    environment: dev
    steps:
      - env: { GH_TOKEN: "${{ secrets.GITHUB_TOKEN }}" }
        run: |
          gh release download "${{ needs.package.outputs.tag }}" \
            --pattern "artifact-*.zip" --dir ./artifact --clobber
      - uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.APP_NAME }}          # App Service name for the dev slot/app
          package: ./artifact/artifact-${{ needs.package.outputs.tag }}.zip
          publish-profile: ${{ secrets.PUBLISH_PROFILE }}   # Azure Portal → App Service → Get publish profile
```

Same secret *names* (`APP_NAME`, `PUBLISH_PROFILE`) in each environment's own secret store —
that's what makes `deploy-qa.yml`/`deploy-prod.yml` below just copies of this job with a
different `environment:` value.

**Done when:** merging the release PR ends with dev already running the new version, no manual
step.

### Phase 6 — Deploy to QA *(manual + approval)*

`.github/workflows/deploy-qa.yml`

Same download+deploy shape as dev, as its own standalone workflow: `workflow_dispatch` (and
optionally `workflow_call`, but see Gotcha #2 before relying on that) with a `tag` input,
targeting the `QA` environment. The pause for approval is entirely config, not code — set up in
phase 9.

**Done when:** dispatching this workflow lands in *waiting* status until a required reviewer
approves it.

### Phase 7 — Deploy to Production *(manual + approval)*

`.github/workflows/deploy-prod.yml`

Identical pattern again, targeting `PROD`. Resist the urge to add extra logic here — the whole
point is that dev/QA/prod are the same code path against a different environment name, so a bug
found in one is a bug in all three.

**Done when:** you can promote the exact tag that passed QA straight to prod with one command, no
rebuild in between.

### Phase 8 — Rollback *(manual)*

`.github/workflows/rollback.yml`

`workflow_dispatch` with an `environment` choice and a `tag` input. A first job lists existing
releases into the job summary (no dynamic dropdowns in Actions) and validates the chosen tag
exists before deploying. Duplicate the inline download+deploy steps per environment here too —
same Gotcha #2.

**Done when:** redeploying release N-3 to any environment is a single dispatch, with the same
approval gates as a forward promotion.

### Phase 9 — Protection *(GitHub settings + `gh api`)*

Wire the config that makes the workflow above actually enforced, not just possible:

- Branch protection on `main`: required PR + 1 review, required status check = your CI job name
  (the literal job id, e.g. `build-test` — check via
  `gh api repos/OWNER/REPO/commits/main/check-runs` to get the exact string), `strict: true`.
- QA and PROD environments: add a required reviewer, and
  `deployment_branch_policy: { protected_branches: true }` so only `main` can trigger a deploy to
  them (remember Gotcha #3: dispatch *from* main, put the version in the tag input).
- Decide deliberately whether `enforce_admins` is on — off means repo admins can bypass
  everything above, which is convenient solo and dangerous in a team.

**Done when:** a non-admin collaborator genuinely cannot merge without review + green CI, and
cannot deploy to QA/PROD without an approval.

### Phase 10 — Quality pass *(cheap, do it last)*

- **Cache** package restores (`actions/cache` keyed on the lockfile/csproj hash) in every workflow
  that builds.
- **Concurrency** groups: cancel superseded CI runs on the same ref; never cancel a release or a
  deploy mid-flight — group those by environment/tag with `cancel-in-progress: false` so two
  deploys to the same target queue instead of racing.
- **Restrict Actions to an allowlist** (Settings → Actions → General → "Allow select actions")
  instead of "Allow all actions", explicitly listing every third-party action you actually use.
- Delete the workflow that this whole pipeline replaces, don't leave it disabled next to the new
  one — two systems racing to tag/release is worse than one broken system.

## Quick reference

| Do this | Command |
|---|---|
| Force a specific version instead of auto-computed | `gh workflow run release-please.yml -f release-as=1.5.0` |
| Deploy a specific tag to QA | `gh workflow run deploy-qa.yml --ref main -f tag=v1.5.0` |
| Backfill a ZIP for an old release | `gh workflow run package-release.yml -f tag=v1.4.2` |
| Check exact CI check-run names for branch protection | `gh api repos/OWNER/REPO/commits/main/check-runs --jq '.check_runs[].name'` |
| Add a required reviewer to an environment | `gh api -X PUT repos/OWNER/REPO/environments/QA --input reviewers.json` |
| Set a boolean via `gh api` | `gh api -X PATCH ... -F strict=true` (capital `-F`, not `-f`) |

---

Distilled from the real build-out of this repo's pipeline (GitHub Actions, Release Please,
SonarCloud). Deploy steps shown here use `azure/webapps-deploy@v3` for an Azure App Service web
app — swap the build command (phase 4) for your stack (Node, ASP.NET, etc.) and the deploy action
for a different host. The ordering, the gotchas, and the environment-gating pattern don't change
either way.
