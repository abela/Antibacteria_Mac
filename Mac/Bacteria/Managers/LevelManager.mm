//
//  LevelManager.mm
//  Game
//
//  Created by Giorgi Abelashvili on 1/7/12.
//  Copyright 2012 __MyCompanyName__. All rights reserved.
//

#import "LevelManager.h"
#import "Level.h"
#import "GameManager.h"
#import "Utils.h"
//
#define LEVEL_IS_UNLOCKED           @"levelisunlocked"
//
@interface LevelManager (PrivateMethods)
-(NSDictionary*)getLevelDataWithNumber:(int) number;
-(void) saveUnlockLevel;
@end

@implementation LevelManager
//
@synthesize levelCounter;
@synthesize currentLevel;
@synthesize totalEnemiesPerCreation;
@synthesize totalLevelTime;
//
static LevelManager *_sharedLevelManager = nil;
//
+(LevelManager*)sharedLevelManager
{
	@synchronized([LevelManager class])
	{
		if(!_sharedLevelManager)
			_sharedLevelManager = [[LevelManager alloc] init];
		return _sharedLevelManager;
	}
}
//
-(id) init
{
    if((self = [super init]))
    {
        //
        levelManageTickCounter = 0;
        totalEnemiesPerCreation = 1;
        levelCounter = 1;
        [self unlockLevel:1];
        //
        return self;
    }
    return nil;
}
//
-(NSDictionary*)getLevelDataWithNumber:(int) number
{
	NSString *levelPath = @"level_";
    levelPath = [levelPath stringByAppendingString:[NSString stringWithFormat:@"%d",number]];
    //
	NSString *path = [[NSBundle mainBundle] pathForResource:levelPath ofType:@"plist"];
	NSDictionary* plistDictionary = [NSDictionary dictionaryWithContentsOfFile:path];
	//
	return plistDictionary;
    //
}
//
-(void) loadNextLevel
{
    //
    levelManageTickCounter = 0;
    totalEnemiesPerCreation = 1;
    currentLevel = [[Level alloc] initWithData:[self getLevelDataWithNumber:levelCounter]];
    currentLevel.unlocked = YES;
    totalLevelTime = (int)[currentLevel time];
    [self saveUnlockLevel];
    levelCounter++;
    //
}
//
-(void) saveUnlockLevel
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSString *levelStrings = LEVEL_IS_UNLOCKED;
    levelStrings = [levelStrings stringByAppendingString:[NSString stringWithFormat:@"%d",levelCounter]];
    levelStrings = [[Utils sharedUtils] hashString:levelStrings withSalt:levelStrings];
    //
    NSString *unlockString = @"YES";
    unlockString = [[Utils sharedUtils] hashString:unlockString withSalt:unlockString];
    //
    [defaults setSecureObject:unlockString forKey:levelStrings];
    [defaults synchronize];
}
//
-(void) unlockLevel:(int) levelNumber
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSString *levelStrings = LEVEL_IS_UNLOCKED;
    levelStrings = [levelStrings stringByAppendingString:[NSString stringWithFormat:@"%d",levelNumber]];
    levelStrings = [[Utils sharedUtils] hashString:levelStrings withSalt:levelStrings];
    //
    NSString *unlockString = @"YES";
    unlockString = [[Utils sharedUtils] hashString:unlockString withSalt:unlockString];
    //
    [defaults setSecureObject:unlockString forKey:levelStrings];
    [defaults synchronize];
    //
}
//
-(BOOL) levelIsUnlocked:(int) levelNumber
{
    //
    //
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSString *levelStrings = LEVEL_IS_UNLOCKED;
    levelStrings = [levelStrings stringByAppendingString:[NSString stringWithFormat:@"%d",levelNumber]];
    levelStrings = [[Utils sharedUtils] hashString:levelStrings withSalt:levelStrings];
    BOOL valid = NO;
    NSString *value = [defaults secureObjectForKey:levelStrings valid:&valid];
    return (value!=nil) ? YES : NO;
    //
    //
}
//
-(int) getNextRandomEnemyType
{
    //
    int neneimesCountInLevel = (int)[[currentLevel levelEnemies] count];
    return [[currentLevel.levelEnemies objectAtIndex:(arc4random_uniform(neneimesCountInLevel))] intValue];
    //
}
//
-(void) restartLevelManager
{
    levelCounter--;
    levelManageTickCounter = 0;
    totalEnemiesPerCreation = 1;
    totalLevelTime = totalLevelTime = (int)[currentLevel time];
    [self loadNextLevel];
}
//
-(void) resetLevelManager
{
    levelCounter = 1;
    levelManageTickCounter = 0;
    totalEnemiesPerCreation = 1;
}
//
-(void) update
{
    //
    if([[GameManager sharedGameManager] gameIsPaused] == NO||
       [[GameManager sharedGameManager] levelHasWon] == NO)
    {
        levelManageTickCounter+=([[GameManager sharedGameManager] bulletinTimeIsOn]) ?
        GAME_NEXT_SIMULATION_STEP_INTERVAL/2
        : GAME_NEXT_SIMULATION_STEP_INTERVAL;
        //
        if(levelManageTickCounter >= currentLevel.enemiesCountGrowConst && currentLevel.enemiesCountGrowConst!=0)
        {
            //
            //NSLog(@"total enemies per level before = %d", totalEnemiesPerCreation);
            //
            levelManageTickCounter = 0;
            if(totalEnemiesPerCreation >= currentLevel.maximumenemies)
                totalEnemiesPerCreation = currentLevel.maximumenemies;
            else totalEnemiesPerCreation++;
            //
            //NSLog(@"total enemies per level after = %d", totalEnemiesPerCreation);
            //
        }
        //
    }
    //
}
//
@end
