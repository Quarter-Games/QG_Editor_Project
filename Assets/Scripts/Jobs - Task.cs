using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PrimeChecker : MonoBehaviour
{
    [BurstCompile]
    public struct PrimeCheckJob : IJobParallelFor
    {
        [ReadOnly] public int N;
        [WriteOnly] public NativeArray<bool> IsDivisible;

        public void Execute(int index)
        {
            int i = index + 2; 
            if (N % i == 0)
            {
                IsDivisible[0] = true; 
            }
        }
    }

    void Start()
    {
        int numberToCheck = 100_000_007; 
        bool isPrime = CheckIfPrimeWithJob(numberToCheck);
        Debug.Log($"{numberToCheck} is {(isPrime ? "prime" : "not prime")}");
        Debug.Log(IsPrime(numberToCheck));
    }

    bool CheckIfPrimeWithJob(int n)
    {
        if (n < 2) return false;

        int limit = (int)math.sqrt(n);
        NativeArray<bool> isDivisible = new NativeArray<bool>(1, Allocator.TempJob);
        isDivisible[0] = false;

        var job = new PrimeCheckJob
        {
            N = n,
            IsDivisible = isDivisible
        };

        
        JobHandle handle = job.Schedule(limit - 1, 64);
        handle.Complete();

        bool result = !isDivisible[0]; 
        isDivisible.Dispose();
        return result;
    }
    public static bool IsPrime(int n)
    {
        if (n < 2) return false;
        int limit = (int)math.sqrt(n);
        for (int i = 2; i <= limit; i++)
        {
            if (n % i == 0) return false;
        }
        return true;
    }
}
