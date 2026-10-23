"""Part 4 - Faster FFT in Python: plain NumPy, scipy.fft and viproc.

The program is the one of the ILNumerics article "Faster Fast Fourier
Transform" (https://ilnumerics.net/Faster-Fast-Fourier-Transform.html),
written as plain NumPy:

    for _ in range(reps):
        B = abs(ifft(fft(A)))          # A: n x n float64, one FFT per row

It runs the same program four ways:

* numpy        plain NumPy (numpy.fft, pocketfft, single-threaded)
* scipy w=1    scipy.fft with one worker
* scipy w=all  scipy.fft with workers=-1 (splits the rows of one call
               over all CPUs: parallelism *inside* each FFT call)
* viproc       the unchanged NumPy program on viproc arrays: every FFT call
               runs as one task, independent calls of different iterations
               overlap (parallelism *between* the calls, array ILP)

and writes the time per expression, the speedup over NumPy and whether the
result is bit-identical to NumPy's into result/values.csv, plus a plot
(result/Part4.svg, result/Part4.png).

Usage: python fft_benchmark.py [--sizes 100 200 ...] [--out result] [--repeat 5]
"""

import argparse
import csv
import os
import platform
import time

import numpy as np
import scipy
import scipy.fft as sfft
import viproc


def program(xp_fft, A, reps):
    """The benchmark program; xp_fft provides fft and ifft."""
    B = None
    for _ in range(reps):
        B = np.abs(xp_fft.ifft(xp_fft.fft(A)))
    return B


class ScipyFFT:
    def __init__(self, workers):
        self.workers = workers

    def fft(self, a):
        return sfft.fft(a, workers=self.workers)

    def ifft(self, a):
        return sfft.ifft(a, workers=self.workers)


def best_time(run, repeat):
    """Shortest wall time of `repeat` runs; each run ends with the result as a NumPy array."""
    best = float("inf")
    for _ in range(repeat):
        t0 = time.perf_counter()
        np.asarray(run())  # a sync point for viproc: waits for the last result
        best = min(best, time.perf_counter() - t0)
    return best


def input_matrix(n):
    # ILNumerics: A = arange<double>(0, n-1); A = A * flip(A).T  (outer product)
    a = np.arange(0.0, n, dtype=np.float64)
    return a[:, None] * a[::-1][None, :]


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sizes", type=int, nargs="+", default=[100, 200, 500, 1000, 2000])
    p.add_argument("--out", default="result")
    p.add_argument("--repeat", type=int, default=5, help="runs per configuration, the best one counts")
    args = p.parse_args()
    os.makedirs(args.out, exist_ok=True)

    methods = [
        ("numpy", lambda A, reps: program(np.fft, A, reps)),
        ("scipy w=1", lambda A, reps: program(ScipyFFT(1), A, reps)),
        ("scipy w=all", lambda A, reps: program(ScipyFFT(-1), A, reps)),
        ("viproc", lambda A, reps: program(np.fft, viproc.asarray(A), reps)),
    ]

    print(f"Python {platform.python_version()}, NumPy {np.__version__}, SciPy {scipy.__version__}, "
          f"viproc {getattr(viproc, '__version__', '?')}, {os.cpu_count()} logical CPUs")
    rows = []
    for n in args.sizes:
        A = input_matrix(n)
        reps = 100 if n <= 1000 else 20
        reference = program(np.fft, A, 1)
        times = {}
        for name, run in methods:
            run(A, 2)  # warm-up (plans, caches, threads)
            times[name] = best_time(lambda: run(A, reps), args.repeat) / reps
            identical = bool(np.array_equal(np.asarray(run(A, 1)), reference))
            rows.append({"n": n, "method": name, "ms_per_expr": times[name] * 1e3,
                         "speedup_vs_numpy": times["numpy"] / times[name], "identical_to_numpy": identical})
        print(f"n={n:5d}  ms/expr: " + "  ".join(f"{k} {v * 1e3:8.3f}" for k, v in times.items())
              + "  | speedup vs numpy: " + "  ".join(f"{k} {times['numpy'] / v:5.2f}x" for k, v in times.items()
                                                     if k != "numpy"), flush=True)

    with open(os.path.join(args.out, "values.csv"), "w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0]))
        w.writeheader()
        w.writerows(rows)
    plot(rows, args.out)
    print(f"results written to {args.out}/")


def plot(rows, out):
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("matplotlib not installed: no plot")
        return
    sizes = sorted({r["n"] for r in rows})
    names = [m for m in dict.fromkeys(r["method"] for r in rows) if m != "numpy"]
    colors = {"scipy w=1": "#9aa5b1", "scipy w=all": "#4a7ab5", "viproc": "#d9822b"}
    fig, ax = plt.subplots(figsize=(8, 4.5))
    width = 0.8 / len(names)
    for i, name in enumerate(names):
        ys = [next(r["speedup_vs_numpy"] for r in rows if r["n"] == n and r["method"] == name) for n in sizes]
        xs = [j + (i - (len(names) - 1) / 2) * width for j in range(len(sizes))]
        ax.bar(xs, ys, width, label=name, color=colors.get(name))
    ax.axhline(1.0, color="black", linewidth=0.8)
    ax.set_xticks(range(len(sizes)), [f"{n} x {n}" for n in sizes])
    ax.set_xlabel("A: n x n float64, program: B = abs(ifft(fft(A))) in a loop")
    ax.set_ylabel("speedup vs. plain NumPy")
    ax.set_title(f"FFT in Python on {os.cpu_count()} CPUs: scipy.fft workers vs. viproc")
    ax.legend(frameon=False)
    fig.tight_layout()
    fig.savefig(os.path.join(out, "Part4.svg"))
    fig.savefig(os.path.join(out, "Part4.png"), dpi=120)


if __name__ == "__main__":
    main()
