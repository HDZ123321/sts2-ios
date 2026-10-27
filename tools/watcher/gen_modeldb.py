"""Batch 6: make ModelDb lookups self-healing so a model constructor can
reference another model that has not been constructed yet.

The ported Watcher card Alpha asks for Beta's hover tip inside its own
constructor (HoverTipFactory.FromCardWithCardHoverTips<Beta>). Vanilla code
never does that, so ModelDb.Init() only worked when the subtype array happened to
list Beta before Alpha. Now a miss inside Init resolves the type on demand.
"""

import io
import os
import sys

PATH = r'E:\sts2_port\ci_bundle\src\Core\Models\ModelDb.cs'

FIELDS_OLD = '''	private static Dictionary<ModelId, Type> _typeById;
'''
# inserted after the _contentById field declaration


def main():
    with io.open(PATH, 'r', encoding='utf-8-sig', newline='') as f:
        text = f.read()
    nl = '\r\n' if '\r\n' in text else '\n'

    sentinel = 'EnsureTypeIndex'
    if sentinel in text:
        print('already patched')
        return

    def sub(old, new, count=1):
        nonlocal text
        old_t = old.replace('\n', nl)
        new_t = new.replace('\n', nl)
        if old_t not in text:
            print('MISS: %r' % old_t[:100])
            sys.exit(1)
        text = text.replace(old_t, new_t, count)

    # 1. Init(): do not rebuild a model that a constructor already pulled in.
    sub('''	public static void Init()
	{
		Type[] allAbstractModelSubtypes = AllAbstractModelSubtypes;
		foreach (Type type in allAbstractModelSubtypes)
		{
			ModelId id = GetId(type);
			AbstractModel value = (AbstractModel)Activator.CreateInstance(type);
			_contentById[id] = value;
		}
	}''',
        '''	public static void Init()
	{
		Type[] allAbstractModelSubtypes = AllAbstractModelSubtypes;
		foreach (Type type in allAbstractModelSubtypes)
		{
			ModelId id = GetId(type);
			if (_contentById.ContainsKey(id))
			{
				// A constructor earlier in this loop already pulled the type in through
				// GetById (see TryResolveMissing); rebuilding it would trip the
				// DuplicateModelException guard in AbstractModel's constructor.
				continue;
			}
			AbstractModel value = (AbstractModel)Activator.CreateInstance(type);
			_contentById[id] = value;
		}
	}''')

    # 2. GetByIdOrNull(): resolve a missing id on demand.
    sub('''	public static T? GetByIdOrNull<T>(ModelId id) where T : AbstractModel
	{
		if (_contentById.TryGetValue(id, out AbstractModel value))
		{
			return (T)value;
		}
		return null;
	}''',
        '''	public static T? GetByIdOrNull<T>(ModelId id) where T : AbstractModel
	{
		if (_contentById.TryGetValue(id, out AbstractModel value))
		{
			return (T)value;
		}
		if (TryResolveMissing(id) && _contentById.TryGetValue(id, out AbstractModel value2))
		{
			return (T)value2;
		}
		return null;
	}''')

    # 3. Get<T>(): go through GetById so it benefits from the same resolution.
    sub('''	private static T Get<T>() where T : AbstractModel
	{
		return (T)_contentById[GetId<T>()];
	}''',
        '''	private static T Get<T>() where T : AbstractModel
	{
		return GetById<T>(GetId<T>());
	}''')

    # 4. The resolver itself, next to Init.
    sub('''	/// <summary>
	/// Injects a model into the ModelDb. Should only be used in tests and mods.
	/// </summary>''',
        '''	/// <summary>
	/// Constructs and registers the model behind <paramref name="id"/> if that type is a known subtype
	/// but has not been built yet. Model constructors may query other models (the ported Watcher card
	/// Alpha does, for Beta's hover tip), and the subtype table order does not guarantee that the
	/// other model exists at that point.
	/// </summary>
	private static bool TryResolveMissing(ModelId id)
	{
		EnsureTypeIndex();
		if (!_typeById.TryGetValue(id, out Type value) || _resolving.Contains(id))
		{
			return false;
		}
		_resolving.Add(id);
		try
		{
			Inject(value);
		}
		finally
		{
			_resolving.Remove(id);
		}
		return _contentById.ContainsKey(id);
	}

	private static void EnsureTypeIndex()
	{
		if (_typeById != null)
		{
			return;
		}
		Dictionary<ModelId, Type> dictionary = new Dictionary<ModelId, Type>();
		Type[] allAbstractModelSubtypes = AllAbstractModelSubtypes;
		foreach (Type type in allAbstractModelSubtypes)
		{
			dictionary[GetId(type)] = type;
		}
		_typeById = dictionary;
	}

	/// <summary>
	/// Injects a model into the ModelDb. Should only be used in tests and mods.
	/// </summary>''')

    # 5. Fields for the index and the reentrancy guard, next to _contentById.
    marker = 'private static readonly Dictionary<ModelId, AbstractModel> _contentById'
    idx = text.find(marker)
    if idx < 0:
        print('MISS: _contentById field')
        sys.exit(1)
    line_end = text.find(nl, idx)
    insert = (nl + nl +
              '\tprivate static Dictionary<ModelId, Type> _typeById;' + nl + nl +
              '\tprivate static readonly HashSet<ModelId> _resolving = new HashSet<ModelId>();')
    text = text[:line_end] + insert + text[line_end:]

    with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
        f.write(text)
    print('patched', PATH)


if __name__ == '__main__':
    main()
