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
        bool Compiled { get; }

        bool IsUsePreserveReferences { get; }

        void UsePreserveReferences();

        object? Map(object sourceObj);

        object? PreserveReferencesMap(object sourceObj, Dictionary<int, object> reuseSet);
    }

    public class MapperProfile<TIn, TOut> : IMapperProfile
    {
        // Normal map
        private List<MemberBinding> _memberBindings = new List<MemberBinding> ();
        private List<MemberBinding> _autoMapMemberBindings = new List<MemberBinding> ();
        // Automap preserve references
        private List<(PropertyInfo, Expression)> _autoMapGetValueExp = new List<(PropertyInfo, Expression)> ();

        private ParameterExpression _paramExpTIn;
        private ParameterExpression _paramExpTOut;
        private ParameterExpression _paramExpReuseSet;

        private Func<TIn, TOut> _createAndSetFunc;
        private Func<TIn, TOut> _preserveReferencesCreateAndSetFunc;
        private Action<TIn, TOut, Dictionary<int, object>> _preserveReferencesSetAction;

        private bool _isPreserveReferences = false;
        public bool IsUsePreserveReferences => _isPreserveReferences;

        private bool _compiled = false;
        public bool Compiled => _compiled;

        public Type InType { get; private set; }

        public Type OutType { get; private set; }

#pragma warning disable 8618
        public MapperProfile()
        {
            InType = typeof(TIn);
            OutType = typeof(TOut);

            _paramExpTIn = Expression.Parameter(InType, "inObj");
            _paramExpReuseSet = Expression.Parameter(typeof(Dictionary<int, object>), "reuseSet");
            _paramExpTOut = Expression.Parameter(OutType, "outObj");
        }
#pragma warning restore 8618

        private void AutoMapper() 
        {
            PropertyInfo[] inTypeProps = InType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] outTypeProps = OutType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Dictionary<string, PropertyInfo> inPropSet = new Dictionary<string, PropertyInfo>(inTypeProps
                .Select(p => KeyValuePair.Create(p.Name, p)));

            foreach (PropertyInfo outTypeProp in outTypeProps)
            {
                // Only allow properties of the same type and name AutoMap
                if (!inPropSet.ContainsKey(outTypeProp.Name) || inPropSet[outTypeProp.Name].PropertyType != outTypeProp.PropertyType || !outTypeProp.CanWrite)
                    continue;

                PropertyInfo inTypeProp = inPropSet[outTypeProp.Name];

                // Simple type 
                if (outTypeProp.PropertyType.IsPrimitive || outTypeProp.PropertyType == typeof(string)) 
                {
                    MemberExpression exp = Expression.MakeMemberAccess(_paramExpTIn, inTypeProp);
                    UnaryExpression propTypeCvtExp = Expression.Convert(exp, outTypeProp.PropertyType);
                    _autoMapGetValueExp.Add((outTypeProp, propTypeCvtExp));
                    _autoMapMemberBindings.Add(Expression.Bind(outTypeProp, propTypeCvtExp));
                    continue;
                }

                // Invoke ICloneable
                if (outTypeProp.PropertyType.GetInterface("ICloneable") == typeof(ICloneable))
                {
                    Expression<Func<object, object?>> invokeCloneExp = 
                        (obj) => (obj != null) ? (obj as ICloneable).Clone() : null;

                    MemberExpression tInMemberExp = Expression.MakeMemberAccess(_paramExpTIn, inTypeProp);
                    // The input type convert
                    UnaryExpression tInMemberCvtExp = Expression.Convert(tInMemberExp, typeof(object)); 
                    InvocationExpression invokeExp = Expression.Invoke(invokeCloneExp, tInMemberCvtExp);
                    // Return type convert
                    UnaryExpression invokeResultCvtExp = Expression.Convert(invokeExp, outTypeProp.PropertyType);  

                    _autoMapGetValueExp.Add((outTypeProp, invokeResultCvtExp));
                    _autoMapMemberBindings.Add(Expression.Bind(outTypeProp, invokeResultCvtExp));

                    continue;
                }

                // Preserve references
                if (true)  
                {
                    Expression<Func<object, Dictionary<int, object>, object?>> getPreserveReferencesExp =
                        (tPrObj, reuseSet) => (tPrObj != null) ? Mapper.CloneMap(tPrObj, reuseSet) : null;

                    Expression memberExp = Expression.MakeMemberAccess(_paramExpTIn, inTypeProp);
                    InvocationExpression invokeExp = Expression.Invoke(getPreserveReferencesExp, new Expression[]
                    {
                        memberExp,
                        _paramExpReuseSet,
                    });
                    UnaryExpression resultTypeCvtExp = Expression.Convert(invokeExp, outTypeProp.PropertyType);

                    // Only preserve references
                    _autoMapGetValueExp.Add((outTypeProp, resultTypeCvtExp));

                    continue;
                }
            }
        }

        public void SetMapper<TMember>(Expression<Func<TIn, TMember>> getValueFunc,
            Expression<Func<TOut, TMember>> setValueExp)
        {
            if(_paramExpTIn == null)
                _paramExpTIn = Expression.Parameter(typeof(TIn), "inObj");

            if (setValueExp.Body is MemberExpression memberExp)
            {
                PropertyInfo? p = typeof(TOut).GetProperty(memberExp.Member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.CanRead)
                {
                    AddMemberBinding(p, getValueFunc);
                    return;
                }

                FieldInfo? f = typeof(TOut).GetField(memberExp.Member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (f != null)
                {
                    AddMemberBinding(f, getValueFunc);
                    return;
                }
            }
            else if(setValueExp.Body is UnaryExpression uexp && uexp.Operand is MemberExpression memberExp2)
            {
                AddMemberBinding(memberExp2.Member, getValueFunc);
                return;
            }
            else 
                throw new InvalidOperationException("Unsupported setting member " + setValueExp.ToString());

        }

        private void AddMemberBinding(MemberInfo member, Expression getValueFunc)
        {
            InvocationExpression invokeFunc = Expression.Invoke(getValueFunc, _paramExpTIn);
            MemberBinding bind = Expression.Bind(member, invokeFunc);
            _memberBindings.Add(bind);
        }

        public void Complie()
        {
            AutoMapper();
            PreserveReferencesComplie();

            Dictionary<string, bool> filterMemberDict = 
                new Dictionary<string, bool>(_memberBindings.Select(mb => KeyValuePair.Create(mb.Member.Name, true)));
            _memberBindings.AddRange(
                _autoMapMemberBindings
                .Where(mb => !filterMemberDict.ContainsKey(mb.Member.Name))
                );

            MemberInitExpression createAndSetExp = Expression.MemberInit(Expression.New(typeof(TOut)), _memberBindings);
            Expression<Func<TIn, TOut>> lambda = Expression.Lambda<Func<TIn, TOut>>(
                createAndSetExp,
                new ParameterExpression[] { _paramExpTIn });
            _createAndSetFunc = lambda.Compile();

            _compiled = true;
        }

        private void PreserveReferencesComplie()
        {
            MemberInitExpression createAndSetExp = Expression.MemberInit(Expression.New(typeof(TOut)), _memberBindings);
            Expression<Func<TIn, TOut>> createAndSetLambda = Expression.Lambda<Func<TIn, TOut>>(
                createAndSetExp,
                new ParameterExpression[] { _paramExpTIn });
            _preserveReferencesCreateAndSetFunc = createAndSetLambda.Compile();

            Dictionary<string, bool> filterMemberDict = 
                new Dictionary<string, bool>(_memberBindings.Select(mb => KeyValuePair.Create(mb.Member.Name, true)));

            List<Expression> assignExps = new List<Expression>(
                _autoMapGetValueExp
                .Where(pair => !filterMemberDict.ContainsKey(pair.Item1.Name))
                .Select(pair =>
                {
                    var (outTypeProp, getValueExp) = pair;
                    MemberExpression outPropExp = Expression.Property(_paramExpTOut, outTypeProp);
                    return Expression.Assign(outPropExp, getValueExp);
                })
                );

            BlockExpression blockExp = Expression.Block(assignExps);
            Expression<Action<TIn, TOut, Dictionary<int, object>>> setLambda = Expression.Lambda<Action<TIn, TOut, Dictionary<int, object>>>(
                blockExp, 
                new ParameterExpression[] { _paramExpTIn, _paramExpTOut, _paramExpReuseSet});
            _preserveReferencesSetAction = setLambda.Compile();
        }

        public TOut Map(TIn sourceObj)
        {
            return _createAndSetFunc(sourceObj);
        }

        public object? Map(object sourceObj)
        {
            if (!(sourceObj is TIn obj))
                throw new InvalidOperationException($"Illegal operation requires type {typeof(TIn)} but the object is type {sourceObj.GetType()}");
            return _createAndSetFunc(obj);
        }

        public void UsePreserveReferences()
        {
            _isPreserveReferences = true;
        }

        public object? PreserveReferencesMap(object sourceObj, Dictionary<int, object> reuseSet)
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

                _preserveReferencesSetAction.Invoke(inObj, outObj, reuseSet);
            }

            return outObj;
        }
    }
}
