//
//  ScoreManager.m
//  AndroTheFish
//
//  Created by Giorgi Abelashvili on 7/14/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//
//
#import "ScoreManager.h"
#import "PlistAction.h"
#import "LevelManager.h"
//
#define LEVEL_SCORE_KEY         @"level_score_"
#define COMPAIGN_HIGH_SCORE     @"compaign_high_score"
//
@implementation ScoreManager
//
@synthesize highScore;
//
static ScoreManager* _sharedScoreManager = nil;
//
+(ScoreManager*)sharedScoreManager
{
    @synchronized(self)
    {
        if(!_sharedScoreManager)
            _sharedScoreManager = [[ScoreManager alloc] init];
        
        return _sharedScoreManager;
    }
}
//
-(id) init
{
    //
    if((self = [super init]))
    {
        return self;
    }
    else return nil;
    //
}
//
-(void) saveScore:(int)score forLevel:(int)levelNumber
{
    //
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    BOOL valid = NO;
    int prevScoreForLevel = [[defaults secureObjectForKey:[NSString stringWithFormat:@"level_score_%d",levelNumber]
                                                    valid:&valid] intValue];
    //
    if (score > prevScoreForLevel) {
        [defaults setSecureObject:[NSNumber numberWithInt:score]
                           forKey:[NSString stringWithFormat:@"level_score_%d",levelNumber]];
    }
    //
}
//
- (void) authenticateLocalPlayer
{
    GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
    [localPlayer authenticateWithCompletionHandler:^(NSError *error) {
        if (localPlayer.isAuthenticated)
        {
            // Player was successfully authenticated.
            // Perform additional tasks for the authenticated player.
            NSLog(@"authentication success");
        }
        else {
            NSLog(@"authentication fail");
        }
    }];
}
//
-(void) saveScoreForCurrentLevel:(int) score
{
    currentLevelScore+=score;
}
//
-(void) synchronizeLevelScore
{
    //
    BOOL valid = NO;
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    // level counter is incremented, so decrement it for current level and get proper level number
    int currentLevelNumber = [[LevelManager sharedLevelManager] levelCounter] - 1;
    int prevLevelScore = [self loadScoreForLevel:currentLevelNumber];
    compaignModeHighScore = [[defaults secureObjectForKey:COMPAIGN_HIGH_SCORE valid:&valid] intValue];
    //
    if(currentLevelScore > prevLevelScore)
    {
        [defaults setSecureObject:[NSNumber numberWithInt:currentLevelScore]
                           forKey:[NSString stringWithFormat:@"level_%d_score",currentLevelNumber]];
        //
        compaignModeHighScore+=(currentLevelScore - prevLevelScore);
        [defaults setSecureObject:[NSNumber numberWithInt:compaignModeHighScore] forKey:COMPAIGN_HIGH_SCORE];
        [defaults synchronize];
        //
        // upload compaignscore for gamecenter
        //
    }
    currentLevelScore = 0;
}
//
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
//
-(void) leaderboardViewControllerDidFinish:(GKLeaderboardViewController *)viewController
{
    
}
//
-(void) achievementViewControllerDidFinish:(GKAchievementViewController *)viewController
{
    
}
//
-(int) loadScoreForLevel:(int)levelNumber
{
    int score = 0;
    BOOL valid = NO;
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    score = [[defaults secureObjectForKey:[NSString stringWithFormat:@"level_%d_score",levelNumber]
                                    valid:&valid] intValue];
    return score;
}
//
-(void) releaseScoreManager
{
    currentLevelScore = 0;
}
//
@end
