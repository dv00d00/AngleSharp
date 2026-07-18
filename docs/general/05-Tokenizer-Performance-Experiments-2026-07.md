# Tokenizer performance experiments, July 2026

This note preserves the conclusions and useful measurements from the UTF-8 tokenizer worktree cleanup performed on 2026-07-18.

## Source revisions

- UTF-8 tokenizer consumer seam: `36aa1d0f4d258c017b67fbf9957f3e7e41108327`
- Mutable DOM parsing speedups: `deda15403030c7450d734cfec9cabd52e837bd6e` (AngleSharp PR #1266)
- Token-copy baseline: `a803a9f67ceb3a629bf2a019790b9a9c510f1472`
- Merged streaming source baseline: `3312d02af3a08215fe8b9da8e3f1540559d5a3ad`

The benchmark sources archived with this note came from stash `09fc14bf95b20a63b93ee9a92ee9f92230598495`, based on the consumer-seam revision.

The real-page inputs are intentionally not committed. The benchmark project expects them under the repository-root `temp` directory. The copies used for the archived build had these SHA-256 hashes:

| File | SHA-256 |
|---|---|
| `stackoverflow.html` | `A8CE1EC5B9934E48C16C5CFE5B6E05AB8149BE73977727F1176CDDEDAC0154A4` |
| `en.wikipedia.html` | `0BB2D8141AE9644B9DE5D19CC1C5916607460BBC908947FF1BC9B786B00771DC` |
| `youtube.html` | `024A1F6B1233276D4BA7841478F268ED41725597FA72ACCA9BDAFC12FF8FE814` |
| `spiegel.html` | `A9E185360B7C5BE4AA4BCB6293D2DB1845FA6A6BBD44C81924497B4C005AB5E5` |

## Settled conclusion

Synthetic ASCII slicing benchmarks made eager decode or direct ASCII string creation look competitive because they repeatedly exercised a small, tight, cache-local loop. The wired tokenizer behaved differently: control moved through a much larger state machine and DOM construction path, with substantially more branching and a larger instruction footprint.

AVX-sized non-ASCII prescanning and ASCII-vector maps did not improve the real tokenizer. The additional branches, state, dispatch, and code size consumed the theoretical saving from avoiding UTF-8 decoding. This is preserved as a negative result; the prescan should not be restored without new end-to-end evidence.

The useful follow-up is to measure mature mutable-DOM construction and native UTF-8 construction with representative pages, instruction-retired counters, and structural ablations. `Utf8MutableDomAblationBenchmark` and the expanded `Utf8MutableDomBenchmark` in this archive implement those probes.

## Preserved measurements

Environment for all measurements: Intel Core i9-9900K, Windows 11, BenchmarkDotNet 0.15.8.

### Streaming text source baseline (.NET 8, short run)

| Method | Mean | Allocated |
|---|---:|---:|
| AccumulatingSource | 5.847 ms | 2.11 MB |
| BoundedSource | 5.120 ms | 1.36 MB |
| AutomaticBoundedSource | 4.570 ms | 1.36 MB |

The bounded designs reduced allocation to 64% of the accumulating source. The timing run was short and noisy, so allocation is the stronger result.

### Native UTF-8 mutable DOM token-copy baseline (.NET 10, medium run)

| Method | Mean | StdDev | Allocated |
|---|---:|---:|---:|
| NativeUtf8Network4K | 3.029 ms | 0.121 ms | 1.34 MB |

### Experimental asynchronous token-source API (.NET 10, short runs)

| Variant | Mean | Allocated |
|---|---:|---:|
| Baseline API | 56.36 ms | 35.53 MB |
| Minimal API | 53.95 ms | 35.72 MB |

The errors were large and allocation was unchanged. This did not demonstrate a meaningful gain, and the adapter project was later removed from ReadOnlyDom.

## Benchmark interpretation rules

- Benchmark the benchmark project directly in `Release`; the full `.slnx` orchestration was observed selecting a Debug AngleSharp project-reference output.
- Segment results by ASCII coverage and character distribution.
- Treat microbenchmarks as mechanism probes, not tokenizer throughput evidence.
- Require an end-to-end wired tokenizer or query benchmark before adopting a fast path.
- Center decisions on mean time and allocated bytes; use instruction-retired and hardware counters to explain gaps.
