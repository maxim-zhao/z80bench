using Konamiman.Z80dotNet;

namespace z80bench
{
    /// <summary>
    /// We need to implement this interface to us to count cycles spent in Z80dotNet.
    /// it also allows applying an upper limit on execution time.
    /// </summary>
    internal class CycleCountingClockSynchronizer : IClockSynchronizer
    {
        private readonly Z80Processor _emulator;
        private readonly long _maxCycles;

        public CycleCountingClockSynchronizer(Z80Processor emulator, long maxCycles)
        {
            _emulator = emulator;
            _maxCycles = maxCycles;
        }

        public void Start()
        {
            // Nothing to do
        }

        public void Stop()
        {
            // Nothing to do
        }

        public void TryWait(int periodLengthInCycles)
        {
            // We simply count the cycles.
            Cycles += periodLengthInCycles;
            
            // We stop the emulator if the limit is reached.
            if (Cycles > _maxCycles)
            {
                // Probably going on too long
                _emulator.Stop();
            }
        }

        /// <summary>
        /// The number of cycles that have been emulated
        /// </summary>
        public long Cycles { get; private set; }

        /// <summary>
        /// This is part of IClockSynchronizer. The actual frequency is irrelevant as we count cycles, not time. 
        /// </summary>
        public decimal EffectiveClockFrequencyInMHz { get; set; }
    }
}