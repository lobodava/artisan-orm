using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class NegativeIdentityRegressionTest
{
    [TestMethod]
    public void Int16UsesEveryNegativeValueOnceThenRemainsExhausted()
    {
        using var counter = new IsolatedCounter<short>(typeof(Int16NegativeIdentity));
        for (var expected = (int)short.MinValue; expected < 0; expected++)
            Assert.AreEqual((short)expected, counter.Next());

        for (var attempt = 0; attempt < 10; attempt++)
            Assert.ThrowsExactly<InvalidOperationException>(() => counter.Next());
    }

    [TestMethod]
    public void Int16ConcurrentExhaustionNeverDuplicatesOrReturnsNonnegativeValues()
    {
        using var counter = new IsolatedCounter<short>(typeof(Int16NegativeIdentity));
        var values = new ConcurrentBag<short>();
        var exhausted = 0;
        Parallel.For(0, 40000, _ =>
        {
            try { values.Add(counter.Next()); }
            catch (InvalidOperationException) { Interlocked.Increment(ref exhausted); }
        });

        Assert.AreEqual(32768, values.Count);
        Assert.AreEqual(32768, values.Distinct().Count());
        Assert.IsTrue(values.All(value => value < 0));
        Assert.AreEqual(40000 - 32768, exhausted);
    }

    [TestMethod]
    public void Int32KeepsItsInitialSequenceAndStopsAtMinusOne()
    {
        using var counter = new IsolatedCounter<int>(typeof(Int32NegativeIdentity));
        Assert.AreEqual(int.MinValue, counter.Next());
        Assert.AreEqual(int.MinValue + 1, counter.Next());
        counter.SetBoundary(-2);
        Assert.AreEqual(-2, counter.Next());
        Assert.AreEqual(-1, counter.Next());
        Assert.ThrowsExactly<InvalidOperationException>(() => counter.Next());
        Assert.ThrowsExactly<InvalidOperationException>(() => counter.Next());
    }

    [TestMethod]
    public void Int64KeepsItsInitialSequenceAndStopsAtMinusOne()
    {
        using var counter = new IsolatedCounter<long>(typeof(Int64NegativeIdentity));
        Assert.AreEqual(long.MinValue, counter.Next());
        Assert.AreEqual(long.MinValue + 1, counter.Next());
        counter.SetBoundary(-2L);
        Assert.AreEqual(-2L, counter.Next());
        Assert.AreEqual(-1L, counter.Next());
        Assert.ThrowsExactly<InvalidOperationException>(() => counter.Next());
        Assert.ThrowsExactly<InvalidOperationException>(() => counter.Next());
    }

    // A separate assembly instance keeps exhaustion tests from changing the counters
    // used by repository/model tests in the same process. Large counters are seeded
    // at the boundary because allocating their complete ranges is not practical.
    private sealed class IsolatedCounter<T> : IDisposable
    {
        private readonly AssemblyLoadContext _context = new(null, isCollectible: true);
        private readonly Type _type;
        public Func<T> Next { get; }

        public IsolatedCounter(Type counterType)
        {
            var assembly = _context.LoadFromAssemblyPath(counterType.Assembly.Location);
            _type = assembly.GetType(counterType.FullName, throwOnError: true);
            Next = (Func<T>)_type.GetProperty("Next").GetMethod.CreateDelegate(typeof(Func<T>));
        }

        public void SetBoundary(T value) =>
            _type.GetField("_identity", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);

        public void Dispose() => _context.Unload();
    }
}
