//
//  ScoreManager.h
//  AndroTheFish
//
//  Created by Giorgi Abelashvili on 7/14/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

// GameKit delegate protocols are implemented in ScoreManager+GameKit category

@interface ScoreManager : NSObject
{
    int currentLevelScore;
    int compaignModeHighScore;
}
//
@property (nonatomic,assign) int highScore;
//
+(ScoreManager*)sharedScoreManager;
-(void) saveScore:(int)score forLevel:(int)levelNumber;
-(int) loadScoreForLevel:(int)levelNumber;
- (void) authenticateLocalPlayer;
-(void) showGameCenterOnWindow:(NSWindow*)window;
-(void) saveScoreForCurrentLevel:(int) score;
-(void) synchronizeLevelScore;
-(void) releaseScoreManager;
@end
