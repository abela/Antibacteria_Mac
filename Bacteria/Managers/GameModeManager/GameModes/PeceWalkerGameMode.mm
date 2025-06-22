//
//  PeceWalkerGameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "PeceWalkerGameMode.h"
#import "cocos2d.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "LevelManager.h"
#import "AgentsManager.h"
#import "GameManager.h"
#import "MainCharacter.h"
#import "BonusManager.h"
#import "InputManager.h"
#import "VitaminManager.h"
//
#define FIRST_ENEMIE_COUNT              2
#define ENEMIES_CREATE_CONST            1
#define LIFE_BONUS_GENERATOR_CONST      8
#define NEXT_FLAG_GENERATOR_CONST       1.0f
#define TOTAL_GAMEMODE_TIME_CONST       10
//
@interface PeceWalkerGameMode(PrivateMethods)
-(void) createFirstEnemies;
-(void) updateTimer;
-(void) resetTimer;
-(void) generateVitamins;
@end
//
@implementation PeceWalkerGameMode
//
-(void) Run
{
    //
    mainCharacter.canShoot = NO;
    enemiesGeneratorCount = 0;
    lifeBonsGeneratorCounter = 0;
    bonusGeneratorTick = 0.0f;
    canGenerateBonus = YES;
    totalGameModeTime = TOTAL_GAMEMODE_TIME_CONST;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //// create first enemies
    [self createFirstEnemies];
    //
}
//
-(void) stopGameModedAfterCharacterDeath
{
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
}
//
-(void) setMainCharacter:(MainCharacter *)character
{
    mainCharacter = character;
    mainCharacter.canShoot = NO;
}
//
-(void) realTimeGameTick
{
    if ([[GameManager sharedGameManager] gameIsPaused] == NO && gameOver == NO) {
        realTimeGameTickCounter+=(0.01f * [Utils timeScale]);
        if (realTimeGameTickCounter>=1) {
            totalGameModeTime--;
            realTimeGameTickCounter = 0.0f;
            if (totalGameModeTime == 0 || [[GameManager sharedGameManager] characterIsDead] == YES) {
                gameOver = YES;
                [self gameOverInGameMode];
            }
            if([[GameManager sharedGameManager] characterIsDead] == NO)
                [self updateTimer];
        }
    }
}
//
-(void) updateTimer
{
    [[GameManager sharedGameManager] updateTickInGameMode:totalGameModeTime];
}
//
-(void) createFirstEnemies
{
    for (int i = 0; i<FIRST_ENEMIE_COUNT; i++) {
        [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0,0) :kTentacle];
    }
}
//
-(void) generateNextRandomBonus
{
    //
    [[BonusManager sharedBonusManager] generateRandomPeaceWalkerBonus];
    //
    if (firstBonusHasCreated == YES) {
        [self generateVitamins];
    }
    if (firstBonusHasCreated == NO) {
        firstBonusHasCreated = YES;
    }
    //
}
//
-(void) gameModeSpecialAction
{
    // we can generate bonus after some time
    canGenerateBonus = YES;
    // reset timer
    [self resetTimer];
}
//
-(void) resetTimer
{
    realTimeGameTickCounter = 0.0f;
    totalGameModeTime = TOTAL_GAMEMODE_TIME_CONST;
    [self updateTimer];
}
//
-(void) generateVitamins
{
    for (int i =0; i<5; i++) {
        CGPoint randomPos = [[VitaminManager sharedVitaminManager]
                             getValidBonusSpawnPointAtPoint:ccp([[CCDirector sharedDirector] winSize].width / 2.0f,
                                                                [[CCDirector sharedDirector] winSize].height / 2.0f)];
        [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
    }
}
//
-(void) update
{
    //
    if (canGenerateBonus == YES)
    {
        if(bonusGeneratorTick >= NEXT_FLAG_GENERATOR_CONST)
        {
            bonusGeneratorTick = 0.0f;
            [self generateNextRandomBonus];
            canGenerateBonus = NO;
            enemiesGeneratorCount++;
            lifeBonsGeneratorCounter++;
            if(enemiesGeneratorCount == ENEMIES_CREATE_CONST)
            {
                enemiesGeneratorCount = 0;
                [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0,0) :kTentacle];
            }
            if(lifeBonsGeneratorCounter == LIFE_BONUS_GENERATOR_CONST)
            {
                [[BonusManager sharedBonusManager] generateLifeBonus];
                lifeBonsGeneratorCounter = 0.0f;
            }
        }
        bonusGeneratorTick+=GAME_NEXT_SIMULATION_STEP_INTERVAL * [Utils timeScale];
    }
    //
    [[AgentsManager sharedAgentsManager] update];
    //
    // realtime Tick 
    [self realTimeGameTick];
    //
}
//
-(void) simulateGameModeRestart
{
    //
    firstBonusHasCreated = NO;
    totalGameModeTime = TOTAL_GAMEMODE_TIME_CONST;
    realTimeGameTickCounter = 0.0f;
    enemiesGeneratorCount = 0;
    lifeBonsGeneratorCounter = 0;
    bonusGeneratorTick = 0.0f;
    canGenerateBonus = YES;
    gameOver = NO;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    [self createFirstEnemies];
    [[InputManager sharedInputManager] setInputIsEnabled:YES];
    //
}
//
//
-(void) releaseGameMode
{
    //
    firstBonusHasCreated = NO;
    totalGameModeTime = TOTAL_GAMEMODE_TIME_CONST;
    realTimeGameTickCounter = 0.0f;
    enemiesGeneratorCount = 0;
    lifeBonsGeneratorCounter = 0;
    bonusGeneratorTick = 0.0f;
    canGenerateBonus = YES;
    gameOver = NO;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
}
//
-(void) gameOverInGameMode
{
    [[InputManager sharedInputManager] setInputIsEnabled:NO];
    [[InputManager sharedInputManager] restartInputManager];
    [[BonusManager sharedBonusManager] releaseBonusManager];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
}
@end
