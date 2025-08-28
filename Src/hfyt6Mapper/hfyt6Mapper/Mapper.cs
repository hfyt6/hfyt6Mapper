using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper
{
    public class Mapper
    {
        private static readonly Dictionary<int, IMapperProfile> _mapperCache = new Dictionary<int, IMapperProfile>();

        private static int GetKey(Type t1, Type t2) => (int) ((long)t1.GetHashCode() << 4 ^ t2.GetHashCode());
        private static int GetKey<TSource, TTarget>() => (int) ((long)typeof(TSource).GetHashCode() << 4 ^ typeof(TTarget).GetHashCode());
        
        public static void Configure<TSource, TTarget>(Action<MapperProfile<TSource, TTarget>> configure = null)
        {
            var profile = new MapperProfile<TSource, TTarget>();
            configure?.Invoke(profile);
            profile.Complie();
            int key = GetKey<TSource, TTarget>();

            if (!_mapperCache.ContainsKey(key))
                _mapperCache[key] = profile;
        }

        public static TTarget Map<TSource, TTarget>(TSource source)
        {
            if (source == null)
                return default;

            int key = GetKey<TSource, TTarget>();

            if (!_mapperCache.ContainsKey(key) || !_mapperCache[key].Compiled)
            {
                throw new InvalidOperationException($"No mapping defined for {typeof(TSource)} to {typeof(TTarget)} !");
            }

            if (_mapperCache[key].IsUsePreserveReferences)
            {
                return (TTarget)_mapperCache[key].PreserveReferencesMap(source, new Dictionary<int, object>());
            }

            return (TTarget)_mapperCache[key].Map(source);
        }

        public static object CloneMap(object source, Dictionary<int, object> hashSet)
        {
            if(source == null)
                return null;

            int key = GetKey(source.GetType(), source.GetType());
            if (!_mapperCache.ContainsKey(key) || !_mapperCache[key].Compiled)
                return null;

            return _mapperCache[key].PreserveReferencesMap(source, hashSet);
        }
    }
}
