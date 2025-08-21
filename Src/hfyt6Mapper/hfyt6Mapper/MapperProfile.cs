using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper
{
    public interface IMapperProfile
    {
        object Map(object sourceObj);
    }

    public class MapperProfile<TIn, TOut> : IMapperProfile
    {
        private List<MemberBinding> _memberBindings = new List<MemberBinding> ();
        private List<MemberBinding> _autoMapMemberBindings = new List<MemberBinding> ();

        private ParameterExpression _tInParameterExpression;

        private Func<TIn, TOut> _converterFunc;

        public Type InType { get; private set; }

        public Type OutType { get; private set; }

        public MapperProfile()
        {
            InType = typeof(TIn);
            OutType = typeof(TOut);
        }

        private void AutoMapper()
        {
            PropertyInfo[] inTypeProps = InType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] outTypeProps = OutType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (_tInParameterExpression == null)
                _tInParameterExpression = Expression.Parameter(typeof(TIn), "inObj");

            Dictionary<string, PropertyInfo> inPropSet = new Dictionary<string, PropertyInfo>(inTypeProps
                .Select(p => KeyValuePair.Create(p.Name, p)));

            foreach (PropertyInfo prop in outTypeProps)
            {
                if (!inPropSet.ContainsKey(prop.Name) || inPropSet[prop.Name].PropertyType != prop.PropertyType || !prop.CanWrite)
                    continue;

                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)) // simple type
                {
                    _autoMapMemberBindings.Add(Expression.Bind(prop, Expression.MakeMemberAccess(_tInParameterExpression, inPropSet[prop.Name])));
                    continue;
                }

                if (prop.PropertyType.GetInterface("ICloneable") == typeof(ICloneable))
                {
                    Expression<Func<object, object>> cloneExp = 
                        (obj) => (obj != null) ? (obj as ICloneable).Clone() : null;
                    MemberExpression memberExp = Expression.MakeMemberAccess(_tInParameterExpression, inPropSet[prop.Name]);
                    UnaryExpression cvtExp = Expression.Convert(memberExp, typeof(object));  // Value type need
                    InvocationExpression invokeExp = Expression.Invoke(cloneExp, cvtExp);
                    UnaryExpression cvtExp2 = Expression.Convert(invokeExp, prop.PropertyType);  // Value type need
                    _autoMapMemberBindings.Add(Expression.Bind(prop, cvtExp2));
                }

            }
        }

        public void SetMapper<TMember>(Expression<Func<TIn, TMember>> getValueFunc,
            Expression<Func<TOut, TMember>> setValueExp)
        {
            if(_tInParameterExpression == null)
                _tInParameterExpression = Expression.Parameter(typeof(TIn), "inObj");

            if (setValueExp.Body is MemberExpression mexp)
            {
                var p = typeof(TOut).GetProperty(mexp.Member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.CanRead)
                {
                    AddMemberBinding(p, getValueFunc);
                    return;
                }

                var f = typeof(TOut).GetField(mexp.Member.Name, BindingFlags.Instance | BindingFlags.Public);

                if (f != null)
                {
                    AddMemberBinding(f, getValueFunc);
                    return;
                }
            }
            else if(setValueExp.Body is UnaryExpression uexp && uexp.Operand is MemberExpression mexp2)
            {
                AddMemberBinding(mexp2.Member, getValueFunc);
                return;
            }
            else 
                throw new InvalidOperationException("Unsupported setting member " + setValueExp.ToString());

        }

        private void AddMemberBinding(MemberInfo member, Expression getValueFunc)
        {
            InvocationExpression invokeFunc = Expression.Invoke(getValueFunc, _tInParameterExpression);
            MemberBinding bind = Expression.Bind(member, invokeFunc);
            _memberBindings.Add(bind);
        }

        public Func<TIn, TOut> Complie()
        {
            AutoMapper();

            List<MemberBinding> addBindings = new List<MemberBinding>();
            Dictionary<string, bool> hashSet = new Dictionary<string, bool>(_memberBindings.Select(mb => KeyValuePair.Create(mb.Member.Name, true)));
            foreach(MemberBinding mb in _autoMapMemberBindings) 
                if(!hashSet.ContainsKey(mb.Member.Name))
                    addBindings.Add(mb);
            _memberBindings.AddRange(addBindings);

            MemberInitExpression memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), _memberBindings);
            Expression<Func<TIn, TOut>> lambda = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression,
                new ParameterExpression[] { _tInParameterExpression });
            _converterFunc = lambda.Compile();
            return _converterFunc;
        }

        public TOut Map(TIn sourceObj)
        {
            return _converterFunc(sourceObj);
        }

        public object Map(object sourceObj)
        {
            if (!(sourceObj is TIn obj))
                throw new InvalidOperationException($"Illegal operation requires type {typeof(TIn)} but the object is type {sourceObj.GetType()}");
            return _converterFunc(obj);
        }
    }
}
