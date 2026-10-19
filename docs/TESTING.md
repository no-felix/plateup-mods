# Testing

Build all projects with `scripts/Build.ps1`. Run `scripts/Run-GameTests.ps1 -Mod refill-containers` with Steam running and PlateUp closed. The runner temporarily installs a test assembly, launches the native player, and removes its test folder when finished. It uses the installed production DLL only when it matches the build.

Each run writes `player.log` and `coverage.md` under `artifacts/tests/refill-containers/<timestamp>`. The report lists every candidate, recipe-variant count, case count and result. Test assemblies and game DLLs are excluded from release packages.

The current [coverage snapshot](../mods/refill-containers/tests/coverage.md) records the verified base-game build. It is updated after a successful native run.

## Automated coverage

The suite enumerates every loaded item with portioning data. An explicit list defines the 19 supported base-game item types. Every other candidate is checked for rejection against declared remainders and known carriers, so new support cannot silently escape the test inventory.

Supported groups are built from complete ingredient combinations accepted by the game's recipe lookup. Baked groups use their declared process transitions. Each valid variant is tested with:

- Full and one-portion sources.
- Empty carriers obtained from their actual providers and from native depletion.
- Player grab-input and the installed combiner settings.
- A second transfer using the returned carrier.

Assertions check both holder links, item identity, recipe components, portion counts and entity conservation. Separate cases cover filtered or locked holders, incomplete recipes, invalid counts, unsupported senders, hidden food in carrier groups and competing transfers. A bootstrap test verifies native world creation, mod injection and scheduled acceptance.

Two representation constraints must remain covered: native recipe merges can mark complete food groups as transient, and providers can represent empty trays as item groups. Neither flag alone establishes whether food or a carrier is valid.

## Release checks requiring a playthrough

- Portion and serve each food after refilling; verify rendered flavour/topping changes.
- Run a complete combiner/conveyor loop, including blocked output.
- Save, reload, remove the mod and reload again.
- Test a modded host with an unmodded client before claiming host-only support.
- Install a private Workshop upload after removing the local copy.

The automated suite runs inside the real Unity player but uses isolated ECS worlds. It does not establish the playthrough, networking or Workshop checks above. Subscribed mods remain active during these runs.
