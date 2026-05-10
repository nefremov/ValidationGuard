using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ValidationGuard;

public static class ValidationAccessorPointerResolver
{
    private static readonly IPathFragmentNameConverter s_defaultNameConverter = PascalCaseNameConverter.Instance;

    public static string Resolve<TContext, TValue>(
        Expression<Func<TContext, TValue>> accessor,
        IPathFragmentNameConverter? nameConverter = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        var effectiveNameConverter = nameConverter ?? s_defaultNameConverter;
        return ResolvePointer(accessor.Body, effectiveNameConverter);
    }

    private static string ResolvePointer(Expression expression, IPathFragmentNameConverter nameConverter)
    {
        expression = StripConvert(expression);

        return expression switch
        {
            ParameterExpression => string.Empty,
            MemberExpression member => CombinePointerWithSegment(
                ResolvePointer(member.Expression!, nameConverter),
                member.Member.Name,
                nameConverter),
            IndexExpression index => ResolveIndexExpression(index, nameConverter),
            MethodCallExpression { Method.Name: "get_Item" } call => ResolveGetItemExpression(call, nameConverter),
            BinaryExpression { NodeType: ExpressionType.ArrayIndex } binary => ResolveArrayIndexExpression(binary, nameConverter),
            _ => throw new NotSupportedException($"Unsupported accessor expression: {expression}")
        };
    }

    private static string ResolveIndexExpression(IndexExpression index, IPathFragmentNameConverter nameConverter)
    {
        var objectPointer = ResolvePointer(index.Object!, nameConverter);
        if (index.Arguments.Count != 1)
        {
            throw new NotSupportedException("Only single-argument indexers are supported.");
        }

        var argument = index.Arguments[0];
        var segmentValue = IsIntegralType(argument.Type)
            ? EvaluateArrayIndexSegment(argument)
            : EvaluateSegment(argument);
        return CombinePointerWithSegment(objectPointer, segmentValue);
    }

    private static string ResolveGetItemExpression(MethodCallExpression call, IPathFragmentNameConverter nameConverter)
    {
        // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-resolve-indexer-get-item
        if (call.Arguments.Count != 1)
        {
            throw new NotSupportedException("Only single-argument indexers are supported.");
        }

        var objectPointer = call.Object is null ? string.Empty : ResolvePointer(call.Object, nameConverter);
        var argument = call.Arguments[0];
        var segmentValue = IsIntegralType(argument.Type)
            ? EvaluateArrayIndexSegment(argument)
            : EvaluateSegment(argument);
        return CombinePointerWithSegment(objectPointer, segmentValue);
        // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-resolve-indexer-get-item
    }

    private static string ResolveArrayIndexExpression(BinaryExpression binary, IPathFragmentNameConverter nameConverter)
    {
        var arrayPointer = ResolvePointer(binary.Left, nameConverter);
        return CombinePointerWithSegment(arrayPointer, EvaluateArrayIndexSegment(binary.Right));
    }

    private static string EvaluateArrayIndexSegment(Expression expression)
    {
        // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-validate-array-index-token-rfc6901
        var raw = EvaluateSegment(expression);

        if (raw.Length == 0)
        {
            throw new ArgumentException("RFC 6901 array index token cannot be empty.", nameof(expression));
        }

        return IsRfc6901ArrayIndex(raw) ? raw : throw new ArgumentException($"Invalid RFC 6901 array index token '{raw}'.", nameof(expression));

        // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-validate-array-index-token-rfc6901
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

    private static string CombinePointerWithSegment(
        string prefix,
        string segment,
        IPathFragmentNameConverter? nameConverter = null)
    {
        var convertedLength = GetConvertedLength(segment, nameConverter);

        if (convertedLength == 0)
        {
            return prefix;
        }

        var escapingAdditionalLength = GetAdditionalSegmentLength(segment);
        var prefixLength = prefix.Length;
        var resultLength = prefixLength + 1 + convertedLength + escapingAdditionalLength;

        return string.Create(resultLength, (prefix, segment, nameConverter, convertedLength, escapingAdditionalLength), static (span, state) =>
        {
            var writeIndex = 0;

            if (!string.IsNullOrEmpty(state.prefix))
            {
                state.prefix.AsSpan().CopyTo(span);
                writeIndex = state.prefix.Length;
            }

            span[writeIndex++] = '/';

            // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-escape-indexer-token-rfc6901
            var segmentSpan = span[writeIndex..];

            WriteConvertedSegment(state.segment, state.nameConverter, segmentSpan, state.convertedLength);
            EscapeSegment(segmentSpan, state.convertedLength, state.escapingAdditionalLength);
            // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-escape-indexer-token-rfc6901
        });
    }

    private static int GetConvertedLength(string segment, IPathFragmentNameConverter? converter)
    {
        return converter?.GetConvertedLength(segment) ?? segment.Length;
    }

    private static void WriteConvertedSegment(
        string segment,
        IPathFragmentNameConverter? converter,
        Span<char> destination,
        int convertedLength)
    {
        if (converter is null)
        {
            segment.AsSpan().CopyTo(destination);
            return;
        }

        converter.WriteConverted(segment, destination[..convertedLength]);
    }

    private static void EscapeSegment(Span<char> segmentSpan, int convertedLength, int escapingAdditionalLength)
    {
        if (escapingAdditionalLength == 0)
        {
            return;
        }

        var readIndex = convertedLength - 1;
        var writeIndex = convertedLength + escapingAdditionalLength - 1;

        while (readIndex >= 0)
        {
            var ch = segmentSpan[readIndex--];
            if (ch == '~')
            {
                segmentSpan[writeIndex--] = '0';
                segmentSpan[writeIndex--] = '~';
            }
            else if (ch == '/')
            {
                segmentSpan[writeIndex--] = '1';
                segmentSpan[writeIndex--] = '~';
            }
            else
            {
                segmentSpan[writeIndex--] = ch;
            }
        }
    }

    private static int GetAdditionalSegmentLength(ReadOnlySpan<char> segment)
    {
        var additionalLength = 0;
        foreach (var ch in segment)
        {
            if (ch is '~' or '/')
            {
                additionalLength++;
            }
        }

        return additionalLength;
    }
}
