from numpy import *
import time as tm
import sys
import os 

def kmeans(X,k):
    n = size(X,1)
    maxit = 20
    it = 0
    centers = X[:,linspace(0, size(X,1) - 2, k, dtype=int64)].copy()
    classes = zeros((1,n))
    oldclasses = 0
    while it < maxit:
        oldclasses = classes.copy()

        for i in range(n):
            dist = sqrt(sum(square(X[:,i,newaxis] - centers), axis=0))
            classes[0,i] = dist.argmin()
             
        for i in range(k):
            found = nonzero(classes == i)[1]; 
            inClass = X[:,found]
            if inClass.size > 0:
                centers[:,i] = inClass.mean(axis=1)
 
        it = it + 1
        if all(oldclasses == classes):
            break
    return (classes, it)

def measure_numpy():
    if len(sys.argv) < 5:
        raise ValueError("Invalid arguments. Required: working_dir K measID rep maxsec append.csv [warmupsec]")

    array = loadtxt(os.path.join(sys.argv[1], 'input.csv'), delimiter=',', dtype=float64)
    k = int(sys.argv[2])
    measID = int(sys.argv[3])
    rep = int(sys.argv[4])
    maxsec = int(sys.argv[5])
    warmup = float(sys.argv[7]) if len(sys.argv) > 7 else 0.0
    # warm-up: not recorded; the elapsed time of the samples starts after it
    start = tm.perf_counter()
    while tm.perf_counter() - start < warmup:
        kmeans(array, k)
    with open(os.path.join(sys.argv[1], sys.argv[6]), "a") as outfile:
        wall = tm.perf_counter()
        while tm.perf_counter() - wall < maxsec: 
            start = tm.perf_counter()
            for r in range(rep):
                (classes, iterations) = kmeans(array, k)
            now = tm.perf_counter()
            dur = (now - start) * 1000 / rep  # ms
            outfile.write("%i, %f, %f\r\n" % (measID, (now - wall) * 1000 , dur))   # all ms
            print("Part 3. ID: %i, %f, %f ms, iterations: %i" % (measID, (now - wall) * 1000, dur, iterations))

    savetxt(os.path.join(sys.argv[1], 'classes.csv'), classes, delimiter=',')
    return iterations

it = measure_numpy()
sys.exit(it)
