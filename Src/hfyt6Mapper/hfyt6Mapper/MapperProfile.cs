using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper
{
    public class MapperProfile
    {
        public Type SourceType { get; private set; }
        public Type TargetType { get; private set; }
        public List<(GetSourceValue, PropertyInfo)> PropertyMappings { get; set; } = new List<(GetSourceValue, PropertyInfo)> ();

        public delegate object GetSourceValue(object s);

        public void CreateMap<TSource, TTarget>()
        {
            SourceType = typeof(TSource);
            TargetType = typeof(TTarget);
        }

        public void ForMember<TSource, TTarget>(
            Func<TSource, object> getSourceFunc,
            Expression<Func<TTarget, object>> targetPropertyExp)
        {
            string propertyName;
            if (targetPropertyExp.Body is UnaryExpression ue)
            {
                propertyName = ((MemberExpression)ue.Operand).Member.Name;
            }
            else
            {
                propertyName = ((MemberExpression)targetPropertyExp.Body).Member.Name;
            }
            var propertyInfo = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"Try to map a property that cannot be obtained {propertyName}");
            }

            PropertyMappings.Add( 
                (
                    obj => getSourceFunc.Invoke((TSource)obj) , 
                    propertyInfo
                ));
        }
    }
}
