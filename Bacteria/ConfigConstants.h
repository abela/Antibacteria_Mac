//
//  ConfigConstants.h
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

//layer types
//
#define CHARACTER_DATA_KEY	@"characterdata"
#define WORLD_DATA_KEY		@"worlddata"	
#define WORLD_OBJECTS_KEY	@"worldobjects"
#define ENEMY_OBJECTS_KEY	@"enemyobjects"
#define ROOT_KEY			@"Root"
#define OBJECT_POS_X		@"posx"
#define OBJECT_POS_Y		@"posy"
#define OBJECT_TYPE			@"type"
#define OBJECT_SPRITE		@"spritename"
//
#define GAME_WORLD_SIZE     1536.0f
//
#define GAME_NEXT_SIMULATION_STEP_INTERVAL  0.01f
//
#define kCompaignLeaderBoardID          @"com.AntiBacteria.CompaignMode"
#define kCaptureTheFlagLeaderBoardID    @"com.AntiBacteria.CaptureTheFlag"
#define kPeaceWalkerLeaderBoardID       @"com.AntiBacteria.PeaceWalker"
#define kRushLeaderBoardID              @"com.AntiBacteria.Rush"

//
typedef enum
{
	zMainMenuLayer,
	zLevelEditorMenuLayer,
	zMainGameSceneLayer,
	zMainGameSceneOverLayer,
	zLevelOverLayer,
	zUserInterfaceLayer,
	zLevelEditorUILayer,
	zLevelEditorMainLayer,
	zLevelEditorOverLayer,
	zChooseLevelEditorLayer,
} 
LayerType;
//
typedef enum
{
	kMainMenuState,
	kSettingsMenuState,
	kChooseLevelState,
	kGameCenterMenuState,
	kGamePrecessState,
	kGamePausedState,
	kPauseMenuState,
	kGameIsRestartingState,
    kCharacterDyingState,
}
GameStates;
//
typedef enum
{
    zBackground,
    zMainCharacter,
}
ObjectZLayer;
//
//game modes
typedef enum
{
	kDeathMatchMode,
	kTeamDeathMatchMode,
	kCaptureFlagGameMode,
}
GameModes;
//
//object types
//
typedef enum
{
	kMainCharacter,
    kBounder,
	kBullet,
    kEnemyBullet,
	kEnemy,
	kSniperLaser,
	kGameWorldObject,
    kBonusObject,
    kRushStation,
    //
    kVitamin,
    //
}
ObjectType;
//
typedef enum
{
	kStoneGameWorldObject,
}
GameWorldObjectType;
//
typedef  enum
{
    kAbelFish,
    kWtfFish,
    kBalalaika,
    kBro,
    kKalmax,
    kLipsit,
    kLenko,
    kParanoic,
    kPirania,
    kSharky,
    kMineFish,
    kFrench,
}
FishType;
//
typedef enum
{
    kFlipDirectionRight,
    kFlipDirectionLeft,
    kFlitpDirectionUp,
    kFlipDirectionDown,
}
CharacterFlipDirection;
//
typedef enum
{
	kDefault,
	kMoving,
	kStaying,
    kDealloc,
    kDying,
    kReset,
}
ObjectEvent;
//
typedef enum
{
	kDontMove,
	kMoveUp,
	kMoveLeft,
	kMoveRight,
	kMoveDown,
}
ObjectMovingFlag;

typedef enum
{
	SHOOT,
	DEALLOC,
	BIGBANG,
	STINGER_MOVE,
}
BulletState;


typedef enum
{
	kDestructableObject,
	kLandscapeObject,
}
WorldObjectType;

typedef enum
{
	kNew,
	kDestroyed,
	kDamaged,
}
WorldObjectState;

typedef enum
{
	kWater,
	kLand,
	kGrass,
}
LandskapeType;


typedef enum
{
	zObjectsTileBatchNode,
	zWorldTilesBatchNode,
	zObjectsTileBatchNode2,
}
LevelEditorBatchNodes;

