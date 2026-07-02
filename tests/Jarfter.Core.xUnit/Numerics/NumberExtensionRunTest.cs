using Jarfter.Core.Diagnostics;
using Jarfter.Core.Numerics;

namespace Jarfter.Core.xUnit.Numerics;

public static class NumberExtensionRunTest
{
    public static void Run()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { LoopCount = 1000 }, [
            new MethodWrapper<double, int, double>(PowBinary, 1234, 3),
            new MethodWrapper<double, int, double>(PowMath, 1234, 3),
        ]);
    }
    
    public static double PowBinary(double value, int power) => value.Pow(power);
    
    public static double PowMath(double value, int power) => Math.Pow(value, power);
    
}
