namespace FusionCacheApplication.Testing;

/*
Testing approachs:
    1. Run request for instance 1, hitting the cache L1 and also saving it onL2 (redis). 
       Run request for instance 2 and verify that factory was not called (got value from L2). 
       Then remove key at Redis, re-run requests for both instances and verify again that factory was not called (hitted L1 em both cases).

    2. 
*/
public class StandardCaching
{
    public const string APP_INSTANCE_1_URL = "";
    public const string APP_INSTANCE_2_URL = "";

    public const string REDIS_PASSWORD = "";

    [Fact]
    public void StandardCaching_GivenRetriveUser_ShouldCacheL1andL2_MultipleInstances()
    {
        
    }
}
