//
//  RushGameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "RushGameMode.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "LevelManager.h"
#import "AgentsManager.h"
#import "GameManager.h"
#import "BonusManager.h"
#import "MainCharacter.h"
#import "InputManager.h"
//
@interface RushGameMode (PrivateMethods)
-(void) generateStation;
@end
//
@implementation RushGameMode
//
-(void) Run
{
    //
    [super Run];
    //
    mainCharacter.canShoot = YES;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    prevEnemyCreateVar = enemyCreateVar = 2.0f;
    //
    canGenerateBonus = YES;
    bonusGeneratorTick = 0.0f;
    bonusGeneratorLimitConst = 1.0f;
    //
    enemiesRandomer = 0;
    enemiesGeneratorCount = 1;
    enemiesGeneratorLimitConst = 1.5f;
    enemiesGeneratorStepCounter = 0.01f;
    //
    // initSpawn Points
    //[super initSpawnPoints];
}
//
//
-(void) generateStation
{
    [[BonusManager sharedBonusManager] generateRushStation];
}
//
-(void) gameModeSpecialAction
{
    //
}
//
-(void) createRandomEnemy:(NSTimer *)timer
{
    
}
//
-(void) setMainCharacter:(MainCharacter *)character
{
    mainCharacter = character;
    mainCharacter.canShoot = YES;
}
//
-(void) createNextEnemy
{
    for(int i = 0; i < enemiesGeneratorCount; i++)
    {
        int enemyQueueNumber = arc4random()%(enemiesRandomer + 1);
        EnemyAgentType nextEnemyAgentType = enemies[enemyQueueNumber];
        [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0.0f,0.0f) :nextEnemyAgentType];
    }
}
//
-(void) stopGameModedAfterCharacterDeath
{
    [self gameOverInGameMode];
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
}
//
-(void) update
{
    [super update];
    [[AgentsManager sharedAgentsManager] update];
}
//
-(void) generateNextRandomBonus
{
    // generate rush station
    [self generateStation];
    rushTakeCount++;
    //
    if(rushTakeCount % 4 == 0)
    {
        enemiesRandomer++;
        if(enemiesRandomer > 19)
            enemiesRandomer = 19;
    }
    //
    if(rushTakeCount % 6 == 0)
    {
        enemiesGeneratorLimitConst-=0.1f;
        if(enemiesGeneratorLimitConst < 0.7f)
            enemiesGeneratorLimitConst = 0.7f;
        enemiesGeneratorCount++;
    }
    //
    if(rushTakeCount % 8 == 0)
    {
        [mainCharacter levelUp:YES];
        [[BonusManager sharedBonusManager] generateRandomBonus];
    }
    //
}
//
-(void) refreshGameMode
{
    [[AgentsManager sharedAgentsManager] refreshAgentsManager];
}
//
-(void) simulateGameModeRestart
{
    //
    gameOver = NO;
    canGenerateBonus = NO;
    bonusGeneratorTick = 0.0f;
    rushTakeCount = 0;
    bonusGeneratorLimitConst = 1.0f;
    //
    enemiesRandomer = 0;
    enemiesGeneratorCount = 1;
    enemiesGeneratorLimitConst = 1.5f;
    enemiesGeneratorStepCounter = 0.01f;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    prevEnemyCreateVar = enemyCreateVar = 2.0f;
    [mainCharacter levelUp:NO];
    canGenerateBonus = YES;
    [[InputManager sharedInputManager] setInputIsEnabled:YES];
    //
}
//
-(void) gameOverInGameMode
{
    gameOver = YES;
    [[InputManager sharedInputManager] setInputIsEnabled:NO];
    [[InputManager sharedInputManager] restartInputManager];
    [[BonusManager sharedBonusManager] releaseBonusManager];
}
//
-(void) releaseGameMode
{
    //
    //[super releaseGameMode];
    //
    gameOver = NO;
    rushTakeCount = 0;
    canGenerateBonus = YES;
    bonusGeneratorLimitConst = 1.0f;
    //
    enemiesRandomer = 0;
    enemiesGeneratorCount = 1;
    enemiesGeneratorLimitConst = 1.5f;
    enemiesGeneratorStepCounter = 0.01f;
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
    [[LevelManager sharedLevelManager] resetLevelManager];
    [[InputManager sharedInputManager] setInputIsEnabled:YES];
    //
    free(enemies);
    //
}
//

@end
