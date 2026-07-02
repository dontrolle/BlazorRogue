using Xunit;

// This codebase uses static holders (see BlazorRogue.References) rather than DI for several
// cross-cutting services (Map, Configuration, SoundManager, EffectsSystem). Several tests mutate
// these statics as a side effect of constructing game objects (e.g. `new Game()`), so we disable
// test parallelization within this assembly to avoid cross-test interference.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
