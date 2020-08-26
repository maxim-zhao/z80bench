using Konamiman.Z80dotNet;

namespace z80bench
{
    internal class CycleCountingClockSynchronizer : IClockSynchronizer
    {
        private readonly Z80Processor _emulator;
        private readonly int _maxCycles;

        public CycleCountingClockSynchronizer(Z80Processor emulator, int maxCycles)
        {
            _emulator = emulator;
            _maxCycles = maxCycles;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void TryWait(int periodLengthInCycles)
        {
            Cycles += periodLengthInCycles;
            if (Cycles > _maxCycles)
            {
                // Probably going on too long
                _emulator.Stop();
            }
        }

        public long Cycles { get; private set; }

        public decimal EffectiveClockFrequencyInMHz { get; set; }
    }
}