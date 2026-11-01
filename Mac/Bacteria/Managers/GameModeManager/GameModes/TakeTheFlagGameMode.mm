//
//  TakeTheFlagGameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "TakeTheFlagGameMode.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "LevelManager.h"
#import "AgentsManager.h"
#import "GameManager.h"
#import "BonusManager.h"
#import "MainCharacter.h"
#import "InputManager.h"
//
@interface TakeTheFlagGameMode (PrivateMethods)
-(void) generateFlag;
@end
//
@implementation TakeTheFlagGameMode
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
-(void) generateFlag
{
    [[BonusManager sharedBonusManager] generateFlag];
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
-(void) createNextEnemy
{
    for(int i =0; i < enemiesGeneratorCount; i++)
    {
        int enemyQueueNumber = arc4random()%(enemiesRandomer + 1);
        EnemyAgentType nextEnemyAgentType = enemies[enemyQueueNumber];
        [[AgentsManager sharedAgentsManager] createRandomAgentAtPosition:ccp(0,0) :nextEnemyAgentType];
    }
}
//
-(void) stopGameModedAfterCharacterDeath
{
    //
    [self gameOverInGameMode];
    //
    [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:NO];
    // enemy create timer
    [[AgentsManager sharedAgentsManager] simulateAgentsManagerRestart];
    //
}
//
-(void) setMainCharacter:(MainCharacter *)character
{
    mainCharacter = character;
    mainCharacter.canShoot = YES;
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
    //
    [self generateFlag];
    flagTakeCount++;
    //
    if(flagTakeCount % 4 == 0)
    {
        enemiesRandomer++;
        if(enemiesRandomer > 19)
            enemiesRandomer = 19;
    }
    //
    if(flagTakeCount % 6 == 0)
    {
        enemiesGeneratorLimitConst-=0.1f;
        if(enemiesGeneratorLimitConst < 0.7f)
            enemiesGeneratorLimitConst = 0.7f;
        enemiesGeneratorCount++;
    }
    //
    if(flagTakeCount % 8 == 0)
    {
        [mainCharacter levelUp:YES];
        [[BonusManager sharedBonusManager] generateRandomBonus];
    }
    //
}
//
//
-(void) refreshGameMode
{
    [[AgentsManager sharedAgentsManager] refreshAgentsManager];
}
//
//
-(void) simulateGameModeRestart
{
    //
    gameOver = NO;
    canGenerateBonus = NO;
    bonusGeneratorTick = 0.0f;
    flagTakeCount = 0;
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
//
-(void) releaseGameMode
{
    //
    gameOver = NO;
    //
    flagTakeCount = 0;
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
//
@end
