//
//  CompaignGameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "CompaignGameMode.h"
#import "AgentsManager.h"
#import "GameResourceManager.h"
#import "LevelManager.h"
#import "Level.h"
#import "GameManager.h"
#import "Utils.h"
#import "BonusManager.h"
#import "MainCharacter.h"
//
#define BONUS_CREATE_LIMIT_CONST        20
//
@interface CompaignGameMode (PrivateMethods)
-(void) startEnemiesAfterSomeTime:(NSTimer*)timer;
@end
//
@implementation CompaignGameMode
//
-(void) Run
{
    firstStart = YES;
    [self loadNextCompaignLevel];
}
//
-(void) loadNextCompaignLevel
{
    //
    mainCharacter.canShoot = YES;
    //
    [[BonusManager sharedBonusManager] releaseBonusManager];
    realTimeTickCounter = 0;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    if(enemyCreateTimer && weHaveBossLevel == NO)
    {
        [enemyCreateTimer invalidate];
        enemyCreateTimer = nil;
    }
    //
    if(realTimeTickVar && weHaveBossLevel == NO)
    {
        [realTimeTickVar invalidate];
        realTimeTickVar = nil;
    }
    //
    [[LevelManager sharedLevelManager] loadNextLevel];
    originalEnemyCreateVar = prevEnemyCreateVar = enemyCreateVar = [[LevelManager sharedLevelManager] currentLevel].enemiescreatetime;
    prevEnemyCreateVar = enemyCreateVar = [[LevelManager sharedLevelManager] currentLevel].enemiescreatetime;
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number]%BOSS_LEVEL_CREATE_CONST == 0)   // we have boss level
        weHaveBossLevel = YES;
    else weHaveBossLevel = NO;
    
    //
    if (firstStart) {
        [NSTimer scheduledTimerWithTimeInterval:enemyCreateVar + 2.0f
                                         target:self selector:@selector(startEnemiesAfterSomeTime:)
                                       userInfo:nil
                                        repeats:NO];
        firstStart = NO;
    }
    
    else {
        //
        enemyCreateTimer = [NSTimer scheduledTimerWithTimeInterval:enemyCreateVar
                                                            target:self selector:@selector(createRandomEnemy:)
                                                          userInfo:nil
                                                           repeats:(!weHaveBossLevel)];
        //
        realTimeTickVar = [NSTimer scheduledTimerWithTimeInterval:1.0f
                                                           target:self
                                                         selector:@selector(realTimeTick:)
                                                         userInfo:nil
                                                          repeats:(!weHaveBossLevel)];
    }
    //
}
//
-(void) startEnemiesAfterSomeTime:(NSTimer*)timer
{
    //
    enemyCreateTimer = [NSTimer scheduledTimerWithTimeInterval:enemyCreateVar
                                                        target:self selector:@selector(createRandomEnemy:)
                                                      userInfo:nil
                                                       repeats:(!weHaveBossLevel)];
    //
    realTimeTickVar = [NSTimer scheduledTimerWithTimeInterval:1.0f
                                                       target:self
                                                     selector:@selector(realTimeTick:)
                                                     userInfo:nil
                                                      repeats:(!weHaveBossLevel)];
}
//
-(void) setMainCharacter:(MainCharacter *)character
{
    mainCharacter = character;
    mainCharacter.canShoot = YES;
}
//
-(void) realTimeTick:(NSTimer*)timer
{
    if([[GameManager sharedGameManager] gameIsPaused] == NO)
    {
        //
        realTimeTickCounter++;
        [[GameManager sharedGameManager] updateTickInGame:[[LevelManager sharedLevelManager] totalLevelTime]];
        [[LevelManager sharedLevelManager] setTotalLevelTime:([[LevelManager sharedLevelManager] totalLevelTime] - 1)];
        //
        // calculate bonus generate time and generate it
        // but we don't have bonuses in boss level :)
        if(realTimeTickCounter%BONUS_CREATE_LIMIT_CONST == 0)
        {
            int levelTime = [[[LevelManager sharedLevelManager] currentLevel] time];
            if(weHaveBossLevel == NO &&
               realTimeTickCounter > 0 &&
               realTimeTickCounter!=levelTime)
            {
                [[BonusManager sharedBonusManager] generateRandomBonus];
            }
        }
    }
}
//
-(void) createRandomEnemy:(NSTimer*)timer
{
    if([[GameManager sharedGameManager] gameIsPaused] == NO)
    {
        for(int i =0; i < [[LevelManager sharedLevelManager] totalEnemiesPerCreation]; i++)
        {
            EnemyAgentType nextEnemyAgentType = (EnemyAgentType)[[LevelManager sharedLevelManager] getNextRandomEnemyType];
            [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0,0) :nextEnemyAgentType];
        }
    }
}
//
-(void) levelHasWon
{
    //
    if(realTimeTickVar!=nil && weHaveBossLevel == NO)
    {
        [realTimeTickVar invalidate];
        realTimeTickVar = nil;
    }
    if(enemyCreateTimer!=nil && weHaveBossLevel == NO)
    {
        [enemyCreateTimer invalidate];
        enemyCreateTimer = nil;
    }
    [[AgentsManager sharedAgentsManager] levelHasWon];
    //
}
-(void) stopGameModedAfterCharacterDeath
{
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    //bc sprite
    // enemy create timer
    if(weHaveBossLevel == NO)
        [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    if(enemyCreateTimer && weHaveBossLevel == NO)
    {
        [enemyCreateTimer invalidate];
        enemyCreateTimer = nil;
    }
    //
    // reset realtimeTick
    if(realTimeTickVar && weHaveBossLevel == NO)
    {
        [realTimeTickVar invalidate];
        realTimeTickVar = nil;
    }
}
-(void) simulateGameModeRestart
{
    realTimeTickCounter = 0.0f;
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    if(enemyCreateTimer && weHaveBossLevel == NO)
    {
        [enemyCreateTimer invalidate];
        enemyCreateTimer = nil;
    }
    //
    if(realTimeTickVar && weHaveBossLevel == NO)
    {
        [realTimeTickVar invalidate];
        realTimeTickVar = nil;
    }
    //
    [[LevelManager sharedLevelManager] restartLevelManager];
    //
    prevEnemyCreateVar = enemyCreateVar = [[LevelManager sharedLevelManager] currentLevel].enemiescreatetime;
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number]%BOSS_LEVEL_CREATE_CONST == 0)   // we have boss level
        weHaveBossLevel = YES;
    //
    enemyCreateTimer = [NSTimer scheduledTimerWithTimeInterval:enemyCreateVar
                                                        target:self selector:@selector(createRandomEnemy:)
                                                      userInfo:nil
                                                       repeats:!weHaveBossLevel];
    //
    realTimeTickVar = [NSTimer scheduledTimerWithTimeInterval:1.0f
                                                       target:self
                                                     selector:@selector(realTimeTick:)
                                                     userInfo:nil
                                                      repeats:!weHaveBossLevel];
    //
}
//
-(void) update
{
    [[AgentsManager sharedAgentsManager] update];
    [[LevelManager sharedLevelManager] update];
}
//
-(void) refreshGameMode
{
    //
    if(weHaveBossLevel == NO)
    {
        enemyCreateVar = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? (enemyCreateVar + 1 - [Utils timeScale]) : prevEnemyCreateVar;
        //
        if(enemyCreateTimer)
        {
            [enemyCreateTimer invalidate];
            enemyCreateTimer = nil;
        }
        enemyCreateTimer = [NSTimer scheduledTimerWithTimeInterval:enemyCreateVar
                                                            target:self selector:@selector(createRandomEnemy:)
                                                          userInfo:nil
                                                           repeats:YES];
        //
        if(realTimeTickVar)
        {
            [realTimeTickVar invalidate];
            realTimeTickVar = nil;
        }
        //
        float tickVal = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? (enemyCreateVar + 1 - [Utils timeScale]) : [Utils timeScale];
        realTimeTickVar = [NSTimer scheduledTimerWithTimeInterval:tickVal
                                                           target:self
                                                         selector:@selector(realTimeTick:)
                                                         userInfo:nil
                                                          repeats:YES];
        //
        
    }
    //
    [[AgentsManager sharedAgentsManager] refreshAgentsManager];
}
//
-(void) releaseGameMode
{
    //
    firstStart = NO;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    if(enemyCreateTimer && weHaveBossLevel == NO)
    {
        [enemyCreateTimer invalidate];
        enemyCreateTimer = nil;
    }
    //
    // reset realtimeTick
    if(realTimeTickVar && weHaveBossLevel == NO)
    {
        [realTimeTickVar invalidate];
        realTimeTickVar = nil;
    }
    //
    [[LevelManager sharedLevelManager] resetLevelManager];
}
//
-(void) gameModeSpecialAction
{
    [self levelHasWon];
}
//
-(void) gameModeActionLoadNextLevel
{
    [self loadNextCompaignLevel];
}
//
@end
