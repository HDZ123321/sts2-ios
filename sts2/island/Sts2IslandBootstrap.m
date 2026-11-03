#import <Foundation/Foundation.h>

// Runs before main(), which is why the island poller is already scheduling by the time Godot
// boots. The call itself only enqueues work on a background queue, so the early callback does not
// touch ActivityKit (or even the file system) inside +load.
//
// sts2/dummy.cpp calls the same entry point from godot_apple_embedded_plugins_initialize() as a
// second trigger; both are idempotent.
extern void sts2_island_bootstrap(void);

@interface Sts2IslandBootstrap : NSObject
@end

@implementation Sts2IslandBootstrap

+ (void)load {
	sts2_island_bootstrap();
}

@end
