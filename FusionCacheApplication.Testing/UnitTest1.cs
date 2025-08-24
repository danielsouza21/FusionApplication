namespace FusionCacheApplication.Testing;

/*
Testing approachs:
    1. Run request for instance 1, hitting the cache L1 and also saving it onL2 (redis). 
       Run request for instance 2 and verify that factory was not called (got value from L2). 
       Then remove key at Redis, re-run requests for both instances and verify again that factory was not called (hitted L1 em both cases).

    2. 
*/
public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
