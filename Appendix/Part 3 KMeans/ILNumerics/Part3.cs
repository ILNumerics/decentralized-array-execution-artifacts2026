using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using ILNumerics.SC25.Appendix;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using static ILNumerics.Globals;
using static ILNumerics.ILMath;

/** 
 * This experiment compares the execution speed of three technologies running equivalent code (in terms of intention: kmeans).
 * It performs and manages all measurements and plotting from within a single file (this file). 
 * The experiment first runs all measurements, one after another. Therefore, it expects that 
 * all dependencies and related software is installed on the execution computer. Consult the file 
 * 'SystemSetup.txt' for instructions on prerequisites. 
 * 
 **/
//ILN(enabled=false)

var exeDir = Path.GetDirectoryName(typeof(Part3_noopt).Assembly.Location);
string OutputFolder = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "result"));  // expected standard output file structure: ILNumerics/bin/Release/net10.0/Part3.dll
const int MeasureTimeSpanSeconds = 20;
const int WarmupSeconds = 2;  // each technology runs this long before its measurement starts (JIT, caches, pools)
const int MeasureRepetitions = 3;
const string MainMeasurementTimesFileName = "values.csv";  // format: MeasurementID (int), SampleReadTime (double), SampleValue (double)
var outValuesPathNq = r2aPQ(Path.Combine(OutputFolder, MainMeasurementTimesFileName), quote: false);
const int dataMatLen = 401 * 3; 

