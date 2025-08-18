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

        public Type InType { get; set; }
        public Type OutType { get; set; }
    }

    public class MapperProfile<TIn, TOut> : IMapperProfile
    {
        private List<MemberBinding> _memberBindings = new List<MemberBinding>();

        private ParameterExpression _tInParameterExpression;

        private Func<TIn, TOut> _converterFunc;

        public Type InType { get; set; }

        public Type OutType { get; set; }

        public MapperProfile()
        {
            InType = typeof(TIn);
            OutType = typeof(TOut);
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
                    InvocationExpression invokeFunc = Expression.Invoke(getValueFunc, _tInParameterExpression);
                    _memberBindings.Add(Expression.Bind(p, invokeFunc));
                    return;
                }

                var f = typeof(TOut).GetField(mexp.Member.Name, BindingFlags.Instance | BindingFlags.Public);

                if (f != null)
                {
                    InvocationExpression invokeFunc = Expression.Invoke(getValueFunc, _tInParameterExpression);
                    _memberBindings.Add(Expression.Bind(f, invokeFunc));
                    return;
                }
            }
            else if(setValueExp.Body is UnaryExpression uexp && uexp.Operand is MemberExpression mexp2)
            {
                InvocationExpression invokeFunc = Expression.Invoke(getValueFunc, _tInParameterExpression);
                _memberBindings.Add(Expression.Bind(mexp2.Member, invokeFunc));
                return;
            }
            else 
                throw new InvalidOperationException("Unsupported setting member " + setValueExp.ToString());

        }

        public Func<TIn, TOut> Complie()
        {
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
