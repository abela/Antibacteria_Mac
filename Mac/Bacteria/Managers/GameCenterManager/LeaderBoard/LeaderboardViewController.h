//
//  LeaderboardViewController.h
//  Turtle
//
//  Created by Giorgi Abelashvili on 5/27/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//
#import <GameKit/GameKit.h>
#import "GameCenterManager.h"


@interface LeaderboardViewController : UIViewController <GKLeaderboardViewControllerDelegate, GKAchievementViewControllerDelegate, GameCenterManagerDelegate>{
    
    GameCenterManager *gameCenterManager;
    //
    GKLeaderboardViewController *gkLeadboardController;
    //
    NSString *currentLeadboarder;
    //
    int64_t currentScore;
    //
}
@property (nonatomic,assign) int64_t currentScore;
@property (nonatomic,retain) GKLeaderboardViewController *gkLeadboardController;
@property (nonatomic,retain) NSString *currentLeadboarder;
- (void) showLeaderboard;
-(void) submitHighScore:(int64_t)score;
@end