typedef enum
{
    zBackgroundImage,
    zPlant1,
    zRock,
    zPlant2,
    zRock2,
    zGrass,
    zCharacters,
}
BGAnimationLayerDepth;
//
typedef enum
{
    kGameOverPauseMenu,
    kPausePauseMenu,
}
PauseMenuType;
//
typedef enum
{
    kStateRandomMovement,
    kStateAppearing,
    kStateWaiting,
	kStateAttack,
	kStateDeffence,
	kStateEvade,
	kStatePatrol,
	kStateFlee,
    kStateIdle,
}
AgentStateType;
//
typedef enum
{
    kBlueCorosia,               //0
	kLoctopus,                  //1
    kRedNoise,                  //2
    kSpiralCorosia,             //3
    kGridium,                   //4
    kBigHunterus,               //5
    kBigBlueCorosia,            //6
    kShootingCorosia,           //7
    kSunux,                     //8
    kAdrenalium,                //9
    kCaterpillar,               //10
    kChorux,                    //11
    kChoruxDefender,            //12
    kSpawner,                   //13
    kSerpentus,                 //14
    kLotusBacterius,            //15
    kLotusBacteriusBody,        //16
    kTentacle,                  //17
    kZigzagus,                  //18
    kShootingSerpentus,         //19
    kGnida,                     //20
    kZamriGnida,                //21
    //
    kBigBoss,                   //22
    kInferno,                   //23
    kBigRedNoise,               //24
    kAnakonda,                  //25
    kFrunctus,                  //26
    kArmagedonBacteria,         //22
    //
}
EnemyAgentType;
//
typedef enum
{
    kCompaignMode,
    kTakeTheFlagMode,
    kRushMode,
    kArmagedonMode,
    kPeaceWalkerMode,
}
GameModeType;
//
typedef enum
{
    kWaterShootBonus,
    kExplosionShootBonus,
    kWindmillShootBonus,
    kTornadoShootBonus,
    kLifeBonusBonus,
    kExplostionBonus,
    kMakeInvisibleBonus,
    kFastMovementBonus,
    kFlagBonus,
    kPeaceFlagBonus,
    kRushBonus,
}
BonusObjectType;
//
typedef enum
{
	kPistol,
	kDoubleRifle,
	kShotgun,
	kRifle,
	kSniperRifle,
	kGrenade,
	kJavelin,
}
WeaponType;
//
typedef enum
{
	kPistolBullet,
	kShotgunBullet,
	kRifleBullet,
	kSniperRifleBullet,
	kGrenadeBullet,
	kJavelinBullet,
}
BulletType;
//
typedef enum 
{
    kMoveState,
    kStartShootingState,
    kShootingState,
    kEndShootingState,
}
CharacterDecisionFlag;
//
typedef enum 
{
    kKeyBoardAndMouse,
    kOnlyKeyboard,
    kOnlyMouse,
}
CharacterMoveType;
//
typedef enum
{
    kNormalShoot,
    kWaterShoot,
    kWindilShoot,
    kExplosionShoot,
    kTornadoShoot,
}
ShootType;
//
typedef enum
{
    kBigExplosion,
    kFastMovement,
    kSlowMotion,
    kInvisible,
}
SpecialAbilities;
//
typedef enum
{
    kNormal,
    kHard,
    kImpossible,
}
GameDifficulty;
//
#define ASPECT_RATIO            32.0f
//
#define GAME_LAYER_MOVEMENT_SPEED	2
//
#define TANK_TOTAL_BULLET_AMOUNT 20 
//
//animation constants
#define MAX_TANK_ANIMATION_FRAME_COUNT     7
#define OBJECT_MOVE_UP_ANIMATION_LAYER     0
#define OBJECT_MOVE_DOWN_ANIMATION_LAYER   32
#define OBJECT_MOVE_LEFT_ANIMATION_LAYER   64
#define OBJECT_MOVE_RIGHT_ANIMATION_LAYER  96
//
#define TILE_WIDTH	    32
#define TILE_HEIGHT		32
#define NEXT_TILE_WIDTH_HEIGHT  32
//
#define BIG_EXPLOSION_HIT_CONST             20
#define BIG_EXPLOSION_DISTANCE              700
//

