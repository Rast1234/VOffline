using System.IO;
using log4net;
using Newtonsoft.Json;

namespace VOffline.Services
{
    public class FileCache<T>
        where T : class, new()
    {
        private readonly ILog log;

        public FileCache(ILog log)
        {
            this.log = log;
        }
        //log.Debug($"found cached value");

        private T value;

        public string Filename => $"FileCache.{typeof(T).Name}.json";

        public T Value
        {
            get
            {
                if (value != null)
                {
                    return value;
                }

                if (File.Exists(Filename))
                {
                    var content = File.ReadAllText(Filename);
                    if (!string.IsNullOrEmpty(content))
                    {
                        value = JsonConvert.DeserializeObject<T>(content);
                        log.Debug($"{Filename} loaded value");
                    }
                }

                return value;
            }
            set
            {
                this.value = value;
                File.WriteAllText(Filename, JsonConvert.SerializeObject(value, Formatting.Indented));
                log.Debug($"{Filename} saved value");
            }
        }
    }
}
