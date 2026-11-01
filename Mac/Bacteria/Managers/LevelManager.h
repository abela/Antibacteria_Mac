//
//  LevelManager.h
//  Game
//
//  Created by Giorgi Abelashvili on 1/7/12.
//  Copyright 2012 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

@class Level;
@interface LevelManager : NSObject
{
    int levelCounter;
    Level *currentLevel;
    float levelManageTickCounter;
    int totalEnemiesPerCreation;
    int totalLevelTime;
}
//
@property (nonatomic,assign) int totalLevelTime;
@property (readonly) Level *currentLevel;
@property (nonatomic,assign) int levelCounter;
@property (readonly) int totalEnemiesPerCreation;
//
+(LevelManager*)sharedLevelManager;
-(void) loadNextLevel;
-(BOOL) levelIsUnlocked:(int) levelNumber;
-(void) update;
-(int) getNextRandomEnemyType;
-(void) restartLevelManager;
-(void) resetLevelManager;
-(void) unlockLevel:(int) levelNumber;
@end
