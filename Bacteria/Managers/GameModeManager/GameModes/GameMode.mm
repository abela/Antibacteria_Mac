//
//  GameMode.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameMode.h"
#import "Utils.h"
#import "cocos2d.h"
#import "GameResourceManager.h"

@implementation GameMode
//
@synthesize canGenerateBonus;
@synthesize mainCharacter;
@synthesize gameOver;
@synthesize gameModeManager;
//
-(void) Run
{
    enemies = (EnemyAgentType*)malloc(19*sizeof(EnemyAgentType));
    enemies[0]  = kBlueCorosia;
    enemies[1]  = kLoctopus;
    enemies[2]  = kRedNoise;
    enemies[3]  = kChorux;
    enemies[4]  = kAdrenalium;
    enemies[5]  = kBigHunterus;
    enemies[6]  = kBigBlueCorosia;
    enemies[7]  = kSunux;
    enemies[8]  = kShootingCorosia;
    enemies[9]  = kBlueCorosia;
    enemies[10] = kSpawner;
    enemies[11] = kZigzagus;
    enemies[12] = kGnida;
    enemies[13] = kZigzagus;
    enemies[14] = kZamriGnida;
    enemies[15] = kCaterpillar;
    enemies[16] = kSerpentus;
    enemies[17] = kLotusBacterius;
    enemies[18] = kShootingSerpentus;
}
//
-(void) initSpawnPoints
{
    //
    CCSprite *spawnPoint1 = [CCSprite spriteWithSpriteFrameName:@"WX_circle_white.png"];
    spawnPoint1.position = ccp([[CoreSettings sharedCoreSettings] upperLeft].x,
                               [[CoreSettings sharedCoreSettings] upperLeft].y);
    [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] addChild:spawnPoint1 z:100];
    //
    CCSprite *spawnPoint2 = [CCSprite spriteWithSpriteFrameName:@"WX_circle_white.png"];
    spawnPoint2.position = ccp([[CoreSettings sharedCoreSettings] upperRight].x,
                               [[CoreSettings sharedCoreSettings] upperRight].y);
    [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] addChild:spawnPoint2 z:100];
    //
    CCSprite *spawnPoint3 = [CCSprite spriteWithSpriteFrameName:@"WX_circle_white.png"];
    spawnPoint3.position = ccp([[CoreSettings sharedCoreSettings] bottomRight].x,
                               [[CoreSettings sharedCoreSettings] bottomRight].y);
    [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] addChild:spawnPoint3 z:100];
    //
    CCSprite *spawnPoint4 = [CCSprite spriteWithSpriteFrameName:@"WX_circle_white.png"];
    spawnPoint4.position = ccp([[CoreSettings sharedCoreSettings] bottomLeft].x,
                               [[CoreSettings sharedCoreSettings] bottomLeft].y);
    [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] addChild:spawnPoint4 z:100];
    //
    //
    spawnPoints.push_back(spawnPoint1);
    spawnPoints.push_back(spawnPoint2);
    spawnPoints.push_back(spawnPoint3);
    spawnPoints.push_back(spawnPoint4);
    //
}
//
-(void) createRandomEnemy:(NSTimer*)timer
{
    
}
//
-(void) simulateGameModeRestart
{
    
}
//
-(void) gameModeSpecialAction
{
    
}
//
-(void) gameModeActionLoadNextLevel
{
    
}
//
-(void) update
{
    //
    if (gameOver) {
        return;
    }
    //
    enemiesGeneratorStepCounter+=GAME_NEXT_SIMULATION_STEP_INTERVAL;
    if(enemiesGeneratorStepCounter>=enemiesGeneratorLimitConst  + (1 - [Utils timeScale]))
    {
        [self createNextEnemy];
        enemiesGeneratorStepCounter = 0.0f;
    }
    //
    if(canGenerateBonus == NO)
        return;
    //
    bonusGeneratorTick+=GAME_NEXT_SIMULATION_STEP_INTERVAL;
    if(bonusGeneratorTick >= bonusGeneratorLimitConst + (1 - [Utils timeScale]))
    {
        bonusGeneratorTick = 0.0f;
        canGenerateBonus = NO;
        [self generateNextRandomBonus];
        enemiesGeneratorStepCounter = 0;
    }
}
//
-(void) createNextEnemy
{
    
}
//
-(void) updateBonusManager
{
    
}
//
-(void) generateNextRandomBonus
{
    
}
//
-(void) releaseGameMode
{
    for (int i =0; i<spawnPoints.size(); i++) {
        [spawnPoints[i] removeFromParentAndCleanup:YES];
    }
    spawnPoints.clear();
}
//
-(void) refreshGameMode
{
    
}
//
-(void) stopGameModedAfterCharacterDeath
{
    
}
//
-(void) gameOverInGameMode
{
    
}
//
@end
