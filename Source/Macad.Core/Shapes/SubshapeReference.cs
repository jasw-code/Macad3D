using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Macad.Common;
using Macad.Common.Serialization;

namespace Macad.Core.Shapes;

[SerializeType]
public class SubshapeReference : ISerializeValue, IEquatable<SubshapeReference>
{
    public SubshapeType Type { get; private set; }
    public Guid ShapeId { get; private set; }
    public string Name { get; private set; }
    public int Index { get; private set; }

    /// <summary>
    /// For subshapes whose identity is derived from other subshapes (e.g. the
    /// mirrored copy of an operand face), these reference the source subshapes.
    /// The owning shape interprets Name/Index/Sources on resolution.
    /// </summary>
    public SubshapeReference[] Sources { get; private set; }

    //--------------------------------------------------------------------------------------------------

    SubshapeReference()
    {
        // Used only in deserialization
    }

    //--------------------------------------------------------------------------------------------------

    public SubshapeReference(SubshapeType type, Guid shapeId, int index)
    {
        Type = type;
        ShapeId = shapeId;
        Index = index;
    }

    //--------------------------------------------------------------------------------------------------

    public SubshapeReference(SubshapeType type, Guid shapeId, string name, int index)
    {
        Type = type;
        ShapeId = shapeId;
        Name = name;
        Index = index;
        if (!Name.IsNullOrEmpty())
        {
            Debug.Assert(!Name.Contains("-"));
        }
    }

    //--------------------------------------------------------------------------------------------------

    public SubshapeReference(SubshapeType type, Guid shapeId, string name, int index, SubshapeReference[] sources)
        : this(type, shapeId, name, index)
    {
        Debug.Assert(sources == null || sources.All(src => src != null));
        Sources = sources;
    }

    //--------------------------------------------------------------------------------------------------

    public bool Equals(SubshapeReference other)
    {
        if (other == null || other.Type != Type || !other.ShapeId.Equals(ShapeId) || other.Name != Name || other.Index != Index)
        {
            return false;
        }

        if (Sources == null || Sources.Length == 0)
        {
            return other.Sources == null || other.Sources.Length == 0;
        }

        return other.Sources != null && Sources.SequenceEqual(other.Sources);
    }

    //--------------------------------------------------------------------------------------------------

    public override bool Equals(object obj)
    {
        if (obj is SubshapeReference other)
        {
            return Equals(other);
        }
        return false;
    }

    //--------------------------------------------------------------------------------------------------

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        var hash = ShapeId.GetHashCode() ^ (int)Type ^ Index ^ (Name ?? string.Empty).GetHashCode();
        if (Sources != null)
        {
            foreach (var source in Sources)
            {
                hash ^= source.GetHashCode();
            }
        }
        return hash;
    }

    //--------------------------------------------------------------------------------------------------

    public override string ToString()
    {
        var sb = new StringBuilder();
        switch (Type)
        {
            case SubshapeType.Vertex:
                sb.Append('V');
                break;

            case SubshapeType.Edge:
                sb.Append('E');
                break;

            case SubshapeType.Face:
                sb.Append('F');
                break;

            default:
                return "Invalid";
        }
        sb.Append('-');
        sb.Append(ShapeId.ToString("N"));
        sb.Append('-');
        if (!Name.IsNullOrEmpty())
        {
            sb.Append(Name);
            sb.Append('-');
        }
        sb.Append(Index.ToString());

        if (Sources is { Length: > 0 })
        {
            sb.Append('(');
            for (var i = 0; i < Sources.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('|');
                }
                sb.Append(Sources[i]);
            }
            sb.Append(')');
        }
        return sb.ToString();
    }

    //--------------------------------------------------------------------------------------------------

    public bool Write(Writer writer, SerializationContext context)
    {
        var s = ToString();
        if (s.IsNullOrEmpty() || s == "Invalid")
            return false;

        writer.WriteRawString(s);

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    public bool Read(Reader reader, SerializationContext context)
    {
        return _ParseFromString(reader.ReadValueString(), context);
    }

    //--------------------------------------------------------------------------------------------------

    bool _ParseFromString(string s, SerializationContext context)
    {
        if (s.IsNullOrEmpty())
            return false;

        ReadOnlySpan<char> span = s.AsSpan();

        // Split off the source list: Type-Guid[-Name]-Index(Source|Source)
        int sourcesStart = span.IndexOf('(');
        if (sourcesStart >= 0)
        {
            if (span[^1] != ')')
            {
                return false;
            }

            var sourcesSpan = span.Slice(sourcesStart + 1, span.Length - sourcesStart - 2);
            Sources = _GetSourceListItems(sourcesSpan).Select(sourceString =>
            {
                var source = new SubshapeReference();
                if (!source._ParseFromString(sourceString, context))
                {
                    return null;
                }
                return source;
            }).ToArray();
            if (Sources.Length == 0 || Sources.Contains(null))
                return false;

            span = span.Slice(0, sourcesStart);
        }

        // Parse: Type-Guid[-Name]-Index
        var parts = new List<Range>();
        int lastIndex = 0;

        for (int i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || span[i] == '-')
            {
                parts.Add(lastIndex..i);
                lastIndex = i + 1;
            }
        }

        if (parts.Count is < 3 or > 4)
            return false;

        // Parse Type - check first character only
        ReadOnlySpan<char> typePart = span[parts[0]];
        if (typePart.Length != 1)
            return false;

        switch (typePart[0])
        {
            case 'V':
                Type = SubshapeType.Vertex;
                break;
            case 'E':
                Type = SubshapeType.Edge;
                break;
            case 'F':
                Type = SubshapeType.Face;
                break;
            default:
                return false;
        }

        if (!Guid.TryParse(span[parts[1]], out var guid))
        {
            return false;
        }
        ShapeId = guid;

        if (parts.Count == 4)
        {
            Name = string.Intern(new string(span[parts[2]]));
            Index = int.Parse(span[parts[3]]);
        }
        else
        {
            Index = int.Parse(span[parts[2]]);
        }

        // Safe for later GUID redirection
        context?.GetInstanceList<SubshapeReference>().Add(this);

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    static List<string> _GetSourceListItems(ReadOnlySpan<char> s)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            switch (s[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case '|' when depth == 0:
                    result.Add(s.Slice(start, i - start).ToString());
                    start = i + 1;
                    break;
            }
        }
        result.Add(s.Slice(start).ToString());
        return result;
    }

    //--------------------------------------------------------------------------------------------------

    #region Static Functions

    static SubshapeReference()
    {
        Serializer.Deserialized += _Serializer_Deserialized;
    }

    //--------------------------------------------------------------------------------------------------

    static void _Serializer_Deserialized(object result, SerializationContext context)
    {
        // The GUID changes in certain deserialization cases, so we need to redirect them
        var instances = context?.GetInstanceList<SubshapeReference>();
        var guidMap = context?.GetInstance<Dictionary<Guid, Guid>>();
        if (guidMap == null || instances == null || instances.Count == 0)
        {
            return;
        }

        foreach (var subshapeReference in instances)
        {
            if (guidMap.TryGetValue(subshapeReference.ShapeId, out var newGuid))
            {
                subshapeReference.ShapeId = newGuid;
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}