using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ValidationGuard;

// Turns an accessor such as x => x.Lines[2].Sku into its path fragments. Indices and keys are evaluated here, once.
internal static class AccessorFragmentResolver
{
    // The accessor's own fragments, root first, in a single exactly-sized array (empty, without allocating, for the
    // parameter itself). Builders render them after the prefix of the scope they were resolved in.
    public static PathFragment[] ResolveFragments<TContext, TValue>(Expression<Func<TContext, TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        return ResolveFragments(accessor.Body);
    }

    private static PathFragment[] ResolveFragments(Expression body)
    {
        var count = CountFragments(body);
        if (count == 0)
        {
            return [];
        }

        var fragments = new PathFragment[count];
        var filled = Fill(body, fragments, 0);
        System.Diagnostics.Debug.Assert(filled == fragments.Length);
        return fragments;
    }

    // Structure only: nothing is evaluated here. Structural checks are made in the same order as Fill makes them, so
    // an unsupported accessor fails with the same exception either way.
    private static int CountFragments(Expression expression)
    {
        expression = StripConvert(expression);

        switch (expression)
        {
            case ParameterExpression:
                return 0;
            case MemberExpression member:
                return 1 + CountFragments(member.Expression!);
            case IndexExpression index:
                var objectCount = CountFragments(index.Object!);
                if (index.Arguments.Count != 1)
                {
                    throw new NotSupportedException("Only single-argument indexers are supported.");
                }

                return 1 + objectCount;
            case MethodCallExpression { Method.Name: "get_Item" } call:
                if (call.Arguments.Count != 1)
                {
                    throw new NotSupportedException("Only single-argument indexers are supported.");
                }

                return 1 + (call.Object is null ? 0 : CountFragments(call.Object));
            case BinaryExpression { NodeType: ExpressionType.ArrayIndex } binary:
                return 1 + CountFragments(binary.Left);
            default:
                throw new NotSupportedException($"Unsupported accessor expression: {expression}");
        }
    }

    // Fills ancestors first, so indexer arguments are evaluated root to leaf, starting at start (after any prefix);
    // returns the next free position. Only the shapes CountFragments accepted reach here: it runs first and throws for
    // any other accessor, so the cases below need no checks of their own.
    private static int Fill(Expression expression, PathFragment[] fragments, int start)
    {
        expression = StripConvert(expression);

        switch (expression)
        {
            case ParameterExpression:
                return start;
            case MemberExpression member:
            {
                var position = Fill(member.Expression!, fragments, start);
                fragments[position] = new PathFragment(PathFragmentKind.Member, member.Member.Name);
                return position + 1;
            }
            case IndexExpression index:
            {
                var position = Fill(index.Object!, fragments, start);
                fragments[position] = IndexerFragment(index.Arguments[0]);
                return position + 1;
            }
            case MethodCallExpression call:
            {
                // A get_Item call: CountFragments rejects any other method.
                var position = call.Object is null ? start : Fill(call.Object, fragments, start);
                fragments[position] = IndexerFragment(call.Arguments[0]);
                return position + 1;
            }
            default:
            {
                // An array element: the only remaining shape CountFragments accepts.
                var binary = (BinaryExpression)expression;
                var position = Fill(binary.Left, fragments, start);
                fragments[position] = new PathFragment(PathFragmentKind.Index, EvaluateArrayIndexSegment(binary.Right));
                return position + 1;
            }
        }
    }

    // Integral indexer arguments are array indices (validated as RFC 6901 index tokens); others are keys.
    private static PathFragment IndexerFragment(Expression argument) =>
        IsIntegralType(argument.Type)
            ? new PathFragment(PathFragmentKind.Index, EvaluateArrayIndexSegment(argument))
            : new PathFragment(PathFragmentKind.Key, EvaluateSegment(argument));

    private static string EvaluateArrayIndexSegment(Expression expression)
    {
        var raw = EvaluateSegment(expression);

        if (raw.Length == 0)
        {
            throw new ArgumentException("RFC 6901 array index token cannot be empty.", nameof(expression));
        }

        return IsRfc6901ArrayIndex(raw) ? raw : throw new ArgumentException($"Invalid RFC 6901 array index token '{raw}'.", nameof(expression));

    }

    private static bool IsRfc6901ArrayIndex(string token)
    {
        return token.Length > 0 && token.All(ch => ch is >= '0' and <= '9');
    }

    private static bool IsIntegralType(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        return effectiveType == typeof(byte)
               || effectiveType == typeof(sbyte)
               || effectiveType == typeof(short)
               || effectiveType == typeof(ushort)
               || effectiveType == typeof(int)
               || effectiveType == typeof(uint)
               || effectiveType == typeof(long)
               || effectiveType == typeof(ulong);
    }

    private static string EvaluateSegment(Expression expression)
    {
        expression = StripConvert(expression);

        if (expression is ConstantExpression constant)
        {
            return ConvertToSegmentString(constant.Value);
        }

        if (expression is MemberExpression member && TryEvaluateMember(member, out var value))
        {
            return ConvertToSegmentString(value);
        }

        var lambda = Expression.Lambda(expression).Compile();
        var result = lambda.DynamicInvoke();
        return ConvertToSegmentString(result);
    }

    private static bool TryEvaluateMember(MemberExpression member, out object? value)
    {
        var target = member.Expression is null ? null : TryEvaluateTarget(member.Expression);
        if (member.Member is FieldInfo field)
        {
            value = field.GetValue(target);
            return true;
        }

        var property = (PropertyInfo)member.Member;
        value = property.GetValue(target);
        return true;
    }

    private static object? TryEvaluateTarget(Expression expression)
    {
        expression = StripConvert(expression);
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        var lambda = Expression.Lambda(expression).Compile();
        return lambda.DynamicInvoke();
    }

    private static string ConvertToSegmentString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
