//
//  GameMode.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import <Foundation/Foundation.h>
#include <vector>
using namespace std;

@class MainCharacter;
@class GameModeManager;
@class CCSprite;
@interface GameMode : NSObject
{
    NSTimer *enemyCreateTimer;
    int enemyCreateTimeCounter;
    float enemyCreateVar;
    float prevEnemyCreateVar;
    float bonusGeneratorTick;
    float bonusGeneratorLimitConst;
    BOOL canGenerateBonus;
    float enemiesGeneratorLimitConst;
    float enemiesGeneratorStepCounter;
    int enemiesGeneratorCount;
    int enemiesRandomer;
    EnemyAgentType *enemies;
    MainCharacter *mainCharacter;
    BOOL gameOver;
    GameModeManager *gameModeManager;
    vector <CCSprite*> spawnPoints;
    BOOL firstStart;
}
//
@property (nonatomic,retain) MainCharacter *mainCharacter;
@property (nonatomic,assign) BOOL canGenerateBonus;
@property (readonly) BOOL gameOver;
@property (nonatomic,retain) GameModeManager *gameModeManager;
-(void) Run;
-(void) createRandomEnemy:(NSTimer*)timer;
-(void) simulateGameModeRestart;
-(void) update;
-(void) updateBonusManager;
-(void) createNextEnemy;
-(void) gameModeSpecialAction;
-(void) gameModeActionLoadNextLevel;
-(void) generateNextRandomBonus;
-(void) releaseGameMode;
-(void) refreshGameMode;
-(void) stopGameModedAfterCharacterDeath;
-(void) gameOverInGameMode;
-(void) initSpawnPoints;
@end
