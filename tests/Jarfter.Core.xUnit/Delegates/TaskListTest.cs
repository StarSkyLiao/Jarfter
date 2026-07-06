using Jarfter.Core.Delegates;

namespace Jarfter.Core.xUnit.Delegates;

public sealed class TaskListTest
{
    [Fact]
    public async Task InvokeAsync_WhenNoSubscribers_ShouldDoNothing()
    {
        TaskList list = new TaskList();

        await list.InvokeAsync();
    }

    [Fact]
    public async Task InvokeAsync_WithSubscriptions_ShouldInvokeInOrder()
    {
        TaskList list = new TaskList();
        List<int> calls = [];

        list.Subscribe(() =>
        {
            calls.Add(1);
            return ValueTask.CompletedTask;
        });
        list.Subscribe(() =>
        {
            calls.Add(2);
            return ValueTask.CompletedTask;
        });

        await list.InvokeAsync();

        Assert.Equal([1, 2], calls);
    }

    [Fact]
    public async Task InvokeAsync_WithAwaitingSubscriber_ShouldAwaitBeforeNextSubscriber()
    {
        TaskList list = new TaskList();
        List<int> calls = [];

        list.Subscribe(async () =>
        {
            calls.Add(1);
            await Task.Yield();
            calls.Add(2);
        });
        list.Subscribe(() =>
        {
            calls.Add(3);
            return ValueTask.CompletedTask;
        });

        await list.InvokeAsync();

        Assert.Equal([1, 2, 3], calls);
    }

    [Fact]
    public async Task Unsubscribe_WhenDuplicateSubscription_ShouldRemoveOneSubscription()
    {
        TaskList list = new TaskList();
        int count = 0;
        ValueTask Handler()
        {
            count++;
            return ValueTask.CompletedTask;
        }

        list.Subscribe(Handler);
        list.Subscribe(Handler);
        list.Unsubscribe(Handler);

        await list.InvokeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CompoundOperators_ShouldSubscribeAndUnsubscribe()
    {
        TaskList list = new TaskList();
        int count = 0;
        ValueTask Handler()
        {
            count++;
            return ValueTask.CompletedTask;
        }

        list += Handler;
        await list.InvokeAsync();
        list -= Handler;
        await list.InvokeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubscriberSubscribes_ShouldThrowInvalidOperationException()
    {
        TaskList list = new TaskList();
        ValueTask Second() => ValueTask.CompletedTask;

        list.Subscribe(() =>
        {
            list.Subscribe(Second);
            return ValueTask.CompletedTask;
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync());

        Assert.Equal("Delegate list cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubscriberSubscribesAfterAwait_ShouldThrowInvalidOperationException()
    {
        TaskList list = new TaskList();
        ValueTask Second() => ValueTask.CompletedTask;

        list.Subscribe(async () =>
        {
            await Task.Yield();
            list.Subscribe(Second);
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync());

        Assert.Equal("Delegate list cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubscriberUnsubscribesAfterAwait_ShouldThrowInvalidOperationException()
    {
        TaskList list = new TaskList();
        ValueTask Second() => ValueTask.CompletedTask;

        list.Subscribe(async () =>
        {
            await Task.Yield();
            list.Unsubscribe(Second);
        });
        list.Subscribe(Second);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync());

        Assert.Equal("Delegate list cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubscriberThrows_ShouldRestoreModificationState()
    {
        TaskList list = new TaskList();
        ValueTask ThrowingHandler() => throw new InvalidOperationException("failed");
        ValueTask Second() => ValueTask.CompletedTask;

        list.Subscribe(ThrowingHandler);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync());
        list.Unsubscribe(ThrowingHandler);
        list.Subscribe(Second);

        Assert.Equal("failed", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubscriberThrowsAfterAwait_ShouldRestoreModificationState()
    {
        TaskList list = new TaskList();
        async ValueTask ThrowingHandler()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }

        ValueTask Second() => ValueTask.CompletedTask;
        list.Subscribe(ThrowingHandler);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync());
        list.Unsubscribe(ThrowingHandler);
        list.Subscribe(Second);

        Assert.Equal("failed", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ForGenericDelegateWhenSubscriberSubscribesAfterAwait_ShouldThrowInvalidOperationException()
    {
        TaskList<int> list = new TaskList<int>();
        ValueTask Second(int value) => ValueTask.CompletedTask;

        list.Subscribe(async _ =>
        {
            await Task.Yield();
            list.Subscribe(Second);
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await list.InvokeAsync(1));

        Assert.Equal("Delegate list cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public void Subscribe_WithNullAction_ShouldThrowArgumentNullException()
    {
        TaskList list = new TaskList();

        Assert.Throws<ArgumentNullException>(() => list.Subscribe(null!));
    }

    [Fact]
    public async Task InvokeAsync_ForOneArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int> list = new TaskList<int>();
        int received = 0;

        list += value =>
        {
            received = value;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(7);

        Assert.Equal(7, received);
    }

    [Fact]
    public async Task InvokeAsync_ForTwoArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int> list = new TaskList<int, int>();
        int received = 0;

        list += (arg1, arg2) =>
        {
            received = arg1 + arg2;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3);

        Assert.Equal(5, received);
    }

    [Fact]
    public async Task InvokeAsync_ForThreeArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int, int> list = new TaskList<int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3) =>
        {
            received = arg1 + arg2 + arg3;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3, 5);

        Assert.Equal(10, received);
    }

    [Fact]
    public async Task InvokeAsync_ForFourArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int, int, int> list = new TaskList<int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4) =>
        {
            received = arg1 + arg2 + arg3 + arg4;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3, 5, 7);

        Assert.Equal(17, received);
    }

    [Fact]
    public async Task InvokeAsync_ForFiveArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int, int, int, int> list = new TaskList<int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5) =>
        {
            received = arg1 + arg2 + arg3 + arg4 + arg5;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3, 5, 7, 11);

        Assert.Equal(28, received);
    }

    [Fact]
    public async Task InvokeAsync_ForSixArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int, int, int, int, int> list = new TaskList<int, int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5, arg6) =>
        {
            received = arg1 + arg2 + arg3 + arg4 + arg5 + arg6;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3, 5, 7, 11, 13);

        Assert.Equal(41, received);
    }

    [Fact]
    public async Task InvokeAsync_ForSevenArgumentDelegate_ShouldForwardArguments()
    {
        TaskList<int, int, int, int, int, int, int> list = new TaskList<int, int, int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5, arg6, arg7) =>
        {
            received = arg1 + arg2 + arg3 + arg4 + arg5 + arg6 + arg7;
            return ValueTask.CompletedTask;
        };
        await list.InvokeAsync(2, 3, 5, 7, 11, 13, 17);

        Assert.Equal(58, received);
    }
}