[DllImport("Part3_KMeans", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SKMEANS")]
static unsafe extern void SKMEANS(double* X, ref long M, ref long N, ref int IT, ref int K, double* classes); 

// run measurement, display + save times & check result
System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");  // required to help csv formats on various platforms 


if (!tryGetMeasurementResults(outValuesPathNq, out Array<double> times)) {
    performMeasurements(outValuesPathNq);
    if (!tryGetMeasurementResults(outValuesPathNq, out times)) {
        Console.WriteLine($"Unexpected error performing / reading measurement. Giving up.");
        Environment.Exit(1);
    }
}
draw(); 

#region helper / function definitions
RetArray<double> createData() {

    // Just some arbitrary data: [1203 x 1203]. We scale realistic terrain data with a sine function
    // to ensure input values, leading to a certain number of iterations reliably. Otherwise, we could 
    // not safely compare an iterative algorithm like kmeans, since intermediate values might differ
    // due to optimization. This could lead to different number of kmeans iterations until convergence
    // which would make the performance comparison much less useful. 
    return todouble(repmat(SpecialData.terrain, 3, 3)) * sin(arange<double>(1, dataMatLen) * 0.1);
}

unsafe bool tryGetMeasurementResults(string filename, out Array<double> result) {
    try {

        var valuesStr = File.ReadAllText(filename);
        result = csvread<double>(valuesStr);
        Console.WriteLine($"Data read from: " + filename + ". Size: " + result.S.ToString()); 
        return true;

    } catch {
        result = null;
        return false;
    }
}

unsafe void performMeasurements(string filename) {
    
    // no data file or no/invalid content -> measure!
    if (File.Exists(filename)) File.Delete(filename);
    Directory.CreateDirectory(OutputFolder);
    using (File.Create(filename)) { }

    ensureFORTRANLibsInWorkingDir();

    // Python for the NumPy and the viproc measurement: the same interpreter (and NumPy) for both.
    // viproc is installed from GitHub if it is missing (see ensureViproc).
    var pythonExeName = ensureViproc(Environment.GetEnvironmentVariable("PART3_PYTHON")
        ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "/opt/venv/bin/python3"));
    
    Array<double> A = createData();

    // measure ILNumerics, no-Accelerator + with Accelerator
    int iterations = -1, checkIterations = -1;
    Array<long> classes = -1, checkClasses = -1;

    // measure ILNumerics: no-opt + optimized (Accelerator). Note, both code variants are authored in individual files.
    // They are identical, except for: one disables the ILNumerics Accelerator. The other doesn't ('Part3_optimized'). 
    measureAction(filename, () => Part3_noopt.kmeans(A, k: 50, classes, out iterations), 0.0, ref iterations, rep: MeasureRepetitions);
    measureAction(filename, () => Part3_optimized.kmeans(A, k: 50, checkClasses, out checkIterations), 1.0, ref checkIterations, rep: MeasureRepetitions);

    // check results
    if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: ILN opt yields invalid values.");

    // measure numpy no-opt
    var sourcefile = r2aPQ(Path.Combine(exeDir, "..", "..", "..", "..", "numpy", "Part3_noopt", "Part3_noopt.py"), true);  // scriptfile
    var cmdargs = r2aPQ(OutputFolder, true);                         // working dir (for reading inputs & writing classes values)
    cmdargs += $"50 2 ";                                              // k  measurementID
    cmdargs += $"{MeasureRepetitions} {MeasureTimeSpanSeconds} ";  // rep maxsec
    cmdargs += r2aPQ(filename, true);                                 // filename to write measured times (out/ append)
    cmdargs += $"{WarmupSeconds} ";                                   // warm-up seconds

    checkClasses.a = -1; checkIterations = -1;
    measureCmd(2.0, A, pythonExeName, sourcefile + " " + cmdargs, checkClasses, out checkIterations);
    if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: numpy noopt yields invalid values.");

    // measure viproc: the same, unchanged NumPy script, run by 'python -m viproc' (https://github.com/hokb/viproc).
    // viproc executes the NumPy program's array instructions in parallel where their data dependencies allow it.
    var viprocArgs = r2aPQ(OutputFolder, true) + $"50 4 {MeasureRepetitions} {MeasureTimeSpanSeconds} " + r2aPQ(filename, true) + $"{WarmupSeconds} ";
    checkClasses.a = -1; checkIterations = -1;
    measureCmd(4.0, A, pythonExeName, "-m viproc " + sourcefile + " " + viprocArgs, checkClasses, out checkIterations);
    if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: viproc yields invalid values.");

    //// measure numpy opt (numba) --DISABLED, because too many issues for compiling with NUMBA
    //sourcefile = r2aPQ(@"..\..\..\..\numpy\Part3_opt\Part3_opt.py", true);  // scriptfile
    //checkClasses.a = -1; checkIterations = -1;
    //measureCmd(2.0, A, "python", sourcefile + " " + cmdargs, checkClasses, out checkIterations, rep: MeasureRepetitionsAvg);
    //if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: numpy opt yields invalid values.");

    long m = A.S[0], n = A.S[1];
    int k = 50;
    checkIterations = -1;
    var fort_action = () => SKMEANS(
                            (double*)A.GetHostPointerForRead(order: StorageOrders.ColumnMajor),
                            ref m, ref n, ref checkIterations, ref k,
                            (double*)checkClasses.GetHostPointerForWrite(order: StorageOrders.ColumnMajor));
    measureAction(filename, fort_action, 3.0, ref checkIterations, rep: MeasureRepetitions);
    if ((bool)anyall(classes != checkClasses - 1)) throw new ArgumentException($"Measurement error: FORTRAN opt yields invalid classes: {checkClasses}.");
    if (iterations != checkIterations) throw new ArgumentException($"Measurement error: FORTRAN opt yields invalid iteration count: {checkIterations}.");
}

// makes sure that all pre-compiled FORTRAN libraries / assoc. files are available in the apps current dir
void ensureFORTRANLibsInWorkingDir() {
#if DEBUG
    var configuration = "Debug"; 
#else 
    var configuration = "Release";
#endif
    var binPath = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "FORTRAN","x64", configuration));
    if (!Directory.Exists(binPath)) {
        Console.WriteLine($"Missing FORTRAN binaries. Make sure to compile the FORTRAN sources for the current architecture / configuration ({configuration})!");
        Environment.FailFast("Missing Fortran binaries");
    }
    var files = Directory.GetFiles(binPath, "Part3_KMeans.*"); 
    foreach (var file in files) {
        var dest = Path.GetFileName(file);
        if (!File.Exists(dest) || File.GetLastWriteTime(dest) < File.GetLastWriteTime(file)) {
            File.Copy(file, dest, true);
            Console.WriteLine($"FORTRAN file copied: {file}"); 
        }
    }
}

// Runs a command, its output goes to the console. Returns its exit code, or -1 if it could not be started.
int runProcess(string cmd, string args) {
    try {
        using var proc = Process.Start(new ProcessStartInfo(cmd, args) { UseShellExecute = false });
        proc.WaitForExit();
        return proc.ExitCode;
    } catch (Exception exc) {
        Console.WriteLine($"Could not start '{cmd}': {exc.Message}");
        return -1;
    }
}

bool pythonModuleAvailable(string python, string module) {
    return runProcess(python, $"-c \"import {module}\"") == 0;
}

