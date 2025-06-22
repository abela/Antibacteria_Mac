//
//  GameCenterManager.h
//  AntiBacteria
//
//  Created by jaba odishelashvili on 7/7/13.
//
//

#import <Foundation/Foundation.h>

@protocol GKGameCenterControllerDelegate;

@interface GameCenterManager : NSObject <GKGameCenterControllerDelegate>

+ (GameCenterManager *) sharedManager;

@property (nonatomic, assign) NSWindow *window;
@property (readonly) BOOL isAuthenticated;

- (void) authenticateLocalPlayer;
- (void) loadLeaderboards;
- (void) reportScore: (int64_t) score forLeaderboardID: (NSString*) category;

@end
