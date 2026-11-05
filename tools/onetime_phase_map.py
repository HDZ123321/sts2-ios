import io

NL = chr(92) + "n"


def patch(path, old, new, needs_using=None, expect=1):
    s = io.open(path, encoding='utf-8').read()
    n = s.count(old)
    if n != expect:
        raise SystemExit('count=%d in %s for %r' % (n, path, old[:60]))
    s = s.replace(old, new, expect)
    if needs_using and needs_using not in s:
        anchor = 'using MegaCrit.Sts2.Core.Helpers;'
        if anchor not in s:
            raise SystemExit('no anchor in %s' % path)
        s = s.replace(anchor, needs_using + NL + anchor, 1)
    io.open(path, 'w', encoding='utf-8', newline=NL).write(s)
    print('patched', path)


# The map -> room transition was the one stretch with no phase label: a hitch while the
# map scrolled sat under whatever label the previous room left behind.
patch(
    'src/Core/GameActions/MoveToMapCoordAction.cs',
    """		else
		{
			await NMapScreen.Instance.TravelToMapCoord(_destination);
		}""",
    """		else
		{
			Perf.Phase("map.travel");
			await NMapScreen.Instance.TravelToMapCoord(_destination);
			Perf.Phase("map.arrive");
		}""",
    needs_using='using MegaCrit.Sts2.Core.Debug;',
)
