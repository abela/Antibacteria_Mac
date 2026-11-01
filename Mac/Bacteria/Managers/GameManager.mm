//
//  GameManager.mm
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "GameManager.h"
#import "Figure.h"
#import "MainMenuScene.h"
#import "ScoreManager.h"
#import "MainGameLayer.h"
#import "GameSettingsManager.h"
#import "MainMenuLayer.h"
#import "AiInput.h"
#import "MainCharacter.h"
#import "GameWorld.h"
#import "InputManager.h"
#import "Camera.h"
#import "EnemyAgent.h"
#import "GameResourceManager.h"
#import "CCSpriteBatchNodeBlur.h"
#import "AgentsManager.h"
#import "GameModeManager.h"
#import "GameMode.h"
#import "LevelManager.h"
#import "Level.h"
#import "ChooseLevelLayer.h"
#import "AboutLayer.h"
//
#define BULLETIN_TIME_DELTA             0.35f
#define LAST_LEVEL                      50
//
@interface GameManager(PrivateMethods)
-(void) updateCameraPositionWithCharacter:(CCSprite*)character;
-(void) loadNextLevelInTime:(NSTimer*)timer;
-(void) showGameOver;
-(void) prepareGameForStart;
@end
//
@implementation GameManager
//
@synthesize mainGameLayerDelegate;
@synthesize gameIsStarted;
@synthesize gameIsPaused;
@synthesize gameState;
@synthesize currentScore;
@synthesize calobrationStartPoint;
@synthesize weAreInGameSettings;
@synthesize currentAccelerometerDelta;
@synthesize bulletinTimeIsOn;
@synthesize levelIsCompleted;
@synthesize gameIsPreparing;
@synthesize mainCamera;
@synthesize characterIsDead;
@synthesize levelHasWon;
@synthesize gameIsFinished;
//
//
static GameManager *_sharedGameManager = nil;
//
//
+(GameManager*)sharedGameManager
{
    //
    @synchronized(self)
    {
        if(!_sharedGameManager)
        {
            _sharedGameManager = [[GameManager alloc] init];
        }
        return _sharedGameManager;
    }
    //
}
//
-(id) init
{
    if((self = [super init]))
    {
        return self;
    }
    return nil;
}
//
-(void) initGameManager
{
    // init input manager
    inputManager = [InputManager sharedInputManager];
    inputManager.gameLayer = mainGameLayerDelegate;
    [inputManager runListener];
    //
    characterIsDead = YES;
    gameIsPaused = YES;
    accelerometerHasGotten = YES;
    gameIsPreparing = YES;
    //
    // init gameworld
	[self initGameWorld];
    // init game physics
	[self initPhysics];
    // start game
    [self prepareGameForStart];
	//
}
//
-(void) startGame
{
    //init gameworld
    [gameWorld Run];
    //init main character
	[self initMaincharacter];
    [[GameModeManager sharedGameModeManager] updateCharacterInGameMode:(MainCharacter*)mainCharacterController.character];
    //
    gameIsStarted = YES;
    gameIsPaused = NO;
    characterIsDead = NO;
    [mainGameLayerDelegate startGame];
    // init main camera
    mainCamera = [[Camera alloc] initWithGameLayer:mainGameLayerDelegate];
    [mainGameLayerDelegate prepareGameForStart];
    //
}
//
-(void) createBossLife:(BOOL) createFlag
{
    
}
//
-(void) prepareGameForStart
{
    gameIsPreparing = YES;
    [self startGame];
}
//
-(void) rotateWithMouse:(CGPoint)mousePosition
{
    [inputManager rotateWithMouse:mousePosition];
}
//
-(void) initPhysics
{
	physics = [Figure sharedPhysicsWorld];
}
//
-(void) resetGameData
{
    //
    [[[CCDirector sharedDirector] scheduler] setTimeScale:1.0f];
    bulletinTimeIsOn = NO;
    gameIsFinished = NO;
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:1.0f];
    //
    [mainCharacterController releaseCharacter];
    //
    [gameWorld releaseGameWorld];
    [gameWorld release];
    gameWorld = nil;
    //
    [inputManager destroyInputManager];
    //
    [[[CCDirector sharedDirector] scheduler] setTimeScale:1.0f];
    //
    characterIsDead = YES;
    gameIsStarted = NO;
    //
    [[CCDirector sharedDirector] resume];
    //
    gameState = kMainMenuState;
    characterIsDead = NO;
    //
    //physics = nil;
    currentScore = 0;
    //
    [mainGameLayerDelegate quitGame];
    //
    [[ScoreManager sharedScoreManager] releaseScoreManager];
    //
}
//
-(void) updateTickInGame:(int) time
{
    if(time == 0 && characterIsDead == NO)
    {
        [self gameLevelHasWon];
    }
    int seconds = time % 60;
    int minutes = (time / 60) % 60;
    [mainGameLayerDelegate updateTimeWithString:[NSString stringWithFormat:@"%i : %02d",minutes, seconds]];
}
//
-(void) updateTickInGameMode:(int)time
{
    if(time == 0)
    {
        [mainGameLayerDelegate gameOverInGameMode];
    }
    int seconds = time % 60;
    int minutes = (time / 60) % 60;
    [mainGameLayerDelegate updateTimeWithString:[NSString stringWithFormat:@"%i : %02d",minutes, seconds]];
}
//
-(void) gameLevelHasWon
{
    //gameIsPreparing = YES;
    levelHasWon = YES;
    [gameWorld gameWorldSpecialAction];
    [(MainGameLayer*)mainGameLayerDelegate levelHasWon];
    [[ScoreManager sharedScoreManager] synchronizeLevelScore];
    [NSTimer scheduledTimerWithTimeInterval:5.0f
                                     target:self
                                   selector:@selector(loadNextLevelInTime:)
                                   userInfo:nil
                                    repeats:NO];
    //
}
//
-(void) levelHasWonAfterBigBoss
{
    if([[AgentsManager sharedAgentsManager] getEnemiesCount] == 1 && levelHasWon == NO)
    {
        int levelNumber = [[[LevelManager sharedLevelManager] currentLevel] number];
        if(levelNumber == LAST_LEVEL)
        {
            gameIsFinished = YES;
            [(MainGameLayer*)mainGameLayerDelegate gameHasWon];
        }
        else {
            levelHasWon = YES;
            [(MainGameLayer*)mainGameLayerDelegate levelHasWon];
        }
        [NSTimer scheduledTimerWithTimeInterval:5.0f
                                         target:self
                                       selector:@selector(loadNextLevelInTime:)
                                       userInfo:nil
                                        repeats:NO];
    }
}
//
-(void) loadNextLevelInTime:(NSTimer*)timer
{
    //
    int levelNumber = [[[LevelManager sharedLevelManager] currentLevel] number];
    //
    if(gameIsFinished == YES)
    {
        [self resetGameData];
        [[CCDirector sharedDirector] replaceScene:[CCTransitionFade
                                                   transitionWithDuration:0.1f
                                                   scene:[AboutLayer node]
                                                   withColor:ccc3(1,1,1)]];
    }
    //
    else {
        //
        levelHasWon = NO;
        // load next level
        //
        [[[CCDirector sharedDirector] scheduler] setTimeScale:1.0f];
        bulletinTimeIsOn = NO;
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:1.0f];
        //
        gameIsStarted = NO;
        gameIsPaused = YES;
        //[mainCharacterController simulateCharacterRestartAtPosition:ccp(240.0f,160.0f)];
        gameState = kGamePrecessState;
        //
        [gameWorld gameModeActionLoadNextLevel];
        //
        [[CCDirector sharedDirector] resume];
        [[CCDirector sharedDirector] startAnimation];
        //
        [mainGameLayerDelegate resetLevelOverLayer];
        [mainGameLayerDelegate resumeGame];
        currentScore = 0;
        gameIsStarted = YES;
        characterIsDead = NO;
        gameIsPaused = NO;
        //
        [((MainCharacter*)mainCharacterController.character) setCanUseSpecialAbilitie:YES];
        [mainCharacterController updateCharacterSettingsAfterLevel];
        //

    }
}
//
-(void) simulateGameRestart
{
    //
    [[[CCDirector sharedDirector] scheduler] setTimeScale:1.0f];
    bulletinTimeIsOn = NO;
    gameIsFinished = NO;
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:1.0f];
    //
    gameIsStarted = NO;
    gameIsPaused = YES;
    [mainCharacterController simulateCharacterRestartAtPosition:ccp(240.0f,160.0f)];
    gameState = kGamePrecessState;
    //
    [gameWorld simulateGameWorldRestart];
    //
    [[CCDirector sharedDirector] resume];
    [[CCDirector sharedDirector] startAnimation];
    //
    [mainGameLayerDelegate resetLevelOverLayer];
    [mainGameLayerDelegate resumeGame];
    currentScore = 0;
    gameIsStarted = YES;
    characterIsDead = NO;
    gameIsPaused = NO;
    gameIsPreparing = NO;
    [[ScoreManager sharedScoreManager] releaseScoreManager];
    //
}
//
-(void) updateVitaminsCount
{
    [mainGameLayerDelegate updateVitaminsCount:[[VitaminManager sharedVitaminManager] vitaminsCount]];
}
//
-(void) updateHeartCountOnLevelOverLayer:(BOOL)increaseFlag
{
    [mainGameLayerDelegate updateHeartCount:increaseFlag];
}
//
-(void) goBulletinTine
{
    //
    if(gameIsPaused)
        return;
    //
    if(bulletinTimeIsOn == NO)
    {
        //
        [[[CCDirector sharedDirector] scheduler] setTimeScale:BULLETIN_TIME_DELTA];
        bulletinTimeIsOn = YES;
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:2.0f];
        //
    }
    else {
        //
        [[[CCDirector sharedDirector] scheduler] setTimeScale:1.0f];
        bulletinTimeIsOn = NO;
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:1.0f];
        //
    }
    //
    // if character is not dead we can refresh game world
    if (characterIsDead == NO) {
        //
        [mainCharacterController refresh];
        [gameWorld refreshGameWorld];
        [[GameResourceManager sharedGameResourceManager] updateWithSlowMotion:bulletinTimeIsOn];
        //
    }
    //
}
//
-(void) characterSpecialAbilitie
{
    //
    if([((MainCharacter*)mainCharacterController.character) canUseSpecialAbilitie] == NO)
        return;
    //
    switch (((MainCharacter*)mainCharacterController.character).currentSpecialAbilitie) {
        case kBigExplosion:
            [((MainCharacter*)mainCharacterController.character) bigExplosion];
            break;
        case kInvisible:
            [((MainCharacter*)mainCharacterController.character) invisible];
            break;
        case kSlowMotion:
            [((MainCharacter*)mainCharacterController.character) slowMotion];
            break;
        case kFastMovement:
            [((MainCharacter*)mainCharacterController.character) fastRun];
            break;
        default:
            break;
    }
    //
}
//
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString
{
    [(MainGameLayer*)mainGameLayerDelegate updateBonusOnLevelOverLayer:bonusString];
}
//
-(void) reFillSpecialAbilitieBar
{
    [((MainCharacter*)mainCharacterController.character) setCanUseSpecialAbilitie:YES];
}
//
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag
{
    [mainGameLayerDelegate updateSpecialAbiliteProgressTimer:updateFlag];
}
//
-(void) bigExplosion
{
    [[AgentsManager sharedAgentsManager] bigExplosion];
}
//
-(void) showPauseMenu
{    
}
//
-(void) initGameWorld
{
	gameWorld = [[GameWorld alloc] initWithGameLayer:mainGameLayerDelegate];
}
-(void)manageLoop:(ccTime)tick
{
    [self managePhysics:tick];
}
//
-(void) managePhysics:(ccTime)tick
{
    int32 velocityIterations = 8;
	int32 positionIterations = 2;
    //ccTime frameRate = ([CCDirector sharedDirector]).frameRate;
	//
    if(gameIsPaused == NO)
    {
        b2World *world = [physics getPhysicsWorld];
        world->Step(tick, velocityIterations, positionIterations);
        //world->ClearForces();
        for (b2Body* b = world->GetBodyList(); b; b = b->GetNext())
        {
            //
            if(b->GetUserData()!=NULL)
            {
                Figure *object = (Figure*)b->GetUserData();
                if(object.type == kMainCharacter)
                {
                    //
                    if(object.flag == kDealloc && characterIsDead == NO)
                    {
                        //
                        [NSTimer scheduledTimerWithTimeInterval:0.1f
                                                         target:self
                                                       selector:@selector(showPauseMenuTimer:)
                                                       userInfo:nil
                                                        repeats:NO];
                        //
                        characterIsDead = YES;
                        //gameIsPaused = YES;
                        return;
                        //
                    }
                    //
                    else //is alive
                    {   
                        // get character position from his body
                        CGPoint characterPos;
                        //
                        characterPos.x = b->GetPosition().x * ASPECT_RATIO;
                        characterPos.y = b->GetPosition().y * ASPECT_RATIO;
                        //update camera position
                        [self updateCameraPositionWithCharacter:object.sprite];
                        //
                    }
                }
                //
                if(characterIsDead == NO && gameIsStarted == YES)
                {
                    if(object.type == kEnemy)
                    {
                        EnemyAgent *enemyAgent = (EnemyAgent*)object;
                        [enemyAgent update];
                    }
                }
                //
			}
		}
        //
        // update character controller
        [mainCharacterController update];
        // update gameworld
        [gameWorld update];
        //
        // listen to events
        [[InputManager sharedInputManager] inputEventListener];
        //
	}
}
//
-(void) showGameOver
{
    //
    [((MainCharacter*)mainCharacterController.character) setCanUseSpecialAbilitie:NO];
    //
    [gameWorld stopGameWorldAfterCharacterDeath];
    //
    [mainGameLayerDelegate showGameOver];
    //
}
//
-(void) updateCameraPositionWithCharacter:(CCSprite*)character
{
    [mainCamera updateCameraWithCharacter:character];
}
//
-(void) moveCharacterWithVelocity:(CGPoint)velocity
{
}
//
-(void) updateCharacterRotation:(float)rotation andDirection:(CGPoint)direction
{
	[mainCharacterController updateCharacterRotation:rotation andDirection:direction];
}
//
-(void) startShoot
{
	[mainCharacterController startShoot];
}
//
-(void) endShoot
{
	[mainCharacterController endShoot];
}
//
-(void) rotateCharacterWithAngle:(float)angle
{
	
}
//
-(void) showPauseMenuTimer:(NSTimer*)timer
{
    [self showGameOver];
}
//
-(void) decideCharacterAction:(float)characterSize :(float)enemySize
{
    
}
//
-(void) accelerateMainCharacterWithPosition:(CGPoint)position
{
    [mainCharacterController moveCharacterToPosition:position];
}
//
-(void) reEnableAccelerometer
{
    
}
//
-(BOOL) getMainCharacterDyingFlag
{
    return characterIsDead;
}
//
-(void) initMaincharacter
{
	mainCharacterController = [[MainCharacterController alloc] initWithgameLayer:mainGameLayerDelegate];
    [[GameModeManager sharedGameModeManager] setMainCharacter:(MainCharacter*)mainCharacterController.character];
}
//
//
-(void) resumeGame
{
    //
    gameIsPaused = NO;
    [[CCDirector sharedDirector] resume];
    [[CCDirector sharedDirector] startAnimation];
    //
    [mainGameLayerDelegate resumeGame];
    [mainCharacterController resume];
    //
}
//
//
-(void) characterHit
{
    
}
//
//
-(void) pauseGame
{
    if(gameIsPaused == NO &&
       levelHasWon == NO &&
       gameIsPreparing == NO &&
       characterIsDead == NO)
    {
		//
        gameState = kGamePausedState;
        [[CCDirector sharedDirector] pause];
        //
        gameIsPaused = YES;
        gameIsStarted = YES;
        //
        [(MainGameLayer*)mainGameLayerDelegate pauseGame];
        //
        [self showPauseMenu];
        //
    }
}
//
-(void) goToMenu
{
    [self resetGameData];
    [[CCDirector sharedDirector] replaceScene:[CCTransitionFade
                                               transitionWithDuration:0.1f
                                               scene:[MainMenuScene node]
                                               withColor:ccc3(1,1,1)]];
}
//
-(void) goToLevelSelect
{
    [self resetGameData];
    [[CCDirector sharedDirector] replaceScene:[CCTransitionFade
                                               transitionWithDuration:0.1f
                                               scene:[ChooseLevelLayer scene]
                                               withColor:ccc3(1,1,1)]];
}
//
-(void) updateLScoreWithValue:(int) score
{
    int gameDifficulty = (int)([[GameSettingsManager sharedGameSettingsManager] gameDifficulty] + 1);
    [mainGameLayerDelegate updateScoreWithValue:score*gameDifficulty];
    [[ScoreManager sharedScoreManager] saveScoreForCurrentLevel:score];
}
//
-(void) showhighScore:(BOOL) enabler
{
    [mainGameLayerDelegate showHighScore:enabler];
}
//
-(void) upgradeScore:(int)score
{
    currentScore+=score;
    //
    [(MainGameLayer*)mainGameLayerDelegate updateScoreWithValue:currentScore];
    //
}
//
@end
