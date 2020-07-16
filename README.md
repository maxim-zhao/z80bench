# z80bench
Program to benchmark Z80 code, especially for Master System

# Usage

Compile some code to run at memory address 0. End it with a dangling `ret` to signal when it's done. 

```
$ z80bench test-program.sms
Executed 136170 cycles in 00:00:00.0130497
```

Now you know how long it took. WHat if I want to add some data?

```
$ z80bench test-program.sms data.dat
Executed 136170 cycles in 00:00:00.0131959
```

The data is inserted at offset 0x4000.

The specific use case for which this was written was testing and measuring depackers to VRAM. Pass a third parameter with the expected VRAM memory contents (from address 0):

```
$ z80bench test-program.sms data.dat expected.bin
VRAM comparison: pass
Compression level is 1:2.43 = 58.9% compression
Data rate is 161.08 bytes per frame at NTSC timings
Executed 3121010 cycles in 00:00:00.0951750
```

And you get stats on the compression level and decompression rate.
