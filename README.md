[![Build Status](https://travis-ci.org/Fundamentally-Uncentralizable-Cookies/Epicoin.svg)](https://travis-ci.org/Fundamentally-Uncentralizable-Cookies/Epicoin) [![Latest](http://github-release-version.herokuapp.com/github/Fundamentally-Uncentralizable-Cookies/Epicoin/release.svg?style=flat)](https://github.com/Fundamentally-Uncentralizable-Cookies/Epicoin/releases/latest)

[![Logo](LOGO.png)](https://epitacoin.ml)
# [Epicoin](https://epitacoin.ml)
Epita S2 Project. Epicoin is a blockchain where the proof of work is done by the solving of NP-Complete problems.
### Structure
Epicore is the core library of Epicoin, providing all components, their API, as well as methods for initialization, execution and termination. All in all, epicore is an active, asynchronous, self-managing library - it is a library with start/stop lifecycle, that runs asynchronously (from caller) on parallel thread(s), and self-manages (between lifecycle calls). Being a library, it has no entry point and caller must use its' lifecycle calls. It is located in `/Core`.
`/Tests` contains various unit tests for all epicore components and utils.
Default baseline command line interface is provided in `/CLI`.

---
**[More information is available on the official website, epitacoin.ml](https://epitacoin.ml)**