// Returns a Python executable that can import viproc. If 'python' cannot, viproc is installed from GitHub:
// first into 'python' itself (pip is bootstrapped with ensurepip if missing); if that is not possible
// (externally managed system Python, no write access, ...), into a dedicated virtual environment
// next to the results, created from 'python'. viproc is built from source (C/C++ extension): this needs
// a C/C++ compiler and the Python headers; CMake and Ninja come from PyPI. VIPROC_PIP_SPEC overrides
// what is installed (e.g. a local checkout or a fixed commit). Ends the program with a message if
// all attempts fail: a plot without viproc would be incomplete.
string ensureViproc(string python) {
    if (pythonModuleAvailable(python, "viproc")) return python;

    var spec = Environment.GetEnvironmentVariable("VIPROC_PIP_SPEC")
        ?? "viproc @ https://github.com/hokb/viproc/archive/refs/heads/main.zip";  // no git needed
    Console.WriteLine($"viproc not found for '{python}'. Installing: {spec}");

    if (runProcess(python, "-c \"import sys; sys.exit(sys.version_info < (3, 12))\"") != 0) {
        Console.WriteLine($"viproc requires Python >= 3.12; '{python}' is older or could not be started. Set PART3_PYTHON to a Python >= 3.12.");
        Environment.Exit(2);
    }

    // 1) into the given Python
    if (!pythonModuleAvailable(python, "pip")) runProcess(python, "-m ensurepip --upgrade");
    if (runProcess(python, $"-m pip install \"{spec}\"") == 0 && pythonModuleAvailable(python, "viproc")) return python;

    // 2) into a dedicated virtual environment (its NumPy is then used for the NumPy measurement as well)
    var venvDir = Path.GetFullPath(Path.Combine(OutputFolder, "..", ".viproc-venv"));
    var venvPython = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Path.Combine(venvDir, "Scripts", "python.exe") : Path.Combine(venvDir, "bin", "python3");
    Console.WriteLine($"Installing viproc into '{python}' failed. Trying a virtual environment: {venvDir}");
    if ((File.Exists(venvPython) || runProcess(python, $"-m venv \"{venvDir}\"") == 0)
        && (pythonModuleAvailable(venvPython, "pip") || runProcess(venvPython, "-m ensurepip --upgrade") == 0)
        && (runProcess(venvPython, "-m pip install --upgrade pip") is var _)   // optional: an old pip may still work
        && runProcess(venvPython, $"-m pip install \"{spec}\"") == 0
        && pythonModuleAvailable(venvPython, "viproc")) {
        return venvPython;
    }

    Console.WriteLine("Could not install viproc. It is built from source and needs: Python >= 3.12 with its headers "
        + "(Debian/Ubuntu: python3-dev, python3-venv), a C/C++ compiler (build-essential / Visual Studio Build Tools) "
        + "and internet access to GitHub and PyPI. Install it manually ('pip install \"" + spec + "\"') "
        + "or set PART3_PYTHON to a Python that has it.");
    Environment.Exit(2);
    return null;
}

string r2aPQ(string rel, bool quote = false) {
    var ret = Path.GetFullPath(rel);
    if (quote) return $"\"{ret}\" ";
    return ret;  
}

void measureCmd(double measureID, InArray<double> X, string cmd, string cmd_args, OutArray<long> classes, out int iterations) {

    using var _ = Scope.Enter();
    
    // ensure input data
    csvwrite(X, Path.Combine(OutputFolder, "input.csv"));
    iterations = -1; 

    var proc = Process.Start(cmd, cmd_args);
    proc.WaitForExit(); 
    iterations = proc.ExitCode; 

    classes.a = csvread<long>(File.ReadAllText(Path.Combine(OutputFolder, "classes.csv")));

}

void measureAction(string filename, Action action, double measureID, ref int iterations, int rep = 5) {
    var sw = new Stopwatch();

    // warm-up: not recorded; the elapsed time of the samples starts after it
    for (var warmup = DateTime.Now; DateTime.Now < warmup + TimeSpan.FromSeconds(WarmupSeconds);) {
        action();
    }
    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(MeasureTimeSpanSeconds);) {
        sw.Restart();
        for (int i = 0; i < rep; i++) {
            action();
        }
        sw.Stop();
        var now = (DateTime.Now - start).TotalMilliseconds; 
        Console.WriteLine($"Part3. ID: {measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}  iterations: {iterations}");
        File.AppendAllText(filename, $"{measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}\r\n");
    }
}

static string getSystemInfo() {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        return @$"{RuntimeInformation.OSDescription}: {Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.Processors[0].Name}";
    } else {
        return @$"{RuntimeInformation.OSDescription}: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1 << 30)} GB RAM, {Environment.ProcessorCount}  cores
{GetLinuxProcessorName()}";
    }
}
static string GetLinuxProcessorName() {
    return File.ReadLines("/proc/cpuinfo")
        .FirstOrDefault(l => l.StartsWith("model name", StringComparison.OrdinalIgnoreCase))?
        .Split(':', 2)[1]
        .Trim()
        ?? "Unknown CPU";
}

