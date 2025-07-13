# Batching design

This document is my brainstorming for designing batching support for MTP.

## User-facing design

From user point of view, they should be able to run a test executable with `--batch-count <number>`.

This should initiate discovery, and run `total_number_of_tests / <number>` test apps, each test app is then requested to run `<number>` tests.

## Concrete technical design

### Ideally

Ideally, I'd implement this via an MTP orchestrator. When `--batch-count` is seen, we start an orchestrator. The orchestrator then runs the individual test hosts and communicate with them with pipe.

### Orchestrator not yet exposed

As MTP orchestrator is not yet publicly exposed, initiation of run will be done via a dotnet tool. The dotnet tool will play the role of an "orchestrator".
It will then start the test apps with `--batching-pipe` command-line option, which then causes the individual running test apps to communicate information back to the orchestrator.
