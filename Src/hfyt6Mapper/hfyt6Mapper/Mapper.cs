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
        private static readonly Dictionary<int, MapperProfile> _mapperCache = new Dictionary<int, MapperProfile>();

        private static int GetKey(Type t1, Type t2) => (int) ((long)t1.GetHashCode() << 4 ^ t2.GetHashCode());
        private static int GetKey<TSource, TTarget>() => (int) ((long)typeof(TSource).GetHashCode() << 4 ^ typeof(TTarget).GetHashCode());
        

        public static void Configure(Action<MapperProfile> configure)
        {
            var profile = new MapperProfile();
            configure(profile);
            int key = GetKey(profile.SourceType, profile.TargetType);

            if(!_mapperCache.ContainsKey(key)) 
                _mapperCache[key] = profile;
        }

        public static void Configure<TSource, TTarget>(Action<MapperProfile> configure)
        {
            var profile = new MapperProfile();
            profile.CreateMap<TSource, TTarget>();
            configure(profile);
            int key = GetKey(profile.SourceType, profile.TargetType);

            if (!_mapperCache.ContainsKey(key))
                _mapperCache[key] = profile;
        }

        public static TTarget Map<TSource, TTarget>(TSource source)
        {
            if (source == null)
                return default;

            int key = GetKey<TSource, TTarget>();

            if (!_mapperCache.ContainsKey(key))
            {
                throw new InvalidOperationException($"No mapping defined for {typeof(TSource)} to {typeof(TTarget)} !");
            }

            return (TTarget)MapObject(source, _mapperCache[key]);
        }

        private static object MapObject(object source, MapperProfile profile) 
        { 
            var targetObj = Activator.CreateInstance(profile.TargetType);

            foreach(var (sourceValueGetter, targetProperty) in profile.PropertyMappings)
            {
                if(targetProperty.CanWrite)
                {
                    object val = sourceValueGetter(source);
                    targetProperty.SetValue(targetObj, ConvertValue(val, targetProperty.PropertyType));
                }
            }

            return targetObj;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            Type sourceType = value.GetType();
            if (value == null || targetType == sourceType)
                return value;

            if ((targetType == typeof(string) && sourceType.IsPrimitive) ||
                (sourceType == typeof(string) && targetType.IsPrimitive) ||
                (targetType.IsPrimitive && sourceType.IsPrimitive))
                return Convert.ChangeType(value, targetType);


            int key = GetKey(sourceType, targetType);
            if(_mapperCache.ContainsKey(key))
                return MapObject(value, _mapperCache[key]);

            throw new InvalidOperationException($"Cannot convert {value.GetType()} to {targetType}");
        }
    }
}
