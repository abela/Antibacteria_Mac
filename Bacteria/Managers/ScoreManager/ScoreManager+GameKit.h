//
//  ScoreManager+GameKit.h
//  AntiBacteria
//
//  Category to isolate GameKit dependencies
//

#import "ScoreManager.h"

@interface ScoreManager (GameKit)

- (void) authenticateLocalPlayer;
- (void) showGameCenterOnWindow:(NSWindow*)window;

@end