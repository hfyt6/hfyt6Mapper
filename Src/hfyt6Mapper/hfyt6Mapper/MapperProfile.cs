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
        bool IsUsePreserveReferences { get; }

        void UsePreserveReferences();

        object Map(object sourceObj);

        object PreserveReferencesMap(object sourceObj, Dictionary<int, object> reuseSet);
    }

    public class MapperProfile<TIn, TOut> : IMapperProfile
    {
        private List<MemberBinding> _memberBindings = new List<MemberBinding> ();
        private List<MemberBinding> _autoMapMemberBindings = new List<MemberBinding> ();
        private List<(PropertyInfo, Expression)> _autoMapMemberExp = new List<(PropertyInfo, Expression)> ();

        private ParameterExpression _tInParameterExpression;
        private ParameterExpression _tOutParameterExpression;
        private ParameterExpression _tPRObjParameterExpression;
        private ParameterExpression _tPRHashSetParameterExpression;

        private Func<TIn, TOut> _createAndSetFunc;
        private Func<TIn, TOut> _preserveReferencesCreateAndSetFunc;
        private Action<TIn, TOut, Dictionary<int, object>> _preserveReferencesSetFunc;

        private bool _isPreserveReferences = false;
        public bool IsUsePreserveReferences => _isPreserveReferences;

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

            Dictionary<string, PropertyInfo> inPropSet = new Dictionary<string, PropertyInfo>(inTypeProps
                .Select(p => KeyValuePair.Create(p.Name, p)));

            foreach (PropertyInfo prop in outTypeProps)
            {
                if (!inPropSet.ContainsKey(prop.Name) || inPropSet[prop.Name].PropertyType != prop.PropertyType || !prop.CanWrite)
                    continue;

                // simple type
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)) 
                {
                    var exp = Expression.MakeMemberAccess(_tInParameterExpression, inPropSet[prop.Name]);
                    //InvocationExpression invokeFunc = Expression.Invoke(exp, _tInParameterExpression);

                    var cvtExp = Expression.Convert(exp, prop.PropertyType);
                    if (_isPreserveReferences)
                        _autoMapMemberExp.Add((prop, cvtExp));
                    else
                        _autoMapMemberBindings.Add(Expression.Bind(prop, cvtExp));
                    continue;
                }

                // invoke ICloneable
                if (prop.PropertyType.GetInterface("ICloneable") == typeof(ICloneable))
                {
                    Expression<Func<object, object>> cloneExp = 
                        (obj) => (obj != null) ? (obj as ICloneable).Clone() : null;
                    MemberExpression memberExp = Expression.MakeMemberAccess(_tInParameterExpression, inPropSet[prop.Name]);
                    UnaryExpression cvtExp = Expression.Convert(memberExp, typeof(object));  // Value type need
                    InvocationExpression invokeExp = Expression.Invoke(cloneExp, cvtExp);
                    UnaryExpression cvtExp2 = Expression.Convert(invokeExp, prop.PropertyType);  // Value type need

                    if (_isPreserveReferences)
                        _autoMapMemberExp.Add((prop, cvtExp2));
                    else
                        _autoMapMemberBindings.Add(Expression.Bind(prop, cvtExp2));

                    continue;
                }

                // preserve references
                if (_isPreserveReferences)  
                {
                    Expression<Func<object, Dictionary<int, object>, object>> getPreserveReferencesExp =
                        (tPrObj, tPrHashSet) => (tPrObj != null) ? Mapper.CloneMap(tPrObj, tPrHashSet) : null;
                    InvocationExpression invokeExp = Expression.Invoke(getPreserveReferencesExp, new ParameterExpression[]
                    {
                        _tPRObjParameterExpression,
                        _tPRHashSetParameterExpression,
                    });

                    _autoMapMemberExp.Add((prop, invokeExp));
                    //_autoMapMemberBindings.Add(Expression.Bind(prop, invokeExp));
                    continue;
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

        public void Complie()
        {
            if (_tInParameterExpression == null)
                _tInParameterExpression = Expression.Parameter(InType, "inObj");

            if (_tPRObjParameterExpression == null)
                _tPRObjParameterExpression = Expression.Parameter(typeof(object), "tPrObj");

            if (_tPRHashSetParameterExpression == null)
                _tPRHashSetParameterExpression = Expression.Parameter(typeof(Dictionary<int, object>), "reuseSet");

            if (_tOutParameterExpression == null)
                _tOutParameterExpression = Expression.Parameter(OutType, "outObj");

            AutoMapper();

            PreserveReferencesComplie();

            List<MemberBinding> addBindings = new List<MemberBinding>();
            Dictionary<string, bool> hashSet = new Dictionary<string, bool>(_memberBindings.Select(mb => KeyValuePair.Create(mb.Member.Name, true)));
            foreach(MemberBinding mb in _autoMapMemberBindings) 
                if(!hashSet.ContainsKey(mb.Member.Name))
                    addBindings.Add(mb);
            _memberBindings.AddRange(addBindings);

            MemberInitExpression memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), _memberBindings);
            Expression<Func<TIn, TOut>> lambda = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression,
                new ParameterExpression[] { _tInParameterExpression });
            _createAndSetFunc = lambda.Compile();
        }

        private void PreserveReferencesComplie()
        {
            MemberInitExpression memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), _memberBindings);
            Expression<Func<TIn, TOut>> lambda1 = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression,
                new ParameterExpression[] { _tInParameterExpression });
            _preserveReferencesCreateAndSetFunc = lambda1.Compile();

            Dictionary<string, bool> hashSet = new Dictionary<string, bool>(_memberBindings.Select(mb => KeyValuePair.Create(mb.Member.Name, true)));

            List<Expression> assignExps = new List<Expression>();
            foreach(var (prop, exp) in _autoMapMemberExp)
            {
                if (hashSet.ContainsKey(prop.Name))
                    continue;

                MemberExpression mexp = Expression.Property(_tOutParameterExpression, prop);
                BinaryExpression assignExp = Expression.Assign(mexp, exp);

                assignExps.Add(assignExp);
            }

            BlockExpression blockExp = Expression.Block(assignExps);

            Expression<Action<TIn, TOut, Dictionary<int, object>>> lambda2 = Expression.Lambda<Action<TIn, TOut, Dictionary<int, object>>>(blockExp, 
                new ParameterExpression[] { _tInParameterExpression, _tOutParameterExpression, _tPRHashSetParameterExpression});
            _preserveReferencesSetFunc = lambda2.Compile();
        }

        public TOut Map(TIn sourceObj)
        {
            return _createAndSetFunc(sourceObj);
        }

        public object Map(object sourceObj)
        {
            if (!(sourceObj is TIn obj))
                throw new InvalidOperationException($"Illegal operation requires type {typeof(TIn)} but the object is type {sourceObj.GetType()}");
            return _createAndSetFunc(obj);
        }

        public void UsePreserveReferences()
        {
            _isPreserveReferences = true;
        }

        public object PreserveReferencesMap(object sourceObj, Dictionary<int, object> reuseSet)
        {
            if (!(sourceObj is TIn inObj))
                throw new InvalidOperationException($"Illegal operation requires type {typeof(TIn)} but the object is type {sourceObj.GetType()}");

            int key = sourceObj.GetHashCode();
            if (reuseSet.ContainsKey(key))
            {
                return reuseSet[key];
            }

            TOut outObj = _preserveReferencesCreateAndSetFunc.Invoke(inObj);
            if (outObj != null)
            {
                reuseSet[key] = outObj;

                _preserveReferencesSetFunc(inObj, outObj, reuseSet);
            }

            return outObj;
        }
    }
}
