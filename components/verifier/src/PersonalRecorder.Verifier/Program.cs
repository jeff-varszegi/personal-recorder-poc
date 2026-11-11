// Personal Recorder — evidence verifier (scaffold).
//
// This tool is deliberately independent: it never contacts our servers, and it is the
// open-source component that makes the evidentiary story checkable by a third party
// (docs/ARCHITECTURE.md §7.3, ADR-014). Verification logic lands with the evidence
// format in Phase 3; the contract it will honour is:
//
//   personal-recorder-verify <bundle-path>
//     exit 0  — chain complete, intact, correctly signed, timestamps consistent
//     exit 1  — verification failed (details on stdout)
//     exit 64 — usage error

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: personal-recorder-verify <bundle-path>");
    return 64;
}

Console.WriteLine($"Personal Recorder verifier (scaffold) — bundle: {args[0]}");
Console.WriteLine("Verification is not implemented yet; the evidence format lands first (ADR-007).");
return 1;
