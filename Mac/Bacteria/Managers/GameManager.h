//
//  GameManager.h
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//
//
//
#import <Foundation/Foundation.h>
#import "cocos2d.h"
#import "Physics.h"
//
//
#import "MainCharacterController.h"
//
//
//
@class GameWorld;
@class InputManager;
@class Camera;
@class GameModeManager;
//
@interface GameManager : NSObject {
    //
    Camera *mainCamera;
    GameStates gameState;
    //
    id mainGameLayerDelegate;
    NSTimer *mainPhysicsLoop;
    Physics *physics;
	BOOL gameIsStarted;
	BOOL gameIsPaused;
	//
	MainCharacterController *mainCharacterController;
	//
    BOOL characterIsDead;
    //
    int currentScore;
    //
    CGPoint calobrationStartPoint;
    //
    BOOL accelerometerHasGotten;
    //
    BOOL weAreInGameSettings;
    //
    CGPoint currentAccelerometerDelta;
    //
    GameWorld *gameWorld;
    InputManager *inputManager;
    BOOL bulletinTimeIsOn;
    BOOL levelIsCompleted;
    BOOL levelHasWon;
    BOOL gameIsPreparing;
    CCSprite *targetSprite;
    BOOL gameIsFinished;
    //
}
//
@property (nonatomic,retain) id mainGameLayerDelegate;
@property (nonatomic,assign) BOOL gameIsStarted;
@property (nonatomic,assign) BOOL gameIsPaused;
@property (nonatomic,assign) GameStates gameState;
@property (nonatomic,assign) int currentScore;
@property (nonatomic,assign) CGPoint calobrationStartPoint;
@property (nonatomic,assign) BOOL weAreInGameSettings;
@property (nonatomic,assign) CGPoint currentAccelerometerDelta;
@property (nonatomic,assign) BOOL bulletinTimeIsOn;
@property (nonatomic,assign) BOOL levelIsCompleted;
@property (nonatomic,assign) BOOL gameIsPreparing;
@property (nonatomic,assign) Camera *mainCamera;
@property (nonatomic,assign) BOOL characterIsDead;
@property (nonatomic,assign) BOOL levelHasWon;
@property (readonly) BOOL gameIsFinished;
//
+(GameManager*)sharedGameManager;
//
-(void) resetGameData;
-(void) simulateGameRestart;
-(void) showPauseMenu;
-(void) showPauseMenuTimer:(NSTimer*)timer;
-(void) initGameManager;
-(void) initGameWorld;
-(void) initPhysics;
-(void) initMaincharacter;
-(void) decideCharacterAction:(float)characterSize :(float)enemySize;
-(void) manageLoop:(ccTime)tick;
-(void) managePhysics:(ccTime)tick;
-(BOOL) getMainCharacterDyingFlag; 
-(void) accelerateMainCharacterWithPosition:(CGPoint)position;
-(void) reEnableAccelerometer;
-(void) startGame;
-(void) resumeGame;
-(void) pauseGame;
-(void) goToLevelSelect;
-(void) goToMenu;
-(void) showhighScore:(BOOL) enabler;
-(void) upgradeScore:(int)score;
-(void) moveCharacterWithVelocity:(CGPoint)velocity;
-(void) startShoot;
-(void) updateCharacterRotation:(float)rotation andDirection:(CGPoint)direction;
-(void) endShoot;
-(void) rotateCharacterWithAngle:(float)angle;
-(void) rotateWithMouse:(CGPoint)mousePosition;
-(void) goBulletinTine;
-(void) updateLScoreWithValue:(int) score;
-(void) updateTickInGame:(int) time;
-(void) updateTickInGameMode:(int) time;
-(void) characterHit;
-(void) updateHeartCountOnLevelOverLayer:(BOOL)increaseFlag;
-(void) createBossLife:(BOOL) createFlag;
-(void) gameLevelHasWon;
-(void) levelHasWonAfterBigBoss;
-(void) characterSpecialAbilitie;
-(void) bigExplosion;
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag;
-(void) reFillSpecialAbilitieBar;
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString;
-(void) updateVitaminsCount;
//
@end
