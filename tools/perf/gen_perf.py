"""Add boot-phase marks and let --perf drive the automated run harness."""

import io
import os
import sys

ROOT = r'E:\sts2_port\ci_bundle'
CORE = os.path.join(ROOT, 'src', 'Core')


def read(p):
    with io.open(p, 'r', encoding='utf-8-sig', newline='') as f:
        return f.read()


def write(p, t):
    with io.open(p, 'w', encoding='utf-8', newline='') as f:
        f.write(t)


def patch(path, marker, old, new, count=1):
    text = read(path)
    if marker in text:
        print('  already patched %s (%s)' % (os.path.basename(path), marker))
        return True
    nl = '\r\n' if '\r\n' in text else '\n'
    old_t, new_t = old.replace('\n', nl), new.replace('\n', nl)
    if old_t not in text:
        print('  MISS %s\n    %r' % (path, old_t[:150]))
        return False
    write(path, text.replace(old_t, new_t, count))
    print('  patched %s (%s)' % (os.path.basename(path), marker))
    return True


def main():
    ok = True
    game = os.path.join(CORE, 'Nodes', 'NGame.cs')

    # Marks bracket every step of the startup sequence, so each line reports the cost of
    # the step it closes.
    ok &= patch(game, 'Perf.Mark("scene ready")',
                '''		await OneTimeInitialization.ExecuteVeryEarly();
		Node child = NDevConsole.Create();''',
                '''		Perf.Mark("start");
		await OneTimeInitialization.ExecuteVeryEarly();
		Perf.Mark("very early init");
		Node child = NDevConsole.Create();''')

    ok &= patch(game, 'Perf.Mark("dev console")',
                '''		GdModHost.Install(this);''',
                '''		Perf.Mark("dev console");
		GdModHost.Install(this);
		Perf.Mark("script host");''')

    ok &= patch(game, 'Perf.Mark("pools")',
                '''		InitPools();
		OneTimeInitialization.ExecuteEssential();''',
                '''		Perf.Mark("save migration and cloud sync kickoff");
		InitPools();
		Perf.Mark("ui pools");
		OneTimeInitialization.ExecuteEssential();
		Perf.Mark("essential init");''')

    ok &= patch(game, 'Perf.Mark("node ready")',
                '''		Callable.From(InitializeGraphicsPreferences).CallDeferred();''',
                '''		Perf.Mark("scene tree ready");
		Callable.From(InitializeGraphicsPreferences).CallDeferred();''')

    ok &= patch(game, 'Perf.Mark("cloud sync wait")',
                '''		SaveManager.Instance.InitProfileId();''',
                '''		Perf.Mark("services and cloud sync");
		SaveManager.Instance.InitProfileId();''')

    ok &= patch(game, 'Perf.Mark("save data")',
                '''		_screenShake.SetMultiplier(NScreenshakePaginator.GetShakeMultiplier(SaveManager.Instance.PrefsSave.ScreenShakeOptionIndex));''',
                '''		Perf.Mark("save data");
		Perf.StartFrameWatch();
		_screenShake.SetMultiplier(NScreenshakePaginator.GetShakeMultiplier(SaveManager.Instance.PrefsSave.ScreenShakeOptionIndex));''')

    # The harness is release-gated; --perf is a developer flag, and iOS builds never
    # receive command line arguments, so this cannot fire on a shipped device.
    ok &= patch(game, '|| Perf.Enabled',
                '''		if (!IsReleaseGame() && CommandLineHelper.HasArg("autoslay"))''',
                '''		if ((!IsReleaseGame() || Perf.Enabled) && CommandLineHelper.HasArg("autoslay"))''')

    # Deferred work runs after the menu appears; it is part of the felt startup cost.
    ok &= patch(os.path.join(CORE, 'Helpers', 'OneTimeInitialization.cs'),
                'Perf.Mark("deferred")',
                '''		AtlasManager.LoadAllAtlases();''',
                '''		Perf.Mark("deferred start");
		AtlasManager.LoadAllAtlases();
		Perf.Mark("all atlases");''')

    ok &= patch(os.path.join(CORE, 'Helpers', 'OneTimeInitialization.cs'),
                'Perf.Mark("preload")',
                '''			PrewarmJit();''',
                '''			Perf.Mark("model preload");
			PrewarmJit();
			Perf.Mark("jit prewarm");''')

    if not ok:
        sys.exit(1)
    print('perf instrumentation applied')


if __name__ == '__main__':
    main()
