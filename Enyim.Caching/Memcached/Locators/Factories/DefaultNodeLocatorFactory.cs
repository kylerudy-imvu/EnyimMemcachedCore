using System.Collections.Generic;

namespace Enyim.Caching.Memcached.LocatorFactories
{
    /// <summary>
    /// Create DefaultNodeLocator with any ServerAddressMutations
    /// </summary>
    public class DefaultNodeLocatorFactory :IProviderFactory<IMemcachedNodeLocator>
    {
        private readonly int serverAddressMutations;

        public DefaultNodeLocatorFactory(int serverAddressMutations)
        {
            this.serverAddressMutations = serverAddressMutations;
        }

        public IMemcachedNodeLocator Create()
        {
            return new DefaultNodeLocator(serverAddressMutations);
        }

        public void Initialize(Dictionary<string, string> parameters)
        {
        }
    }
}