// the samples of one measurement for a line plot: [elapsed time in s; exec time in ms]
RetArray<float> series(double measureID) {
    using var _ = Scope.Enter();
    Array<float> s = tosingle(times[times[full, 0] == measureID, "1,2"]);
    s[full, 0] = s[full, 0] / 1000f;   // ms -> s
    return s.T;
}

void draw() {
    Console.WriteLine("Generating plots."); 
    // increase the font size for plotting
    var allLabelFonts = new ILNumerics.Drawing.Compat.Font("Linux Libertine", 13.0f);
    Label.DefaultFont = allLabelFonts;
    var ILNVersion = FileVersionInfo.GetVersionInfo(typeof(Array<>).Assembly.Location);
    var ILNVersStr = $"{ILNVersion.FileMajorPart}.{ILNVersion.FileMinorPart}.{ILNVersion.FileBuildPart}";

    var scene = new Scene {
        new PlotCube(twoDMode: true) {
            new LinePlot(series(0), tag: $"ILNumerics v{ILNVersStr}, no  Accelerator ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Black, lineStyle: DashStyle.Dashed),
            new LinePlot(series(2), tag: "numpy ", lineWidth: 3, markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Green, lineStyle: DashStyle.Solid),
            new LinePlot(series(3), tag: "FORTRAN /O3 optimization ", lineWidth: 3, markerStyle: MarkerStyle.Square, lineColor: Color.Blue, lineStyle: DashStyle.Solid),
            new LinePlot(series(1), tag: $"ILNumerics Accelerator v{ILNVersStr} ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Red, lineStyle: DashStyle.Solid),
            new Legend() {
                Location = new PointF(.9f, .88f), Anchor = new PointF(1, 1)   // bottom right, below the data
            }
        }
    };
    var pc = scene.First<PlotCube>();
    if ((bool)any(times[full, 0] == 4)) {   // viproc line (absent in measurements made before it was added)
        pc.Add(new LinePlot(series(4), tag: "numpy + viproc ", lineWidth: 3, markerStyle: MarkerStyle.Diamond, lineColor: Color.DarkOrange, lineStyle: DashStyle.Solid));
    }

    // set all fonts
    pc.Axes.XAxis.Label.Font = allLabelFonts;
    pc.Axes.XAxis.Ticks.DefaultLabel.Font = allLabelFonts;
    pc.Axes.YAxis.Label.Font = allLabelFonts;
    pc.Axes.YAxis.Ticks.DefaultLabel.Font = allLabelFonts;

    // configure X axis
    pc.Axes.XAxis.Label.Text = "Elapsed time [s]";   // automatic ticks, label at its default position

    // configure Y axis
    pc.ScaleModes.YAxisScale = AxisScale.Logarithmic;
    pc.Axes.YAxis.Label.Text = "Exec time [ms]";
    // label at its default position, clear of the tick labels

    // increase markers size
    foreach (var lineplot in pc.Find<LinePlot>()) {
        lineplot.Marker.Size = 13;  
    }

    var title = pc.Add(new Title(
    @$"\bfExecution speed: KMeans clustering\reset
    Data:  [{dataMatLen},{dataMatLen}] double
    \fontsize{{-3}}System: {getSystemInfo()}"));
    title.Location = new PointF(.13f, .88f);   // bottom left, below the data
    title.Anchor = new PointF(0, 1);
    // y limits (log10 units): extend down to the next decade below the data, so that the logarithmic axis
    // has a labeled tick at its lower end, not only one label in the middle; the upper end fits the data
    var yLogMin = (float)Math.Floor(Math.Log10((double)minall(times[full, 2])));
    pc.Limits.Update(new Vector3(0, yLogMin, 0), 2);

    var pbILabel = scene.First<Label>(predicate: l => l.Text.Length > 20);
    pbILabel.Position = new Vector3(0.9, 0.88, 0);

    scene.Configure();
    var gdi = new GDIDriver(1500, 1000);
    gdi.Scene = scene;
    gdi.Render();
    gdi.BackBuffer.SaveBitmap(Path.Combine(OutputFolder, $"Part3.bmp"));

    using var outStream = new FileStream(Path.Combine(OutputFolder, $"Part3.svg"), FileMode.Create);
    new SVGDriver(outStream, 1500, 1000, scene: scene).Render();

    Console.WriteLine("Generating plots completed. Results are found in: " + OutputFolder);

}

#endregion