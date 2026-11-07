using System.Collections;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class MergeJoinDisposalRegressionTest
{
    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void EveryOverloadDisposesEnumeratorsAfterJoining(int overload)
    {
        var inputs = new Inputs();
        var joined = new List<string>();
        Join(overload, inputs, (master, detail) => joined.Add($"{master.Id}:{detail.Id}"));

        CollectionAssert.AreEqual(new[] { "1:101", "2:202" }, joined);
        AssertDisposed(inputs, overload, master: true);
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void EmptyMastersStillDisposePrefetchedDetails(int overload)
    {
        var inputs = new Inputs { Masters = new TrackedEnumerable<Master>(Array.Empty<Master>()) };
        Join(overload, inputs, (_, _) => Assert.Fail("An empty master sequence cannot join a detail."));
        AssertDisposed(inputs, overload, master: true);
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void CallbackFailureDisposesAllAcquiredEnumerators(int overload)
    {
        var inputs = new Inputs();
        var failure = new InvalidOperationException("Callback failure.");
        var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Join(overload, inputs, (_, _) => throw failure));
        Assert.AreSame(failure, actual);
        AssertDisposed(inputs, overload, master: true);
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void FirstDetailPrefetchFailureDisposesItsEnumerator(int overload)
    {
        var inputs = new Inputs();
        inputs.Details.FailMoveNext = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => Join(overload, inputs, (_, _) => { }));
        Assert.AreEqual(1, inputs.Details.Disposals);
        Assert.AreEqual(0, inputs.SubDetails.Disposals);
        Assert.AreEqual(0, inputs.Masters.Disposals);
    }

    [TestMethod]
    [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void SecondDetailAcquisitionFailureDisposesFirstEnumerator(int overload)
    {
        var inputs = new Inputs();
        inputs.SubDetails.FailGetEnumerator = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => Join(overload, inputs, (_, _) => { }));
        Assert.AreEqual(1, inputs.Details.Disposals);
        Assert.AreEqual(0, inputs.SubDetails.Disposals);
        Assert.AreEqual(0, inputs.Masters.Disposals);
    }

    [TestMethod]
    [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void SecondDetailPrefetchFailureDisposesBothEnumerators(int overload)
    {
        var inputs = new Inputs();
        inputs.SubDetails.FailMoveNext = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => Join(overload, inputs, (_, _) => { }));
        AssertDisposed(inputs, overload, master: false);
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void MasterAcquisitionFailureDisposesPrefetchedDetails(int overload)
    {
        var inputs = new Inputs();
        inputs.Masters.FailGetEnumerator = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => Join(overload, inputs, (_, _) => { }));
        AssertDisposed(inputs, overload, master: false);
    }

    private static void AssertDisposed(Inputs inputs, int overload, bool master)
    {
        Assert.AreEqual(master ? 1 : 0, inputs.Masters.Disposals);
        Assert.AreEqual(1, inputs.Details.Disposals);
        Assert.AreEqual(overload >= 2 ? 1 : 0, inputs.SubDetails.Disposals);
    }

    private static void Join(int overload, Inputs inputs, Action<Master, Detail> action)
    {
        Func<Master, Detail, bool> firstLink = (master, detail) => master.Id == detail.MasterId;
        Func<Master, SubDetail, bool> siblingLink = (master, detail) => master.Id == detail.MasterId;
        Func<Detail, SubDetail, bool> childLink = (detail, child) => detail.Id == child.DetailId;
        Action<Master> initialize = _ => { };
        Action<Master, SubDetail> siblingAction = (_, _) => { };
        Action<Detail, SubDetail> childAction = (_, _) => { };
        switch (overload)
        {
            case 0: inputs.Masters.MergeJoin(inputs.Details, firstLink, action); break;
            case 1: inputs.Masters.MergeJoin(initialize, inputs.Details, firstLink, action); break;
            case 2: inputs.Masters.MergeJoin(inputs.Details, firstLink, action, inputs.SubDetails, siblingLink, siblingAction); break;
            case 3: inputs.Masters.MergeJoin(initialize, inputs.Details, firstLink, action, inputs.SubDetails, siblingLink, siblingAction); break;
            case 4: inputs.Masters.MergeJoin(inputs.Details, firstLink, action, inputs.SubDetails, childLink, childAction); break;
            case 5: inputs.Masters.MergeJoin(initialize, inputs.Details, firstLink, action, inputs.SubDetails, childLink, childAction); break;
            default: throw new ArgumentOutOfRangeException(nameof(overload));
        }
    }

    private sealed record Master(int Id);
    private sealed record Detail(int Id, int MasterId);
    private sealed record SubDetail(int MasterId, int DetailId);

    private sealed class Inputs
    {
        public TrackedEnumerable<Master> Masters { get; init; } = new(new[] { new Master(1), new Master(2) });
        public TrackedEnumerable<Detail> Details { get; } = new(new[] { new Detail(101, 1), new Detail(202, 2) });
        public TrackedEnumerable<SubDetail> SubDetails { get; } = new(new[] { new SubDetail(1, 101), new SubDetail(2, 202) });
    }

    private sealed class TrackedEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int Disposals { get; private set; }
        public bool FailGetEnumerator { get; set; }
        public bool FailMoveNext { get; set; }

        public IEnumerator<T> GetEnumerator()
        {
            if (FailGetEnumerator) throw new InvalidOperationException("Acquisition failure.");
            return new Enumerator(this, items.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator(TrackedEnumerable<T> owner, IEnumerator<T> inner) : IEnumerator<T>
        {
            public T Current => inner.Current;
            object IEnumerator.Current => Current;
            public bool MoveNext() => owner.FailMoveNext
                ? throw new InvalidOperationException("Prefetch failure.") : inner.MoveNext();
            public void Reset() => inner.Reset();
            public void Dispose()
            {
                owner.Disposals++;
                inner.Dispose();
            }
        }
    }
}
