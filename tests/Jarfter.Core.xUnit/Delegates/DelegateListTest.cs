using Jarfter.Core.Delegates;

namespace Jarfter.Core.xUnit.Delegates;

public sealed class DelegateListTest
{
    [Fact]
    public void Invoke_WhenNoSubscribers_ShouldDoNothing()
    {
        DelegateList list = new DelegateList();

        list.Invoke();
    }

    [Fact]
    public void Invoke_WithSubscriptions_ShouldInvokeInOrder()
    {
        DelegateList list = new DelegateList();
        List<int> calls = [];

        list.Subscribe(() => calls.Add(1));
        list.Subscribe(() => calls.Add(2));

        list.Invoke();

        Assert.Equal([1, 2], calls);
    }

    [Fact]
    public void Unsubscribe_WhenDuplicateSubscription_ShouldRemoveOneSubscription()
    {
        DelegateList list = new DelegateList();
        int count = 0;
        void Handler() => count++;

        list.Subscribe(Handler);
        list.Subscribe(Handler);
        list.Unsubscribe(Handler);

        list.Invoke();

        Assert.Equal(1, count);
    }

    [Fact]
    public void CompoundOperators_ShouldSubscribeAndUnsubscribe()
    {
        DelegateList list = new DelegateList();
        int count = 0;
        void Handler() => count++;

        list += Handler;
        list.Invoke();
        list -= Handler;
        list.Invoke();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Invoke_WhenSubscriberUnsubscribes_ShouldThrowInvalidOperationException()
    {
        DelegateList list = new DelegateList();
        List<int> calls = [];
        void Second() => calls.Add(2);
        void First()
        {
            calls.Add(1);
            list.Unsubscribe(Second);
        }

        list.Subscribe(First);
        list.Subscribe(Second);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(list.Invoke);

        Assert.Equal("DelegateList cannot be modified while it is invoking delegates.", exception.Message);
        Assert.Equal([1], calls);
    }

    [Fact]
    public void Invoke_WhenSubscriberSubscribes_ShouldThrowInvalidOperationException()
    {
        DelegateList list = new DelegateList();
        void Second()
        {
        }

        list.Subscribe(() => list.Subscribe(Second));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(list.Invoke);

        Assert.Equal("DelegateList cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public void Invoke_ForGenericDelegateWhenSubscriberSubscribes_ShouldThrowInvalidOperationException()
    {
        DelegateList<int> list = new DelegateList<int>();
        void Second(int value)
        {
        }

        list.Subscribe(_ => list.Subscribe(Second));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => list.Invoke(1));

        Assert.Equal("DelegateList cannot be modified while it is invoking delegates.", exception.Message);
    }

    [Fact]
    public void Subscribe_WithNullAction_ShouldThrowArgumentNullException()
    {
        DelegateList list = new DelegateList();

        Assert.Throws<ArgumentNullException>(() => list.Subscribe(null!));
    }

    [Fact]
    public void Invoke_ForOneArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int> list = new DelegateList<int>();
        int received = 0;

        list += value => received = value;
        list.Invoke(7);

        Assert.Equal(7, received);
    }

    [Fact]
    public void Invoke_ForTwoArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int> list = new DelegateList<int, int>();
        int received = 0;

        list += (arg1, arg2) => received = arg1 + arg2;
        list.Invoke(2, 3);

        Assert.Equal(5, received);
    }

    [Fact]
    public void Invoke_ForThreeArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int, int> list = new DelegateList<int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3) => received = arg1 + arg2 + arg3;
        list.Invoke(2, 3, 5);

        Assert.Equal(10, received);
    }

    [Fact]
    public void Invoke_ForFourArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int, int, int> list = new DelegateList<int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4) => received = arg1 + arg2 + arg3 + arg4;
        list.Invoke(2, 3, 5, 7);

        Assert.Equal(17, received);
    }

    [Fact]
    public void Invoke_ForFiveArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int, int, int, int> list = new DelegateList<int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5) => received = arg1 + arg2 + arg3 + arg4 + arg5;
        list.Invoke(2, 3, 5, 7, 11);

        Assert.Equal(28, received);
    }

    [Fact]
    public void Invoke_ForSixArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int, int, int, int, int> list = new DelegateList<int, int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5, arg6) => received = arg1 + arg2 + arg3 + arg4 + arg5 + arg6;
        list.Invoke(2, 3, 5, 7, 11, 13);

        Assert.Equal(41, received);
    }

    [Fact]
    public void Invoke_ForSevenArgumentDelegate_ShouldForwardArguments()
    {
        DelegateList<int, int, int, int, int, int, int> list = new DelegateList<int, int, int, int, int, int, int>();
        int received = 0;

        list += (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => received = arg1 + arg2 + arg3 + arg4 + arg5 + arg6 + arg7;
        list.Invoke(2, 3, 5, 7, 11, 13, 17);

        Assert.Equal(58, received);
    }
}
