using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DataServices;

namespace Tests.Tests;

[TestClass]
public class DataServiceFailureRegressionTest
{
    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(12)]
    [DataRow(700)]
    public async Task ErrorConversion_AcceptsMissingShortAndLongStackTraces(int? traceLength)
    {
        var trace = traceLength.HasValue ? new string('x', traceLength.Value) : null;
        var error = new ControlledTraceException(trace);
        var replies = new DataReply[]
        {
            DataServiceBase.Get<int>(() => throw error),
            await DataServiceBase.GetAsync(() => Task.FromException<int>(error)),
            DataServiceBase.Execute(() => throw error),
            await DataServiceBase.ExecuteAsync(() => Task.FromException<bool>(error))
        };

        foreach (var reply in replies)
        {
            Assert.AreEqual(DataReplyStatus.Error, reply.Status);
            Assert.AreEqual("Expected error.", reply.Messages.Single(message => message.Code == "ErrorMessage").Text);
            Assert.AreEqual(new string('x', Math.Min(traceLength ?? 0, 500)), reply.Messages.Single(message => message.Code == "StackTrace").Text);
        }
    }

    [TestMethod]
    public async Task Cancellation_IsPropagatedByAllSampleWrappers()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException(cancellation.Token);

        Assert.AreSame(expected, Assert.ThrowsExactly<OperationCanceledException>(() => DataServiceBase.Get<int>(() => throw expected)));
        Assert.AreSame(expected, Assert.ThrowsExactly<OperationCanceledException>(() => DataServiceBase.Execute(() => throw expected)));
        Assert.AreSame(expected, await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => DataServiceBase.GetAsync(() => Task.FromException<int>(expected))));
        Assert.AreSame(expected, await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => DataServiceBase.ExecuteAsync(() => Task.FromException<bool>(expected))));
    }

    private sealed class ControlledTraceException(string trace) : Exception("Expected error.")
    {
        public override string StackTrace => trace;
    }
}
