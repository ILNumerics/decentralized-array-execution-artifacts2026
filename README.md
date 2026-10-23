# ILNumerics Accelerator (.NET) - Benchmarks and Artifacts 2026
This repository collects performance benchmarks for the [ILNumerics Accelerator JIT compiler](https://ilnumerics.net/ilnumerics-accelerator-compiler.html). This compiler is the driving force behind high-performance numerical array code optimizations for the [ILNumerics](https://ilnumerics.net) numerical application development platform on .NET.
The [initial state of the repository](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/paper2026) (May 2026) represents artifacts for the 2026 paper ["A Virtual Processor brings back the Free Lunch"](http://arxiv.org/abs/2605.30507) published by [ILNumerics GmbH](https://ilnumerics.net), using ILNumerics version 7.4. It is subsequently extended with more and updated benchmarks as ILNumerics Accelerator evolves. 

Since [Part 4](Appendix/Part%204%20FFT), the comparisons include Python: [viproc](https://github.com/hokb/viproc) brings the execution model of ILNumerics Accelerator (array instruction level parallelism) to regular NumPy programs.

Each benchmark can be downloaded and reproduced locally. Docker images / build scripts are provided where suitable. 

## Content
<table style="border:solid black 0px">
  <tr>
    <td width="55%" valign="top">

### [Part 1 - Low Level Expressions](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%201%20Low%20Level%20Expressions)
Performance comparison of executing the array expression `sum((m0 & (A << shift)) | (~m0 & B), dim: 1)` with moderate array sizes on NumPy, FORTRAN and [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html). 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%201%20Low%20Level%20Expressions/Part1.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 2a - `Parallel.For` vers. ILNumerics Accelerator](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%202a%20Loop%20Parallelization)
Compares the performance of automatic parallelization by [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) with the manual `Parallel.For` attempt on the array expression `sum(abs(sin(A)))`. 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%202a%20Loop%20Parallelization/ILNumerics/Part2a_1500x1500_rep10.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 2b - ILNumerics Accelerator vers. *Invalid* `Parallel.For`](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%202b%20Loop%20Parallelization)
Demonstrates strong-scaling capabilities by [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) where `Parallel.For` fails: an order dependent loop is parallelized. 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%202b%20Loop%20Parallelization/ILNumerics/Part2b_1500x1500.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 3 - Strong Scaling the K-Means Algorithm`](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%203%20KMeans)
[ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) performs strong scaling on the iterative K-Means algorithm and beats FORTRAN by factor 3.  
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%203%20KMeans/Part3.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 4 - Faster FFT in Python: NumPy, scipy.fft and viproc](Appendix/Part%204%20FFT)
The [Faster FFT](https://ilnumerics.net/Faster-Fast-Fourier-Transform.html) program `abs(ifft(fft(A)))` in Python: plain NumPy vs. `scipy.fft` (parallel inside each FFT, `workers=-1`) vs. [viproc](https://github.com/hokb/viproc), which brings the AILP execution model of ILNumerics Accelerator to unchanged NumPy programs. viproc: 2.2x-6.4x over NumPy on 4 cores, bit-identical to NumPy; `scipy.fft`: 0.5x-2.6x.
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="Appendix/Part%204%20FFT/Part4.svg" alt="FFT performance of NumPy, scipy.fft and viproc" width="100%">
</td>
  </tr>
</table>



