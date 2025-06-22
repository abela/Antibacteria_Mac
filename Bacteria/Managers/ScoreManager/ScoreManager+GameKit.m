//
//  ScoreManager+GameKit.m
//  AntiBacteria
//
//  Category to isolate GameKit dependencies
//

#import "ScoreManager+GameKit.h"
#import <GameKit/GameKit.h>

@implementation ScoreManager (GameKit)

- (void) authenticateLocalPlayer
{
    GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
    
    localPlayer.authenticateHandler = ^(NSViewController *viewController, NSError *error){
        if (viewController != nil)
        {
            //showAuthenticationDialogWhenReasonable: is an example method name. Create your own method that displays an authentication view when appropriate for your app.
            //[self showAuthenticationDialogWhenReasonable: viewController];
        }
        else if (localPlayer.isAuthenticated)
        {
            //authenticatedPlayer: is an example method name. Create your own method that is called after the local player is authenticated.
            //[self authenticatedPlayer: localPlayer];
        }
        else
        {
            //[self disableGameCenter];
        }
    };
}

-(void) showGameCenterOnWindow:(NSWindow*)window
{
    GKGameCenterViewController *gameCenterController = [[GKGameCenterViewController alloc] init];
    if (gameCenterController != nil)
    {
        GKDialogController *sdc = [GKDialogController sharedDialogController];
        sdc.parentWindow = window;
        [sdc presentViewController: gameCenterController];
    }
}

-(void) leaderboardViewControllerDidFinish:(GKLeaderboardViewController *)viewController
{
    
}

-(void) achievementViewControllerDidFinish:(GKAchievementViewController *)viewController
{
    
}

@end