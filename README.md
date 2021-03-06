# z80bench
Program to benchmark Z80 code, especially for Master System

[![Build status](https://ci.appveyor.com/api/projects/status/00jwpyp7n2n3qfeo?svg=true)](https://ci.appveyor.com/project/maxim-zhao/z80bench)

[Download binaries at AppVeyor](https://ci.appveyor.com/project/maxim-zhao/z80bench/build/artifacts)

# Usage

This program needs to run against the .Net Framework. It may work in Mono but it's untested.

Compile some code. End it with a dangling `ret` to signal when it's done. 

```
$ z80bench test-program.sms
Executed 136170 cycles in 00:00:00.0130497
```

Now you know how long it took (in CPU cycles; the second time is wall-clock time for the emulation). If you want to specify the load address for your program, append `@<address>` to the filename; it's implicitly at address 0 if unspecified.

What if you want to add some data?

```
$ z80bench test-program.sms@0000 data.dat@4000
Executed 136170 cycles in 00:00:00.0131959
```

The data is inserted at offset 0x4000. In fact, all filenames can be appended with an `@address` suffix#.

The specific use case for which this was written was testing and measuring depackers to VRAM for Sega 8-bit systems (which map a TMS9918a-like VDP to I/O ports $be and $bf). Pass `--vram-compare` to compare the VRAM contents:

```
$ z80bench test-program.sms --vram-compare expected.bin@0000
VRAM comparison: pass
Executed 3121010 cycles in 00:00:00.0951750
```

The default execution is from address 0 with the stack pointer set to $dff0. You can change these:

```
$ z80bench test-program.sms@1000 --execute 1000 --stack-pointer c100
Executed 314159 cycles in 00:00:00.02653561
```

execution will terminate if more than 1,000,000,000 cycles are emulated without the program ending. You can change this limit by passing e.g. `--max-cycles 1000000`.