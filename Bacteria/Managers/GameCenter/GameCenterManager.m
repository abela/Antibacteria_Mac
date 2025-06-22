//
//  GameCenterManager.m
//  AntiBacteria
//
//  Created by jaba odishelashvili on 7/7/13.
//
//

#import "GameCenterManager.h"

@implementation GameCenterManager

static GameCenterManager *_sharedGameCenterManager;

@synthesize window          = _window;
@synthesize isAuthenticated = _isAuthenticated;

/** Create GameCenter Singleton Object */
+ (GameCenterManager *) sharedManager
{
    @synchronized([GameCenterManager class])
    {
        if(_sharedGameCenterManager == nil)
            _sharedGameCenterManager = [[GameCenterManager alloc] init];
        return _sharedGameCenterManager;
    }
}

- (void) authenticateLocalPlayer
{
    GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
    [localPlayer authenticateWithCompletionHandler:^(NSError *error)
    {
        if(error == nil)
            _isAuthenticated = localPlayer.isAuthenticated;
    }];
}

- (void) loadLeaderboards
{
    [GKLeaderboard loadLeaderboardsWithCompletionHandler:^(NSArray *leaderboards, NSError *error)
    {
        GKGameCenterViewController *gameCenterController = [[GKGameCenterViewController alloc] init];
        gameCenterController.gameCenterDelegate = self;
        if (gameCenterController != nil)
        {
            GKDialogController *sdc = [GKDialogController sharedDialogController];
            sdc.parentWindow = _window;
            [sdc presentViewController: gameCenterController];
        }
    }];
}

- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController *)gameCenterViewController
{
    GKDialogController *sdc = [GKDialogController sharedDialogController];
    [sdc dismiss: self];
}

- (void) reportScore: (int64_t) score forLeaderboardID: (NSString*) category
{
    GKScore *scoreReporter = [[GKScore alloc] initWithCategory:category];
    scoreReporter.value = score;
    scoreReporter.context = 0;
    
    [scoreReporter reportScoreWithCompletionHandler:^(NSError *error) {
        // Do something interesting here.
    }];
}

@end