//bullet tile animations

#define BULLET_DOWN		0
#define BULLET_RIGHT	32
#define BULLET_LEFT		64
#define BULLET_UP		96
//
#define IPAD_SCALE_X_DELTA          1.2f
#define IPAD_SCALE_Y_DELTA          1.06f
//
#define WORLD_TILE_OBJECT_COUNT	50
#define DESTRUCTABLE_OBJECT_COUNT 16

#define OBSTACLE_START_X_IPHONE             500.0f
#define OBSTACLE_START_X_IPAD               1100.0f

#define IPAD_CENTER_POINT              ccp(512.0f,384.0f)

#define KEY_UP                              63232
#define KEY_DOWN                            63233
#define KEY_LEFT                            63234
#define KEY_RIGHT                           63235
#define KEY_SPACE                           32
#define ESC_KEY                             27
//
//
#define KEY_W                               119
#define KEY_S                               115
#define KEY_A                               97
#define KEY_D                               100
//
#define BOUNDER_ESCAPE_DELTA                30
#define ENEMY_CIRCLE_SPAWN_RADIUS           500.0f
#define BONUS_SPAWN_RADIUS                  700.0f
#define BONUS_SPAWN_RADIUS_2                710.0f
#define ENEMY_CIRCLE_SPAWN_RADIUS_2         600.0f
#define ENEMY_ARROUND_RANDOM_POINT_RADIUS   500.0f
#define ENEMY_ARROUND_RANDOM_POINT_RADIUS2  520.0f
#define FRUNCTUS_RANDOM_AROUND_POINT        300.0f
#define FRUNCTUS_RANDOM_AROUND_POINT_2      301.0f
//
#define  UPPER_LEFT_X                       -555.0f
#define  UPPER_LEFT_Y                       1124.0f
//
#define  BOTTOM_LEFT_X                       -555.0f
#define  BOTTOM_LEFT_Y                       -375.0f
//
#define  UPPER_RIGHT_X                       1576.0f
#define  UPPER_RIGHT_Y                       1124.0f
//
#define  BOTTOM_RIGHT_X                       1576.0f
#define  BOTTOM_RIGHT_Y                       -375.0f
//
#define ENEMY_CREATE_BORDER_DELTA            20.0f
//
#define CHARACTER_BULLET_TAG                  1
#define SHOOTING_COROSIA_BULLET_TAG           2
#define FRUNCTUS_BULLET_TAG                   3
//
#define CHARACTER_CAMERA_STOP_X               0
#define CHARACTER_CAMERA_STOP_Y               0
//
#define SPECIAL_POWER_TIME_LIMIT                10
#define SPECIAL_ABILITIE_TIMER_STEP             0.1f
#define SPECIAL_ABILITE_PROGRESS_STEP           1.0f
//
#define FLAG_CONST_POINT                        100
#define RUSH_CONS_POINT                         250
//
#define WATER_SHOOT_IS_BOUGHT_KEY               @"watershootisboughtkey"
#define WINDMILL_SHOOT_IS_BOUGHT_KEY            @"windmillshootisboughtkey"
#define TORNADO_SHOOT_IS_BOUGHT_KEY             @"tornadoshootisboughtkey"
#define CIRCLE_SHOOT_IS_BOUGHT_KEY              @"circleshootisboughtkey"
#define DOUBLE_SPEED_IS_BOUGHT_KEY              @"doublespeedisboughtkey"
#define IMMORTALITY_IS_BOUGHT_KEY               @"immortalityisboughtkey"
//
#define WATER_SHOOT_IS_BOUGHT_VALUE             @"watershootisboughtvalue"
#define WINDMILL_SHOOT_IS_BOUGHT_VALUE          @"windmillshootisboughtvalue"
#define TORNADO_SHOOT_IS_BOUGHT_VALUE           @"tornadoshootisboughtvalue"
#define CIRCLE_SHOOT_IS_BOUGHT_VALUE            @"circleshootisboughtvalue"
#define DOUBLE_SPEED_IS_BOUGHT_VALUE            @"doublespeedisboughtvalue"
#define IMMORTALITY_IS_BOUGHT_VALUE             @"immortalityisboughtvalue"
//
#define BOSS_LEVEL_CREATE_CONST                 10
//
//
//
typedef enum
{
    kEnglishLocalization,
    kRussianLocalization,
    kFrenchLocalization,
    kDeutscheLocalization,
    kSpanishLocalization,
}
Localizations;
//
//
#define LANGUAGE_COUNT                          4
//
#define _LANGUAGE                               @"_LANGUAGE"
#define _LANGUAGE_VALUE                         @"_LANGUAGE_VALUE"
#define _START_GAME                             @"_START_GAME"
#define _UPGRADE_CHARACTER                      @"_UPGRADE_CHARACTER"
#define _VITAMIN_STORE                          @"_VITAMIN_STORE"
#define _LEADERBOARDS                           @"_LEADERBOARDS"
#define _GAME_OPTIONS                           @"_GAME_OPTIONS"
#define _ABOUT                                  @"_ABOUT"
#define _QUIT                                   @"_QUIT"
#define _CHOOSE_GAME                            @"_CHOOSE_GAME"
#define _COMPAIGN                               @"_COMPAIGN"
#define _CAPTURE_FLAG                           @"_CAPTURE_FLAG"
#define _RUSH                                   @"_RUSH"
#define _PEACE_WALKER                           @"_PEACE_WALKER"
#define _UPGRADE_SOME_FEATURES                  @"_UPGRADE_SOME_FEATURES"
#define _YOU_HAVE                               @"_YOU_HAVE"
#define _NORMAL_SHOOT                           @"_NORMAL_SHOOT"
#define _WATER_SHOOT                            @"_WATER_SHOOT"
#define _WINDMLL_SHOOT                          @"_WINDMLL_SHOOT"
#define _CIRCLE_SHOOT                           @"_CIRCLE_SHOOT"
#define _TORNADO_SHOOT                          @"_TORNADO_SHOOT"
#define _DOUBLE_SPEED                           @"_DOUBLE_SPEED"
#define _IMMORTALITY                            @"_IMMORTALITY"
#define _VITAMINS                               @"_VITAMINS"
#define _DIFFICULTY                             @"_DIFFICULTY"
#define _NORMAL                                 @"_NORMAL"
#define _HARD                                   @"_HARD"
#define _IMPOSSIBLE                             @"_IMPOSSIBLE"
#define _AUTO_SHOOT                             @"_AUTO_SHOOT"
#define _CONTROL_TYPE                           @"_CONTROL_TYPE"
#define _WASD_ARROWS                            @"_WASD_ARROWS"
#define _ARROWS                                 @"_ARROWS"
#define _ONLY_MOUSE                             @"_ONLY_MOUSE"
#define _SOUND_SFX                              @"_SOUND_SFX"
#define _GAME_MUSIC                             @"_GAME_MUSIC"
#define _RESTART                                @"_RESTART"
#define _RESUME                                 @"_RESUME"
#define _CHOOSE_LEVEL                           @"_CHOOSE_LEVEL"
#define _GOTO_MENU                              @"_GOTO_MENU"
#define _CHOOSE_LEVEL_IN_PAUSE                  @"_CHOOSE_LEVEL_IN_PAUSE"
#define _GET_READY                              @"_GET_READY"
#define _LEVEL                                  @"_LEVEL"
#define _YOU_WON                                @"_YOU_WON"
#define _GET_READY_FOR_BOSS                     @"_GET_READY_FOR_BOSS"
#define _GAME_OVER                              @"_GAME_OVER"
#define _YOUR_TIME_IS                           @"_YOUR_TIME_IS"
#define _SLOW_MOTION                            @"_SLOW_MOTION"
#define _BOOOM                                  @"_BOOOM"
#define _SCORE                                  @"_SCORE"
#define _NO_VITAMINS                            @"_NO_VITAMINS"
#define _FINISHED_GAME                          @"_FINISHED_GAME"
#define _TIME_OFF                               @"_TIME_OFF"
#define _LIFE                                   @"_LIFE"
//
//
//
//
@interface ConfigConstants : NSObject {

}

@end
