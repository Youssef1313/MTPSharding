# Batching design

This document is my brainstorming for designing batching support for MTP.

## User-facing design

From user point of view, they should be able to run a test executable with `--batch-count <number>`.

This should initiate discovery, and run `total_number_of_tests / <number>` test apps, each test app is then requested to run `<number>` tests.

## Concrete technical design

### Ideally

Ideally, I'd implement this via an MTP orchestrator. When `--batch-count` is seen, we start an orchestrator. The orchestrator then runs the individual test hosts and communicate with them with pipe.

### Orchestrator not yet exposed

As MTP orchestrator is not yet publicly exposed, a possible idea is for the test app entry point to recognize `--batch-count` command-line, and go through different code path.
Instead of running the test app normally, we go to a code path that will discover tests, and run multiple test apps with uid filters. We have three situations to consider:

#### Running as normal executable or via dotnet test pipe protocol

When running as a normal executable (either directly or via dotnet test pipe protocol), the entry point could build a "fake" test framework that runs. We run the child processes and communicate with them via the pipe protocol. The child processes communicate back test results to the "fake" test framework. The fake test framework responsibility is then to only report the reuslts received via pipe.

Note: the "fake" test framework knows nothing about anything. So it's not passed any arguments (except `--server dotnettestcli --dotnet-test-pipe ...` when running with dotnet test). This means, `--report-trx` or any other extensions are not going to work properly with batching. We could pass `--report-trx` etc down to each individual child process, but that means we are not collecting a single TRX.

Maybe we can start making the "fake" test framework aware of some common features.

#### Running in Json RPC server mode

This is not going to be supported, at least for now.

initiation of run will be done via a dotnet tool. The dotnet tool will play the role of an "orchestrator".
It will then start the test apps with `--batching-pipe` command-line option, which then causes the individual running test apps to communicate information back to the orchestrator.

### Summary

1. User runs `dotnet test --batch-count <number>` (or runs the exe directly with `--batch-count <number>`)
2. Test app entry point detects `--batch-count`, builds and runs a fake test framework that communicates via pipes.
3. Test app entry point starts doing discovery (via pipe protocol).
4. Test app entry point splits the received test node uids.
5. Test app entry point starts child processes with `--batch-pipe` and `--filter-uid`, and doesn't pass the original dotnet test pipe (if originally run with dotnet test pipe protocol)
6. The actual test hosts will recognize `--batch-pipe`, and will have a data consumer that forwards relevant information via pipe to the fake test framework. This information possible include test results and attachments (anything else?).
