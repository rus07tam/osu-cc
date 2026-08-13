using System.Collections.Concurrent;
using System.Reflection;

namespace osucc.Data
{
    public class OsuCcStorageManager : IOsuCcStorageManager
    {
        private readonly osu.Framework.Platform.Storage rootStorage;
        private readonly ConcurrentDictionary<string, IOsuCcStorage> storages = new();

        public OsuCcStorageManager(osu.Framework.Platform.Storage rootStorage)
        {
            this.rootStorage = rootStorage;
        }

        public IOsuCcStorage GetStorage(string scope, Assembly? resourceAssembly = null)
        {
            return storages.GetOrAdd(scope, s =>
            {
                var diskStorage = rootStorage.GetStorageForDirectory("osu-cc/data/" + s);
                return new ScopedStorage(s, diskStorage, resourceAssembly);
            });
        }
    }
}
