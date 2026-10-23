# Plain NumPy, scipy.fft and viproc on Debian (CPython 3.12).
FROM python:3.12-bookworm

# viproc is built from source (C/C++; pip fetches CMake and Ninja):
# https://github.com/hokb/viproc
# VIPROC_REF: a branch, tag or commit of viproc. Asynchronous numpy.fft needs
# viproc PR #36 (https://github.com/hokb/viproc/pull/36); this is its commit.
ARG VIPROC_REF=75715d2747f8d5f05e5a2a475ae2ed2cc79b22b9

RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir "numpy>=2.5" scipy matplotlib && \
    pip install --no-cache-dir "viproc @ git+https://github.com/hokb/viproc@${VIPROC_REF}"

WORKDIR /Artefact4
COPY fft_benchmark.py ./

ENTRYPOINT ["python", "fft_benchmark.py", "--out", "/Artefact4/result"]
